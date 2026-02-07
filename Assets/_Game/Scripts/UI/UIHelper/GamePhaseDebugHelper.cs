using UnityEngine;

namespace TheBunkerGames
{
    /// <summary>
    /// Drives the game loop through the 5 phases.
    /// Provides manual AdvancePhase() for button clicks and optional auto-advance.
    /// </summary>
    public class GamePhaseDebugHelper : MonoBehaviour
    {
        [Header("Flow Settings")]
        [SerializeField] private bool autoAdvance = false;
        [SerializeField] private float phaseInterval = 5f;

        private float phaseTimer;

        private void Start()
        {
            phaseTimer = phaseInterval;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

            if (autoAdvance)
            {
                phaseTimer -= Time.deltaTime;
                if (phaseTimer <= 0f)
                {
                    AdvancePhase();
                    phaseTimer = phaseInterval;
                }
            }
        }

        public void AdvancePhase()
        {
            if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return;

            GameState current = GameManager.Instance.CurrentState;
            switch (current)
            {
                case GameState.StatusReview:
                    StatusReviewManager.Instance?.CompleteStatusReview();
                    break;
                case GameState.AngelInteraction:
                    AngelInteractionManager.Instance?.CompleteInteraction();
                    break;
                case GameState.CityExploration:
                    CityExplorationManager.Instance?.CompleteExplorationPhase();
                    break;
                case GameState.DailyChoice:
                    DailyChoiceManager.Instance?.CompleteChoicePhase();
                    break;
                case GameState.NightCycle:
                    NightCycleManager.Instance?.CompleteNightCycle();
                    break;
            }
        }
    }
}
