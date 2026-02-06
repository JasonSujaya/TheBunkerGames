using UnityEngine;
using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Singleton manager holding master game state.
    /// Tracks the 28-day loop and game phases.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static GameManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action OnDayStart;
        public static event Action OnNightStart;
        public static event Action<GameState> OnStateChanged;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Game State")]
        [ReadOnly]
        #endif
        [SerializeField] private GameState currentState = GameState.Morning;

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] private int currentDay = 1;

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] private bool isGameOver = false;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public GameState CurrentState => currentState;
        public int CurrentDay => currentDay;
        public bool IsGameOver => isGameOver;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void SetState(GameState newState)
        {
            if (currentState == newState) return;
            
            currentState = newState;
            Debug.Log($"[GameManager] State changed to: {newState}");
            OnStateChanged?.Invoke(newState);

            if (newState == GameState.Morning)
            {
                OnDayStart?.Invoke();
            }
            else if (newState == GameState.NightProcessing)
            {
                OnNightStart?.Invoke();
            }
        }

        public void AdvanceDay()
        {
            currentDay++;
            Debug.Log($"[GameManager] Day advanced to: {currentDay}");
            
            var config = GameConfig.Instance;
            if (config != null && currentDay > config.TotalDays)
            {
                EndGame(true);
            }
        }

        public void EndGame(bool survived)
        {
            isGameOver = true;
            Debug.Log($"[GameManager] Game Over! Survived: {survived}");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Set Morning", ButtonSizes.Medium)]
        [GUIColor(1f, 0.9f, 0.5f)]
        private void Debug_SetMorning() { if (Application.isPlaying) SetState(GameState.Morning); }

        [Button("Set Scavenge", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.9f, 0.5f)]
        private void Debug_SetScavenge() { if (Application.isPlaying) SetState(GameState.Scavenge); }

        [Button("Set Voting", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.7f, 1f)]
        private void Debug_SetVoting() { if (Application.isPlaying) SetState(GameState.Voting); }

        [Button("Set Night", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.3f, 0.6f)]
        private void Debug_SetNight() { if (Application.isPlaying) SetState(GameState.NightProcessing); }

        [Button("Advance Day", ButtonSizes.Large)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void Debug_AdvanceDay() { if (Application.isPlaying) AdvanceDay(); }
        #endif
    }
}
