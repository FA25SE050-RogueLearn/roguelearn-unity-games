using UnityEngine;
using TMPro;
using UnityEngine.UI;
using BossFight2D.Systems;

namespace BossFight2D.UI {
    public class QuestionPanelController : MonoBehaviour {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI questionText;
        [SerializeField] private Button answerA;
        [SerializeField] private Button answerB;
        [SerializeField] private Button answerC;
        [SerializeField] private Button answerD;
        [SerializeField] private Slider timerSlider;
        
        [Header("Text References")]
        [SerializeField] private TextMeshProUGUI answerAText;
        [SerializeField] private TextMeshProUGUI answerBText;
        [SerializeField] private TextMeshProUGUI answerCText;
        [SerializeField] private TextMeshProUGUI answerDText;

        [Header("Lifelines UI")]
        [SerializeField] private Button lifeline50Button;   // LifeLine1_Button
        [SerializeField] private Button lifelineFreezeButton; // LifeLine2_Button

        [Header("Timer Visuals")]
        [SerializeField] private Image timerFillImage; // Fill image of the slider
        [SerializeField] private Color timerNormalColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.65f, 0f);
        [SerializeField] private Color timerDangerColor = new Color(0.9f, 0.2f, 0.2f);
        [Range(0f,1f)] [SerializeField] private float warningThreshold = 0.3f;
        [Range(0f,1f)] [SerializeField] private float dangerThreshold = 0.15f;

        [Header("Answer Feedback")] 
        [SerializeField] private Color correctFlashColor = new Color(0.2f, 0.9f, 0.2f);
        [SerializeField] private Color incorrectFlashColor = new Color(0.9f, 0.25f, 0.25f);
        [SerializeField] private float flashDuration = 0.35f;
        
        private QuestionManager questionManager;
        private LifelineSystem lifelineSystem;
        private bool isActive = false;
        private bool fiftyApplied = false;

        // Cache base colors to restore after flashes
        private Color baseColorA = Color.white;
        private Color baseColorB = Color.white;
        private Color baseColorC = Color.white;
        private Color baseColorD = Color.white;

        // Event-driven UI overlays
        bool showAdvancePrompt = false;
        bool showPowerPlayBanner = false;
        float powerPlayEndTime = 0f;
        
        void Awake()
        {
            // Prefer an explicitly named child for the question prompt
            if (questionText == null)
            {
                var qTransform = transform.Find("Question");
                if (qTransform != null)
                {
                    questionText = qTransform.GetComponent<TextMeshProUGUI>();
                    if (questionText == null)
                    {
                        // In case TMP is deeper
                        questionText = qTransform.GetComponentInChildren<TextMeshProUGUI>();
                    }
                }
                // Fallback: first TMP under panel (may pick a button label if layout differs)
                if (questionText == null)
                {
                    questionText = GetComponentInChildren<TextMeshProUGUI>();
                }
            }
            
            if (answerA == null) answerA = transform.Find("Answer_A")?.GetComponent<Button>();
            if (answerB == null) answerB = transform.Find("Answer_B")?.GetComponent<Button>();
            if (answerC == null) answerC = transform.Find("Answer_C")?.GetComponent<Button>();
            if (answerD == null) answerD = transform.Find("Answer_D")?.GetComponent<Button>();
            
            // Get text components from buttons
            if (answerAText == null && answerA != null) answerAText = answerA.GetComponentInChildren<TextMeshProUGUI>();
            if (answerBText == null && answerB != null) answerBText = answerB.GetComponentInChildren<TextMeshProUGUI>();
            if (answerCText == null && answerC != null) answerCText = answerC.GetComponentInChildren<TextMeshProUGUI>();
            if (answerDText == null && answerD != null) answerDText = answerD.GetComponentInChildren<TextMeshProUGUI>();
            
            if (timerSlider == null)
                timerSlider = GetComponentInChildren<Slider>();

            // Try to resolve slider fill image
            if (timerFillImage == null && timerSlider != null && timerSlider.fillRect != null)
            {
                timerFillImage = timerSlider.fillRect.GetComponent<Image>();
            }

            // Cache base colors from target graphics
            if (answerA != null && answerA.targetGraphic != null) baseColorA = answerA.targetGraphic.color;
            if (answerB != null && answerB.targetGraphic != null) baseColorB = answerB.targetGraphic.color;
            if (answerC != null && answerC.targetGraphic != null) baseColorC = answerC.targetGraphic.color;
            if (answerD != null && answerD.targetGraphic != null) baseColorD = answerD.targetGraphic.color;
        }
        
        void Start()
        {
            questionManager = FindFirstObjectByType<QuestionManager>();
            lifelineSystem = FindFirstObjectByType<LifelineSystem>();
            
            // Wire up answer button events
            if (answerA != null) answerA.onClick.AddListener(() => SubmitAnswer(0));
            if (answerB != null) answerB.onClick.AddListener(() => SubmitAnswer(1));
            if (answerC != null) answerC.onClick.AddListener(() => SubmitAnswer(2));
            if (answerD != null) answerD.onClick.AddListener(() => SubmitAnswer(3));

            // Resolve and wire lifeline buttons by name if not assigned
            if (lifeline50Button == null)
            {
                var go = GameObject.Find("LifeLine1_Button");
                if (go != null) lifeline50Button = go.GetComponent<Button>();
            }
            if (lifelineFreezeButton == null)
            {
                var go = GameObject.Find("LifeLine2_Button");
                if (go != null) lifelineFreezeButton = go.GetComponent<Button>();
            }
            if (lifeline50Button != null) lifeline50Button.onClick.AddListener(OnFiftyFiftyClicked);
            if (lifelineFreezeButton != null) lifelineFreezeButton.onClick.AddListener(OnFreezeClicked);
            
            // Subscribe to events
            EventBus.QuestionStarted += OnQuestionStarted;
            EventBus.AnswerSubmitted += OnAnswerSubmitted;
            EventBus.QuestionTimeout += OnQuestionTimeout;
            EventBus.AdvancePromptShown += OnAdvanceShown;
            EventBus.AdvancePromptHidden += OnAdvanceHidden;
            EventBus.PowerPlayStarted += OnPowerPlayStarted;
            EventBus.PowerPlayEnded += OnPowerPlayEnded;
            
            // Validate bindings and warn if anything is missing
            ValidateReferences();
            
            // Start hidden
            gameObject.SetActive(false);
        }
        
        void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.QuestionStarted -= OnQuestionStarted;
            EventBus.AnswerSubmitted -= OnAnswerSubmitted;
            EventBus.QuestionTimeout -= OnQuestionTimeout;
            EventBus.AdvancePromptShown -= OnAdvanceShown;
            EventBus.AdvancePromptHidden -= OnAdvanceHidden;
            EventBus.PowerPlayStarted -= OnPowerPlayStarted;
            EventBus.PowerPlayEnded -= OnPowerPlayEnded;
        }
        
        void Update()
        {
            if (isActive && questionManager != null && timerSlider != null)
            {
                // Update timer visual
                float progress = questionManager.RemainingTime / GetCurrentQuestionTimeLimit();
                progress = Mathf.Clamp01(progress);
                timerSlider.value = progress;

                // Update timer color based on thresholds
                if (timerFillImage != null)
                {
                    if (progress <= dangerThreshold)
                        timerFillImage.color = timerDangerColor;
                    else if (progress <= warningThreshold)
                        timerFillImage.color = timerWarningColor;
                    else
                        timerFillImage.color = timerNormalColor;
                }
            }
        }
        
        private void OnQuestionStarted(QuestionData question)
        {
            // Ensure no pending hide from previous question interferes
            CancelInvoke(nameof(HidePanel));

            isActive = true;
            gameObject.SetActive(true);
            fiftyApplied = false;
            showAdvancePrompt = false; // hidden while a question is active
            
            // Populate question text
            if (questionText != null)
                questionText.text = question.prompt;
            
            // Populate answer options
            if (question.options != null && question.options.Length >= 4)
            {
                if (answerAText != null) answerAText.text = "A) " + question.options[0];
                if (answerBText != null) answerBText.text = "B) " + question.options[1];
                if (answerCText != null) answerCText.text = "C) " + question.options[2];
                if (answerDText != null) answerDText.text = "D) " + question.options[3];
            }
            
            // Reset timer
            if (timerSlider != null)
            {
                timerSlider.maxValue = 1f;
                timerSlider.value = 1f;
            }
            if (timerFillImage != null)
                timerFillImage.color = timerNormalColor;
            
            // Enable buttons and reset visuals/visibility
            SetButtonsInteractable(true);
            ResetAnswerVisibilityAndColors();

            // Re-enable lifeline buttons for the new question
            if (lifeline50Button != null) lifeline50Button.interactable = true;
            if (lifelineFreezeButton != null) lifelineFreezeButton.interactable = true;
        }
        
        private void OnAnswerSubmitted(int selectedIndex, bool correct)
        {
            isActive = false;
            SetButtonsInteractable(false);

            // Flash selected button to indicate correctness
            var selectedBtn = GetButtonByIndex(selectedIndex);
            if (selectedBtn != null)
                StartCoroutine(FlashButton(selectedBtn, correct ? correctFlashColor : incorrectFlashColor, flashDuration));
            
            // Optional: Briefly show correct answer before hiding
            Invoke(nameof(HidePanel), 1.5f);
        }
        
        private void OnQuestionTimeout()
        {
            isActive = false;
            SetButtonsInteractable(false);
            HidePanel();
        }
        
        private void SubmitAnswer(int choice)
        {
            if (questionManager != null && isActive)
            {
                questionManager.SubmitAnswer(choice);
            }
        }

        private void OnFiftyFiftyClicked()
        {
            if (!isActive || questionManager == null || fiftyApplied) return;
            if (lifelineSystem == null) lifelineSystem = FindFirstObjectByType<LifelineSystem>();
            if (lifelineSystem != null && lifelineSystem.UseFiftyFifty(questionManager))
            {
                ApplyFiftyFifty();
                fiftyApplied = true;
                if (lifeline50Button != null) lifeline50Button.interactable = false;
            }
        }

        private void OnFreezeClicked()
        {
            if (!isActive || questionManager == null) return;
            if (lifelineSystem == null) lifelineSystem = FindFirstObjectByType<LifelineSystem>();
            if (lifelineSystem != null && lifelineSystem.UseFreeze(questionManager))
            {
                if (lifelineFreezeButton != null) lifelineFreezeButton.interactable = false;
            }
        }
        
        private void HidePanel()
        {
            gameObject.SetActive(false);
        }
        
        private void SetButtonsInteractable(bool interactable)
        {
            if (answerA != null) answerA.interactable = interactable;
            if (answerB != null) answerB.interactable = interactable;
            if (answerC != null) answerC.interactable = interactable;
            if (answerD != null) answerD.interactable = interactable;
        }
        
        private float GetCurrentQuestionTimeLimit()
        {
            if (questionManager?.Pack?.questions != null && 
                questionManager.CurrentIndex >= 0 && 
                questionManager.CurrentIndex < questionManager.Pack.questions.Count)
            {
                return questionManager.Pack.questions[questionManager.CurrentIndex].timeLimitSec;
            }
            return 20f; // Default fallback
        }

        private void ResetAnswerVisibilityAndColors()
        {
            if (answerA != null)
            {
                answerA.gameObject.SetActive(true);
                if (answerA.targetGraphic != null) answerA.targetGraphic.color = baseColorA;
            }
            if (answerB != null)
            {
                answerB.gameObject.SetActive(true);
                if (answerB.targetGraphic != null) answerB.targetGraphic.color = baseColorB;
            }
            if (answerC != null)
            {
                answerC.gameObject.SetActive(true);
                if (answerC.targetGraphic != null) answerC.targetGraphic.color = baseColorC;
            }
            if (answerD != null)
            {
                answerD.gameObject.SetActive(true);
                if (answerD.targetGraphic != null) answerD.targetGraphic.color = baseColorD;
            }
        }

        private void ApplyFiftyFifty()
        {
            // Hide two wrong options
            if (questionManager?.Pack?.questions == null) return;
            var q = questionManager.Pack.questions[questionManager.CurrentIndex];
            int correct = q.correctIndex;
            // Collect wrong indices
            System.Collections.Generic.List<int> wrong = new System.Collections.Generic.List<int>();
            for (int i = 0; i < 4 && i < (q.options?.Length ?? 0); i++)
            {
                if (i != correct) wrong.Add(i);
            }
            if (wrong.Count < 2) return;
            // Randomly select two wrong indices to hide
            int i1 = Random.Range(0, wrong.Count);
            int first = wrong[i1];
            wrong.RemoveAt(i1);
            int i2 = Random.Range(0, wrong.Count);
            int second = wrong[i2];

            // Hide the selected wrong answers
            var b1 = GetButtonByIndex(first);
            var b2 = GetButtonByIndex(second);
            if (b1 != null) b1.gameObject.SetActive(false);
            if (b2 != null) b2.gameObject.SetActive(false);
        }

        private UnityEngine.UI.Button GetButtonByIndex(int idx)
        {
            switch (idx)
            {
                case 0: return answerA;
                case 1: return answerB;
                case 2: return answerC;
                case 3: return answerD;
                default: return null;
            }
        }

        private System.Collections.IEnumerator FlashButton(Button btn, Color flashColor, float duration)
        {
            if (btn == null || btn.targetGraphic == null) yield break;
            var g = btn.targetGraphic;
            Color original = g.color;
            g.color = flashColor;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime; // flash unaffected by time freeze
                yield return null;
            }
            // Smoothly revert back
            float back = 0.15f;
            t = 0f;
            while (t < back)
            {
                t += Time.unscaledDeltaTime;
                g.color = Color.Lerp(flashColor, original, t / back);
                yield return null;
            }
            g.color = original;
        }

        private void ValidateReferences()
        {
            if (questionText == null)
                Debug.LogWarning("QuestionPanelController: Missing reference for 'Question' text. Ensure a child named 'Question' with a TextMeshProUGUI is present.");
            if (answerA == null || answerAText == null)
                Debug.LogWarning("QuestionPanelController: Missing binding for Answer_A button or its TMP text child.");
            if (answerB == null || answerBText == null)
                Debug.LogWarning("QuestionPanelController: Missing binding for Answer_B button or its TMP text child.");
            if (answerC == null || answerCText == null)
                Debug.LogWarning("QuestionPanelController: Missing binding for Answer_C button or its TMP text child.");
            if (answerD == null || answerDText == null)
                Debug.LogWarning("QuestionPanelController: Missing binding for Answer_D button or its TMP text child.");
            if (timerSlider == null)
                Debug.LogWarning("QuestionPanelController: Timer Slider was not found. Timer UI will not update.");
            if (timerSlider != null && timerFillImage == null)
                Debug.LogWarning("QuestionPanelController: Timer fill Image not found. Urgency coloring will be disabled.");
            if (lifeline50Button == null)
                Debug.LogWarning("QuestionPanelController: LifeLine1_Button not found in scene. 50/50 lifeline will not be clickable.");
            if (lifelineFreezeButton == null)
                Debug.LogWarning("QuestionPanelController: LifeLine2_Button not found in scene. Freeze lifeline will not be clickable.");
        }
        
        // Event handlers for overlays
        void OnAdvanceShown(){ showAdvancePrompt = true; }
        void OnAdvanceHidden(){ showAdvancePrompt = false; }
        void OnPowerPlayStarted(float duration){ showPowerPlayBanner = true; powerPlayEndTime = Time.time + duration; }
        void OnPowerPlayEnded(){ showPowerPlayBanner = false; }

        void OnGUI(){
            // Simple overlay prompts for prototype
            if(showAdvancePrompt){
                var gm = BossFight2D.Core.GameObjectFactory.FindOrCreate<BossFight2D.Core.GameManager>();
                if(gm!=null && gm.State==BossFight2D.Core.GameState.Playing){
                    var style = new GUIStyle(GUI.skin.label); style.fontSize=20; style.alignment=TextAnchor.LowerCenter; style.normal.textColor=Color.white;
                    GUI.Label(new Rect(0, Screen.height-40, Screen.width, 30), "Press E to continue", style);
                }
            }
            if(showPowerPlayBanner){
                float remaining = Mathf.Max(0f, powerPlayEndTime - Time.time);
                string txt = $"POWER PLAY! First Hit Bonus – {Mathf.CeilToInt(remaining)}s";
                var style2 = new GUIStyle(GUI.skin.box); style2.fontSize=18; style2.alignment=TextAnchor.UpperCenter;
                GUI.Box(new Rect(Screen.width/2 - 180, 10, 360, 28), txt, style2);
            }
        }
    }
}