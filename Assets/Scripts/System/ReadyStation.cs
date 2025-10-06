using UnityEngine;
using BossFight2D.Core;

namespace BossFight2D.Systems
{
    // ReadyStation.cs - Single-player station to gate game start
    // Player enters the trigger and presses E to toggle readiness; first ready starts the game.
    [RequireComponent(typeof(BoxCollider2D))]
    public class ReadyStation : MonoBehaviour
    {
        [Header("Prompt Settings")]
        [SerializeField] private string interactPrompt = "Press E to READY at Station";
        [SerializeField] private string readyPrompt = "Press E to UNREADY";
        [SerializeField] private Color readyColor = new Color(0.3f, 0.9f, 0.3f);
        [SerializeField] private Color idleColor = new Color(0.9f, 0.9f, 0.3f);

        // Global safe-zone flag: active when player is inside the station AND has toggled ready
        public static bool SafeZoneActive { get; private set; }

        public static BoxCollider2D StationCollider { get; private set; }
        Transform player;
        bool _ready;
        bool _playerInside;
        int _playerColliderCount;
        float _debounceUntil;

        SpriteRenderer _sr;

        void Awake()
        {
            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
            StationCollider = col;
            if (GetComponent<SpriteRenderer>() == null)
            {
                _sr = gameObject.AddComponent<SpriteRenderer>();
                _sr.sprite = CreateRectSprite();
            }
            else
            {
                _sr = GetComponent<SpriteRenderer>();
            }
            _sr.color = idleColor;
            gameObject.name = "ReadyStation";
            UpdateSafeZone();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerColliderCount++;
                if (player == null) { player = other.gameObject.transform; }
                _playerInside = true;
                UpdateSafeZone();
                ShowPrompt(true);
                Debug.Log($"ReadyStation: Player collider entered, count={_playerColliderCount}");
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                _playerColliderCount = Mathf.Max(0, _playerColliderCount - 1);
                Debug.Log($"ReadyStation: Player collider exited, count={_playerColliderCount}");
                if (_playerColliderCount == 0)
                {
                    player = null;
                    _playerInside = false;
                    UpdateSafeZone();
                    ShowPrompt(false);
                }
            }
        }

        void Update()
        {
            if (!_playerInside) return;
            if (Time.time < _debounceUntil) return;
            if (Input.GetKeyDown(KeyCode.R))
            {
                _ready = !_ready;
                _debounceUntil = Time.time + 0.5f;
                _sr.color = _ready ? readyColor : idleColor;
                UpdateSafeZone();
                // In single-player, first ready press starts the game if still in Init
                var gm = FindFirstObjectByType<GameManager>();
                if (gm != null && gm.State == GameState.Init && _ready)
                {
                    gm.StartGame();
                    // Hide prompt after start
                    ShowPrompt(false);
                }
            }
        }

        void UpdateSafeZone()
        {
            SafeZoneActive = _playerInside && _ready;
        }

        void ShowPrompt(bool show)
        {
            // Decoupled: station prompts no longer use the shared AdvancePrompt events, to avoid hiding QuestionManager hints
            // You can wire this to a dedicated UI element if desired.
        }

        Sprite CreateRectSprite()
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var c = new Color(1f, 1f, 1f, 0.5f);
            var pixels = new Color[8 * 8];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
        }

        void OnEnable() { BossFight2D.Systems.EventBus.AnswerSubmitted += OnAnswerSubmitted; }
        void OnDisable() { BossFight2D.Systems.EventBus.AnswerSubmitted -= OnAnswerSubmitted; BossFight2D.Systems.EventBus.AnswerModeExited -= OnAnswerModeExited; }
        void Start() { BossFight2D.Systems.EventBus.AnswerModeExited += OnAnswerModeExited; }
        void OnAnswerSubmitted(int _, bool correct)
        {
            Debug.Log($"ReadyStation: OnAnswerSubmitted called - correct: {correct}, playerInside: {_playerInside}, ready: {_ready}");
            if (!correct && _playerInside && _ready)
            {
                Debug.Log("ReadyStation: Ejecting player due to wrong answer");
                EjectAndCancel();
            }
        }

        void EjectAndCancel()
        {
            Debug.Log("ReadyStation: EjectAndCancel called");
            // Cancel ready state
            _ready = false; _sr.color = idleColor;
            // Compute an ejection position just outside the station bounds, away from center
            var stationCol = GetComponent<BoxCollider2D>();
            if (stationCol == null) return;
            var center = stationCol.bounds.center;
            var ext = stationCol.bounds.extents;
            if (player)
            {
                var pos = player.transform.position; Vector2 dir = (Vector2)(pos - center);
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up; dir.Normalize();
                float margin = 5f;
                float pushDist = Mathf.Max(ext.x, ext.y) + margin;
                Vector3 newPos = center + (Vector3)(dir * pushDist);
                Debug.Log($"ReadyStation: Moving player from {pos} to {newPos}");
                player.parent.position = newPos;
            }

            // Update internal flags and safe zone
            _playerInside = false;
            UpdateSafeZone();
            ShowPrompt(false);
            Debug.Log("ReadyStation: Ejection complete");
        }

        // Public method to turn off ready state (used when the question phase is canceled)
        public void TurnOffReady()
        {
            if (_ready)
            {
                _ready = false;
                _sr.color = idleColor;
                UpdateSafeZone();
                Debug.Log("ReadyStation: TurnOffReady invoked; safe zone deactivated");
            }
        }

        void OnAnswerModeExited()
        {
            // Whenever answer mode exits (timeout, submit, or manual cancel), ensure ready is off
            TurnOffReady();
        }

        // Force return player inside station and re-enable ready state (used after Power Play ends)
        public void ForceReturnPlayerAndReady()
        {
            var col = GetComponent<BoxCollider2D>(); if (col == null) return;
            Vector3 center = col.bounds.center;
            if (player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null) player = go.transform;
            }
            if (player != null)
            {
                var root = player.parent != null ? player.parent : player;
                root.position = center;
            }
            _playerInside = true;
            _playerColliderCount = Mathf.Max(_playerColliderCount, 1);
            _ready = true;
            _sr.color = readyColor;
            UpdateSafeZone();
            ShowPrompt(true);
            Debug.Log("ReadyStation: ForceReturnPlayerAndReady executed; player placed inside and ready ON");
        }
    }
}