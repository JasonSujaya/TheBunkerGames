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

            // Root panel
            var rp = MakeUI("RootPanel", transform);
            Stretch(rp);
            var rpBg = rp.gameObject.AddComponent<Image>();
            rpBg.color = new Color(0.05f, 0.05f, 0.1f, 0.9f);

            // Header area
            var header = MakeUI("Header", rp);
            header.anchorMin = new Vector2(0, 0.92f);
            header.anchorMax = new Vector2(1, 1);
            header.offsetMin = header.offsetMax = Vector2.zero;

            var hText = MakeTMP("HeaderText", header, "DAILY ACTIONS", 28, TextAlignmentOptions.Center);
            Stretch(hText.rectTransform);

            var dLabel = MakeTMP("DayLabel", header, "Day 1", 20, TextAlignmentOptions.TopRight);
            dLabel.rectTransform.anchorMin = new Vector2(0.8f, 0);
            dLabel.rectTransform.anchorMax = Vector2.one;
            dLabel.rectTransform.offsetMin = Vector2.zero;
            dLabel.rectTransform.offsetMax = new Vector2(-10, 0);

            // Content area
            var content = MakeUI("ContentArea", rp);
            content.anchorMin = new Vector2(0.02f, 0.12f);
            content.anchorMax = new Vector2(0.98f, 0.9f);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var hlg = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(10, 10, 10, 10);

            // 3 category panels
            var expPanel = BuildCategoryPanel("ExplorationPanel", content, PlayerActionCategory.Exploration);
            var dilPanel = BuildCategoryPanel("DilemmaPanel", content, PlayerActionCategory.Dilemma);
            var famPanel = BuildCategoryPanel("FamilyPanel", content, PlayerActionCategory.FamilyRequest);

            // Completion panel
            var comp = MakeUI("CompletionPanel", rp);
            comp.anchorMin = new Vector2(0.15f, 0.15f);
            comp.anchorMax = new Vector2(0.85f, 0.85f);
            comp.offsetMin = comp.offsetMax = Vector2.zero;
            var compBg = comp.gameObject.AddComponent<Image>();
            compBg.color = new Color(0.1f, 0.15f, 0.1f, 0.95f);
            comp.gameObject.SetActive(false);

            var cText = MakeTMP("CompletionText", comp, "All actions complete!", 18, TextAlignmentOptions.TopLeft);
            cText.rectTransform.anchorMin = new Vector2(0.05f, 0.2f);
            cText.rectTransform.anchorMax = new Vector2(0.95f, 0.95f);
            cText.rectTransform.offsetMin = cText.rectTransform.offsetMax = Vector2.zero;

            var contBtn = MakeButton("ContinueButton", comp, "Continue");
            contBtn.anchorMin = new Vector2(0.3f, 0.03f);
            contBtn.anchorMax = new Vector2(0.7f, 0.15f);
            contBtn.offsetMin = contBtn.offsetMax = Vector2.zero;

            // Wire all references
            explorationPanel = expPanel;
            dilemmaPanel = dilPanel;
            familyRequestPanel = famPanel;
            rootPanel = rp.gameObject;
            headerText = hText;
            dayLabel = dLabel;
            completionPanel = comp.gameObject;
            completionText = cText;
            continueButton = contBtn.GetComponent<Button>();

            EditorUtility.SetDirty(this);
            Debug.Log("[PlayerActionUI] Auto Setup complete! All UI built and wired.");
            #else
            Debug.LogWarning("[PlayerActionUI] AutoSetup only works in the Editor.");
            #endif
        }

        #if UNITY_EDITOR
        // -------------------------------------------------------------------------
        // Panel builder (used by AutoSetup)
        // -------------------------------------------------------------------------
        private PlayerActionCategoryPanel BuildCategoryPanel(string name, RectTransform parent, PlayerActionCategory cat)
        {
            var panel = MakeUI(name, parent);
            var bg = panel.gameObject.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.18f, 1f);

            var vl = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 8;
            vl.padding = new RectOffset(10, 10, 10, 10);
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
            vl.childControlHeight = false;
            vl.childControlWidth = true;

            var catLabel = MakeTMP("CategoryLabel", panel, cat.ToString().ToUpper(), 22, TextAlignmentOptions.Center);
            catLabel.color = new Color(1f, 0.85f, 0.3f);
            PrefH(catLabel.gameObject, 35);

            var titleText = MakeTMP("ChallengeTitle", panel, "Challenge Title", 18, TextAlignmentOptions.Center);
            titleText.fontStyle = FontStyles.Bold;
            PrefH(titleText.gameObject, 30);

            var descText = MakeTMP("ChallengeDescription", panel, "Challenge description...", 14, TextAlignmentOptions.TopLeft);
            PrefH(descText.gameObject, 80);

            // Input field
            var inputGO = new GameObject("PlayerInput");
            inputGO.transform.SetParent(panel, false);
            var inputRT = inputGO.AddComponent<RectTransform>();
            inputRT.sizeDelta = new Vector2(0, 80);
            var inputBg = inputGO.AddComponent<Image>();
            inputBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            var inputField = inputGO.AddComponent<TMP_InputField>();
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;

            var textArea = MakeUI("Text Area", inputRT);
            Stretch(textArea, 5);
            var inputTxt = MakeTMP("Text", textArea, "", 14, TextAlignmentOptions.TopLeft);
            Stretch(inputTxt.rectTransform);
            var ph = MakeTMP("Placeholder", textArea, "Type your response...", 14, TextAlignmentOptions.TopLeft);
            ph.fontStyle = FontStyles.Italic;
            ph.color = new Color(1, 1, 1, 0.3f);
            Stretch(ph.rectTransform);

            inputField.textViewport = textArea;
            inputField.textComponent = inputTxt;
            inputField.placeholder = ph;
            PrefH(inputGO, 80);

            // Item container
            var itemC = MakeUI("ItemToggleContainer", panel);
            PrefH(itemC.gameObject, 40);
            var ihl = itemC.gameObject.AddComponent<HorizontalLayoutGroup>();
            ihl.spacing = 5;
            ihl.childForceExpandWidth = false;
            ihl.childForceExpandHeight = true;

            // Submit button
            var subBtn = MakeButton("SubmitButton", panel, "Submit");
            PrefH(subBtn.gameObject, 40);

            // Status text
            var statusTxt = MakeTMP("StatusText", panel, "", 12, TextAlignmentOptions.Center);
            statusTxt.color = new Color(0.7f, 0.7f, 0.7f);
            PrefH(statusTxt.gameObject, 20);

            // Loading indicator
            var loadGO = new GameObject("LoadingIndicator");
            loadGO.transform.SetParent(panel, false);
            loadGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            var lt = MakeTMP("LoadingText", loadGO.GetComponent<RectTransform>(), "Processing...", 14, TextAlignmentOptions.Center);
            lt.color = new Color(1f, 0.9f, 0.3f);
            Stretch(lt.rectTransform);
            PrefH(loadGO, 20);
            loadGO.SetActive(false);

            // Result panel
            var resPanel = MakeUI("ResultPanel", panel);
            var resBg = resPanel.gameObject.AddComponent<Image>();
            resBg.color = new Color(0.08f, 0.15f, 0.08f, 1f);
            var rvl = resPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            rvl.spacing = 4;
            rvl.padding = new RectOffset(8, 8, 8, 8);
            rvl.childForceExpandWidth = true;
            rvl.childForceExpandHeight = false;
            rvl.childControlHeight = false;
            rvl.childControlWidth = true;
            PrefH(resPanel.gameObject, 120);

            var resTitle = MakeTMP("ResultTitle", resPanel, "Result Title", 16, TextAlignmentOptions.Center);
            resTitle.fontStyle = FontStyles.Bold;
            resTitle.color = new Color(0.4f, 1f, 0.4f);
            PrefH(resTitle.gameObject, 25);
            var resDesc = MakeTMP("ResultDescription", resPanel, "", 13, TextAlignmentOptions.TopLeft);
            PrefH(resDesc.gameObject, 50);
            var resEffects = MakeTMP("ResultEffects", resPanel, "", 11, TextAlignmentOptions.TopLeft);
            resEffects.color = new Color(1f, 0.7f, 0.3f);
            PrefH(resEffects.gameObject, 35);
            resPanel.gameObject.SetActive(false);

            // Add component and wire via SerializedObject
            var comp = panel.gameObject.AddComponent<PlayerActionCategoryPanel>();
            var so = new SerializedObject(comp);
            so.FindProperty("category").enumValueIndex = (int)cat;
            so.FindProperty("categoryLabel").objectReferenceValue = catLabel;
            so.FindProperty("challengeTitleText").objectReferenceValue = titleText;
            so.FindProperty("challengeDescriptionText").objectReferenceValue = descText;
            so.FindProperty("playerInputField").objectReferenceValue = inputField;
            so.FindProperty("submitButton").objectReferenceValue = subBtn.GetComponent<Button>();
            so.FindProperty("submitButtonText").objectReferenceValue = subBtn.GetComponentInChildren<TMP_Text>();
            so.FindProperty("itemToggleContainer").objectReferenceValue = itemC;
            so.FindProperty("resultPanel").objectReferenceValue = resPanel.gameObject;
            so.FindProperty("resultTitleText").objectReferenceValue = resTitle;
            so.FindProperty("resultDescriptionText").objectReferenceValue = resDesc;
            so.FindProperty("resultEffectsText").objectReferenceValue = resEffects;
            so.FindProperty("loadingIndicator").objectReferenceValue = loadGO;
            so.FindProperty("statusText").objectReferenceValue = statusTxt;
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
            t.color = Color.white;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        private TextMeshProUGUI MakeTMP(string n, Transform p, string text, int size, TextAlignmentOptions align)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = Color.white;
            t.enableWordWrapping = true;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        private RectTransform MakeButton(string n, RectTransform p, string label)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.color = new Color(0.2f, 0.5f, 0.2f, 1f);
            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.highlightedColor = new Color(0.3f, 0.65f, 0.3f, 1f);
            c.pressedColor = new Color(0.15f, 0.4f, 0.15f, 1f);
            btn.colors = c;
            MakeTMP("Text", rt, label, 16, TextAlignmentOptions.Center);
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
