using UnityEngine;
using BossFight2D.Systems;
using BossFight2D.Player;

namespace BossFight2D.Boss {
  public class BossCombat : MonoBehaviour {
    public Animator animator;
    public BossFight2D.Combat.Hitbox2D hitbox;
    public float windup = 0.2f;
    public float hitboxWindow = 0.2f;
    [SerializeField] private Transform playerTransform;

    [Header("Timing")]
    [Tooltip("If true, rely on Animation Events (AnimationEvent_HitboxStart/End) to start/stop the hitbox. If false, use Invoke() based on windup/hitboxWindow.")]
    public bool useAnimationEvents = true;
    
    // Debug visualization
    [Header("Debug")]
    public Color debugTelegraphColor = Color.yellow;
    public Color debugAttackColor = Color.red;

    // Orientation
    [Header("Orientation")]
    public bool facePlayer = true;
    SpriteRenderer sr;

    // Melee Aiming
    [Header("Melee Aiming")]
    [Tooltip("Rotate hitbox to face the player when the attack starts (melee aim).")]
    public bool rotateHitboxTowardPlayer = true;
    [Tooltip("Z rotation offset (degrees) to apply after aiming at the player.")]
    public float hitboxAngleOffset = 0f;
    Quaternion _hitboxDefaultLocalRotation;
    
    [Tooltip("Move hitbox forward toward the player when the attack starts (world-space offset).")]
    public bool moveHitboxTowardPlayer = true;
    [Tooltip("Distance (units) to push the hitbox forward in the aimed direction during the attack.")]
    public float hitboxForwardDistance = 1f;
    Vector3 _hitboxDefaultLocalPosition;

    // Patrol during question
    [Header("Patrol During Question")]
    [Tooltip("If true, boss will patrol (orbit) around the player while the question is active.")]
    public bool patrolDuringQuestion = true;
    [Tooltip("Only patrol when currently within attack range of the player.")]
    public bool onlyPatrolWhenWithinRange = true;
    [Tooltip("Orbit radius around the player while patrolling.")]
    public float patrolRadius = 1.5f;
    [Tooltip("Max allowed distance from player to remain in attack range.")]
    public float attackRange = 2.0f;
    [Tooltip("Linear movement speed while patrolling (units/sec).")]
    public float patrolSpeed = 1.5f;
    Rigidbody2D rb;
    bool _patrolling;
    float _orbitAngle;

    int _queuedDamage = 1;

    void Awake(){ if(animator==null) animator = GetComponentInChildren<Animator>(); if(hitbox==null) hitbox = GetComponentInChildren<BossFight2D.Combat.Hitbox2D>(); if(playerTransform==null){ var pc = FindFirstObjectByType<PlayerController2D>(); if(pc!=null) playerTransform = pc.transform; } if(sr==null) sr = GetComponentInChildren<SpriteRenderer>(); if(hitbox!=null){ _hitboxDefaultLocalRotation = hitbox.transform.localRotation; _hitboxDefaultLocalPosition = hitbox.transform.localPosition; } rb = GetComponent<Rigidbody2D>(); }

    void OnEnable(){ BossFight2D.Systems.EventBus.QuestionStarted += OnQuestionStarted; BossFight2D.Systems.EventBus.AnswerSubmitted += OnAnswerSubmitted; BossFight2D.Systems.EventBus.QuestionTimeout += OnQuestionTimeout; }
    void OnDisable(){ BossFight2D.Systems.EventBus.QuestionStarted -= OnQuestionStarted; BossFight2D.Systems.EventBus.AnswerSubmitted -= OnAnswerSubmitted; BossFight2D.Systems.EventBus.QuestionTimeout -= OnQuestionTimeout; }

    void OnQuestionStarted(BossFight2D.Systems.QuestionData q){
      if(!patrolDuringQuestion){ _patrolling = false; return; }
      if(onlyPatrolWhenWithinRange && playerTransform!=null){
        _patrolling = Vector2.Distance(playerTransform.position, transform.position) <= attackRange;
      } else {
        _patrolling = true;
      }
    }
    void OnAnswerSubmitted(int choice, bool correct){ _patrolling = false; }
    void OnQuestionTimeout(){ _patrolling = false; }

    public void QueueAttack(int damage, GameObject target=null){
      _queuedDamage = damage;
      if(animator!=null) animator.SetTrigger("Attack");
      if(hitbox!=null && !useAnimationEvents){
          Invoke(nameof(BeginHitbox), windup);
          Invoke(nameof(EndHitbox), windup+hitboxWindow);
      }
      // Draw telegraph line toward player during windup
      if(playerTransform!=null){
          Debug.DrawLine(transform.position, playerTransform.position, debugTelegraphColor, windup);
      }
    }

    void BeginHitbox(){
        if(hitbox!=null){
            if(rotateHitboxTowardPlayer && playerTransform!=null){
                Vector3 dir = (playerTransform.position - transform.position);
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + hitboxAngleOffset;
                hitbox.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }
            if(moveHitboxTowardPlayer){
                // Base world position preserving default local offset
                Vector3 baseWorld = transform.TransformPoint(_hitboxDefaultLocalPosition);
                // Push forward along the aimed (right) vector
                Vector3 forward = hitbox.transform.right;
                hitbox.transform.position = baseWorld + forward * Mathf.Clamp((transform.position - playerTransform.position).magnitude, 0,hitboxForwardDistance);
            }
            hitbox.Activate(hitboxWindow, _queuedDamage);
        }
        // Draw active attack line toward player for hitbox window duration
        if(playerTransform!=null){
            Debug.DrawLine(transform.position, playerTransform.position, debugAttackColor, hitboxWindow);
        }
    }
    void EndHitbox(){ if(hitbox!=null){ hitbox.Deactivate(); hitbox.transform.localRotation = _hitboxDefaultLocalRotation; hitbox.transform.localPosition = _hitboxDefaultLocalPosition; } _queuedDamage = 1; EventBus.RaiseWrongAnswerChallengeEnded(); }

    public void AnimationEvent_HitboxStart(){ BeginHitbox(); }
    public void AnimationEvent_HitboxEnd(){ EndHitbox(); }
    void Update(){
      if(facePlayer && playerTransform!=null && sr!=null){
        bool faceRight = playerTransform.position.x >= transform.position.x;
        // If default sprite faces right, flipX should be true when facing left
        sr.flipX = !faceRight;
      }
    }

    void FixedUpdate(){
      if(_patrolling && playerTransform!=null && rb!=null){
        if(onlyPatrolWhenWithinRange && Vector2.Distance(playerTransform.position, transform.position) > attackRange){
          _patrolling = false; return;
        }
        float radius = Mathf.Min(patrolRadius, attackRange);
        // Advance orbit angle; convert linear speed to angular step by v = r * omega => omega = v / r
        float omega = radius > 0.001f ? (patrolSpeed / radius) : 0f;
        _orbitAngle += omega * Time.fixedDeltaTime;
        Vector2 desiredPos = (Vector2)playerTransform.position + new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * radius;
        Vector2 next = Vector2.MoveTowards(rb.position, desiredPos, patrolSpeed * Time.fixedDeltaTime);
        rb.MovePosition(next);
      }
    }
  }
}