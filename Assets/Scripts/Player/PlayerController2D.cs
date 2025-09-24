using UnityEngine;
using Cinemachine;
namespace BossFight2D.Player {
  [RequireComponent(typeof(Rigidbody2D))]
  public class PlayerController2D : MonoBehaviour 
  {
    public float moveSpeed=6f, dashSpeed=12f, dashCooldown=1.2f, dashDuration=0.15f; 
    Rigidbody2D rb; 
    Vector2 input; 
    float lastDashTime=-999f; 
    bool dashing; 
    float dashEnd;
    public Animator animator;
    SpriteRenderer sr; // optional sprite renderer for flipX
    public bool inputEnabled = true;
    void Awake()
    {
       rb=GetComponent<Rigidbody2D>(); 
       animator=GetComponent<Animator>();
       sr=GetComponent<SpriteRenderer>();
       if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
       rb.gravityScale=0; 
       rb.freezeRotation=true;
    }
    void Start()
    {
      var camera = FindFirstObjectByType<CinemachineVirtualCamera>();
      if(camera!=null)
      {
        camera.Follow = transform;
      }
    }
    void Update()
    { 
      if(!inputEnabled)
      { 
        rb.velocity = Vector2.zero; animator.SetFloat("Speed", 0f); 
        return; 
      }
      input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
      animator.SetFloat("Speed", input.magnitude);
      if(Input.GetKeyDown(KeyCode.LeftShift) && Time.time-lastDashTime>=dashCooldown)
      { 
        dashing=true; dashEnd=Time.time+dashDuration; lastDashTime=Time.time; 
      } 
    }
    void FixedUpdate()
    { 
      if(!inputEnabled){ rb.velocity = Vector2.zero; return; }
      if(dashing)
      { 
        rb.velocity = input*dashSpeed; if(Time.time>=dashEnd) dashing=false; 
        return;
      } 
      rb.velocity=input*moveSpeed;
      var cam=Camera.main; 
      if(cam)
      { 
        var mouse = cam.ScreenToWorldPoint(Input.mousePosition); 
        // Only flip left/right based on mouse X relative to player
        if (Mathf.Abs(mouse.x - transform.position.x) > 0.001f)
        {
          bool faceRight = mouse.x >= transform.position.x;
          if (sr != null)
          {
            // If your default sprite faces right, flipX should be true when facing left
            sr.flipX = !faceRight;
          }
          else
          {
            var scale = transform.localScale;
            var sign = faceRight ? 1f : -1f;
            scale.x = Mathf.Abs(scale.x) * sign;
            transform.localScale = scale;
          }
        }
      } 
      // Removed full rotation toward mouse to prevent constant spinning
    }
    public bool IsDashing(){ return dashing; }
  }
}