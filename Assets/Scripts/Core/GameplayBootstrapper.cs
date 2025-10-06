using UnityEngine;
using BossFight2D.Core;
using BossFight2D.UI;

namespace BossFight2D.Core {
  /// <summary>
  /// Bootstraps gameplay by starting the GameManager when the Gameplay scene loads.
  /// </summary>
  public class GameplayBootstrapper : MonoBehaviour {
    void Start(){
      // If a Main Menu exists in this scene, do not auto-start. Let the menu control game start.
      var menu = FindFirstObjectByType<MainMenuUI>();
      if(menu != null) return;
      GameObjectFactory.FindOrCreate<GameManager>()?.StartGame();
    }
  }
}
