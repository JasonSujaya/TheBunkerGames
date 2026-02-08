using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Inbox-style canvas controller for the Player Action system.
    /// Shows a task list of active categories. Click a row to open the detail view,
    /// type a response, and save it. Once all are saved, submit all to the LLM.
    /// </summary>
    public class PlayerActionUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static PlayerActionUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Visual Assets (drag & drop in Inspector)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Visual Assets – Fonts")]
        #endif
        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset bodyFont;

        #if ODIN_INSPECTOR
        [Title("Visual Assets – Sprites")]
        #endif
        [Header("Sprites – Backgrounds & Frames")]
        [SerializeField] private Sprite panelBackgroundSprite;    // main panel bg
        [SerializeField] private Sprite headerBackgroundSprite;   // header strip bg
        [SerializeField] private Sprite listItemSprite;           // inbox row bg
        [SerializeField] private Sprite detailPanelSprite;        // detail view bg
        [SerializeField] private Sprite inputFieldSprite;         // text input bg
        [SerializeField] private Sprite separatorSprite;          // horizontal divider

        [Header("Sprites – Buttons")]
        [SerializeField] private Sprite buttonSprite;             // general button bg
        [SerializeField] private Sprite saveButtonSprite;         // save/confirm button bg
        [SerializeField] private Sprite submitButtonSprite;       // submit all button bg
        [SerializeField] private Sprite backButtonSprite;         // back / navigation button bg
        [SerializeField] private Sprite closeButtonSprite;        // close (X) button bg

        [Header("Sprites – Icons")]
        [SerializeField] private Sprite iconClose;                // X icon
        [SerializeField] private Sprite iconBack;                 // back arrow icon
        [SerializeField] private Sprite iconSave;                 // save/checkmark icon
        [SerializeField] private Sprite iconSubmit;               // submit icon
        [SerializeField] private Sprite iconExploration;          // exploration category icon
        [SerializeField] private Sprite iconDilemma;              // dilemma category icon
        [SerializeField] private Sprite iconFamilyRequest;        // family request category icon
        [SerializeField] private Sprite iconLock;                 // locked/saved indicator

        #if ODIN_INSPECTOR
        [Title("Visual Assets – Colors")]
        #endif
        [Header("Colors")]
        [SerializeField] private Color panelBgColor          = new Color(0.05f, 0.05f, 0.1f, 0.92f);
        [SerializeField] private Color headerBgColor         = new Color(0.08f, 0.08f, 0.14f, 1f);
        [SerializeField] private Color listItemBgColor       = new Color(0.12f, 0.12f, 0.18f, 1f);
        [SerializeField] private Color listItemHoverColor    = new Color(0.18f, 0.18f, 0.28f, 1f);
        [SerializeField] private Color detailBgColor         = new Color(0.08f, 0.08f, 0.14f, 1f);
        [SerializeField] private Color inputBgColor          = new Color(0.15f, 0.15f, 0.2f, 1f);
        [SerializeField] private Color inputLockedBgColor    = new Color(0.08f, 0.08f, 0.1f, 1f);
        [SerializeField] private Color textColor             = Color.white;
        [SerializeField] private Color textMutedColor        = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color textLockedColor       = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color categoryLabelColor    = new Color(1f, 0.85f, 0.3f, 1f);
        [SerializeField] private Color saveButtonColor       = new Color(0.15f, 0.6f, 0.2f, 1f);
        [SerializeField] private Color submitButtonColor     = new Color(0.15f, 0.4f, 0.7f, 1f);
        [SerializeField] private Color closeButtonColor      = new Color(0.4f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color completionBgColor     = new Color(0.1f, 0.15f, 0.1f, 0.95f);

        // -------------------------------------------------------------------------
        // Category Data Panels (hidden, hold per-category state)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Category Data Panels")]
        [InfoBox("These hold per-category state. They are NOT visible in-game.")]
        #endif
        [Header("Category Panels (Data Holders)")]
        [SerializeField] private PlayerActionCategoryPanel explorationPanel;
        [SerializeField] private PlayerActionCategoryPanel dilemmaPanel;
        [SerializeField] private PlayerActionCategoryPanel familyRequestPanel;

        // -------------------------------------------------------------------------
        // Root & Header
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Root & Header")]
        #endif
        [Header("Root & Header")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text dayLabel;
        [SerializeField] private Button closeButton;

        // -------------------------------------------------------------------------
        // Inbox List
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Inbox List")]
        #endif
        [Header("Inbox List")]
        [SerializeField] private GameObject inboxContainer;
        [SerializeField] private PlayerActionListItem explorationListItem;
        [SerializeField] private PlayerActionListItem dilemmaListItem;
        [SerializeField] private PlayerActionListItem familyRequestListItem;
        [SerializeField] private Button submitAllButton;
        [SerializeField] private TMP_Text submitAllButtonText;

        // -------------------------------------------------------------------------
        // Detail Panel
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Detail Panel")]
        #endif
        [Header("Detail Panel")]
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text detailCategoryLabel;
        [SerializeField] private TMP_Text detailChallengeTitle;
        [SerializeField] private TMP_Text detailChallengeDescription;
        [SerializeField] private TMP_InputField detailInputField;
        [SerializeField] private Transform detailItemToggleContainer;
        [SerializeField] private Button saveButton;
        [SerializeField] private TMP_Text saveButtonText;
        [SerializeField] private TMP_Text detailStatusText;

        // -------------------------------------------------------------------------
        // Confirmation Popup
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Confirmation Popup")]
        #endif
        [Header("Confirmation Popup")]
        [SerializeField] private ConfirmationPopup confirmationPopup;

        // -------------------------------------------------------------------------
        // Completion Panel
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Completion")]
        #endif
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
        private PlayerActionCategory? currentlyViewingCategory;
        private Dictionary<PlayerActionCategory, bool> savedCategories = new Dictionary<PlayerActionCategory, bool>();
        private int resultsReceived;
        private int totalActive;
        private bool isInputListenerActive;

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
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSaveClicked);
            if (submitAllButton != null)
                submitAllButton.onClick.AddListener(OnSubmitAllClicked);

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
            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);
            if (saveButton != null)
                saveButton.onClick.RemoveListener(OnSaveClicked);
            if (submitAllButton != null)
                submitAllButton.onClick.RemoveListener(OnSubmitAllClicked);

            DetachInputListener();
        }

        // -------------------------------------------------------------------------
        // Event Handlers
        // -------------------------------------------------------------------------
        private void HandleDailyActionsReady(DailyActionState state)
        {
            currentState = state;
            resultsReceived = 0;
            savedCategories.Clear();
            currentlyViewingCategory = null;

            var active = state.GetActiveCategories();
            totalActive = active.Count;

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionUI] Showing inbox for Day {state.Day} ({totalActive} active categories)");

            // Setup category data panels
            SetupCategoryPanels(state);

            // Setup list items
            SetupListItems(state);

            // Show inbox view
            ShowInbox();

            // Show root
            if (rootPanel != null)
                rootPanel.SetActive(true);

            // Header
            if (headerText != null)
                headerText.text = "DAILY ACTIONS";
            if (dayLabel != null)
                dayLabel.text = $"Day {state.Day}";

            // Hide completion
            if (completionPanel != null)
                completionPanel.SetActive(false);
        }

        private void HandleCategoryResult(PlayerActionResult result)
        {
            if (result == null) return;

            resultsReceived++;

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionUI] Result received for [{result.Category}] ({resultsReceived}/{totalActive})");

            // Update list item
            var listItem = GetListItem(result.Category);
            if (listItem != null)
                listItem.SetComplete();

            // Route result to category panel for storage
            var panel = GetPanel(result.Category);
            if (panel != null)
                panel.ShowResult(result);
        }

        private void HandleAllActionsComplete()
        {
            if (enableDebugLogs)
                Debug.Log("[PlayerActionUI] All actions complete!");

            ShowCompletionSummary();
        }

        // -------------------------------------------------------------------------
        // Setup
        // -------------------------------------------------------------------------
        private void SetupCategoryPanels(DailyActionState state)
        {
            if (explorationPanel != null)
            {
                if (state.ExplorationActive && state.ExplorationChallenge != null)
                    explorationPanel.Setup(state.ExplorationChallenge);
                else
                    explorationPanel.Hide();
            }

            if (dilemmaPanel != null)
            {
                if (state.DilemmaActive && state.DilemmaChallenge != null)
                    dilemmaPanel.Setup(state.DilemmaChallenge);
                else
                    dilemmaPanel.Hide();
            }

            if (familyRequestPanel != null)
            {
                if (state.FamilyRequestActive && state.FamilyRequestChallenge != null)
                    familyRequestPanel.Setup(state.FamilyRequestChallenge, state.FamilyRequestTarget);
                else
                    familyRequestPanel.Hide();
            }
        }

        private void SetupListItems(DailyActionState state)
        {
            // Exploration (always active)
            if (explorationListItem != null)
            {
                if (state.ExplorationActive && state.ExplorationChallenge != null)
                {
                    explorationListItem.Initialize(
                        PlayerActionCategory.Exploration,
                        "EXPLORATION",
                        state.ExplorationChallenge.Title,
                        OnListItemClicked);
                }
                else
                {
                    explorationListItem.Hide();
                }
            }

            // Dilemma
            if (dilemmaListItem != null)
            {
                if (state.DilemmaActive && state.DilemmaChallenge != null)
                {
                    dilemmaListItem.Initialize(
                        PlayerActionCategory.Dilemma,
                        "DILEMMA",
                        state.DilemmaChallenge.Title,
                        OnListItemClicked);
                }
                else
                {
                    dilemmaListItem.Hide();
                }
            }

            // Family Request
            if (familyRequestListItem != null)
            {
                if (state.FamilyRequestActive && state.FamilyRequestChallenge != null)
                {
                    string displayName = string.IsNullOrEmpty(state.FamilyRequestTarget)
                        ? "FAMILY REQUEST"
                        : $"FAMILY ({state.FamilyRequestTarget})";
                    familyRequestListItem.Initialize(
                        PlayerActionCategory.FamilyRequest,
                        displayName,
                        state.FamilyRequestChallenge.Title,
                        OnListItemClicked);
                }
                else
                {
                    familyRequestListItem.Hide();
                }
            }
        }

        // -------------------------------------------------------------------------
        // View Switching
        // -------------------------------------------------------------------------

        /// <summary>
        /// Show the inbox task list view.
        /// </summary>
        private void ShowInbox()
        {
            if (inboxContainer != null)
                inboxContainer.SetActive(true);
            if (detailPanel != null)
                detailPanel.SetActive(false);

            DetachInputListener();
            currentlyViewingCategory = null;

            // Deselect all list items
            if (explorationListItem != null) explorationListItem.SetSelected(false);
            if (dilemmaListItem != null) dilemmaListItem.SetSelected(false);
            if (familyRequestListItem != null) familyRequestListItem.SetSelected(false);

            // Update submit all button visibility
            CheckAllSaved();
        }

        /// <summary>
        /// Show the detail view for a specific category.
        /// </summary>
        private void ShowDetail(PlayerActionCategory category)
        {
            var panel = GetPanel(category);
            if (panel == null) return;

            currentlyViewingCategory = category;

            // Switch views
            if (inboxContainer != null)
                inboxContainer.SetActive(false);
            if (detailPanel != null)
                detailPanel.SetActive(true);

            // Highlight list item
            var listItem = GetListItem(category);
            if (explorationListItem != null) explorationListItem.SetSelected(false);
            if (dilemmaListItem != null) dilemmaListItem.SetSelected(false);
            if (familyRequestListItem != null) familyRequestListItem.SetSelected(false);
            if (listItem != null) listItem.SetSelected(true);

            // Populate detail panel from category data
            if (detailCategoryLabel != null)
                detailCategoryLabel.text = panel.GetCategoryDisplayName();

            if (detailChallengeTitle != null)
                detailChallengeTitle.text = panel.CurrentChallenge != null ? panel.CurrentChallenge.Title : "No Challenge";

            if (detailChallengeDescription != null)
            {
                string desc = panel.CurrentChallenge != null
                    ? panel.CurrentChallenge.GetDescription(panel.FamilyTarget)
                    : "";
                detailChallengeDescription.text = desc;
            }

            // Load input text (detach listener first to avoid feedback loop)
            DetachInputListener();
            if (detailInputField != null)
            {
                detailInputField.text = panel.PlayerInput;
                detailInputField.interactable = !panel.IsSaved;
                // Visual feedback: darken input bg when locked
                var inputBgImg = detailInputField.GetComponent<Image>();
                if (inputBgImg != null)
                    inputBgImg.color = panel.IsSaved ? inputLockedBgColor : inputBgColor;
                // Dim the text when saved
                if (detailInputField.textComponent != null)
                    detailInputField.textComponent.color = panel.IsSaved ? textLockedColor : textColor;
            }
            AttachInputListener();

            // Save button state
            if (saveButton != null)
                saveButton.interactable = !panel.IsSaved;
            if (saveButtonText != null)
                saveButtonText.text = panel.IsSaved ? "SAVED \u2713" : "SAVE RESPONSE";

            // Status
            if (detailStatusText != null)
            {
                if (panel.IsSaved)
                    detailStatusText.text = "\ud83d\udd12 Response saved and locked. Cannot be edited.";
                else
                    detailStatusText.text = "Type your response and save when ready.";
            }

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionUI] Showing detail for [{category}]");
        }

        // -------------------------------------------------------------------------
        // Input Field Listener Management
        // -------------------------------------------------------------------------
        private void AttachInputListener()
        {
            if (detailInputField != null && !isInputListenerActive)
            {
                detailInputField.onValueChanged.AddListener(OnDetailInputChanged);
                isInputListenerActive = true;
            }
        }

        private void DetachInputListener()
        {
            if (detailInputField != null && isInputListenerActive)
            {
                detailInputField.onValueChanged.RemoveListener(OnDetailInputChanged);
                isInputListenerActive = false;
            }
        }

        private void OnDetailInputChanged(string newValue)
        {
            if (!currentlyViewingCategory.HasValue) return;

            var panel = GetPanel(currentlyViewingCategory.Value);
            if (panel != null)
                panel.SetInputText(newValue);
        }

        // -------------------------------------------------------------------------
        // User Actions
        // -------------------------------------------------------------------------
        private void OnListItemClicked(PlayerActionCategory category)
        {
            ShowDetail(category);
        }

        private void OnCloseClicked()
        {
            HideAll();
        }

        private void OnBackClicked()
        {
            // Sync current input back to panel before leaving
            if (currentlyViewingCategory.HasValue && detailInputField != null)
            {
                var panel = GetPanel(currentlyViewingCategory.Value);
                if (panel != null && !panel.IsSaved)
                    panel.SetInputText(detailInputField.text);
            }

            ShowInbox();
        }

        private void OnSaveClicked()
        {
            if (!currentlyViewingCategory.HasValue) return;

            var panel = GetPanel(currentlyViewingCategory.Value);
            if (panel == null || panel.IsSaved) return;

            // Sync latest input
            if (detailInputField != null)
                panel.SetInputText(detailInputField.text);

            // Validate
            if (!panel.ValidateInput())
            {
                if (detailStatusText != null)
                    detailStatusText.text = panel.Category == PlayerActionCategory.Dilemma
                        ? "You must respond to the dilemma!"
                        : "Describe your approach!";
                return;
            }

            // Show confirmation popup
            if (confirmationPopup != null)
            {
                confirmationPopup.Show(
                    "Are you sure you want to save your response?\nOnce saved, it cannot be edited until the next day.",
                    OnSaveConfirmed,
                    null,
                    "Confirm Save",
                    "Cancel");
            }
            else
            {
                // No popup — just save directly
                OnSaveConfirmed();
            }
        }

        private void OnSaveConfirmed()
        {
            if (!currentlyViewingCategory.HasValue) return;

            var category = currentlyViewingCategory.Value;
            var panel = GetPanel(category);
            if (panel == null) return;

            // Save and lock
            panel.SaveInput();
            savedCategories[category] = true;

            // Update detail panel visuals — fully lock input
            if (detailInputField != null)
            {
                detailInputField.interactable = false;
                var inputBgImg = detailInputField.GetComponent<Image>();
                if (inputBgImg != null)
                    inputBgImg.color = inputLockedBgColor;
                if (detailInputField.textComponent != null)
                    detailInputField.textComponent.color = textLockedColor;
            }
            if (saveButton != null)
                saveButton.interactable = false;
            if (saveButtonText != null)
                saveButtonText.text = "SAVED \u2713";
            if (detailStatusText != null)
                detailStatusText.text = "\ud83d\udd12 Response saved and locked. Cannot be edited.";

            // Update list item
            var listItem = GetListItem(category);
            if (listItem != null)
                listItem.SetSaved(true);

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionUI] [{category}] saved.");

            // Return to inbox
            ShowInbox();
        }

        private void CheckAllSaved()
        {
            if (currentState == null)
            {
                if (submitAllButton != null)
                    submitAllButton.gameObject.SetActive(false);
                return;
            }

            var active = currentState.GetActiveCategories();
            bool allSaved = true;
            foreach (var cat in active)
            {
                if (!savedCategories.ContainsKey(cat) || !savedCategories[cat])
                {
                    allSaved = false;
                    break;
                }
            }

            if (submitAllButton != null)
                submitAllButton.gameObject.SetActive(allSaved && active.Count > 0);

            if (allSaved && active.Count > 0 && enableDebugLogs)
                Debug.Log("[PlayerActionUI] All categories saved! Submit All button shown.");
        }

        private void OnSubmitAllClicked()
        {
            if (PlayerActionManager.Instance == null) return;

            if (enableDebugLogs)
                Debug.Log("[PlayerActionUI] Submitting all saved actions to LLM...");

            // Update list items to show processing
            if (currentState != null)
            {
                foreach (var cat in currentState.GetActiveCategories())
                {
                    var listItem = GetListItem(cat);
                    if (listItem != null)
                        listItem.SetProcessing();
                }
            }

            // Hide submit button
            if (submitAllButton != null)
                submitAllButton.gameObject.SetActive(false);

            // Submit all via manager
            PlayerActionManager.Instance.SubmitAllActions();
        }

        // -------------------------------------------------------------------------
        // Completion
        // -------------------------------------------------------------------------
        private void ShowCompletionSummary()
        {
            // Hide inbox and detail
            if (inboxContainer != null)
                inboxContainer.SetActive(false);
            if (detailPanel != null)
                detailPanel.SetActive(false);

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

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Hide all panels and the root.
        /// </summary>
        public void HideAll()
        {
            if (rootPanel != null)
                rootPanel.SetActive(false);
            if (inboxContainer != null)
                inboxContainer.SetActive(false);
            if (detailPanel != null)
                detailPanel.SetActive(false);
            if (completionPanel != null)
                completionPanel.SetActive(false);

            DetachInputListener();
            currentlyViewingCategory = null;
        }

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

        private PlayerActionListItem GetListItem(PlayerActionCategory category)
        {
            switch (category)
            {
                case PlayerActionCategory.Exploration: return explorationListItem;
                case PlayerActionCategory.Dilemma: return dilemmaListItem;
                case PlayerActionCategory.FamilyRequest: return familyRequestListItem;
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

            savedCategories.Clear();
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug")]
        [Button("Show Inbox (Test)", ButtonSizes.Medium)]
        private void Debug_ShowInbox()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            ShowInbox();
        }

        [Button("Hide All", ButtonSizes.Medium)]
        private void Debug_HideAll()
        {
            HideAll();
        }
        #endif

        // -------------------------------------------------------------------------
        // Auto Setup — builds all child UI and wires references
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Auto Setup")]
        [Button("Build All UI Children + Wire References", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        #endif
        public void AutoSetup()
        {
            #if UNITY_EDITOR
            // Ensure this object has Canvas
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
            }
            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Destroy old children
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            // ----- Root Panel -----
            var rp = MakeUI("RootPanel", transform);
            Stretch(rp);
            var rpBg = rp.gameObject.AddComponent<Image>();
            ApplySprite(rpBg, panelBackgroundSprite, panelBgColor);

            // ----- Header -----
            var header = MakeUI("Header", rp);
            header.anchorMin = new Vector2(0, 0.92f);
            header.anchorMax = new Vector2(1, 1);
            header.offsetMin = header.offsetMax = Vector2.zero;

            var hdrHL = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            hdrHL.padding = new RectOffset(15, 15, 8, 8);
            hdrHL.spacing = 10;
            hdrHL.childForceExpandWidth = false;
            hdrHL.childForceExpandHeight = true;
            hdrHL.childControlWidth = false;
            hdrHL.childControlHeight = true;
            hdrHL.childAlignment = TextAnchor.MiddleCenter;

            // Close button (X) — left side
            var closeBtnRT = MakeButton("CloseButton", header, "X");
            var closeBtnLE = closeBtnRT.gameObject.AddComponent<LayoutElement>();
            closeBtnLE.preferredWidth = 40;
            closeBtnLE.preferredHeight = 40;
            var closeBtnBg = closeBtnRT.GetComponent<Image>();
            if (closeBtnBg != null) ApplySprite(closeBtnBg, closeButtonSprite, closeButtonColor);
            var closeBtnColors = closeBtnRT.GetComponent<Button>().colors;
            closeBtnColors.highlightedColor = new Color(0.6f, 0.2f, 0.2f, 1f);
            closeBtnColors.pressedColor = new Color(0.3f, 0.1f, 0.1f, 1f);
            closeBtnRT.GetComponent<Button>().colors = closeBtnColors;

            // Header title — takes remaining space
            var hText = MakeTMP("HeaderText", header, "DAILY ACTIONS", 28, TextAlignmentOptions.Center);
            if (titleFont != null) hText.font = titleFont;
            hText.fontStyle = FontStyles.Bold;
            var hTextLE = hText.gameObject.AddComponent<LayoutElement>();
            hTextLE.flexibleWidth = 1;

            // Day label — right side
            var dLabel = MakeTMP("DayLabel", header, "Day 1", 20, TextAlignmentOptions.Right);
            var dLabelLE = dLabel.gameObject.AddComponent<LayoutElement>();
            dLabelLE.preferredWidth = 100;

            // ===================================================================
            // INBOX LIST CONTAINER
            // ===================================================================
            var inbox = MakeUI("InboxContainer", rp);
            inbox.anchorMin = new Vector2(0.1f, 0.12f);
            inbox.anchorMax = new Vector2(0.9f, 0.9f);
            inbox.offsetMin = inbox.offsetMax = Vector2.zero;

            var inboxVL = inbox.gameObject.AddComponent<VerticalLayoutGroup>();
            inboxVL.spacing = 8;
            inboxVL.padding = new RectOffset(20, 20, 20, 20);
            inboxVL.childForceExpandWidth = true;
            inboxVL.childForceExpandHeight = false;
            inboxVL.childControlHeight = false;
            inboxVL.childControlWidth = true;

            // Info label
            var infoLabel = MakeTMP("InboxInfo", inbox, "Click a task to view details and write your response.", 14, TextAlignmentOptions.Center);
            infoLabel.color = textMutedColor;
            PrefH(infoLabel.gameObject, 25);

            // 3 list items
            var expLI = BuildListItem("ExplorationListItem", inbox, PlayerActionCategory.Exploration, "EXPLORATION");
            var dilLI = BuildListItem("DilemmaListItem", inbox, PlayerActionCategory.Dilemma, "DILEMMA");
            var famLI = BuildListItem("FamilyRequestListItem", inbox, PlayerActionCategory.FamilyRequest, "FAMILY REQUEST");

            // Spacer
            var spacer = MakeUI("Spacer", inbox);
            var spacerLE = spacer.gameObject.AddComponent<LayoutElement>();
            spacerLE.flexibleHeight = 1;

            // Submit All button (hidden by default)
            var submitAll = MakeButton("SubmitAllButton", inbox, "Submit All Actions");
            PrefH(submitAll.gameObject, 50);
            var submitBtnImg = submitAll.GetComponent<Image>();
            if (submitBtnImg != null) ApplySprite(submitBtnImg, submitButtonSprite, submitButtonColor);
            submitAll.gameObject.SetActive(false);

            // ===================================================================
            // DETAIL PANEL — simple 3-zone layout (top bar, content, bottom bar)
            // ===================================================================
            var detail = MakeUI("DetailPanel", rp);
            detail.anchorMin = new Vector2(0.1f, 0.12f);
            detail.anchorMax = new Vector2(0.9f, 0.9f);
            detail.offsetMin = detail.offsetMax = Vector2.zero;
            detail.gameObject.SetActive(false);

            var detailBg = detail.gameObject.AddComponent<Image>();
            ApplySprite(detailBg, detailPanelSprite, detailBgColor);

            // --- TOP: Back button row, pinned to top ---
            var backRow = MakeUI("BackRow", detail);
            backRow.anchorMin = new Vector2(0, 1);
            backRow.anchorMax = new Vector2(1, 1);
            backRow.pivot = new Vector2(0.5f, 1);
            backRow.sizeDelta = new Vector2(0, 40);
            backRow.anchoredPosition = Vector2.zero;
            var backHL = backRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            backHL.padding = new RectOffset(15, 15, 5, 5);
            backHL.childForceExpandWidth = false;
            backHL.childForceExpandHeight = true;

            var backBtn = MakeButton("BackButton", backRow, "< Back");
            var backLE = backBtn.gameObject.AddComponent<LayoutElement>();
            backLE.preferredWidth = 100;
            backLE.preferredHeight = 30;
            var backBtnImg = backBtn.GetComponent<Image>();
            if (backBtnImg != null) ApplySprite(backBtnImg, backButtonSprite, new Color(0.25f, 0.25f, 0.35f, 1f));

            // --- BOTTOM: Save button + status, pinned to bottom ---
            var bottomBar = MakeUI("BottomBar", detail);
            bottomBar.anchorMin = new Vector2(0, 0);
            bottomBar.anchorMax = new Vector2(1, 0);
            bottomBar.pivot = new Vector2(0.5f, 0);
            bottomBar.sizeDelta = new Vector2(0, 70);
            bottomBar.anchoredPosition = Vector2.zero;
            var bottomBg = bottomBar.gameObject.AddComponent<Image>();
            bottomBg.color = new Color(0.06f, 0.06f, 0.1f, 1f);

            var bottomVL = bottomBar.gameObject.AddComponent<VerticalLayoutGroup>();
            bottomVL.spacing = 4;
            bottomVL.padding = new RectOffset(25, 25, 6, 6);
            bottomVL.childForceExpandWidth = true;
            bottomVL.childForceExpandHeight = false;
            bottomVL.childControlHeight = false;
            bottomVL.childControlWidth = true;

            // Save button — large, bright green
            var saveBtnRT = MakeButton("SaveButton", bottomBar, "SAVE RESPONSE");
            PrefH(saveBtnRT.gameObject, 42);
            var saveBtnImg = saveBtnRT.GetComponent<Image>();
            if (saveBtnImg != null) ApplySprite(saveBtnImg, saveButtonSprite, saveButtonColor);
            var saveBtnColors = saveBtnRT.GetComponent<Button>().colors;
            saveBtnColors.highlightedColor = new Color(0.2f, 0.7f, 0.25f, 1f);
            saveBtnColors.pressedColor = new Color(0.1f, 0.45f, 0.15f, 1f);
            saveBtnColors.disabledColor = new Color(0.15f, 0.2f, 0.15f, 0.5f);
            saveBtnRT.GetComponent<Button>().colors = saveBtnColors;

            // Status text
            var dStatus = MakeTMP("DetailStatusText", bottomBar, "", 12, TextAlignmentOptions.Center);
            dStatus.color = textMutedColor;
            PrefH(dStatus.gameObject, 16);

            // --- MIDDLE: Content area between top bar and bottom bar ---
            var contentArea = MakeUI("ContentArea", detail);
            contentArea.anchorMin = new Vector2(0, 0);
            contentArea.anchorMax = new Vector2(1, 1);
            contentArea.offsetMin = new Vector2(0, 72);  // above bottom bar
            contentArea.offsetMax = new Vector2(0, -45); // below back row (with gap)

            var contentVL = contentArea.gameObject.AddComponent<VerticalLayoutGroup>();
            contentVL.spacing = 8;
            contentVL.padding = new RectOffset(25, 25, 10, 15);
            contentVL.childForceExpandWidth = true;
            contentVL.childForceExpandHeight = false;
            contentVL.childControlHeight = true;
            contentVL.childControlWidth = true;

            // Category label
            var dCatLabel = MakeTMP("DetailCategoryLabel", contentArea, "CATEGORY", 22, TextAlignmentOptions.Left);
            dCatLabel.color = categoryLabelColor;
            dCatLabel.fontStyle = FontStyles.Bold;
            PrefH(dCatLabel.gameObject, 30);

            // Challenge title
            var dTitle = MakeTMP("DetailChallengeTitle", contentArea, "Challenge Title", 18, TextAlignmentOptions.Left);
            dTitle.fontStyle = FontStyles.Bold;
            PrefH(dTitle.gameObject, 26);

            // Challenge description
            var dDesc = MakeTMP("DetailChallengeDescription", contentArea, "Challenge description...", 14, TextAlignmentOptions.TopLeft);
            PrefH(dDesc.gameObject, 60);

            // Separator
            var sep = MakeUI("Separator", contentArea);
            PrefH(sep.gameObject, 2);
            var sepImg = sep.gameObject.AddComponent<Image>();
            sepImg.color = new Color(0.3f, 0.3f, 0.4f, 0.5f);

            // Input field label
            var inputLabel = MakeTMP("InputLabel", contentArea, "Your Response:", 14, TextAlignmentOptions.Left);
            inputLabel.color = new Color(0.7f, 0.7f, 0.7f);
            PrefH(inputLabel.gameObject, 22);

            // Input field — uses flexibleHeight to fill remaining space
            var inputGO = new GameObject("DetailInputField");
            inputGO.transform.SetParent(contentArea, false);
            var inputRT = inputGO.AddComponent<RectTransform>();
            var inputBg = inputGO.AddComponent<Image>();
            ApplySprite(inputBg, inputFieldSprite, inputBgColor);
            var dInputField = inputGO.AddComponent<TMP_InputField>();
            dInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            var inputLE = inputGO.AddComponent<LayoutElement>();
            inputLE.flexibleHeight = 1;  // fill remaining space
            inputLE.minHeight = 80;

            var textArea = MakeUI("Text Area", inputRT);
            Stretch(textArea, 8);
            var inputTxt = MakeTMP("Text", textArea, "", 14, TextAlignmentOptions.TopLeft);
            Stretch(inputTxt.rectTransform);
            var ph = MakeTMP("Placeholder", textArea, "Type your response...", 14, TextAlignmentOptions.TopLeft);
            ph.fontStyle = FontStyles.Italic;
            ph.color = new Color(1, 1, 1, 0.3f);
            Stretch(ph.rectTransform);

            dInputField.textViewport = textArea;
            dInputField.textComponent = inputTxt;
            dInputField.placeholder = ph;

            // Item toggle container (for inventory items, if any)
            var dItemC = MakeUI("DetailItemToggleContainer", contentArea);
            PrefH(dItemC.gameObject, 0);  // hidden by default, grows when items added
            var dItemHL = dItemC.gameObject.AddComponent<HorizontalLayoutGroup>();
            dItemHL.spacing = 5;
            dItemHL.childForceExpandWidth = false;
            dItemHL.childForceExpandHeight = true;

            // ===================================================================
            // CONFIRMATION POPUP
            // ===================================================================
            var popupRoot = MakeUI("ConfirmationPopup", rp);
            Stretch(popupRoot);
            popupRoot.gameObject.SetActive(false);

            // Overlay
            var overlay = MakeUI("Overlay", popupRoot);
            Stretch(overlay);
            var overlayImg = overlay.gameObject.AddComponent<Image>();
            overlayImg.color = new Color(0, 0, 0, 0.6f);
            overlayImg.raycastTarget = true;

            // Card
            var card = MakeUI("Card", popupRoot);
            card.anchorMin = new Vector2(0.25f, 0.3f);
            card.anchorMax = new Vector2(0.75f, 0.65f);
            card.offsetMin = card.offsetMax = Vector2.zero;
            var cardBg = card.gameObject.AddComponent<Image>();
            cardBg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            var cardVL = card.gameObject.AddComponent<VerticalLayoutGroup>();
            cardVL.spacing = 15;
            cardVL.padding = new RectOffset(30, 30, 25, 20);
            cardVL.childForceExpandWidth = true;
            cardVL.childForceExpandHeight = false;
            cardVL.childControlHeight = false;
            cardVL.childControlWidth = true;

            var msgText = MakeTMP("MessageText", card, "Are you sure you want to save?", 18, TextAlignmentOptions.Center);
            PrefH(msgText.gameObject, 80);

            var btnRow = MakeUI("ButtonRow", card);
            PrefH(btnRow.gameObject, 45);
            var btnHL = btnRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            btnHL.spacing = 20;
            btnHL.childForceExpandWidth = true;
            btnHL.childForceExpandHeight = true;

            var cancBtn = MakeButton("CancelButton", btnRow, "Cancel");
            var cancBtnImg = cancBtn.GetComponent<Image>();
            if (cancBtnImg != null) cancBtnImg.color = new Color(0.35f, 0.2f, 0.2f, 1f);

            var confBtn = MakeButton("ConfirmButton", btnRow, "Save");
            var confBtnImg = confBtn.GetComponent<Image>();
            if (confBtnImg != null) confBtnImg.color = new Color(0.2f, 0.55f, 0.2f, 1f);

            // Wire ConfirmationPopup component
            var popupComp = popupRoot.gameObject.AddComponent<ConfirmationPopup>();
            var popupSO = new SerializedObject(popupComp);
            popupSO.FindProperty("popupRoot").objectReferenceValue = popupRoot.gameObject;
            popupSO.FindProperty("overlayBackground").objectReferenceValue = overlayImg;
            popupSO.FindProperty("messageText").objectReferenceValue = msgText;
            popupSO.FindProperty("confirmButton").objectReferenceValue = confBtn.GetComponent<Button>();
            popupSO.FindProperty("confirmButtonText").objectReferenceValue = confBtn.GetComponentInChildren<TMP_Text>();
            popupSO.FindProperty("cancelButton").objectReferenceValue = cancBtn.GetComponent<Button>();
            popupSO.FindProperty("cancelButtonText").objectReferenceValue = cancBtn.GetComponentInChildren<TMP_Text>();
            popupSO.ApplyModifiedPropertiesWithoutUndo();

            // ===================================================================
            // COMPLETION PANEL
            // ===================================================================
            var comp = MakeUI("CompletionPanel", rp);
            comp.anchorMin = new Vector2(0.15f, 0.15f);
            comp.anchorMax = new Vector2(0.85f, 0.85f);
            comp.offsetMin = comp.offsetMax = Vector2.zero;
            var compBg = comp.gameObject.AddComponent<Image>();
            compBg.color = completionBgColor;
            comp.gameObject.SetActive(false);

            var cText = MakeTMP("CompletionText", comp, "All actions complete!", 18, TextAlignmentOptions.TopLeft);
            cText.rectTransform.anchorMin = new Vector2(0.05f, 0.2f);
            cText.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            cText.rectTransform.offsetMin = cText.rectTransform.offsetMax = Vector2.zero;

            var contBtn = MakeButton("ContinueButton", comp, "Continue");
            contBtn.anchorMin = new Vector2(0.3f, 0.03f);
            contBtn.anchorMax = new Vector2(0.7f, 0.15f);
            contBtn.offsetMin = contBtn.offsetMax = Vector2.zero;

            // ===================================================================
            // CATEGORY DATA HOLDERS (invisible)
            // ===================================================================
            var expData = BuildDataPanel("ExplorationData", rp, PlayerActionCategory.Exploration);
            var dilData = BuildDataPanel("DilemmaData", rp, PlayerActionCategory.Dilemma);
            var famData = BuildDataPanel("FamilyRequestData", rp, PlayerActionCategory.FamilyRequest);

            // ===================================================================
            // WIRE ALL REFERENCES
            // ===================================================================
            explorationPanel = expData;
            dilemmaPanel = dilData;
            familyRequestPanel = famData;

            rootPanel = rp.gameObject;
            headerText = hText;
            dayLabel = dLabel;
            closeButton = closeBtnRT.GetComponent<Button>();

            inboxContainer = inbox.gameObject;
            explorationListItem = expLI;
            dilemmaListItem = dilLI;
            familyRequestListItem = famLI;
            submitAllButton = submitAll.GetComponent<Button>();
            submitAllButtonText = submitAll.GetComponentInChildren<TMP_Text>();

            detailPanel = detail.gameObject;
            backButton = backBtn.GetComponent<Button>();
            detailCategoryLabel = dCatLabel;
            detailChallengeTitle = dTitle;
            detailChallengeDescription = dDesc;
            detailInputField = dInputField;
            detailItemToggleContainer = dItemC;
            saveButton = saveBtnRT.GetComponent<Button>();
            saveButtonText = saveBtnRT.GetComponentInChildren<TMP_Text>();
            detailStatusText = dStatus;

            confirmationPopup = popupComp;

            completionPanel = comp.gameObject;
            completionText = cText;
            continueButton = contBtn.GetComponent<Button>();

            EditorUtility.SetDirty(this);
            Debug.Log("[PlayerActionUI] Auto Setup complete! Inbox-style UI built and wired.");
            #else
            Debug.LogWarning("[PlayerActionUI] AutoSetup only works in the Editor.");
            #endif
        }

        #if UNITY_EDITOR
        // -------------------------------------------------------------------------
        // List Item builder (used by AutoSetup)
        // -------------------------------------------------------------------------
        private PlayerActionListItem BuildListItem(string name, RectTransform parent, PlayerActionCategory cat, string displayName)
        {
            var row = MakeUI(name, parent);
            var rowBg = row.gameObject.AddComponent<Image>();
            ApplySprite(rowBg, listItemSprite, listItemBgColor);

            var btn = row.gameObject.AddComponent<Button>();
            var btnColors = btn.colors;
            btnColors.highlightedColor = listItemHoverColor;
            btnColors.pressedColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            btn.colors = btnColors;

            var hlg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            PrefH(row.gameObject, 60);

            // Category label
            var catLabel = MakeTMP("CategoryLabel", row, displayName, 16, TextAlignmentOptions.Left);
            catLabel.fontStyle = FontStyles.Bold;
            var catLE = catLabel.gameObject.AddComponent<LayoutElement>();
            catLE.preferredWidth = 200;

            // Challenge title (flexible)
            var titleText = MakeTMP("ChallengeTitle", row, "Challenge title...", 14, TextAlignmentOptions.Left);
            titleText.color = new Color(0.8f, 0.8f, 0.8f);
            var titleLE = titleText.gameObject.AddComponent<LayoutElement>();
            titleLE.flexibleWidth = 1;

            // Status label
            var statusLabel = MakeTMP("StatusLabel", row, "Draft", 14, TextAlignmentOptions.Right);
            statusLabel.color = new Color(0.5f, 0.5f, 0.5f);
            var statusLE = statusLabel.gameObject.AddComponent<LayoutElement>();
            statusLE.preferredWidth = 100;

            // Add and wire PlayerActionListItem
            var listItem = row.gameObject.AddComponent<PlayerActionListItem>();
            var so = new SerializedObject(listItem);
            so.FindProperty("categoryLabel").objectReferenceValue = catLabel;
            so.FindProperty("challengeTitleText").objectReferenceValue = titleText;
            so.FindProperty("statusLabel").objectReferenceValue = statusLabel;
            so.FindProperty("backgroundImage").objectReferenceValue = rowBg;
            so.FindProperty("selectButton").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            return listItem;
        }

        // -------------------------------------------------------------------------
        // Data Panel builder (used by AutoSetup) — invisible state holder
        // -------------------------------------------------------------------------
        private PlayerActionCategoryPanel BuildDataPanel(string name, RectTransform parent, PlayerActionCategory cat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.SetActive(false); // Hidden data holder

            var comp = go.AddComponent<PlayerActionCategoryPanel>();
            var so = new SerializedObject(comp);
            so.FindProperty("category").enumValueIndex = (int)cat;
            so.ApplyModifiedPropertiesWithoutUndo();

            return comp;
        }

        // -------------------------------------------------------------------------
        // Tiny UI helpers (used by AutoSetup)
        // -------------------------------------------------------------------------
        private RectTransform MakeUI(string n, RectTransform p)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            return go.AddComponent<RectTransform>();
        }

        private RectTransform MakeUI(string n, Transform p)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            return go.AddComponent<RectTransform>();
        }

        private TextMeshProUGUI MakeTMP(string n, RectTransform p, string text, int size, TextAlignmentOptions align)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = textColor;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Ellipsis;
            if (bodyFont != null) t.font = bodyFont;
            return t;
        }

        private void ApplySprite(Image img, Sprite sprite, Color fallback)
        {
            if (sprite != null)
            { img.sprite = sprite; img.type = Image.Type.Sliced; img.color = Color.white; }
            else
            { img.color = fallback; }
        }

        private RectTransform MakeButton(string n, RectTransform p, string label)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            ApplySprite(img, buttonSprite, new Color(0.2f, 0.5f, 0.2f, 1f));
            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.highlightedColor = new Color(0.3f, 0.65f, 0.3f, 1f);
            c.pressedColor = new Color(0.15f, 0.4f, 0.15f, 1f);
            btn.colors = c;
            var tmp = MakeTMP("Text", rt, label, 16, TextAlignmentOptions.Center);
            if (titleFont != null) tmp.font = titleFont;
            Stretch(go.transform.GetChild(0).GetComponent<RectTransform>());
            return rt;
        }

        private void Stretch(RectTransform r, float pad = 0)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(pad, pad);
            r.offsetMax = new Vector2(-pad, -pad);
        }

        private void PrefH(GameObject go, float h)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredHeight = h;
        }
        #endif
    }
}
