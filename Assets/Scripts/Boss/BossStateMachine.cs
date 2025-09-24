using UnityEngine;
using BossFight2D.Systems;

namespace BossFight2D.Boss {
  public enum BossPhase{ Phase1, Transition, Phase2, Dead }
  public class BossStateMachine : MonoBehaviour, BossFight2D.Combat.IDamageable {
    public string bossName="The Professor"; public int maxHP=100; public int hp=100; public BossPhase phase=BossPhase.Phase1;
    public BossCombat combat;
    [Header("Wrong Answer Telegraph")]
    public float wrongTelegraph = 0.6f; // delay before perfect window
    public float perfectWindow = 0.2f;   // window length
    bool perfectSuccess;

    void Awake(){ if(combat==null) combat = GetComponent<BossCombat>(); }
    void OnEnable(){ EventBus.PerfectDodgeSuccess += OnPerfectDodgeSuccess; }
    void OnDisable(){ EventBus.PerfectDodgeSuccess -= OnPerfectDodgeSuccess; }

    void OnPerfectDodgeSuccess(){ perfectSuccess = true; }

    public void ApplyDamage(int dmg){ if(phase==BossPhase.Dead) return; hp=Mathf.Max(0,hp-dmg); if(hp==0){ phase=BossPhase.Dead; EventBus.RaiseGameWon(); } else if(hp<=maxHP/2 && phase==BossPhase.Phase1){ phase=BossPhase.Transition; Invoke(nameof(EnterPhase2),1.0f); } }
    void EnterPhase2(){ phase=BossPhase.Phase2; }

    public void OnWrongAnswer(){
      // Start telegraph, then open a perfect dodge window
      perfectSuccess = false;
      if(combat!=null){
        Invoke(nameof(BeginPerfectWindow), wrongTelegraph);
        Invoke(nameof(EndPerfectWindowAndResolve), wrongTelegraph + perfectWindow);
        EventBus.RaisePerfectDodgeWindowStarted(perfectWindow);
      } else {
        // No combat component, end challenge immediately
        EventBus.RaisePerfectDodgeWindowStarted(perfectWindow);
        EventBus.RaisePerfectDodgeWindowEnded();
        EventBus.RaiseWrongAnswerChallengeEnded();
      }
    }

    void BeginPerfectWindow(){ /* window open */ }
    void EndPerfectWindowAndResolve(){
      EventBus.RaisePerfectDodgeWindowEnded();
      if(perfectSuccess){
        // Player succeeded: no attack, end challenge now
        EventBus.RaiseWrongAnswerChallengeEnded();
        return;
      }
      if(combat!=null){
        // Attack will run; end signal will be raised by BossCombat when hitbox ends
        combat.QueueAttack(1);
      } else {
        // Safety net if combat missing
        EventBus.RaiseWrongAnswerChallengeEnded();
      }
    }

    public void TakeDamage(int amount){ ApplyDamage(amount);} 
  }
}