using UnityEngine;
using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Singleton manager holding master game state.
    /// Orchestrates the 5-phase Core Loop:
    /// StatusReview -> AngelInteraction -> CityExploration -> DailyChoice -> NightCycle
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
        public static event Action<bool> OnGameOver;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Game State")]
        [ReadOnly]
        #endif
        [SerializeField] private GameState currentState = GameState.StatusReview;

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

        private void OnEnable()
        {
            // Wire up phase completion events to auto-advance
            StatusReviewManager.OnStatusReviewComplete += AdvanceToAngelInteraction;
            AngelInteractionManager.OnInteractionComplete += AdvanceToCityExploration;
            CityExplorationManager.OnExplorationPhaseComplete += AdvanceToDailyChoice;
            DailyChoiceManager.OnChoicePhaseComplete += AdvanceToNightCycle;
            NightCycleManager.OnNightCycleComplete += AdvanceToNextDay;
        }

        private void OnDisable()
        {
            StatusReviewManager.OnStatusReviewComplete -= AdvanceToAngelInteraction;
            AngelInteractionManager.OnInteractionComplete -= AdvanceToCityExploration;
            CityExplorationManager.OnExplorationPhaseComplete -= AdvanceToDailyChoice;
            DailyChoiceManager.OnChoicePhaseComplete -= AdvanceToNightCycle;
            NightCycleManager.OnNightCycleComplete -= AdvanceToNextDay;
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void StartNewGame()
        {
            currentDay = 1;
            isGameOver = false;
            Debug.Log("[GameManager] New game started.");
            SetState(GameState.StatusReview);
            OnDayStart?.Invoke();
        }

        public void SetState(GameState newState)
        {
            if (isGameOver) return;
            if (currentState == newState) return;

            currentState = newState;
            Debug.Log($"[GameManager] State changed to: {newState}");
            OnStateChanged?.Invoke(newState);

            if (newState == GameState.StatusReview)
            {
                OnDayStart?.Invoke();
            }
            else if (newState == GameState.NightCycle)
            {
                OnNightStart?.Invoke();
            }
        }

        public void AdvanceDay()
        {
            currentDay++;
            Debug.Log($"[GameManager] Day advanced to: {currentDay}");

            var config = GameConfigDataSO.Instance;
            if (config != null && currentDay > config.TotalDays)
            {
                EndGame(true);
            }
        }

        public void GoToPreviousDay()
        {
            if (currentDay > 1)
            {
                currentDay--;
                Debug.Log($"[GameManager] Day reversed to: {currentDay}");
                // Re-trigger day start for the previous day
                OnDayStart?.Invoke();
            }
            else
            {
                Debug.LogWarning("[GameManager] Already at Day 1.");
            }
        }

        public void EndGame(bool survived)
        {
            isGameOver = true;
            Debug.Log($"[GameManager] Game Over! Survived: {survived}");
            OnGameOver?.Invoke(survived);
        }

        // -------------------------------------------------------------------------
        // Phase Advancement (wired to phase completion events)
        // -------------------------------------------------------------------------
        private void AdvanceToAngelInteraction()
        {
            SetState(GameState.AngelInteraction);
        }

        private void AdvanceToCityExploration()
        {
            SetState(GameState.CityExploration);
        }

        private void AdvanceToDailyChoice()
        {
            SetState(GameState.DailyChoice);
        }

        private void AdvanceToNightCycle()
        {
            SetState(GameState.NightCycle);
        }

        private void AdvanceToNextDay()
        {
            if (!isGameOver)
            {
                SetState(GameState.StatusReview);
            }
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Start New Game", ButtonSizes.Large)]
        [GUIColor(0f, 1f, 0f)]
        private void Debug_StartNewGame() { if (Application.isPlaying) StartNewGame(); }

        [Button("Set StatusReview", ButtonSizes.Medium)]
        [GUIColor(1f, 0.9f, 0.5f)]
        private void Debug_SetStatusReview() { if (Application.isPlaying) SetState(GameState.StatusReview); }

        [Button("Set AngelInteraction", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.5f, 1f)]
        private void Debug_SetAngel() { if (Application.isPlaying) SetState(GameState.AngelInteraction); }

        [Button("Set CityExploration", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.9f, 0.5f)]
        private void Debug_SetExploration() { if (Application.isPlaying) SetState(GameState.CityExploration); }

        [Button("Set DailyChoice", ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.5f)]
        private void Debug_SetChoice() { if (Application.isPlaying) SetState(GameState.DailyChoice); }

        [Button("Set NightCycle", ButtonSizes.Medium)]
        [GUIColor(0.4f, 0.3f, 0.6f)]
        private void Debug_SetNight() { if (Application.isPlaying) SetState(GameState.NightCycle); }

        [Button("Advance Day", ButtonSizes.Large)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void Debug_AdvanceDay() { if (Application.isPlaying) AdvanceDay(); }

        [Button("Previous Day", ButtonSizes.Large)]
        [GUIColor(0.5f, 0.5f, 1f)]
        private void Debug_PrevDay() { if (Application.isPlaying) GoToPreviousDay(); }
        #endif
    }
}
