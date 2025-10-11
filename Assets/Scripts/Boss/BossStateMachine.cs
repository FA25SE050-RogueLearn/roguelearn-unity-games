using UnityEngine;
using BossFight2D.Systems;
using BossFight2D.Core;
namespace BossFight2D.Boss
{
  public enum BossPhase { Phase1, Transition, Phase2, Dead }
  public class BossStateMachine : MonoBehaviour, BossFight2D.Combat.IDamageable
  {
    public string bossName = "The Professor"; public int maxHP = 100; public int hp = 100; public BossPhase phase = BossPhase.Phase1;
    public BossCombat combat;
    [Header("Wrong Answer Telegraph")]
    public float wrongTelegraph = 0.6f; // delay before perfect window
    public float perfectWindow = 0.2f;   // window length

    public Animator animator;
    bool perfectSuccess;

    void Awake() { if (combat == null) combat = GetComponent<BossCombat>(); }
    void OnEnable() { EventBus.PerfectDodgeSuccess += OnPerfectDodgeSuccess; EventBus.GamePaused += OnGamePaused; EventBus.GameResumed += OnGameResumed; EventBus.GameWon += OnGameWon; }
    void OnDisable() { EventBus.PerfectDodgeSuccess -= OnPerfectDodgeSuccess; EventBus.GamePaused -= OnGamePaused; EventBus.GameResumed -= OnGameResumed; EventBus.GameWon -= OnGameWon; }

    void OnPerfectDodgeSuccess() { perfectSuccess = true; }

    public void ApplyDamage(int dmg)
    {
      if (phase == BossPhase.Dead)
      {
        return;
      }
      hp = Mathf.Max(0, hp - dmg);
      if (hp <= 0)
      {
        phase = BossPhase.Dead;
        BossFight2D.Core.GameObjectFactory.FindOrCreate<BossFight2D.Core.GameManager>()?.WinGame();
      }
      else if (hp <= maxHP / 2 && phase == BossPhase.Phase1)
      {
        phase = BossPhase.Transition; Invoke(nameof(EnterPhase2), 1.0f);
      }


    }
    void EnterPhase2() { phase = BossPhase.Phase2; }

    public void OnWrongAnswer()
    {
      // Start telegraph, then open a perfect dodge window
      perfectSuccess = false;
      if (combat != null)
      {
        Invoke(nameof(BeginPerfectWindow), wrongTelegraph);
        Invoke(nameof(EndPerfectWindowAndResolve), wrongTelegraph + perfectWindow);
        EventBus.RaisePerfectDodgeWindowStarted(perfectWindow);
      }
      else
      {
        // No combat component, end challenge immediately
        EventBus.RaisePerfectDodgeWindowStarted(perfectWindow);
        EventBus.RaisePerfectDodgeWindowEnded();
        EventBus.RaiseWrongAnswerChallengeEnded();
      }
    }

    void BeginPerfectWindow() { /* window open */ }
    void EndPerfectWindowAndResolve()
    {
      EventBus.RaisePerfectDodgeWindowEnded();
      if (perfectSuccess)
      {
        // Player succeeded: no attack, end challenge now
        EventBus.RaiseWrongAnswerChallengeEnded();
        return;
      }
      if (combat != null)
      {
        // Attack will run; end signal will be raised by BossCombat when hitbox ends
        combat.QueueAttack(1);
      }
      else
      {
        // Safety net if combat missing
        EventBus.RaiseWrongAnswerChallengeEnded();
      }
    }

    void OnGamePaused()
    {
      // Freeze combat and cancel any pending invocations during pause
      if (combat != null) combat.enabled = false;
      CancelInvoke();
      // Ensure boss combat/Hitbox are disabled after victory to prevent stray interactions
      if (combat != null)
      {
        combat.enabled = false;
        if (combat.hitbox != null) combat.hitbox.Deactivate();
      }
    }
    void OnGameResumed()
    {
      if (combat != null) combat.enabled = true;
      // Do not re-schedule previous invokes; resume normal flow
    }

    public void TakeDamage(int amount) { ApplyDamage(amount); }

    void OnGameWon()
    {
      if (animator != null) animator.SetTrigger("Death");

      // Ensure boss combat/Hitbox are disabled after victory to prevent stray interactions
      if (combat != null)
      {
        combat.enabled = false;
        if (combat.hitbox != null) combat.hitbox.Deactivate();
      }
      // End any pending invocations when game is won
      CancelInvoke();
    }

  }
}
