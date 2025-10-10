using UnityEngine;

namespace RogueLearn.UI
{
    [DisallowMultipleComponent]
    public class CharacterPreviewController : MonoBehaviour
    {
        [Header("Animator Setup")]
        [Tooltip("Animator to control. If not set, will try to find one on this GameObject.")]
        public Animator animator;
        [Tooltip("Float parameter name used for locomotion (idle/run). Typical: 'Speed'.")]
        public string speedParam = "Speed";
        [Tooltip("Trigger parameter name used for primary attack animation. Typical: 'Attack'.")]
        public string attackTrigger = "Attack";
        [Tooltip("Trigger parameter name used for secondary attack animation.")]
        public string attack2Trigger = "Attack2";

        [Header("Guard Settings")]
        [Tooltip("Use a Bool parameter to hold the Guard state. If false, will use a Trigger to enter Guard.")]
        public bool guardUsesBool = true;
        [Tooltip("Bool parameter name for Guard state when guardUsesBool = true.")]
        public string guardBoolParam = "Guard";
        [Tooltip("Trigger parameter name for Guard state when guardUsesBool = false.")]
        public string guardTrigger = "Guard";
        [Tooltip("Duration (seconds) to keep the Guard animation looping.")]
        [Min(0.1f)] public float guardDuration = 3f;
        [Tooltip("ParticleSystem to play while Guard is active (e.g., arrow field).")]
        public ParticleSystem guardArrowFieldFX;
        [Tooltip("Optionally keep running after Guard ends until the next cycle. If false, returns to idle.")]
        public bool resumeRunAfterGuard = false;

        [Header("Behaviour")]
        [Tooltip("If true, the preview will auto-cycle animations.")]
        public bool autoCycle = true;
        [Tooltip("Seconds between auto cycles when enabled.")]
        [Min(0.1f)] public float cycleInterval = 3f;
        [Tooltip("Probability (0..1) that the next auto cycle will perform an attack.")]
        [Range(0f, 1f)] public float attackProbability = 0.25f;
        [Tooltip("Probability (0..1) that the next auto cycle will perform the secondary attack.")]
        [Range(0f, 1f)] public float attack2Probability = 0.15f;
        [Tooltip("Probability (0..1) that the next auto cycle will enter Guard for guardDuration seconds.")]
        [Range(0f, 1f)] public float guardProbability = 0.20f;
        [Tooltip("If true, clicking/tapping will cycle animations (Idle → Run → Guard → Attack → Attack2 → Idle).")]
        public bool clickCycles = true;

        [Header("Locomotion Values")]
        [Tooltip("Speed value used to represent Running (Idle uses 0). Matches your Animator blend.")]
        public float runSpeedValue = 1f;
        [Tooltip("Optionally keep running after an attack until the next cycle. If false, returns to idle.")]
        public bool resumeRunAfterAttack = false;

        enum PreviewState { Idle, Run, Guard }
        PreviewState _state = PreviewState.Idle;
        float _nextCycleAt;
        float _guardUntil;

        void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            ApplyState(_state);
            _nextCycleAt = Time.time + cycleInterval;
            _guardUntil = 0f;
            StopGuardFX();
        }

        void Update()
        {
            // End guard automatically when duration is reached
            if (_state == PreviewState.Guard && Time.time >= _guardUntil)
            {
                EndGuard();
            }

            if (clickCycles && Input.GetMouseButtonDown(0))
            {
                CycleOnClick();
            }

            if (autoCycle && Time.time >= _nextCycleAt)
            {
                AutoCycle();
                _nextCycleAt = Time.time + cycleInterval;
            }
        }

        void AutoCycle()
        {
            if (animator == null && Time.time <= _guardUntil ) return;

            float r = Random.value;
            if (r < guardProbability)
            {
                StartGuard(guardDuration);
                return;
            }
            r = Random.value;
            if (r < attackProbability)
            {
                DoAttack();
                if (!resumeRunAfterAttack) SetIdle();
                return;
            }
            r = Random.value;
            if (r < attack2Probability)
            {
                DoAttack2();
                if (!resumeRunAfterAttack) SetIdle();
                return;
            }

            // Toggle between idle and run if no special actions selected
            if (_state == PreviewState.Idle) SetRun(); else SetIdle();
        }

        void CycleOnClick()
        {
            if (animator == null) return;

            // Click sequence: Idle → Run → Guard → Attack → Attack2 → Idle
            switch (_state)
            {
                case PreviewState.Idle:
                    SetRun();
                    break;
                case PreviewState.Run:
                    StartGuard(guardDuration);
                    break;
                case PreviewState.Guard:
                    // If clicked during Guard, end Guard and perform attacks
                    EndGuard();
                    DoAttack();
                    // Chain into Attack2 then back to idle for preview
                    DoAttack2();
                    if (!resumeRunAfterAttack) SetIdle();
                    break;
            }
        }

        void SetIdle()
        {
            _state = PreviewState.Idle;
            if (animator != null && !string.IsNullOrEmpty(speedParam))
            {
                animator.SetFloat(speedParam, 0f);
            }
            // Ensure guard off
            SetGuardParam(false);
            StopGuardFX();
        }

        void SetRun()
        {
            _state = PreviewState.Run;
            if (animator != null && !string.IsNullOrEmpty(speedParam))
            {
                animator.SetFloat(speedParam, runSpeedValue);
            }
            // Ensure guard off
            SetGuardParam(false);
            StopGuardFX();
        }

        void StartGuard(float duration)
        {
            _state = PreviewState.Guard;
            SetGuardParam(true);
            _guardUntil = Time.time + Mathf.Max(0.1f, duration);
            PlayGuardFX();
            // Hold locomotion at idle speed while guarding
            if (!string.IsNullOrEmpty(speedParam)) animator.SetFloat(speedParam, 0f);
        }

        void EndGuard()
        {
            SetGuardParam(false);
            StopGuardFX();
            _state = resumeRunAfterGuard ? PreviewState.Run : PreviewState.Idle;
            ApplyState(_state);
        }

        void SetGuardParam(bool active)
        {
            if (animator == null) return;
            if (guardUsesBool && !string.IsNullOrEmpty(guardBoolParam))
            {
                animator.SetBool(guardBoolParam, active);
            }
            else if (!guardUsesBool && active && !string.IsNullOrEmpty(guardTrigger))
            {
                animator.SetTrigger(guardTrigger);
            }
        }

        void PlayGuardFX()
        {
            if (guardArrowFieldFX != null && !guardArrowFieldFX.isPlaying)
            {
                guardArrowFieldFX.Play();
            }
        }

        void StopGuardFX()
        {
            if (guardArrowFieldFX != null && guardArrowFieldFX.isPlaying)
            {
                guardArrowFieldFX.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        void DoAttack()
        {
            if (animator != null && !string.IsNullOrEmpty(attackTrigger))
            {
                animator.SetTrigger(attackTrigger);
            }
        }

        void DoAttack2()
        {
            if (animator != null && !string.IsNullOrEmpty(attack2Trigger))
            {
                animator.SetTrigger(attack2Trigger);
            }
        }

        void ApplyState(PreviewState s)
        {
            if (s == PreviewState.Idle) SetIdle();
            else if (s == PreviewState.Run) SetRun();
            else StartGuard(guardDuration);
        }
    }
}