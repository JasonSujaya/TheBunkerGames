using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// UI manager for family/character selection.
    /// Displays a portrait grid of all characters from available FamilyListSO profiles.
    /// Clicking a portrait shows character details (name, traits, bio) in a side panel.
    /// Selected characters are highlighted; confirm button applies the family profile.
    /// </summary>
    public class FamilySelectUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static FamilySelectUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<FamilyListSO> OnFamilySelected;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Configuration")]
        #endif
        [SerializeField] private List<FamilyListSO> availableFamilies = new List<FamilyListSO>();
        [SerializeField] private int canvasSortOrder = 101;
        [SerializeField] private int portraitGridColumns = 5;
        [SerializeField] private float portraitSize = 140f;
        [SerializeField] private float portraitSpacing = 10f;
        [SerializeField] private bool enableDebugLogs = false;

        // -------------------------------------------------------------------------
        // Visual Assets (drag & drop in Inspector)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Visual Assets")]
        #endif
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset subtitleFont;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite portraitFrameSprite;

        // -------------------------------------------------------------------------
        // Style
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Style")]
        #endif
        [SerializeField] private Color panelBgColor = new Color(0.12f, 0.11f, 0.1f, 0.95f);
        [SerializeField] private Color portraitDefaultBorder = new Color(0.35f, 0.32f, 0.28f, 1f);
        [SerializeField] private Color portraitSelectedBorder = new Color(0.8f, 0.25f, 0.2f, 1f);
        [SerializeField] private Color detailPanelBg = new Color(0.9f, 0.87f, 0.8f, 0.95f);
        [SerializeField] private Color detailTextColor = new Color(0.15f, 0.12f, 0.1f, 1f);

        // -------------------------------------------------------------------------
        // Generated References (populated by AutoSetup)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Generated References")]
        [ReadOnly]
        #endif
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private GameObject panel;

        // -------------------------------------------------------------------------
        // Runtime State
        // -------------------------------------------------------------------------
        private FamilyListSO selectedFamily;
        private CharacterDefinitionSO inspectedCharacter;
        private List<GameObject> portraitCards = new List<GameObject>();

        // Runtime UI references (found after auto-setup)
        private Transform portraitGridContainer;
        private TMP_Text detailNameText;
        private TMP_Text detailTraitsText;
        private TMP_Text detailBioText;
        private Image detailPortraitImage;
        private GameObject detailPanel;

        public FamilyListSO SelectedFamily => selectedFamily;

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
        // Auto Setup
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Auto Setup")]
        [Button("Auto Setup", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        [ContextMenu("Auto Setup")]
        public void AutoSetup()
        {
            // Clean up existing
            if (canvasRoot != null)
            {
                DestroyImmediate(canvasRoot);
                canvasRoot = null;
            }

            // Canvas root
            canvasRoot = UIBuilderUtils.CreateCanvasRoot(transform, "FamilySelectCanvas", canvasSortOrder);
            UIBuilderUtils.EnsureEventSystem();

            // Main panel (full screen)
            panel = UIBuilderUtils.CreatePanel(canvasRoot.transform, "FamilySelectPanel", panelBgColor);

            // ---- Title Banner ----
            BuildTitleBanner(panel.transform);

            // ---- Portrait Grid (left side, ~65% width) ----
            BuildPortraitGrid(panel.transform);

            // ---- Detail Panel (right side, ~30% width) ----
            BuildDetailPanel(panel.transform);

            // ---- Confirm Button (bottom right) ----
            BuildConfirmButton(panel.transform);

            if (enableDebugLogs) Debug.Log("[FamilySelectUI] Auto Setup complete.");

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        // -------------------------------------------------------------------------
        // UI Builders
        // -------------------------------------------------------------------------
        private void BuildTitleBanner(Transform parent)
        {
            GameObject bannerObj = new GameObject("TitleBanner");
            bannerObj.transform.SetParent(parent, false);

            RectTransform rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.88f);
            rect.anchorMax = new Vector2(0.62f, 0.96f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = bannerObj.AddComponent<Image>();
            bg.color = new Color(0.25f, 0.22f, 0.18f, 0.9f);
            if (backgroundSprite != null) { bg.sprite = backgroundSprite; bg.type = Image.Type.Sliced; }

            // Title text
            GameObject textObj = new GameObject("TitleText");
            textObj.transform.SetParent(bannerObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-20, 0);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "CHARACTER SELECTION";
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            if (titleFont != null) text.font = titleFont;
        }

        private void BuildPortraitGrid(Transform parent)
        {
            // Scroll area for portraits
            GameObject scrollObj = new GameObject("PortraitScrollArea");
            scrollObj.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.05f, 0.08f);
            scrollRect.anchorMax = new Vector2(0.62f, 0.86f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.15f);

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);

            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;

            viewport.AddComponent<Image>().color = Color.clear;
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content with GridLayout
            GameObject content = new GameObject("PortraitGrid");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(portraitSize, portraitSize * 1.2f);
            grid.spacing = new Vector2(portraitSpacing, portraitSpacing);
            grid.padding = new RectOffset(15, 15, 15, 15);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = portraitGridColumns;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewRect;
            scroll.content = contentRect;
        }

        private void BuildDetailPanel(Transform parent)
        {
            // Detail panel container (right side)
            detailPanel = new GameObject("DetailPanel");
            detailPanel.transform.SetParent(parent, false);

            RectTransform detailRect = detailPanel.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.65f, 0.14f);
            detailRect.anchorMax = new Vector2(0.95f, 0.96f);
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = Vector2.zero;

            Image detailBg = detailPanel.AddComponent<Image>();
            detailBg.color = detailPanelBg;
            if (backgroundSprite != null) { detailBg.sprite = backgroundSprite; detailBg.type = Image.Type.Sliced; }

            // Portrait preview (top of detail panel)
            GameObject portraitPreview = new GameObject("PortraitPreview");
            portraitPreview.transform.SetParent(detailPanel.transform, false);

            RectTransform previewRect = portraitPreview.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.15f, 0.6f);
            previewRect.anchorMax = new Vector2(0.85f, 0.95f);
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;

            detailPortraitImage = portraitPreview.AddComponent<Image>();
            detailPortraitImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            detailPortraitImage.preserveAspect = true;

            // Name label
            GameObject nameLabel = new GameObject("NameLabel");
            nameLabel.transform.SetParent(detailPanel.transform, false);

            RectTransform nameRect = nameLabel.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.05f, 0.48f);
            nameRect.anchorMax = new Vector2(0.95f, 0.58f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            var nameText = nameLabel.AddComponent<TextMeshProUGUI>();
            nameText.text = "NAME:";
            nameText.fontSize = 22;
            nameText.alignment = TextAlignmentOptions.MidlineLeft;
            nameText.color = detailTextColor;
            nameText.fontStyle = FontStyles.Bold;
            if (titleFont != null) nameText.font = titleFont;
            detailNameText = nameText;

            // Traits label
            GameObject traitsLabel = new GameObject("TraitsLabel");
            traitsLabel.transform.SetParent(detailPanel.transform, false);

            RectTransform traitsRect = traitsLabel.AddComponent<RectTransform>();
            traitsRect.anchorMin = new Vector2(0.05f, 0.32f);
            traitsRect.anchorMax = new Vector2(0.95f, 0.47f);
            traitsRect.offsetMin = Vector2.zero;
            traitsRect.offsetMax = Vector2.zero;

            var traitsText = traitsLabel.AddComponent<TextMeshProUGUI>();
            traitsText.text = "TRAITS:";
            traitsText.fontSize = 18;
            traitsText.alignment = TextAlignmentOptions.TopLeft;
            traitsText.color = detailTextColor;
            traitsText.fontStyle = FontStyles.Bold;
            if (subtitleFont != null) traitsText.font = subtitleFont;
            detailTraitsText = traitsText;

            // Bio label
            GameObject bioHeader = new GameObject("BioHeader");
            bioHeader.transform.SetParent(detailPanel.transform, false);

            RectTransform bioHeaderRect = bioHeader.AddComponent<RectTransform>();
            bioHeaderRect.anchorMin = new Vector2(0.05f, 0.24f);
            bioHeaderRect.anchorMax = new Vector2(0.95f, 0.32f);
            bioHeaderRect.offsetMin = Vector2.zero;
            bioHeaderRect.offsetMax = Vector2.zero;

            var bioHeaderTmp = bioHeader.AddComponent<TextMeshProUGUI>();
            bioHeaderTmp.text = "BIO:";
            bioHeaderTmp.fontSize = 18;
            bioHeaderTmp.alignment = TextAlignmentOptions.MidlineLeft;
            bioHeaderTmp.color = detailTextColor;
            bioHeaderTmp.fontStyle = FontStyles.Bold;
            if (subtitleFont != null) bioHeaderTmp.font = subtitleFont;

            // Bio text
            GameObject bioContent = new GameObject("BioContent");
            bioContent.transform.SetParent(detailPanel.transform, false);

            RectTransform bioContentRect = bioContent.AddComponent<RectTransform>();
            bioContentRect.anchorMin = new Vector2(0.05f, 0.08f);
            bioContentRect.anchorMax = new Vector2(0.95f, 0.24f);
            bioContentRect.offsetMin = Vector2.zero;
            bioContentRect.offsetMax = Vector2.zero;

            var bioText = bioContent.AddComponent<TextMeshProUGUI>();
            bioText.text = "";
            bioText.fontSize = 16;
            bioText.alignment = TextAlignmentOptions.TopLeft;
            bioText.color = detailTextColor;
            if (subtitleFont != null) bioText.font = subtitleFont;
            detailBioText = bioText;

            // Start with placeholder text
            ClearDetailPanel();
        }

        private void BuildConfirmButton(Transform parent)
        {
            GameObject btnObj = new GameObject("ConfirmButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.65f, 0.04f);
            rect.anchorMax = new Vector2(0.95f, 0.12f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.3f, 0.28f, 0.25f, 1f);
            if (buttonSprite != null) { btnBg.sprite = buttonSprite; btnBg.type = Image.Type.Sliced; }

            Button btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.4f, 0.35f, 0.3f, 1f);
            colors.pressedColor = new Color(0.2f, 0.18f, 0.15f, 1f);
            colors.disabledColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
            btn.colors = colors;
            btn.interactable = false;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "CONFIRM";
            text.fontSize = 24;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            if (titleFont != null) text.font = titleFont;
        }

        // -------------------------------------------------------------------------
        // Show / Hide
        // -------------------------------------------------------------------------
        public void Show()
        {
            if (canvasRoot == null) return;
            canvasRoot.SetActive(true);
            selectedFamily = null;
            inspectedCharacter = null;

            CacheUIReferences();
            PopulatePortraitGrid();
            ClearDetailPanel();

            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", false);

            // Wire confirm button
            Button confirmBtn = UIBuilderUtils.FindButton(panel, "ConfirmButton");
            if (confirmBtn != null)
            {
                confirmBtn.onClick.RemoveAllListeners();
                confirmBtn.onClick.AddListener(OnConfirm);
            }

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Shown with {availableFamilies.Count} profiles.");
        }

        public void Hide()
        {
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Cache UI References
        // -------------------------------------------------------------------------
        private void CacheUIReferences()
        {
            if (panel == null) return;

            // Find portrait grid
            Transform gridTransform = panel.transform.Find("PortraitScrollArea/Viewport/PortraitGrid");
            portraitGridContainer = gridTransform;

            // Find detail panel elements
            detailPanel = panel.transform.Find("DetailPanel")?.gameObject;
            if (detailPanel != null)
            {
                detailPortraitImage = detailPanel.transform.Find("PortraitPreview")?.GetComponent<Image>();
                detailNameText = detailPanel.transform.Find("NameLabel")?.GetComponent<TMP_Text>();
                detailTraitsText = detailPanel.transform.Find("TraitsLabel")?.GetComponent<TMP_Text>();
                detailBioText = detailPanel.transform.Find("BioContent")?.GetComponent<TMP_Text>();
            }
        }

        // -------------------------------------------------------------------------
        // Portrait Grid
        // -------------------------------------------------------------------------
        private void PopulatePortraitGrid()
        {
            if (portraitGridContainer == null) return;

            UIBuilderUtils.ClearChildren(portraitGridContainer);
            portraitCards.Clear();

            // For now, use the first available family to show characters
            // (or combine all characters from all families)
            foreach (var family in availableFamilies)
            {
                if (family == null || family.DefaultFamilyMembers == null) continue;

                foreach (var charDef in family.DefaultFamilyMembers)
                {
                    if (charDef == null) continue;
                    var capturedChar = charDef;
                    var capturedFamily = family;
                    GameObject card = CreatePortraitCard(portraitGridContainer, charDef, () =>
                    {
                        InspectCharacter(capturedChar);
                        SelectFamily(capturedFamily);
                    });
                    portraitCards.Add(card);
                }
            }
        }

        private GameObject CreatePortraitCard(Transform parent, CharacterDefinitionSO charDef, UnityEngine.Events.UnityAction onClick)
        {
            GameObject card = new GameObject("Portrait_" + charDef.CharacterName);
            card.transform.SetParent(parent, false);

            // Border / background
            Image borderImg = card.AddComponent<Image>();
            borderImg.color = portraitDefaultBorder;
            if (portraitFrameSprite != null) { borderImg.sprite = portraitFrameSprite; borderImg.type = Image.Type.Sliced; }

            Button btn = card.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.5f, 0.45f, 0.38f, 1f);
            colors.pressedColor = new Color(0.3f, 0.27f, 0.22f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            // Portrait image (inset from border)
            GameObject portraitObj = new GameObject("PortraitImage");
            portraitObj.transform.SetParent(card.transform, false);

            RectTransform portraitRect = portraitObj.AddComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0.06f, 0.12f);
            portraitRect.anchorMax = new Vector2(0.94f, 0.94f);
            portraitRect.offsetMin = Vector2.zero;
            portraitRect.offsetMax = Vector2.zero;

            Image portraitImg = portraitObj.AddComponent<Image>();
            if (charDef.Portrait != null)
            {
                portraitImg.sprite = charDef.Portrait;
                portraitImg.preserveAspect = true;
            }
            else
            {
                portraitImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            // Name text (bottom of card)
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(card.transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 0.14f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            // Name background
            Image nameBg = nameObj.AddComponent<Image>();
            nameBg.color = new Color(0, 0, 0, 0.6f);

            GameObject nameTextObj = new GameObject("Text");
            nameTextObj.transform.SetParent(nameObj.transform, false);

            RectTransform nameTextRect = nameTextObj.AddComponent<RectTransform>();
            nameTextRect.anchorMin = Vector2.zero;
            nameTextRect.anchorMax = Vector2.one;
            nameTextRect.offsetMin = new Vector2(4, 0);
            nameTextRect.offsetMax = new Vector2(-4, 0);

            var nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
            nameText.text = charDef.CharacterName.ToUpper();
            nameText.fontSize = 12;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 8;
            nameText.fontSizeMax = 14;
            if (subtitleFont != null) nameText.font = subtitleFont;

            return card;
        }

        // -------------------------------------------------------------------------
        // Character Inspection (Detail Panel)
        // -------------------------------------------------------------------------
        private void InspectCharacter(CharacterDefinitionSO charDef)
        {
            inspectedCharacter = charDef;

            if (detailNameText != null)
                detailNameText.text = $"NAME:\n{charDef.CharacterName.ToUpper()}";

            if (detailTraitsText != null)
            {
                string traits = "TRAITS:\n";
                if (charDef.Role != CharacterRole.Other)
                    traits += charDef.Role.ToString().ToUpper() + "\n";

                // Access startingTraits via reflection since it's private
                var traitsField = typeof(CharacterDefinitionSO).GetField("startingTraits",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (traitsField != null)
                {
                    string[] traitsList = traitsField.GetValue(charDef) as string[];
                    if (traitsList != null)
                    {
                        foreach (var trait in traitsList)
                        {
                            if (!string.IsNullOrEmpty(trait))
                                traits += trait.ToUpper() + "\n";
                        }
                    }
                }
                detailTraitsText.text = traits.TrimEnd('\n');
            }

            if (detailBioText != null)
                detailBioText.text = !string.IsNullOrEmpty(charDef.Description) ? charDef.Description : "No bio available.";

            if (detailPortraitImage != null)
            {
                if (charDef.Portrait != null)
                {
                    detailPortraitImage.sprite = charDef.Portrait;
                    detailPortraitImage.color = Color.white;
                    detailPortraitImage.preserveAspect = true;
                }
                else
                {
                    detailPortraitImage.sprite = null;
                    detailPortraitImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                }
            }

            // Highlight the selected portrait card
            HighlightPortrait(charDef.CharacterName);

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Inspecting: {charDef.CharacterName}");
        }

        private void HighlightPortrait(string characterName)
        {
            foreach (var card in portraitCards)
            {
                if (card == null) continue;
                Image borderImg = card.GetComponent<Image>();
                if (borderImg != null)
                {
                    bool isSelected = card.name == "Portrait_" + characterName;
                    borderImg.color = isSelected ? portraitSelectedBorder : portraitDefaultBorder;
                }
            }
        }

        private void ClearDetailPanel()
        {
            if (detailNameText != null) detailNameText.text = "NAME:";
            if (detailTraitsText != null) detailTraitsText.text = "TRAITS:";
            if (detailBioText != null) detailBioText.text = "Select a character to view details.";
            if (detailPortraitImage != null)
            {
                detailPortraitImage.sprite = null;
                detailPortraitImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            }
        }

        // -------------------------------------------------------------------------
        // Family Selection
        // -------------------------------------------------------------------------
        private void SelectFamily(FamilyListSO family)
        {
            selectedFamily = family;
            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", true);

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Family selected: {family.name}");
        }

        // -------------------------------------------------------------------------
        // Confirm
        // -------------------------------------------------------------------------
        private void OnConfirm()
        {
            if (selectedFamily == null) return;

            ApplyFamilyProfile(selectedFamily);

            Hide();
            OnFamilySelected?.Invoke(selectedFamily);

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Family confirmed: {selectedFamily.name}");
        }

        private void ApplyFamilyProfile(FamilyListSO profile)
        {
            if (FamilyManager.Instance == null) return;

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var so = new UnityEditor.SerializedObject(FamilyManager.Instance);
                var prop = so.FindProperty("defaultFamilyProfile");
                if (prop != null)
                {
                    prop.objectReferenceValue = profile;
                    so.ApplyModifiedProperties();
                }
                return;
            }
            #endif

            // Runtime: use reflection to set the private serialized field
            var field = typeof(FamilyManager).GetField("defaultFamilyProfile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(FamilyManager.Instance, profile);
                if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Applied profile to FamilyManager: {profile.name}");
            }
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [Button("Show", ButtonSizes.Medium)]
        private void Debug_Show() => Show();

        [Button("Hide", ButtonSizes.Medium)]
        private void Debug_Hide() => Hide();
        #endif
    }
}
