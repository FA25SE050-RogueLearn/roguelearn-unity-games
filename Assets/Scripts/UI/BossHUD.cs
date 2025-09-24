// BossHUD.cs - Binds BossStateMachine HP to a UI Slider named "BossHealth"
using UnityEngine;
using UnityEngine.UI;

namespace BossFight2D.UI
{
    public class BossHUD : MonoBehaviour
    {
        [Header("Optional explicit bindings (auto-resolved by name if empty)")]
        [SerializeField] private Slider bossHealthSlider;
        [SerializeField] private BossFight2D.Boss.BossStateMachine boss;

        private void Awake()
        {
            // Resolve UI by common name if not explicitly assigned
            if (bossHealthSlider == null)
            {
                var go = GameObject.Find("BossHealth");
                if (go != null) bossHealthSlider = go.GetComponent<Slider>();
            }

            // Initialize once
            UpdateBar(force: true);
        }

        private void Start()
        {
            // Resolve gameplay component
            if (boss == null) boss = FindFirstObjectByType<BossFight2D.Boss.BossStateMachine>();
            // Ensure slider normalized range
            if (bossHealthSlider != null)
            {
                bossHealthSlider.minValue = 0f;
                bossHealthSlider.maxValue = 1f;
            }
        }

        private void Update()
        {
            // Polling keeps UI in sync without requiring gameplay events
            UpdateBar(force: false);
        }

        private void UpdateBar(bool force)
        {
            if (boss != null && bossHealthSlider != null)
            {
                float h = boss.maxHP > 0 ? (float)boss.hp / boss.maxHP : 0f;
                if (force || !Mathf.Approximately(bossHealthSlider.value, h))
                {
                    bossHealthSlider.SetValueWithoutNotify(h);
                }
            }
        }
    }
}