using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

namespace BossFight2D.Systems
{
    public class QuestionManager_Client : NetworkBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI questionText;
        public TextMeshProUGUI timerText;
        public Button[] answerButtons;

        private void Awake()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int index = i;
                answerButtons[i].onClick.AddListener(() => SubmitAnswer(index));
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                enabled = false;
                return;
            }
            if (QuestionManager_Server.Singleton != null)
            {
                QuestionManager_Server.Singleton.RemainingTime.OnValueChanged += UpdateTimerUI;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (QuestionManager_Server.Singleton != null)
            {
                QuestionManager_Server.Singleton.RemainingTime.OnValueChanged -= UpdateTimerUI;
            }
        }

        private void UpdateTimerUI(float previousValue, float newValue)
        {
            if (timerText != null)
            {
                timerText.text = Mathf.CeilToInt(newValue).ToString();
            }
        }

        [ClientRpc]
        public void DisplayQuestionClientRpc(string question, string answer1, string answer2, string answer3, string answer4)
        {
            if (questionText != null) { questionText.text = question; }

            if (answerButtons != null && answerButtons.Length == 4)
            {
                answerButtons[0].GetComponentInChildren<TextMeshProUGUI>().text = answer1;
                answerButtons[1].GetComponentInChildren<TextMeshProUGUI>().text = answer2;
                answerButtons[2].GetComponentInChildren<TextMeshProUGUI>().text = answer3;
                answerButtons[3].GetComponentInChildren<TextMeshProUGUI>().text = answer4;
            }
        }

        public void SubmitAnswer(int choice)
        {
            SubmitAnswerServerRpc(choice);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SubmitAnswerServerRpc(int choice, ServerRpcParams rpcParams = default)
        {
            QuestionManager_Server.Singleton.ValidateAnswer(rpcParams.Receive.SenderClientId, choice);
        }
    }
}