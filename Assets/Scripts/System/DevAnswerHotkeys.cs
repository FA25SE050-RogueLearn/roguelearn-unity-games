// DevAnswerHotkeys.cs - temporary helper to submit answers via keys 1..4
using UnityEngine;
using BossFight2D.Systems;

public class DevAnswerHotkeys : MonoBehaviour {
  public QuestionManager qm;
  void Awake(){ if(qm==null) qm = FindFirstObjectByType<QuestionManager>(); }
  void Update(){
    if(qm==null || !qm.QuestionActive) return;
    if(Input.GetKeyDown(KeyCode.Alpha1)) qm.SubmitAnswer(0);
    if(Input.GetKeyDown(KeyCode.Alpha2)) qm.SubmitAnswer(1);
    if(Input.GetKeyDown(KeyCode.Alpha3)) qm.SubmitAnswer(2);
    if(Input.GetKeyDown(KeyCode.Alpha4)) qm.SubmitAnswer(3);
  }
}