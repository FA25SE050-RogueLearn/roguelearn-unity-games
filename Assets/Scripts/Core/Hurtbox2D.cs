using UnityEngine;

namespace BossFight2D.Combat 
{
  // Marker component designating a collider as a valid hurtbox target.
  // Attach this to the collider object you want the hitbox to damage.
  // It will route damage to an IDamageable found on the same object or its parents.
  [RequireComponent(typeof(Collider2D))]
  public class Hurtbox2D : MonoBehaviour 
  {
    // Optional explicit target; if null, will search in parents
    public MonoBehaviour damageTarget;

    public IDamageable GetDamageable()
    {
      if(damageTarget is IDamageable id) return id;
      var id2 = GetComponentInParent<IDamageable>();
      return id2;
    }

    void Awake()
    {
      var col = GetComponent<Collider2D>();
      if(col!=null) col.isTrigger = false; // typical hurtboxes are non-trigger colliders
    }
  }
}