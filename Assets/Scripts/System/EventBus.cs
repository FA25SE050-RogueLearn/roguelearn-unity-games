using System;

namespace BossFight2D.Systems {
  public static class EventBus {
    public static event Action GameStarted; public static event Action GameWon; public static event Action GameLost;
    public static event Action<QuestionData> QuestionStarted; public static event Action<int,bool> AnswerSubmitted; public static event Action QuestionTimeout;
    public static event Action<float> PerfectDodgeWindowStarted;
    public static event Action PerfectDodgeWindowEnded;
    public static event Action PerfectDodgeSuccess;
    public static event Action WrongAnswerChallengeEnded;
    // Power Play events
    public static event Action<float> PowerPlayStarted;
    public static event Action PowerPlayEnded;
    // Advance prompt events
    public static event Action AdvancePromptShown;
    public static event Action AdvancePromptHidden;
    public static void RaiseGameStarted()=>GameStarted?.Invoke();
    public static void RaiseGameWon()=>GameWon?.Invoke();
    public static void RaiseGameLost()=>GameLost?.Invoke();
    public static void RaiseQuestionStarted(QuestionData q)=>QuestionStarted?.Invoke(q);
    public static void RaiseAnswerSubmitted(int selected,bool correct)=>AnswerSubmitted?.Invoke(selected,correct);
    public static void RaiseQuestionTimeout()=>QuestionTimeout?.Invoke();
    // Raisers for new events
    public static void RaisePerfectDodgeWindowStarted(float duration)=>PerfectDodgeWindowStarted?.Invoke(duration);
    public static void RaisePerfectDodgeWindowEnded()=>PerfectDodgeWindowEnded?.Invoke();
    public static void RaisePerfectDodgeSuccess()=>PerfectDodgeSuccess?.Invoke();
    public static void RaiseWrongAnswerChallengeEnded()=>WrongAnswerChallengeEnded?.Invoke();
    public static void RaisePowerPlayStarted(float duration)=>PowerPlayStarted?.Invoke(duration);
    public static void RaisePowerPlayEnded()=>PowerPlayEnded?.Invoke();
    public static void RaiseAdvancePromptShown()=>AdvancePromptShown?.Invoke();
    public static void RaiseAdvancePromptHidden()=>AdvancePromptHidden?.Invoke();
  }
}