using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// UI for multi-select character picking.
    /// Click portraits to toggle characters onto your team.
    /// Confirm spawns them into FamilyManager.
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
        /// <summary>Fired when the player confirms their team. Passes the list of selected character definitions.</summary>
        public static event Action<List<CharacterDefinitionSO>> OnCharactersSelected;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Configuration")]
        #endif
        [SerializeField] private List<FamilyListSO> availableFamilies = new List<FamilyListSO>();
        [SerializeField] private int canvasSortOrder = 101;
        [SerializeField] private int portraitGridColumns = 3;
        [SerializeField] private float portraitSize = 220f;
        [SerializeField] private float portraitSpacing = 15f;
        [SerializeField] private int maxSelections = 3;
        [SerializeField] private bool enableDebugLogs = false;

        // -------------------------------------------------------------------------
        // Visual Assets (drag & drop in Inspector — same slots as ThemeSelectUI)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Visual Assets")]
        #endif
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset subtitleFont;
        [SerializeField] private Sprite buttonSprite;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Sprite portraitFrameSprite;
        [SerializeField] private Sprite notesSprite;
        [SerializeField] private Sprite titleBannerSprite;
        [SerializeField] private Sprite checkmarkSprite;

        #if ODIN_INSPECTOR
        [Title("Video Background")]
        #endif
        [Tooltip("Assign a video clip to play as the background instead of the dark panel color.")]
        [SerializeField] private VideoClip backgroundVideo;
        [SerializeField] [Range(0f, 1f)] private float videoAlpha = 1f;

        // -------------------------------------------------------------------------
        // Style
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Style")]
        #endif
        [SerializeField] private Color panelBgColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
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
        private CharacterDefinitionSO inspectedCharacter;
        private HashSet<CharacterDefinitionSO> selectedCharacters = new HashSet<CharacterDefinitionSO>();
        private List<GameObject> portraitCards = new List<GameObject>();

        // Runtime UI references (found after auto-setup)
        private Transform portraitGridContainer;
        private TextMeshProUGUI detailNameText;
        private TextMeshProUGUI detailTraitsText;
        private TextMeshProUGUI detailBioText;
        private GameObject detailPanel;

        // Idle shake animation
        private Coroutine idleShakeCoroutine;
        private int currentShakingIndex = -1;

        // Video background runtime references
        private RenderTexture videoRenderTexture;
        private VideoPlayer videoPlayer;

        // Flattened list of all characters across all families
        private List<CharacterDefinitionSO> allCharacters = new List<CharacterDefinitionSO>();
        private Dictionary<CharacterDefinitionSO, FamilyListSO> charToFamily = new Dictionary<CharacterDefinitionSO, FamilyListSO>();

        public List<CharacterDefinitionSO> SelectedCharacters => selectedCharacters.ToList();

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

        private void Start()
        {
            // Auto-show on play if canvas exists and we have families
            if (canvasRoot != null && availableFamilies.Count > 0)
                Show();
        }

        private void OnDestroy()
        {
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                Destroy(videoRenderTexture);
                videoRenderTexture = null;
            }
            if (Instance == this) Instance = null;
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

            // Apply background sprite to main panel if assigned
            if (backgroundSprite != null)
            {
                Image panelImg = panel.GetComponent<Image>();
                if (panelImg != null)
                {
                    panelImg.sprite = backgroundSprite;
                    panelImg.type = Image.Type.Sliced;
                    panelImg.color = Color.white;
                }
            }

            // ---- Video Background (renders behind all UI if clip assigned) ----
            BuildVideoBackground(panel.transform);

            // ---- Title Banner ----
            BuildTitleBanner(panel.transform);

            // ---- Portrait Grid (left side, ~60% width) ----
            BuildPortraitGrid(panel.transform);

            // ---- Detail Panel / Notes (right side) ----
            BuildDetailPanel(panel.transform);

            // ---- Confirm Button ----
            BuildConfirmButton(panel.transform);

            // ---- Team Panel (disabled for now) ----
            // BuildTeamPanel(panel.transform);

            if (enableDebugLogs) Debug.Log("[FamilySelectUI] Auto Setup complete.");

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        // -------------------------------------------------------------------------
        // UI Builders
        // -------------------------------------------------------------------------
        private void BuildVideoBackground(Transform parent)
        {
            // Clean up previous render texture
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                DestroyImmediate(videoRenderTexture);
                videoRenderTexture = null;
            }

            if (backgroundVideo == null) return;

            // Create a RawImage that fills the entire panel as the video surface
            GameObject videoObj = new GameObject("VideoBackground");
            videoObj.transform.SetParent(parent, false);
            videoObj.transform.SetAsFirstSibling(); // Behind everything

            RectTransform rect = videoObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage rawImg = videoObj.AddComponent<RawImage>();
            rawImg.color = new Color(1f, 1f, 1f, videoAlpha);
            rawImg.raycastTarget = false;

            // Create render texture matching video dimensions
            int rtWidth = (int)backgroundVideo.width;
            int rtHeight = (int)backgroundVideo.height;
            if (rtWidth <= 0) rtWidth = 1920;
            if (rtHeight <= 0) rtHeight = 1080;

            videoRenderTexture = new RenderTexture(rtWidth, rtHeight, 0);
            videoRenderTexture.name = "FamilySelectVideoBG_RT";
            videoRenderTexture.Create();

            rawImg.texture = videoRenderTexture;

            // VideoPlayer on the same object, renders to our RenderTexture
            videoPlayer = videoObj.AddComponent<VideoPlayer>();
            videoPlayer.clip = backgroundVideo;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None; // No audio for BG
            videoPlayer.aspectRatio = VideoAspectRatio.FitOutside;

            // Make the main panel semi-transparent so video shows through
            Image panelImg = parent.GetComponent<Image>();
            if (panelImg != null)
            {
                Color c = panelImg.color;
                c.a = 0f; // Fully transparent — video replaces the bg
                panelImg.color = c;
            }
        }

        private void BuildTitleBanner(Transform parent)
        {
            GameObject bannerObj = new GameObject("TitleBanner");
            bannerObj.transform.SetParent(parent, false);

            RectTransform rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.10f, 0.89f);
            rect.anchorMax = new Vector2(0.55f, 0.99f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = bannerObj.AddComponent<Image>();
            if (titleBannerSprite != null)
            {
                bg.sprite = titleBannerSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.25f, 0.22f, 0.18f, 0.9f);
            }

            GameObject textObj = new GameObject("TitleText");
            textObj.transform.SetParent(bannerObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(20, 0);
            textRect.offsetMax = new Vector2(-20, 0);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "CHARACTER SELECTION";
            text.fontSize = 48;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 24;
            text.fontSizeMax = 56;
            if (titleFont != null) text.font = titleFont;
        }

        private void BuildPortraitGrid(Transform parent)
        {
            GameObject scrollObj = new GameObject("PortraitScrollArea");
            scrollObj.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.01f, 0.02f);
            scrollRect.anchorMax = new Vector2(0.63f, 0.87f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.02f);

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);

            RectTransform viewRect = viewport.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;

            // Image needed for ScrollRect drag detection (raycastTarget)
            Image viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = new Color(1, 1, 1, 0);
            viewport.AddComponent<RectMask2D>();

            GameObject content = new GameObject("PortraitGrid");
            content.transform.SetParent(viewport.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Use 5 columns with auto-calculated cell size based on available width
            // This ignores serialized portraitSize/portraitGridColumns to avoid stale values
            int cols = 5;
            float availableWidth = scrollRect.rect.width;
            // At edit-time rect may be 0, so estimate from screen reference (1920 * 0.62)
            if (availableWidth <= 0) availableWidth = 1920f * 0.62f;
            float padding = 8f;
            float spacing = 4f;
            float cellWidth = (availableWidth - padding * 2f - spacing * (cols - 1)) / cols;
            float cellHeight = cellWidth * 1.2f;

            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(cellWidth, cellHeight);
            grid.spacing = new Vector2(spacing, spacing);
            grid.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewRect;
            scroll.content = contentRect;
        }

        private void BuildDetailPanel(Transform parent)
        {
            detailPanel = new GameObject("DetailPanel");
            detailPanel.transform.SetParent(parent, false);

            RectTransform detailRect = detailPanel.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.62f, 0.10f);
            detailRect.anchorMax = new Vector2(0.97f, 0.88f);
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = Vector2.zero;

            Image detailBg = detailPanel.AddComponent<Image>();
            if (notesSprite != null)
            {
                detailBg.sprite = notesSprite;
                detailBg.type = Image.Type.Sliced;
                detailBg.color = Color.white;
            }
            else
            {
                detailBg.color = detailPanelBg;
            }

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(detailPanel.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.08f, 0.05f);
            contentRect.anchorMax = new Vector2(0.92f, 0.95f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(5, 5, 10, 10);

            BuildDetailLabel(contentObj.transform, "NameLabel", "NAME:", 32);
            BuildDetailValue(contentObj.transform, "NameValue", "", 28, 50);
            BuildSpacer(contentObj.transform, 10);
            BuildDetailLabel(contentObj.transform, "TraitsLabel", "TRAITS:", 32);
            BuildDetailValue(contentObj.transform, "TraitsValue", "", 26, 80);
            BuildSpacer(contentObj.transform, 10);
            BuildDetailLabel(contentObj.transform, "BioLabel", "BIO:", 32);
            BuildDetailValue(contentObj.transform, "BioValue", "Select a character to view details.", 24, 180);
        }

        private void BuildDetailLabel(Transform parent, string name, string labelText, float fontSize)
        {
            GameObject labelObj = new GameObject(name);
            labelObj.transform.SetParent(parent, false);

            RectTransform rect = labelObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 36);

            var text = labelObj.AddComponent<TextMeshProUGUI>();
            text.text = labelText;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = detailTextColor;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableAutoSizing = false;
            if (titleFont != null) text.font = titleFont;
        }

        private void BuildDetailValue(Transform parent, string name, string valueText, float fontSize, float height)
        {
            GameObject valueObj = new GameObject(name);
            valueObj.transform.SetParent(parent, false);

            RectTransform rect = valueObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            var text = valueObj.AddComponent<TextMeshProUGUI>();
            text.text = valueText;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.color = detailTextColor;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.enableAutoSizing = false;
            if (subtitleFont != null) text.font = subtitleFont;
        }

        private void BuildSpacer(Transform parent, float height)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);

            RectTransform rect = spacer.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            LayoutElement le = spacer.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }

        private void BuildConfirmButton(Transform parent)
        {
            GameObject btnObj = new GameObject("ConfirmButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.72f, 0.02f);
            rect.anchorMax = new Vector2(0.95f, 0.09f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image btnBg = btnObj.AddComponent<Image>();
            if (buttonSprite != null)
            {
                btnBg.sprite = buttonSprite;
                btnBg.type = Image.Type.Sliced;
                btnBg.color = Color.white;
            }
            else
            {
                btnBg.color = new Color(0.3f, 0.28f, 0.25f, 1f);
            }

            Button btn = btnObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.9f, 0.85f, 0.8f, 1f);
            colors.pressedColor = new Color(0.7f, 0.65f, 0.6f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            btn.colors = colors;
            btn.interactable = false;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "CONFIRM";
            text.fontSize = 36;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20;
            text.fontSizeMax = 42;
            if (titleFont != null) text.font = titleFont;
        }

        private void BuildTeamPanel(Transform parent)
        {
            GameObject teamObj = new GameObject("TeamPanel");
            teamObj.transform.SetParent(parent, false);

            RectTransform rect = teamObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.85f, 0.10f);
            rect.anchorMax = new Vector2(0.97f, 0.50f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image bg = teamObj.AddComponent<Image>();
            if (notesSprite != null)
            {
                bg.sprite = notesSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.2f, 0.18f, 0.15f, 0.8f);
            }

            // Clip overflow so portraits don't spill out
            teamObj.AddComponent<RectMask2D>();

            GameObject labelObj = new GameObject("TeamLabel");
            labelObj.transform.SetParent(teamObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.88f);
            labelRect.anchorMax = new Vector2(1, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObj.AddComponent<TextMeshProUGUI>();
            label.text = "TEAM";
            label.fontSize = 18;
            label.alignment = TextAlignmentOptions.Center;
            label.color = detailTextColor;
            label.fontStyle = FontStyles.Bold;
            label.enableAutoSizing = true;
            label.fontSizeMin = 12;
            label.fontSizeMax = 22;
            if (subtitleFont != null) label.font = subtitleFont;

            GameObject portraitsObj = new GameObject("TeamPortraits");
            portraitsObj.transform.SetParent(teamObj.transform, false);

            RectTransform portraitsRect = portraitsObj.AddComponent<RectTransform>();
            portraitsRect.anchorMin = new Vector2(0.08f, 0.03f);
            portraitsRect.anchorMax = new Vector2(0.92f, 0.85f);
            portraitsRect.offsetMin = Vector2.zero;
            portraitsRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = portraitsObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(4, 4, 4, 4);
        }

        // -------------------------------------------------------------------------
        // Show / Hide
        // -------------------------------------------------------------------------
        public void Show()
        {
            if (canvasRoot == null) return;
            canvasRoot.SetActive(true);
            inspectedCharacter = null;
            selectedCharacters.Clear();

            BuildCharacterList();
            CacheUIReferences();
            PopulatePortraitGrid();
            ClearDetailPanel();
            // UpdateTeamPanel(); // disabled
            UpdateSelectionCounter();

            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", false);

            WireButtons();

            // Start idle shake animation
            if (idleShakeCoroutine != null) StopCoroutine(idleShakeCoroutine);
            idleShakeCoroutine = StartCoroutine(IdleShakeRoutine());

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Shown with {availableFamilies.Count} profiles, {allCharacters.Count} characters.");
        }

        public void Hide()
        {
            if (idleShakeCoroutine != null)
            {
                StopCoroutine(idleShakeCoroutine);
                idleShakeCoroutine = null;
            }
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Build Flattened Character List
        // -------------------------------------------------------------------------
        private void BuildCharacterList()
        {
            allCharacters.Clear();
            charToFamily.Clear();

            foreach (var family in availableFamilies)
            {
                if (family == null || family.DefaultFamilyMembers == null) continue;

                foreach (var charDef in family.DefaultFamilyMembers)
                {
                    if (charDef == null) continue;
                    allCharacters.Add(charDef);
                    charToFamily[charDef] = family;
                }
            }
        }

        // -------------------------------------------------------------------------
        // Cache UI References
        // -------------------------------------------------------------------------
        private void CacheUIReferences()
        {
            if (panel == null) return;

            Transform gridTransform = panel.transform.Find("PortraitScrollArea/Viewport/PortraitGrid");
            portraitGridContainer = gridTransform;

            detailPanel = panel.transform.Find("DetailPanel")?.gameObject;
            if (detailPanel != null)
            {
                Transform contentTr = detailPanel.transform.Find("Content");
                if (contentTr != null)
                {
                    detailNameText = contentTr.Find("NameValue")?.GetComponent<TextMeshProUGUI>();
                    detailTraitsText = contentTr.Find("TraitsValue")?.GetComponent<TextMeshProUGUI>();
                    detailBioText = contentTr.Find("BioValue")?.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        // -------------------------------------------------------------------------
        // Button Wiring
        // -------------------------------------------------------------------------
        private void WireButtons()
        {
            if (panel == null) return;

            Button confirmBtn = UIBuilderUtils.FindButton(panel, "ConfirmButton");
            if (confirmBtn != null)
            {
                confirmBtn.onClick.RemoveAllListeners();
                confirmBtn.onClick.AddListener(OnConfirm);
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

            foreach (var charDef in allCharacters)
            {
                var captured = charDef;
                GameObject card = CreatePortraitCard(portraitGridContainer, charDef, () =>
                {
                    ToggleCharacter(captured);
                });
                portraitCards.Add(card);
            }

            // Force layout rebuild so ContentSizeFitter recalculates after adding children
            Canvas.ForceUpdateCanvases();
            RectTransform gridRect = portraitGridContainer as RectTransform;
            if (gridRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);

            // Apply random rotations to each card for a hand-pinned look
            for (int i = 0; i < portraitCards.Count; i++)
            {
                if (portraitCards[i] == null) continue;
                float randomAngle = UnityEngine.Random.Range(-6f, 6f);
                portraitCards[i].transform.localRotation = Quaternion.Euler(0, 0, randomAngle);
            }
        }

        private GameObject CreatePortraitCard(Transform parent, CharacterDefinitionSO charDef, UnityEngine.Events.UnityAction onClick)
        {
            GameObject card = new GameObject("Portrait_" + charDef.CharacterName);
            card.transform.SetParent(parent, false);

            // Card IS the portrait — no paper frame, image fills the entire card
            Image cardImg = card.AddComponent<Image>();
            if (charDef.Portrait != null)
            {
                cardImg.sprite = charDef.Portrait;
                cardImg.type = Image.Type.Simple;
                cardImg.preserveAspect = false;
                cardImg.color = Color.white;
            }
            else
            {
                cardImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            }

            Button btn = card.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(onClick);

            // Name text overlaid at bottom of portrait
            GameObject nameObj = new GameObject("NameText");
            nameObj.transform.SetParent(card.transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 0.18f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            Image nameBg = nameObj.AddComponent<Image>();
            nameBg.color = new Color(0, 0, 0, 0.6f);
            nameBg.raycastTarget = false;

            GameObject nameTextObj = new GameObject("Text");
            nameTextObj.transform.SetParent(nameObj.transform, false);

            RectTransform nameTextRect = nameTextObj.AddComponent<RectTransform>();
            nameTextRect.anchorMin = Vector2.zero;
            nameTextRect.anchorMax = Vector2.one;
            nameTextRect.offsetMin = new Vector2(4, 0);
            nameTextRect.offsetMax = new Vector2(-4, 0);

            var nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
            nameText.text = charDef.CharacterName.ToUpper();
            nameText.fontSize = 18;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 10;
            nameText.fontSizeMax = 22;
            nameText.fontStyle = FontStyles.Bold;
            nameText.raycastTarget = false;
            if (subtitleFont != null) nameText.font = subtitleFont;

            // Checkmark overlay (hidden by default, shown when selected)
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(card.transform, false);

            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.65f, 0.72f);
            checkRect.anchorMax = new Vector2(0.98f, 0.98f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            Image checkImg = checkObj.AddComponent<Image>();
            checkImg.raycastTarget = false;

            if (checkmarkSprite != null)
            {
                // Use assigned checkmark sprite
                checkImg.sprite = checkmarkSprite;
                checkImg.type = Image.Type.Simple;
                checkImg.preserveAspect = true;
                checkImg.color = Color.white;
            }
            else
            {
                // Fallback: green circle with unicode checkmark
                checkImg.color = new Color(0.1f, 0.7f, 0.2f, 0.95f);

                GameObject checkTextObj = new GameObject("CheckText");
                checkTextObj.transform.SetParent(checkObj.transform, false);

                RectTransform checkTextRect = checkTextObj.AddComponent<RectTransform>();
                checkTextRect.anchorMin = Vector2.zero;
                checkTextRect.anchorMax = Vector2.one;
                checkTextRect.offsetMin = Vector2.zero;
                checkTextRect.offsetMax = Vector2.zero;

                var checkText = checkTextObj.AddComponent<TextMeshProUGUI>();
                checkText.text = "\u2713";
                checkText.fontSize = 28;
                checkText.alignment = TextAlignmentOptions.Center;
                checkText.color = Color.white;
                checkText.fontStyle = FontStyles.Bold;
                checkText.enableAutoSizing = true;
                checkText.fontSizeMin = 16;
                checkText.fontSizeMax = 36;
                checkText.raycastTarget = false;
            }

            checkObj.SetActive(false); // Hidden until selected

            return card;
        }

        // -------------------------------------------------------------------------
        // Toggle Character Selection (multi-select)
        // -------------------------------------------------------------------------
        private void ToggleCharacter(CharacterDefinitionSO charDef)
        {
            if (selectedCharacters.Contains(charDef))
            {
                selectedCharacters.Remove(charDef);
                if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Deselected: {charDef.CharacterName}");
            }
            else
            {
                // Enforce max selection limit
                if (selectedCharacters.Count >= maxSelections)
                {
                    if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Max {maxSelections} characters reached! Cannot select {charDef.CharacterName}.");
                    // Still inspect the character so they can see the bio
                    InspectCharacter(charDef);
                    return;
                }

                selectedCharacters.Add(charDef);
                if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Selected: {charDef.CharacterName}");
            }

            // Always inspect the character we just clicked
            InspectCharacter(charDef);

            // Refresh visuals
            RefreshPortraitHighlights();
            // UpdateTeamPanel(); // disabled
            UpdateSelectionCounter();
            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", selectedCharacters.Count > 0);
        }

        // -------------------------------------------------------------------------
        // Character Inspection (Detail Panel)
        // -------------------------------------------------------------------------
        private void InspectCharacter(CharacterDefinitionSO charDef)
        {
            inspectedCharacter = charDef;

            if (detailNameText != null)
                detailNameText.text = charDef.CharacterName.ToUpper();

            if (detailTraitsText != null)
            {
                string traits = "";
                if (charDef.Role != CharacterRole.Other)
                    traits += charDef.Role.ToString().ToUpper() + "\n";

                if (charDef.StartingTraits != null)
                {
                    foreach (var trait in charDef.StartingTraits)
                    {
                        if (!string.IsNullOrEmpty(trait))
                            traits += trait.ToUpper() + "\n";
                    }
                }
                detailTraitsText.text = traits.TrimEnd('\n');
            }

            if (detailBioText != null)
                detailBioText.text = !string.IsNullOrEmpty(charDef.Description) ? charDef.Description : "No bio available.";

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Inspecting: {charDef.CharacterName}");
        }

        private void RefreshPortraitHighlights()
        {
            for (int i = 0; i < portraitCards.Count && i < allCharacters.Count; i++)
            {
                var card = portraitCards[i];
                var charDef = allCharacters[i];
                if (card == null) continue;

                bool isSelected = selectedCharacters.Contains(charDef);
                bool maxReached = selectedCharacters.Count >= maxSelections && !isSelected;

                // Scale up selected cards
                card.transform.localScale = isSelected ? Vector3.one * 1.08f : Vector3.one;

                // Show/hide checkmark
                Transform checkmark = card.transform.Find("Checkmark");
                if (checkmark != null)
                    checkmark.gameObject.SetActive(isSelected);

                // Dim the card when max reached
                Image cardImg = card.GetComponent<Image>();
                if (cardImg != null)
                    cardImg.color = maxReached ? new Color(0.4f, 0.4f, 0.4f, 0.6f) : Color.white;
            }
        }

        private void ClearDetailPanel()
        {
            if (detailNameText != null) detailNameText.text = "";
            if (detailTraitsText != null) detailTraitsText.text = "";
            if (detailBioText != null) detailBioText.text = "Select characters for your team.";
        }

        // -------------------------------------------------------------------------
        // Team Panel Update (shows all selected characters)
        // -------------------------------------------------------------------------
        private void UpdateTeamPanel()
        {
            if (panel == null) return;
            Transform teamPortraits = panel.transform.Find("TeamPanel/TeamPortraits");
            if (teamPortraits == null) return;

            UIBuilderUtils.ClearChildren(teamPortraits);

            foreach (var charDef in selectedCharacters)
            {
                if (charDef == null) continue;

                GameObject miniPortrait = new GameObject("Mini_" + charDef.CharacterName);
                miniPortrait.transform.SetParent(teamPortraits, false);

                LayoutElement le = miniPortrait.AddComponent<LayoutElement>();
                le.preferredHeight = 70;
                le.minHeight = 70;

                Image img = miniPortrait.AddComponent<Image>();
                if (charDef.Portrait != null)
                {
                    img.sprite = charDef.Portrait;
                    img.preserveAspect = true;
                }
                else
                {
                    img.color = new Color(0.4f, 0.4f, 0.4f, 1f);
                }
            }
        }

        // -------------------------------------------------------------------------
        // Confirm — spawns all selected characters into FamilyManager
        // -------------------------------------------------------------------------
        private void OnConfirm()
        {
            if (selectedCharacters.Count == 0) return;

            var selected = selectedCharacters.ToList();

            // Clear existing family and add each selected character
            if (FamilyManager.Instance != null)
            {
                FamilyManager.Instance.ClearFamily();

                foreach (var charDef in selected)
                {
                    FamilyManager.Instance.AddCharacter(charDef);
                }

                if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Spawned {selected.Count} characters into FamilyManager.");
            }
            else
            {
                Debug.LogWarning("[FamilySelectUI] FamilyManager.Instance is null — cannot spawn characters.");
            }

            Hide();
            OnCharactersSelected?.Invoke(selected);

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Confirmed team: {string.Join(", ", selected.Select(c => c.CharacterName))}");
        }

        // -------------------------------------------------------------------------
        // Selection Counter
        // -------------------------------------------------------------------------
        private void UpdateSelectionCounter()
        {
            if (panel == null) return;

            // Find or create counter text
            Transform counterTr = panel.transform.Find("SelectionCounter");
            TextMeshProUGUI counterText;

            if (counterTr == null)
            {
                GameObject counterObj = new GameObject("SelectionCounter");
                counterObj.transform.SetParent(panel.transform, false);

                RectTransform rect = counterObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.05f, 0.01f);
                rect.anchorMax = new Vector2(0.60f, 0.06f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                counterText = counterObj.AddComponent<TextMeshProUGUI>();
                counterText.fontSize = 22;
                counterText.alignment = TextAlignmentOptions.Center;
                counterText.enableAutoSizing = true;
                counterText.fontSizeMin = 14;
                counterText.fontSizeMax = 26;
                counterText.fontStyle = FontStyles.Bold;
                counterText.raycastTarget = false;
                if (subtitleFont != null) counterText.font = subtitleFont;
            }
            else
            {
                counterText = counterTr.GetComponent<TextMeshProUGUI>();
            }

            if (counterText != null)
            {
                counterText.text = $"SELECTED: {selectedCharacters.Count} / {maxSelections}";
                counterText.color = selectedCharacters.Count >= maxSelections
                    ? new Color(0.9f, 0.3f, 0.2f, 1f)
                    : new Color(0.9f, 0.85f, 0.75f, 1f);
            }
        }

        // -------------------------------------------------------------------------
        // Idle Shake Animation (1 random card wobbles at a time)
        // -------------------------------------------------------------------------
        private IEnumerator IdleShakeRoutine()
        {
            while (true)
            {
                if (portraitCards.Count == 0)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                // Pick a random card that's NOT currently selected
                List<int> unselectedIndices = new List<int>();
                for (int i = 0; i < portraitCards.Count && i < allCharacters.Count; i++)
                {
                    if (!selectedCharacters.Contains(allCharacters[i]))
                        unselectedIndices.Add(i);
                }

                if (unselectedIndices.Count == 0)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }

                int idx = unselectedIndices[UnityEngine.Random.Range(0, unselectedIndices.Count)];
                currentShakingIndex = idx;
                GameObject card = portraitCards[idx];

                if (card != null)
                {
                    yield return StartCoroutine(ShakeCard(card.transform));
                }

                currentShakingIndex = -1;

                // Wait 1-3 seconds before shaking another card
                yield return new WaitForSeconds(UnityEngine.Random.Range(1.5f, 3.5f));
            }
        }

        private IEnumerator ShakeCard(Transform cardTransform)
        {
            Quaternion originalRot = cardTransform.localRotation;
            Vector3 originalPos = cardTransform.localPosition;

            // Choose a random shake style
            int style = UnityEngine.Random.Range(0, 3);
            float duration;

            switch (style)
            {
                case 0: // Quick jitter shake
                    duration = 0.4f;
                    float elapsed = 0f;
                    while (elapsed < duration)
                    {
                        float t = elapsed / duration;
                        float intensity = (1f - t) * 3f; // Fade out
                        float angle = Mathf.Sin(elapsed * 40f) * intensity;
                        cardTransform.localRotation = originalRot * Quaternion.Euler(0, 0, angle);
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    break;

                case 1: // Gentle wobble
                    duration = 0.8f;
                    elapsed = 0f;
                    while (elapsed < duration)
                    {
                        float t = elapsed / duration;
                        float intensity = Mathf.Sin(t * Mathf.PI) * 2.5f; // Ramp up then down
                        float angle = Mathf.Sin(elapsed * 12f) * intensity;
                        cardTransform.localRotation = originalRot * Quaternion.Euler(0, 0, angle);
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    break;

                case 2: // Bounce (slight scale pulse + tilt)
                    duration = 0.6f;
                    elapsed = 0f;
                    Vector3 baseScale = cardTransform.localScale;
                    while (elapsed < duration)
                    {
                        float t = elapsed / duration;
                        float scalePulse = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.04f;
                        float angle = Mathf.Sin(elapsed * 18f) * (1f - t) * 2f;
                        cardTransform.localScale = baseScale * scalePulse;
                        cardTransform.localRotation = originalRot * Quaternion.Euler(0, 0, angle);
                        elapsed += Time.deltaTime;
                        yield return null;
                    }
                    cardTransform.localScale = baseScale;
                    break;
            }

            // Reset to original
            cardTransform.localRotation = originalRot;
            cardTransform.localPosition = originalPos;
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
