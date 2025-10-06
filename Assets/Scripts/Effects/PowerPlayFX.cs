
using UnityEngine;
using UnityEngine.UI;

namespace BossFight2D.Effects
{
  // Plays SFX and triggers a brief screen flash on confirmed Power Play hits
  public class PowerPlayFX : MonoBehaviour
  {
    [Header("Audio")] public AudioSource audioSource; public AudioClip powerPlayHitClip; public float volume = 0.9f;

    [Header("Screen Flash")] public Color flashColor = new Color(0.85f, 0.95f, 1f, 0.25f); public float flashDuration = 0.15f;
    [Tooltip("Provide an Image used as a full-screen overlay for flash. If not set, one will be created.")]
    public Image overlayImage;
    Canvas _overlayCanvas;

    void Awake()
    {
      // Ensure AudioSource exists
      if (audioSource == null)
      {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
      }

      // Ensure overlay image exists
      if (overlayImage == null)
      {
        var canvasGO = new GameObject("PowerPlayOverlayCanvas");
        _overlayCanvas = canvasGO.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        var imgGO = new GameObject("OverlayImage");
        imgGO.transform.SetParent(canvasGO.transform, false);
        overlayImage = imgGO.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0);
        overlayImage.raycastTarget = false;

        // Stretch to full screen
        var rt = overlayImage.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
      }
      else
      {
        _overlayCanvas = overlayImage.GetComponentInParent<Canvas>();
      }
    }

    void OnEnable() { BossFight2D.Systems.EventBus.PowerPlayHitConfirmed += OnPowerPlayHitConfirmed; }
    void OnDisable() { BossFight2D.Systems.EventBus.PowerPlayHitConfirmed -= OnPowerPlayHitConfirmed; }

    void OnPowerPlayHitConfirmed()
    {
      PlaySFX();
      TriggerFlash();
    }

    void PlaySFX()
    {
      if (audioSource != null && powerPlayHitClip != null)
      {
        audioSource.PlayOneShot(powerPlayHitClip, volume);
      }
    }

    void TriggerFlash()
    {
      if (overlayImage == null) return;
      StopAllCoroutines();
      StartCoroutine(FlashCoroutine());
    }

    System.Collections.IEnumerator FlashCoroutine()
    {
      float start = Time.realtimeSinceStartup;
      // fade in quickly, then out over flashDuration, using unscaled time
      float half = flashDuration * 0.25f; // quick pop
      // Fade in
      UnityEngine.Color startColor = overlayImage.color;
      while (Time.realtimeSinceStartup - start < half)
      {
        float t = (Time.realtimeSinceStartup - start) / half;
        overlayImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, Mathf.Lerp(0f, flashColor.a, t));
        yield return null;
      }
      overlayImage.color = flashColor;

      // Fade out
      float outStart = Time.realtimeSinceStartup;
      while (Time.realtimeSinceStartup - outStart < flashDuration)
      {
        float t = (Time.realtimeSinceStartup - outStart) / flashDuration;
        overlayImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, Mathf.Lerp(flashColor.a, 0f, t));
        yield return null;
      }
      overlayImage.color = startColor;
    }
  }
}