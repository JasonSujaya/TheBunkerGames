using UnityEngine;

namespace TheBunkerGames
{
    /// <summary>
    /// Drives the game loop through the 5 phases.
    /// Provides manual AdvancePhase() for button clicks and optional auto-advance.
    /// </summary>
    public class GameFlowController : MonoBehaviour
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
                    StatusReviewController.Instance?.CompleteStatusReview();
                    break;
                case GameState.AngelInteraction:
                    AngelInteractionController.Instance?.CompleteInteraction();
                    break;
                case GameState.CityExploration:
                    CityExplorationController.Instance?.CompleteExplorationPhase();
                    break;
                case GameState.DailyChoice:
                    DailyChoiceController.Instance?.CompleteChoicePhase();
                    break;
                case GameState.NightCycle:
                    NightCycleController.Instance?.CompleteNightCycle();
                    break;
            }
        }
    }
}
