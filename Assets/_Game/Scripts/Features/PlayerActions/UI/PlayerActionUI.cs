using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Canvas controller for the Player Action system.
    /// Shows/hides category panels based on active daily actions,
    /// routes results to the correct panels, and manages overall flow.
    /// </summary>
    public class PlayerActionUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static PlayerActionUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Panel References")]
        [InfoBox("Assign one panel per category. Inactive categories will be hidden automatically.")]
        #endif
        [Header("Category Panels")]
        [SerializeField] private PlayerActionCategoryPanel explorationPanel;
        [SerializeField] private PlayerActionCategoryPanel dilemmaPanel;
        [SerializeField] private PlayerActionCategoryPanel familyRequestPanel;

        #if ODIN_INSPECTOR
        [Title("UI Elements")]
        #endif
        [Header("Root & Header")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text dayLabel;

        [Header("Summary / Completion")]
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private TMP_Text completionText;
        [SerializeField] private Button continueButton;

        [Header("Settings")]
        [SerializeField] private bool enableDebugLogs = true;

        // -------------------------------------------------------------------------
        // Runtime State
        // -------------------------------------------------------------------------
        private DailyActionState currentState;
        private int resultsReceived;
        private int totalActive;

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

            if (continueButton != null)
                continueButton.onClick.AddListener(OnContinueClicked);

            HideAll();
        }

        private void OnEnable()
        {
            PlayerActionManager.OnDailyActionsReady += HandleDailyActionsReady;
            PlayerActionManager.OnCategoryResultReceived += HandleCategoryResult;
            PlayerActionManager.OnAllActionsComplete += HandleAllActionsComplete;
        }

        private void OnDisable()
        {
            PlayerActionManager.OnDailyActionsReady -= HandleDailyActionsReady;
            PlayerActionManager.OnCategoryResultReceived -= HandleCategoryResult;
            PlayerActionManager.OnAllActionsComplete -= HandleAllActionsComplete;
        }

        private void OnDestroy()
        {
            if (continueButton != null)
                continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        // -------------------------------------------------------------------------
        // Event Handlers
        // -------------------------------------------------------------------------
        private void HandleDailyActionsReady(DailyActionState state)
        {
            currentState = state;
            resultsReceived = 0;
            totalActive = state.GetActiveCategories().Count;

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionUI] Showing panels for Day {state.Day} ({totalActive} active categories)");

            ShowPanels(state);
        }

        private void HandleCategoryResult(PlayerActionResult result)
        {
            if (result == null) return;

            resultsReceived++;

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionUI] Result received for [{result.Category}] ({resultsReceived}/{totalActive})");

            // Route result to correct panel
            var panel = GetPanel(result.Category);
            if (panel != null)
            {
                panel.ShowResult(result);
            }
        }

        private void HandleAllActionsComplete()
        {
            if (enableDebugLogs)
                Debug.Log("[PlayerActionUI] All actions complete!");

            ShowCompletionSummary();
        }

        // -------------------------------------------------------------------------
        // Panel Management
        // -------------------------------------------------------------------------
        private void ShowPanels(DailyActionState state)
        {
            // Show root
            if (rootPanel != null)
                rootPanel.SetActive(true);

            // Header
            if (headerText != null)
                headerText.text = "Daily Actions";
            if (dayLabel != null)
                dayLabel.text = $"Day {state.Day}";

            // Hide completion
            if (completionPanel != null)
                completionPanel.SetActive(false);

            // Exploration (always active)
            if (explorationPanel != null)
            {
                if (state.ExplorationActive && state.ExplorationChallenge != null)
                    explorationPanel.Setup(state.ExplorationChallenge);
                else
                    explorationPanel.Hide();
            }

            // Dilemma
            if (dilemmaPanel != null)
            {
                if (state.DilemmaActive && state.DilemmaChallenge != null)
                    dilemmaPanel.Setup(state.DilemmaChallenge);
                else
                    dilemmaPanel.Hide();
            }

            // Family Request
            if (familyRequestPanel != null)
            {
                if (state.FamilyRequestActive && state.FamilyRequestChallenge != null)
                    familyRequestPanel.Setup(state.FamilyRequestChallenge, state.FamilyRequestTarget);
                else
                    familyRequestPanel.Hide();
            }
        }

        private void ShowCompletionSummary()
        {
            if (completionPanel != null)
                completionPanel.SetActive(true);

            if (completionText != null && PlayerActionManager.Instance != null)
            {
                var results = PlayerActionManager.Instance.DayResults;
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Day {currentState?.Day ?? 0} Actions Complete!");
                sb.AppendLine();

                foreach (var kvp in results)
                {
                    if (kvp.Value.HasError)
                    {
                        sb.AppendLine($"[{kvp.Key}] Error: {kvp.Value.Error}");
                    }
                    else if (kvp.Value.StoryEvent != null)
                    {
                        sb.AppendLine($"[{kvp.Key}] {kvp.Value.StoryEvent.Title}");
                        sb.AppendLine($"  {kvp.Value.StoryEvent.Description}");
                        sb.AppendLine();
                    }
                }

                completionText.text = sb.ToString();
            }
        }

        /// <summary>
        /// Hide all panels and the root.
        /// </summary>
        public void HideAll()
        {
            if (rootPanel != null)
                rootPanel.SetActive(false);

            if (explorationPanel != null)
                explorationPanel.Hide();
            if (dilemmaPanel != null)
                dilemmaPanel.Hide();
            if (familyRequestPanel != null)
                familyRequestPanel.Hide();

            if (completionPanel != null)
                completionPanel.SetActive(false);
        }

        /// <summary>
        /// Show the UI and prepare for a new day.
        /// Typically called by PlayerActionManager via event, but can be called manually.
        /// </summary>
        public void Show()
        {
            if (rootPanel != null)
                rootPanel.SetActive(true);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------
        private PlayerActionCategoryPanel GetPanel(PlayerActionCategory category)
        {
            switch (category)
            {
                case PlayerActionCategory.Exploration: return explorationPanel;
                case PlayerActionCategory.Dilemma: return dilemmaPanel;
                case PlayerActionCategory.FamilyRequest: return familyRequestPanel;
                default: return null;
            }
        }

        private void OnContinueClicked()
        {
            if (enableDebugLogs)
                Debug.Log("[PlayerActionUI] Continue clicked. Hiding UI.");

            HideAll();

            // Reset panels for next day
            if (explorationPanel != null) explorationPanel.ResetPanel();
            if (dilemmaPanel != null) dilemmaPanel.ResetPanel();
            if (familyRequestPanel != null) familyRequestPanel.ResetPanel();
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug")]
        [Button("Show All Panels (Test)", ButtonSizes.Medium)]
        private void Debug_ShowAllPanels()
        {
            if (rootPanel != null)
                rootPanel.SetActive(true);
            if (explorationPanel != null)
                explorationPanel.gameObject.SetActive(true);
            if (dilemmaPanel != null)
                dilemmaPanel.gameObject.SetActive(true);
            if (familyRequestPanel != null)
                familyRequestPanel.gameObject.SetActive(true);
        }

        [Button("Hide All", ButtonSizes.Medium)]
        private void Debug_HideAll()
        {
            HideAll();
        }
        #endif
    }
}
