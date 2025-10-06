using UnityEngine;

namespace BossFight2D.Effects {
  // Simple visual pulse for telegraph sprites: animates alpha (or color) over time.
  // Does NOT change scale by default to respect existing sizing.
  public class TelegraphPulse : MonoBehaviour {
    public SpriteRenderer target;
    [Tooltip("Base color to start from (tint). Alpha will be pulsed if pulseAlphaOnly is true.")]
    public Color baseColor = new Color(1f, 0f, 0f, 0.4f);
    [Tooltip("If true, only alpha is animated. If false, color channels will also pulse.")]
    public bool pulseAlphaOnly = true;
    [Range(0f,1f)] public float alphaMin = 0.25f;
    [Range(0f,1f)] public float alphaMax = 0.6f;
    [Tooltip("Pulse speed (cycles per second approximately).")]
    public float speed = 2f;
    [Tooltip("Optional: set a duration (seconds) to auto-stop the pulse. <=0 means run indefinitely (until object is destroyed).")]
    public float duration = 0f;

    float _t;
    float _elapsed;

    void Awake(){ if(target==null) target = GetComponent<SpriteRenderer>(); _t = Random.value * 10f; }
    void OnEnable(){ _elapsed = 0f; ApplyColor(baseColor); }

    void Update(){
      if(target == null) return;
      if(duration > 0f){ _elapsed += Time.deltaTime; if(_elapsed >= duration){ enabled = false; ApplyColor(baseColor); return; } }
      _t += speed * Time.deltaTime;
      float s = (Mathf.Sin(_t) + 1f) * 0.5f; // 0..1
      var c = baseColor;
      if(pulseAlphaOnly){ c.a = Mathf.Lerp(alphaMin, alphaMax, s); }
      else {
        c.r = Mathf.Lerp(baseColor.r * 0.85f, Mathf.Min(1f, baseColor.r * 1.1f), s);
        c.g = Mathf.Lerp(baseColor.g * 0.85f, Mathf.Min(1f, baseColor.g * 1.1f), s);
        c.b = Mathf.Lerp(baseColor.b * 0.85f, Mathf.Min(1f, baseColor.b * 1.1f), s);
        c.a = Mathf.Lerp(alphaMin, alphaMax, s);
      }
      ApplyColor(c);
    }

    void ApplyColor(Color c){ if(target != null) target.color = c; }
  }
}