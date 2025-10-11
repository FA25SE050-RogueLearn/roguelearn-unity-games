using UnityEngine;

namespace BossFight2D.Combat
{
  using BossFight2D.Boss;
  using BossFight2D.Systems;
  using BossFight2D.Player;
  public static class CombatResolver
  {
    public static float baseEasy = 8f, baseMed = 12f, baseHard = 18f;
    public static float comboBonus = 0.15f;
    public static float maxMultiplier = 2.0f;
    public static float timeBonusMax = 0.5f; // up to +50% if answered instantly
    public static void ApplyAnswerDamage(QuestionData q, QuestionManager qm)
    {
      var boss = (qm != null && qm.bossOverride != null) ? qm.bossOverride : Object.FindFirstObjectByType<BossStateMachine>();
      if (boss == null) return;
      float baseDmg = q.difficulty == "Easy" ? baseEasy : q.difficulty == "Hard" ? baseHard : baseMed;
      float mult = 1f + Mathf.Min(qm.Combo * comboBonus, maxMultiplier - 1f);
      float timeRatio = Mathf.Clamp01(qm.RemainingTime / Mathf.Max(0.01f, q.timeLimitSec));
      float timeMult = 1f + timeBonusMax * timeRatio;
      float nextBonus = (qm != null ? qm.ConsumeNextAnswerBonus() : 0f);
      float bonusMult = 1f + Mathf.Max(0f, nextBonus);
      int dmg = Mathf.CeilToInt(baseDmg * mult * timeMult * bonusMult);
      var playerHealth = (qm != null && qm.playerOverride != null) ? qm.playerOverride : Object.FindFirstObjectByType<PlayerHealth>();
      var playerCombat = playerHealth != null ? playerHealth.GetComponent<PlayerCombat>() : Object.FindFirstObjectByType<PlayerCombat>();
      if (playerCombat != null && playerCombat.hitbox != null)
      {
        playerCombat.QueueAttack(dmg, boss.gameObject);
      }
      else
      {
        boss.ApplyDamage(dmg);
      }
      // qm.AddCombo(); // moved to QuestionManager to avoid double increment in future flows
    }
    public static void ApplyPenalty(QuestionData q, QuestionManager qm)
    {
      var boss = (qm != null && qm.bossOverride != null) ? qm.bossOverride : Object.FindFirstObjectByType<BossStateMachine>();
      var player = (qm != null && qm.playerOverride != null) ? qm.playerOverride : Object.FindFirstObjectByType<PlayerHealth>();
      if (boss != null)
      {
        boss.OnWrongAnswer();
      }
      else
      {
        player?.Damage(1);
      }
    }
  }
}