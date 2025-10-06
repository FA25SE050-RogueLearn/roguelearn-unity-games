using UnityEngine;
using UnityEngine.UI;

// Simple manual trigger to start a Power Play window for testing or special ability usage
// Attach to a UI Button and configure duration in Inspector
public class PowerPlayButton : MonoBehaviour
{
    [Range(1f,10f)] public float durationSeconds = 5f;
    Button _btn;
    void Awake(){ _btn = GetComponent<Button>(); if(_btn!=null) _btn.onClick.AddListener(OnClick); }
    void OnDestroy(){ if(_btn!=null) _btn.onClick.RemoveListener(OnClick); }
    void OnClick(){ var ppm = BossFight2D.Core.GameObjectFactory.FindOrCreate<BossFight2D.Core.PowerPlayManager>(); if(ppm!=null) ppm.StartWindow(durationSeconds); }
}