using UnityEngine;
using System;

namespace BossFight2D.Player {
  public class PlayerHealth : MonoBehaviour, BossFight2D.Combat.IDamageable {
    public int maxHearts=5; public int hearts=5; public event Action OnDeath;
    public bool invincible = false;
    public float invincibleFlashInterval = 0.1f; // optional visual cue future use

    public void Damage(int amount){ if(invincible) return; hearts=Mathf.Max(0,hearts-amount); if(hearts==0){ OnDeath?.Invoke(); BossFight2D.Core.GameObjectFactory.FindOrCreate<BossFight2D.Core.GameManager>()?.LoseGame(); } }
    public void Heal(int amount){ hearts=Mathf.Min(maxHearts,hearts+amount);} 
    public void TakeDamage(int amount){ Damage(amount);} 
  }
}