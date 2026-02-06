using UnityEngine;
using UnityEngine.UI;

namespace TheBunkerGames
{
    /// <summary>
    /// Master UI coordinator. Updates background, day counter, state label, and button text
    /// based on game state changes. Wires button click in code.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Background")]
        [SerializeField] private Image backgroundPanel;

        [Header("Top Bar")]
        [SerializeField] private Text dayText;
        [SerializeField] private Text stateText;

        [Header("Bottom Bar")]
        [SerializeField] private Button nextPhaseButton;
        [SerializeField] private Text nextPhaseButtonText;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverText;

        [Header("State Colors")]
        [SerializeField] private Color statusReviewColor = new Color(1f, 0.85f, 0.4f, 1f);
        [SerializeField] private Color angelColor = new Color(0.6f, 0.35f, 0.85f, 1f);
        [SerializeField] private Color explorationColor = new Color(0.4f, 0.7f, 0.35f, 1f);
        [SerializeField] private Color choiceColor = new Color(0.85f, 0.55f, 0.35f, 1f);
        [SerializeField] private Color nightColor = new Color(0.25f, 0.15f, 0.35f, 1f);

        private void Start()
        {
            if (nextPhaseButton != null)
            {
                var flowController = FindObjectOfType<GameFlowController>();
                if (flowController != null)
                {
                    nextPhaseButton.onClick.AddListener(flowController.AdvancePhase);
                }
            }

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (GameManager.Instance != null)
                UpdateUI(GameManager.Instance.CurrentState);
        }

        private void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameState newState)
        {
            UpdateUI(newState);
        }

        private void UpdateUI(GameState state)
        {
            if (backgroundPanel != null)
                backgroundPanel.color = GetColorForState(state);

            if (dayText != null && GameManager.Instance != null)
            {
                int totalDays = GameConfigSO.Instance != null ? GameConfigSO.Instance.TotalDays : 28;
                dayText.text = $"Day {GameManager.Instance.CurrentDay} / {totalDays}";
            }

            if (stateText != null)
                stateText.text = GetStateDisplayName(state);

            if (nextPhaseButtonText != null)
            {
                switch (state)
                {
                    case GameState.StatusReview:
                        nextPhaseButtonText.text = "Talk to A.N.G.E.L.";
                        break;
                    case GameState.AngelInteraction:
                        nextPhaseButtonText.text = "Send Explorers";
                        break;
                    case GameState.CityExploration:
                        nextPhaseButtonText.text = "Face the Choice";
                        break;
                    case GameState.DailyChoice:
                        nextPhaseButtonText.text = "End Day";
                        break;
                    case GameState.NightCycle:
                        nextPhaseButtonText.text = "Next Morning";
                        break;
                }
            }

            if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            {
                if (gameOverPanel != null) gameOverPanel.SetActive(true);
                if (gameOverText != null)
                {
                    int totalDays = GameConfigSO.Instance != null ? GameConfigSO.Instance.TotalDays : 28;
                    gameOverText.text = GameManager.Instance.CurrentDay > totalDays
                        ? "You Survived!" : "Game Over";
                }
                if (nextPhaseButton != null) nextPhaseButton.interactable = false;
            }
        }

        private Color GetColorForState(GameState state)
        {
            switch (state)
            {
                case GameState.StatusReview: return statusReviewColor;
                case GameState.AngelInteraction: return angelColor;
                case GameState.CityExploration: return explorationColor;
                case GameState.DailyChoice: return choiceColor;
                case GameState.NightCycle: return nightColor;
                default: return Color.gray;
            }
        }

        private string GetStateDisplayName(GameState state)
        {
            switch (state)
            {
                case GameState.StatusReview: return "STATUS REVIEW";
                case GameState.AngelInteraction: return "A.N.G.E.L.";
                case GameState.CityExploration: return "EXPLORATION";
                case GameState.DailyChoice: return "THE CHOICE";
                case GameState.NightCycle: return "NIGHT";
                default: return "UNKNOWN";
            }
        }
    }
}
