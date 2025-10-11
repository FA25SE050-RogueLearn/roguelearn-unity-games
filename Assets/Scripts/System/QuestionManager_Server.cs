using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

namespace BossFight2D.Systems
{
    public class QuestionManager_Server : NetworkBehaviour
    {
        public static QuestionManager_Server Singleton { get; private set; }

        [Header("Load from Resources/QuestionPacks/questions_pack1.json (as TextAsset)")]
        public TextAsset questionsJson;
        public QuestionPackData Pack;
        public int CurrentIndex = -1;

        public NetworkVariable<float> RemainingTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private QuestionManager_Client questionManager_Client;
        private Coroutine questionTimerCoroutine;
        private bool isQuestionActive = false;

        private void Awake()
        {
            if (Singleton != null && Singleton != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Singleton = this;
            }
            questionManager_Client = GetComponent<QuestionManager_Client>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsServer)
            {
                enabled = false;
                return;
            }
            LoadQuestions();
            SelectAndSendNextQuestion();
        }

        private void LoadQuestions()
        {
            if (questionsJson == null) { questionsJson = Resources.Load<TextAsset>("QuestionPacks/questions_pack1"); }
            if (questionsJson != null)
            {
                var w = JsonUtility.FromJson<Wrapper>(questionsJson.text);
                Pack = w != null ? w.pack : null;
                if (Pack != null) { Debug.Log($"Loaded Question Pack: {Pack.name} with {Pack.questions.Count} questions."); }
                else { Debug.LogError("Failed to load or parse question pack."); }
            }
            else { Debug.LogError("questionsJson TextAsset is null. Make sure 'Resources/QuestionPacks/questions_pack1.json' exists."); }
        }

        public void SelectAndSendNextQuestion()
        {
            if (Pack == null || Pack.questions == null) { Debug.LogError("Question pack not loaded."); return; }

            CurrentIndex++;
            if (CurrentIndex >= Pack.questions.Count) { Debug.Log("End of questions."); return; }

            var q = Pack.questions[CurrentIndex];

            if (q.options.Length != 4)
            {
                Debug.LogError($"Question ID {q.id} does not have exactly 4 options. Skipping.");
                SelectAndSendNextQuestion();
                return;
            }

            isQuestionActive = true;
            questionManager_Client.DisplayQuestionClientRpc(q.prompt, q.options[0], q.options[1], q.options[2], q.options[3]);
            StartQuestionTimer(q.timeLimitSec);
        }

        private void StartQuestionTimer(float time)
        {
            if (questionTimerCoroutine != null)
            {
                StopCoroutine(questionTimerCoroutine);
            }
            RemainingTime.Value = time;
            questionTimerCoroutine = StartCoroutine(QuestionTimer());
        }

        private IEnumerator QuestionTimer()
        {
            while (RemainingTime.Value > 0)
            {
                RemainingTime.Value -= Time.deltaTime;
                yield return null;
            }

            isQuestionActive = false;
            Debug.Log("Server: Question timed out.");
            SelectAndSendNextQuestion();
        }

        public void ValidateAnswer(ulong clientId, int answerIndex)
        {
            if (!IsServer || !isQuestionActive) return;

            isQuestionActive = false;
            if (questionTimerCoroutine != null)
            {
                StopCoroutine(questionTimerCoroutine);
            }

            var q = Pack.questions[CurrentIndex];
            bool isCorrect = q.correctIndex == answerIndex;

            Debug.Log($"Server received answer '{answerIndex}' from client {clientId}. Correct: {isCorrect}");

            // For now, just move to the next question regardless of the answer.
            // We will add reward/penalty logic later.
            StartCoroutine(NextQuestionAfterDelay(1.5f));
        }

        private IEnumerator NextQuestionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SelectAndSendNextQuestion();
        }
    }
}