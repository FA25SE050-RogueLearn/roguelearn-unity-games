using UnityEngine;
using System;

namespace BossFight2D.Core {
  public enum GameState { Init, Playing, Win, Lose, Paused }
  public class GameManager : MonoBehaviour {
    public GameState State = GameState.Init;
    void Awake(){ DontDestroyOnLoad(gameObject); }
    public void StartGame(){ State = GameState.Playing; Systems.EventBus.RaiseGameStarted(); }
    public void WinGame(){ State = GameState.Win; Systems.EventBus.RaiseGameWon(); }
    public void LoseGame(){ State = GameState.Lose; Systems.EventBus.RaiseGameLost(); }
    public void Restart(){ var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex; UnityEngine.SceneManagement.SceneManager.LoadScene(s); }
  }
}