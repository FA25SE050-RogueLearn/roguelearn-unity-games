using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BossFight2D.Systems;

namespace BossFight2D.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        public Animator animator;
        public BossFight2D.Combat.Hitbox2D hitbox;
        public int defaultDamage = 10;
        public float attackCooldown = 0.5f;
        public float hitboxWindow = 0.2f;
        public bool inputEnabled = true;

        [Header("Attack Charges (Stamina)")]
        [Tooltip("Current number of attack charges the player has.")]
        public int attackCharges = 0;
        [Tooltip("Maximum attack charges the player can hold.")]
        public int maxAttackCharges = 3;
        [Tooltip("Charges gained when answering a question correctly.")]
        public int chargesPerCorrect = 1;
        [Tooltip("Charges consumed per attack (applies even during Power Play).")]
        public int chargeCostPerAttack = 1;
        [Tooltip("Optional placeholder for a UI element (e.g., Text/Slider/Image) to reflect charges in the Inspector.")]
        public GameObject chargesUIPlaceholder;

        float _nextAttackAllowed;
        int _queuedDamage;
        float _inputSuppressUntil;
        bool _inAnswerMode;

        void Awake() { if (animator == null) animator = GetComponent<Animator>(); if (hitbox == null) hitbox = GetComponentInChildren<BossFight2D.Combat.Hitbox2D>(); }

        void OnEnable() { EventBus.AnswerSubmitted += OnAnswerSubmitted; EventBus.QuestionStarted += OnQuestionStarted; EventBus.AnswerModeExited += OnAnswerModeExited; EventBus.PowerPlayStarted += OnPowerPlayStarted; }
        void OnDisable() { EventBus.AnswerSubmitted -= OnAnswerSubmitted; EventBus.QuestionStarted -= OnQuestionStarted; EventBus.AnswerModeExited -= OnAnswerModeExited; EventBus.PowerPlayStarted -= OnPowerPlayStarted; }

        void Update()
        {
            if (!inputEnabled) return;
            // Allow attacks during Power Play even if answer mode flag is on
            bool powerPlayActive = false;
            var ppmCheck = BossFight2D.Core.GameObjectFactory.FindOrCreate<BossFight2D.Core.PowerPlayManager>();
            if (ppmCheck != null) powerPlayActive = ppmCheck.Active;
            // No attacks during answer mode unless Power Play is active
            if (_inAnswerMode && !powerPlayActive) return;
            // Short suppression window after submitting an answer to prevent the same click from triggering an attack
            if (Time.time < _inputSuppressUntil) return;
            // Block UI clicks (e.g., answer buttons) from triggering attacks
            if (!powerPlayActive && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            // Prevent attacks while inside the station safe zone during question phase
            if (!powerPlayActive && BossFight2D.Systems.ReadyStation.SafeZoneActive) return;
            if (Input.GetMouseButtonDown(0)) QueueAttack(defaultDamage);
        }

        void OnAnswerSubmitted(int selected, bool correct)
        {
            // Suppress input briefly to avoid attack being queued by the same click that submitted the answer
            _inputSuppressUntil = Time.time + 0.2f;
            // Award attack charges on correct answer
            if (correct)
            {
                attackCharges = Mathf.Clamp(attackCharges + Mathf.Max(1, chargesPerCorrect), 0, Mathf.Max(1, maxAttackCharges));
                UpdateChargesUI();
            }
        }

        void OnQuestionStarted(QuestionData q) { _inAnswerMode = true; }
        void OnAnswerModeExited() { _inAnswerMode = false; }
        void OnPowerPlayStarted(float duration) { _inAnswerMode = false; _inputSuppressUntil = 0f; }

        public void QueueAttack(int damage, GameObject target = null)
        {
            // Respect input gating and Answer Mode even if QueueAttack is called externally
            if (!inputEnabled) return;
            // Allow attacks during Power Play even if _inAnswerMode is true
            bool powerPlayActive = false;
            var ppmCheck = BossFight2D.Core.GameObjectFactory.FindOrCreate<BossFight2D.Core.PowerPlayManager>();
            if (ppmCheck != null) powerPlayActive = ppmCheck.Active;
            if (_inAnswerMode && !powerPlayActive)
            {
                // Block attacks while answering questions; Power Play clears this flag explicitly
                Debug.Log("PlayerCombat: Attack blocked during AnswerMode");
                return;
            }
            // Suppress accidental double-use from UI clicks
            if (!powerPlayActive && Time.time < _inputSuppressUntil) return;
            // Prevent attacks when inside station safe zone during question phase
            if (!powerPlayActive && BossFight2D.Systems.ReadyStation.SafeZoneActive) return;
            // Avoid UI pointer-over causing attacks
            if (!powerPlayActive && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            // Require sufficient attack charges to attempt any attack (applies even during Power Play)
            if (!HasChargesForAttack())
            {
                Debug.Log("PlayerCombat: Not enough attack charges");
                return;
            }

            _queuedDamage = damage;
            if (Time.time < _nextAttackAllowed) return;
            DoAttack();
        }

        void DoAttack()
        {
            // Final guard to ensure no attacks slip through during Answer Mode
            if (_inAnswerMode) return;
            // Consume charges before executing the attack
            if (!ConsumeChargesForAttack()) return;
            _nextAttackAllowed = Time.time + attackCooldown;
            int dmg = _queuedDamage > 0 ? _queuedDamage : defaultDamage;
            if (animator != null) animator.SetTrigger("Attack");
            // if (hitbox != null) { hitbox.Activate(hitboxWindow, dmg); Invoke(nameof(EndHitbox), hitboxWindow); }
            _queuedDamage = 0;
        }

        void EndHitbox() { if (hitbox != null) hitbox.Deactivate(); }

        public void AnimationEvent_HitboxStart() { if (hitbox != null) { hitbox.Activate(hitboxWindow, _queuedDamage > 0 ? _queuedDamage : defaultDamage); } }
        public void AnimationEvent_HitboxEnd() { EndHitbox(); }

        // Charges helpers
        public bool HasChargesForAttack()
        {
            return attackCharges >= Mathf.Max(1, chargeCostPerAttack);
        }

        bool ConsumeChargesForAttack()
        {
            int cost = Mathf.Max(1, chargeCostPerAttack);
            if (attackCharges < cost) return false;
            attackCharges -= cost;
            UpdateChargesUI();
            return true;
        }

        public bool TryConsumeChargeForQuestionEscape()
        {
            // Spend one charge to exit question mode early
            if (attackCharges <= 0) return false;
            attackCharges -= 1;
            UpdateChargesUI();
            return true;
        }

        void UpdateChargesUI()
        {
            // Placeholder for UI hook: if you assign a Text/Slider/Image in chargesUIPlaceholder,
            // you can update it here. We keep it minimal per request.
            if (chargesUIPlaceholder == null) return;
            var txt = chargesUIPlaceholder.GetComponent<Text>();
            if (txt != null) { txt.text = $"Charges: {attackCharges}/{maxAttackCharges}"; return; }
            var slider = chargesUIPlaceholder.GetComponent<Slider>();
            if (slider != null) { slider.maxValue = Mathf.Max(1, maxAttackCharges); slider.value = attackCharges; return; }
            var img = chargesUIPlaceholder.GetComponent<Image>();
            if (img != null)
            {
                float pct = Mathf.Clamp01(maxAttackCharges > 0 ? (float)attackCharges / maxAttackCharges : 0f);
                img.fillAmount = pct;
            }
        }
    }
}