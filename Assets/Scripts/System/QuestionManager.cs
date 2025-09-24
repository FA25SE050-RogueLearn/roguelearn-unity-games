using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace BossFight2D.Systems {
  [Serializable] public class QuestionData { public int id; public string topic; public string difficulty; public string prompt; public string[] options; public int correctIndex; public int timeLimitSec=20; public string explanation; }
  [Serializable] public class QuestionPackData { public string name; public List<QuestionData> questions = new(); }
  [Serializable] class Wrapper { public QuestionPackData pack; }

  public class QuestionManager : MonoBehaviour {
    [Header("Load from Resources/QuestionPacks/questions_pack1.json (as TextAsset)")]
    public TextAsset questionsJson; 
    public QuestionPackData Pack; 
    public int CurrentIndex=-1; 
    public float RemainingTime; 
    public bool QuestionActive;

    [Header("References (optional)")]
    public BossFight2D.Boss.BossStateMachine bossOverride;
    public BossFight2D.Player.PlayerHealth playerOverride;
    [Tooltip("If assigned, will be SetActive(true) on question start and false on answer/timeout")] public GameObject questionPanel;

    [Header("Answer Bonuses")]
    [Tooltip("Temporary bonus applied to the next correct answer (e.g., from perfect dodge). Value is a multiplier delta: 0.1 = +10%.")]
    public float nextAnswerBonus = 0f;
    public float perfectDodgeBonus = 0.1f;

    // Manual progression state (press E to continue)
    [Header("Manual Progression")]
    [Tooltip("When true, question progression waits for the player to press E after combat window resolves.")]
    public bool manualAdvance = true;
    bool awaitingAdvance = false;

    void OnEnable(){ EventBus.PerfectDodgeSuccess += OnPerfectDodge; }
    void OnDisable(){ EventBus.PerfectDodgeSuccess -= OnPerfectDodge; }

    void OnPerfectDodge(){ nextAnswerBonus = Mathf.Max(nextAnswerBonus, perfectDodgeBonus); }

    void Start(){
      if(Pack==null){
        if(questionsJson==null){ questionsJson = Resources.Load<TextAsset>("QuestionPacks/questions_pack1"); }
        if(questionsJson!=null){ var w = JsonUtility.FromJson<Wrapper>(questionsJson.text); Pack = w!=null? w.pack : null; }
      }
      NextQuestion();
    }
    public void NextQuestion(){ CurrentIndex++; if(Pack==null || Pack.questions==null || CurrentIndex>=Pack.questions.Count){ BossFight2D.Core.GameObjectFactory.FindOrCreate<BossFight2D.Core.GameManager>()?.WinGame(); return; } var q=Pack.questions[CurrentIndex]; RemainingTime=q.timeLimitSec; QuestionActive=true; awaitingAdvance=false; EventBus.RaiseAdvancePromptHidden(); EventBus.RaiseQuestionStarted(q); if(questionPanel) questionPanel.SetActive(true); }
    void Update(){
      if(QuestionActive){
        RemainingTime-=Time.deltaTime; if(RemainingTime<=0f){ QuestionActive=false; EventBus.RaiseQuestionTimeout(); if(questionPanel) questionPanel.SetActive(false); ResetCombo(); if(manualAdvance){ StartCoroutine(PrepareAdvanceAfterTimeout()); } else { StartCoroutine(NextQuestionAfter(0.75f)); } }
      } else if(awaitingAdvance && manualAdvance){
        if(Input.GetKeyDown(KeyCode.E)) { awaitingAdvance=false; EventBus.RaiseAdvancePromptHidden(); NextQuestion(); }
      }
    }
    public void SubmitAnswer(int choice){ if(!QuestionActive) return; var q=Pack.questions[CurrentIndex]; bool correct = choice==q.correctIndex; QuestionActive=false; EventBus.RaiseAnswerSubmitted(choice,correct); if(questionPanel) questionPanel.SetActive(false); if(correct){
        // Start Power Play window before applying damage so the first hit benefits
        var ppm = BossFight2D.Core.GameObjectFactory.FindOrCreate<BossFight2D.Core.PowerPlayManager>();
        if(ppm!=null){ ppm.StartWindow(5f); }
        BossFight2D.Combat.CombatResolver.ApplyAnswerDamage(q,this);
        AddCombo();
        if(manualAdvance){ StartCoroutine(PrepareAdvanceAfterPowerPlay(ppm)); } else { StartCoroutine(NextQuestionAfter(0.75f)); }
      } else { BossFight2D.Combat.CombatResolver.ApplyPenalty(q,this); ResetCombo(); if(manualAdvance){ StartCoroutine(PrepareAdvanceAfterWrong()); } else { StartCoroutine(NextQuestionAfterWrong()); } } }
    int combo=0; public void AddCombo(){ combo=Mathf.Min(combo+1,10);} public void ResetCombo(){ combo=0;} public int Combo=>combo;

    public float ConsumeNextAnswerBonus(){ var v = nextAnswerBonus; nextAnswerBonus = 0f; return v; }

    IEnumerator NextQuestionAfter(float delay){ yield return new WaitForSeconds(delay); var boss = (bossOverride!=null? bossOverride : UnityEngine.Object.FindFirstObjectByType<BossFight2D.Boss.BossStateMachine>()); if(boss!=null && boss.phase==BossFight2D.Boss.BossPhase.Dead) yield break; NextQuestion(); }

    // Wait for boss wrong-answer challenge to end (attack finished or canceled) before starting the next question
    IEnumerator NextQuestionAfterWrong(){
      bool ended = false;
      System.Action handler = () => { ended = true; };
      EventBus.WrongAnswerChallengeEnded += handler;
      // Safety timeout in case event was raised immediately or boss has no combat; keep short to avoid hanging
      float timeout = 2.0f; float end = Time.time + timeout;
      while(!ended && Time.time < end){ yield return null; }
      EventBus.WrongAnswerChallengeEnded -= handler;
      // Small grace period for flow polish
      yield return new WaitForSeconds(0.25f);
      var boss = (bossOverride!=null? bossOverride : UnityEngine.Object.FindFirstObjectByType<BossFight2D.Boss.BossStateMachine>());
      if(boss!=null && boss.phase==BossFight2D.Boss.BossPhase.Dead) yield break;
      NextQuestion();
    }

    // Manual-advance variants
    IEnumerator PrepareAdvanceAfterPowerPlay(BossFight2D.Core.PowerPlayManager ppm){
      // Wait until power play window ends (consumed or timed out)
      float safety = 6f; float until = Time.unscaledTime + safety;
      while(ppm!=null && ppm.Active && Time.unscaledTime < until){ yield return null; }
      yield return new WaitForSeconds(0.1f);
      var boss = (bossOverride!=null? bossOverride : UnityEngine.Object.FindFirstObjectByType<BossFight2D.Boss.BossStateMachine>());
      if(boss!=null && boss.phase==BossFight2D.Boss.BossPhase.Dead) yield break;
      awaitingAdvance = true; EventBus.RaiseAdvancePromptShown();
    }

    IEnumerator PrepareAdvanceAfterWrong(){
      bool ended = false; System.Action handler = () => { ended = true; };
      EventBus.WrongAnswerChallengeEnded += handler; float timeout = 3.0f; float end = Time.time + timeout;
      while(!ended && Time.time < end){ yield return null; }
      EventBus.WrongAnswerChallengeEnded -= handler;
      yield return new WaitForSeconds(0.1f);
      var boss = (bossOverride!=null? bossOverride : UnityEngine.Object.FindFirstObjectByType<BossFight2D.Boss.BossStateMachine>());
      if(boss!=null && boss.phase==BossFight2D.Boss.BossPhase.Dead) yield break;
      awaitingAdvance = true; EventBus.RaiseAdvancePromptShown();
    }

    IEnumerator PrepareAdvanceAfterTimeout(){
      // On timeout, there is no punish window; allow immediate advance
      yield return new WaitForSeconds(0.1f);
      awaitingAdvance = true; EventBus.RaiseAdvancePromptShown();
    }
  }
}