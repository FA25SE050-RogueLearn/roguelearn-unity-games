using UnityEngine;

namespace BossFight2D.Core {
  public static class GameObjectFactory {
    public static T FindOrCreate<T>() where T: MonoBehaviour {
      var e = Object.FindFirstObjectByType<T>();
      if(e!=null) return e; var go = new GameObject(typeof(T).Name); return go.AddComponent<T>();
    }
  }
}