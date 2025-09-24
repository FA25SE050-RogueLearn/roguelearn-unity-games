// QuestionDebugOverlay.cs - Minimal on-screen question display for debugging
using UnityEngine;
using BossFight2D.Systems;

namespace BossFight2D.UI {
  public class QuestionDebugOverlay : MonoBehaviour {
    public QuestionManager qm;
    private void Awake(){ if(qm==null) qm = FindFirstObjectByType<QuestionManager>(); EventBus.QuestionStarted += OnQuestionStarted; }
    private void OnDestroy(){ EventBus.QuestionStarted -= OnQuestionStarted; }
    private void OnQuestionStarted(QuestionData q){ Debug.Log($"[Question] {q.prompt}"); }

    private void OnGUI(){
      if(qm==null || !qm.QuestionActive || qm.Pack==null) return;
      var q = qm.Pack.questions[qm.CurrentIndex];
      var w = Mathf.Min(600, Screen.width-20); var x = 10; var y = 10; var line = 22; int h = 10 + line* (3 + q.options.Length);
      GUI.Box(new Rect(x,y,w,h), "Question");
      y += 24;
      GUI.Label(new Rect(x+8,y,w-16,line*2), q.prompt); y += line*2;
      GUI.Label(new Rect(x+8,y,w-16,line), $"Time: {Mathf.CeilToInt(qm.RemainingTime)}s  (Press 1-4 or click options)"); y += line;
      for(int i=0;i<q.options.Length;i++){
        if(GUI.Button(new Rect(x+8,y,w-16,line+6), $"{i+1}. {q.options[i]}")){
          qm.SubmitAnswer(i);
        }
        y += line+8;
      }
    }
  }
}