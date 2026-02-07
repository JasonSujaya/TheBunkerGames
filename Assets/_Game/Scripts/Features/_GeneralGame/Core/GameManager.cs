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
        [Title("Game State")]
        [ReadOnly]
        public GameState CurrentState = GameState.StatusReview;
        
        [ReadOnly]
        public int CurrentDay = 1;
        
        [ReadOnly]
        public bool IsGameOver = false;

        // -------------------------------------------------------------------------
        // References (Auto-Wired)
        // -------------------------------------------------------------------------
        [Title("Systems")]
        [Required] public GameFlowController FlowController;
        [Required] public SurvivalManager Survival;
        [Required] public StatusReviewManager StatusReview;
        [Required] public AngelInteractionManager AngelInteraction;
        [Required] public CityExplorationManager CityExploration;
        [Required] public DailyChoiceManager DailyChoice;
        [Required] public NightCycleManager NightCycle;

        // -------------------------------------------------------------------------
        // Static Event Triggers (Called by GameFlowController)
        // -------------------------------------------------------------------------
        public static void FireDayStart() => OnDayStart?.Invoke();
        public static void FireNightStart() => OnNightStart?.Invoke();
        public static void FireStateChanged(GameState state) => OnStateChanged?.Invoke(state);
        public static void FireGameOver(bool survived) => OnGameOver?.Invoke(survived);

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
            
            // Ensure controller has reference
            if (FlowController == null) FlowController = GetComponent<GameFlowController>();
            if (FlowController != null) FlowController.Initialize(this);
        }

        private void Reset()
        {
            // Auto Setup Logic
            FlowController = GetOrAdd<GameFlowController>();
            Survival = GetOrAdd<SurvivalManager>();
            StatusReview = GetOrAdd<StatusReviewManager>();
            AngelInteraction = GetOrAdd<AngelInteractionManager>();
            CityExploration = GetOrAdd<CityExplorationManager>();
            DailyChoice = GetOrAdd<DailyChoiceManager>();
            NightCycle = GetOrAdd<NightCycleManager>();
        }

        private T GetOrAdd<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null) component = gameObject.AddComponent<T>();
            return component;
        }

        // -------------------------------------------------------------------------
        // Public Methods (Redirects to Controller for easier access if needed)
        // -------------------------------------------------------------------------
        public void SetState(GameState newState) => FlowController?.SetState(newState);
        public void StartNewGame() => FlowController?.StartNewGame();
        public void AdvanceDay() => FlowController?.AdvanceDay();
        public void EndGame(bool survived) => FlowController?.EndGame(survived);
        public void GoToPreviousDay() => FlowController?.PreviousDay();

    }
}
