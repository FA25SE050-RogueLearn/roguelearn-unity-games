using UnityEngine;
using System.Collections;
using BossFight2D.Player;
using BossFight2D.Systems;

namespace BossFight2D.Core {
  // Manages single-player Power Play window: lasts until first successful player hit on boss or timeout
  public class PowerPlayManager : MonoBehaviour {
    [Header("Window")]
    public float windowDurationDefault = 5f; // seconds
    public float cooldownSeconds = 10f; // user confirmed

    [Header("Bonuses (applied to first hit only)")]
    [Tooltip("Boss takes extra damage multiplier on first hit during window.")]
    public float bossVulnerabilityMultiplier = 1.75f;
    [Tooltip("Player outgoing damage bonus on first hit during window. 0.35 = +35%.")]
    public float playerDamageBonus = 0.35f;

    [Header("Mobility Buff During Window")]
    [Tooltip("Temporary movement speed bonus while window is active.")]
    public float sprintSpeedBonus = 0.20f; // +20% per user

    public bool Active { get; private set; }
    public bool Consumed { get; private set; }
    public float Remaining => Active ? Mathf.Max(0f, _windowEnd - Time.time) : 0f;

    float _windowEnd;
    float _lastEndTime = -999f;

    // Player speed cache
    PlayerController2D _player;
    float _origMove, _origDash;
    bool _buffApplied;

    void Awake(){ DontDestroyOnLoad(this.gameObject); }

    public void StartWindow(float durationSec = -1f){
      // Respect cooldown and ignore if already active
      if(Active) return;
      if(Time.time < _lastEndTime + cooldownSeconds) return;
      Active = true; Consumed = false;
      float dur = durationSec > 0f ? durationSec : windowDurationDefault;
      _windowEnd = Time.time + dur;
      ApplySprintBuff();
      // Notify UI/Systems
      EventBus.RaisePowerPlayStarted(dur);
      StopAllCoroutines();
      StartCoroutine(CoAutoEnd());
    }

    IEnumerator CoAutoEnd(){
      // End on first consume or timeout
      while(Active && Time.time < _windowEnd && !Consumed){ yield return null; }
      if(Active) EndWindow();
    }

    public int ModifyDamageOnBossHit(int baseDamage){
      if(!Active || Consumed) return baseDamage;
      float mult = (1f + Mathf.Max(0f, playerDamageBonus)) * Mathf.Max(0.01f, bossVulnerabilityMultiplier);
      int final = Mathf.CeilToInt(baseDamage * mult);
      Consumed = true;
      EndWindow();
      return final;
    }

    void ApplySprintBuff(){
      if(_buffApplied) return;
      _player = _player ?? FindFirstObjectByType<PlayerController2D>();
      if(_player!=null){
        _origMove = _player.moveSpeed; _origDash = _player.dashSpeed;
        _player.moveSpeed = _origMove * (1f + sprintSpeedBonus);
        _player.dashSpeed = _origDash * (1f + sprintSpeedBonus);
        _buffApplied = true;
      }
    }

    void RemoveSprintBuff(){
      if(!_buffApplied) return;
      if(_player!=null){ _player.moveSpeed = _origMove; _player.dashSpeed = _origDash; }
      _buffApplied = false; _player = null;
    }

    public void EndWindow(){
      if(!Active) return;
      Active = false;
      RemoveSprintBuff();
      _lastEndTime = Time.time;
      // Notify UI/Systems
      EventBus.RaisePowerPlayEnded();
    }
  }
}