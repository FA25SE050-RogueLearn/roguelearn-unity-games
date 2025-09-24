using UnityEngine;

namespace BossFight2D.Player {
  public class PlayerCombat : MonoBehaviour {
    public Animator animator;
    public BossFight2D.Combat.Hitbox2D hitbox;
    public int defaultDamage = 10;
    public float attackCooldown = 0.5f;
    public float hitboxWindow = 0.2f;
    public bool inputEnabled = true;

    float _nextAttackAllowed;
    int _queuedDamage;

    void Awake(){ if(animator==null) animator = GetComponentInChildren<Animator>(); if(hitbox==null) hitbox = GetComponentInChildren<BossFight2D.Combat.Hitbox2D>(); }

    void Update(){
      if(inputEnabled && Input.GetMouseButtonDown(0)) QueueAttack(defaultDamage);
    }

    public void QueueAttack(int damage, GameObject target=null){
      _queuedDamage = damage;
      if(Time.time < _nextAttackAllowed) return;
      DoAttack();
    }

    void DoAttack(){
      _nextAttackAllowed = Time.time + attackCooldown;
      int dmg = _queuedDamage>0? _queuedDamage : defaultDamage;
      // Route through PowerPlay first-hit modifier if active
      var ppm = BossFight2D.Core.GameObjectFactory.FindOrCreate<BossFight2D.Core.PowerPlayManager>();
      if(ppm!=null){ dmg = ppm.ModifyDamageOnBossHit(dmg); }
      if(animator!=null) animator.SetTrigger("Attack");
      if(hitbox!=null){ hitbox.Activate(hitboxWindow, dmg); Invoke(nameof(EndHitbox), hitboxWindow); }
      _queuedDamage = 0;
    }

    void EndHitbox(){ if(hitbox!=null) hitbox.Deactivate(); }

    public void AnimationEvent_HitboxStart(){ if(hitbox!=null){ hitbox.Activate(hitboxWindow, _queuedDamage>0? _queuedDamage : defaultDamage);} }
    public void AnimationEvent_HitboxEnd(){ EndHitbox(); }
  }
}