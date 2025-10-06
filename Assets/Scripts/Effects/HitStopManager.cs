using UnityEngine;

namespace BossFight2D.Effects
{
  // Lightweight global hitstop that pauses gameplay briefly using unscaled time
  public class HitStopManager : MonoBehaviour
  {
    [Header("Hitstop Settings")]
    [Tooltip("Duration of the freeze in seconds (unscaled time)")]
    public float duration = 0.06f;

    [Tooltip("Only apply hitstop on confirmed Power Play hits")]
    public bool powerPlayOnly = true;

    float _prevTimeScale = 1f;
    float _prevFixedDelta = 0.02f;
    bool _active;

    void OnEnable()
    {
      BossFight2D.Systems.EventBus.PowerPlayHitConfirmed += OnPowerPlayHitConfirmed;
    }

    void OnDisable()
    {
      BossFight2D.Systems.EventBus.PowerPlayHitConfirmed -= OnPowerPlayHitConfirmed;
    }

    void OnPowerPlayHitConfirmed()
    {
      if (powerPlayOnly == false)
      {
        // If not restricted, we still only act on this event
      }
      if (!_active && Time.timeScale > 0f)
      {
        StartCoroutine(DoHitStop());
      }
    }

    System.Collections.IEnumerator DoHitStop()
    {
      _active = true;
      _prevTimeScale = Time.timeScale;
      _prevFixedDelta = Time.fixedDeltaTime;
      // Pause gameplay; UI will continue because we wait using realtime
      Time.timeScale = 0f;
      // Keep physics step consistent when resuming by freezing fixedDeltaTime
      Time.fixedDeltaTime = 0f;
      yield return new WaitForSecondsRealtime(duration);
      Time.timeScale = _prevTimeScale;
      Time.fixedDeltaTime = _prevFixedDelta;
      _active = false;
    }
  }
}