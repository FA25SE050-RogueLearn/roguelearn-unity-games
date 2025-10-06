using UnityEngine;

namespace BossFight2D.UI {
  [RequireComponent(typeof(Transform))]
  public class PowerPlayRing : MonoBehaviour {
    public LineRenderer line;
    [Tooltip("Ring radius relative to player size (world units)")]
    public float radius = 0.9f;
    [Tooltip("Ring thickness (world units)")]
    public float thickness = 0.08f;
    public int segments = 64;

    [Header("Calm Tech Palette")]
    [Tooltip("Active start color (Teal #00C4B3)")]
    public Color activeStart = new Color(0f, 0.7686f, 0.7019f);
    [Tooltip("Active end color (Blue #2D9CFF)")]
    public Color activeEnd = new Color(0.1765f, 0.6118f, 1f);
    [Tooltip("Cooldown color (Amber #FFB74D)")]
    public Color cooldownColor = new Color(1f, 0.7176f, 0.3020f);
    [Tooltip("Hit flash color (White)")]
    public Color hitFlashColor = Color.white;

    float _windowDuration;
    float _windowEnd;
    bool _active;

    void Awake(){
      if(line==null){
        line = gameObject.GetComponent<LineRenderer>();
        if(line==null) line = gameObject.AddComponent<LineRenderer>();
      }
      // Configure line renderer for a world-space ring around the player
      line.useWorldSpace = false;
      line.loop = false;
      line.widthMultiplier = thickness;
      line.numCornerVertices = 2;
      line.numCapVertices = 2;
      line.material = new Material(Shader.Find("Sprites/Default"));
      SetActiveGradient();
      line.enabled = false;
    }

    void OnEnable(){ BossFight2D.Systems.EventBus.PowerPlayStarted += OnPowerPlayStarted; BossFight2D.Systems.EventBus.PowerPlayEnded += OnPowerPlayEnded; BossFight2D.Systems.EventBus.PowerPlayHitConfirmed += OnPowerPlayHit; }
    void OnDisable(){ BossFight2D.Systems.EventBus.PowerPlayStarted -= OnPowerPlayStarted; BossFight2D.Systems.EventBus.PowerPlayEnded -= OnPowerPlayEnded; BossFight2D.Systems.EventBus.PowerPlayHitConfirmed -= OnPowerPlayHit; }

    void Update(){
      if(!_active) return;
      float remaining = Mathf.Max(0f, _windowEnd - Time.time);
      float fraction = (_windowDuration>0f? (remaining / _windowDuration) : 0f);
      DrawArc(fraction);
      if(remaining <= 0f){ EndRing(); }
    }

    void OnPowerPlayStarted(float duration){ _windowDuration = duration; _windowEnd = Time.time + duration; _active = true; SetActiveGradient(); line.enabled = true; DrawArc(1f); }
    void OnPowerPlayEnded(){ EndRing(); }
    void OnPowerPlayHit(){ if(!_active) return; StopAllCoroutines(); StartCoroutine(Pulse()); }

    void EndRing(){ _active = false; line.enabled = false; }

    void SetActiveGradient(){
      var grad = new Gradient();
      grad.SetKeys(
        new GradientColorKey[]{ new GradientColorKey(activeStart, 0f), new GradientColorKey(activeEnd, 1f) },
        new GradientAlphaKey[]{ new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
      );
      line.colorGradient = grad;
    }

    void DrawArc(float fraction){
      fraction = Mathf.Clamp01(fraction);
      int points = Mathf.Max(2, Mathf.RoundToInt(segments * fraction));
      line.positionCount = points;
      float maxAngle = fraction * Mathf.PI * 2f;
      for(int i=0;i<points;i++){
        float t = (points<=1? 0f : (float)i/(points-1));
        float ang = t * maxAngle;
        Vector3 p = new Vector3(Mathf.Cos(ang)*radius, Mathf.Sin(ang)*radius, 0f);
        line.SetPosition(i,p);
      }
    }

    System.Collections.IEnumerator Pulse(){
      // Brief flash to white, then restore gradient
      Color originalStart = activeStart, originalEnd = activeEnd;
      line.startColor = hitFlashColor; line.endColor = hitFlashColor;
      yield return new WaitForSeconds(0.08f);
      SetActiveGradient();
    }
  }
}