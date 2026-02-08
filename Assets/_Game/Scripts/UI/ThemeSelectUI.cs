using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// UI manager for scenario/theme selection.
    /// Displays a 2-column grid of theme cards, each showing a looping video
    /// inside a polaroid-style card frame with the scenario name below.
    /// Click a card to select it, then confirm.
    /// </summary>
    public class ThemeSelectUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static ThemeSelectUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<GameThemeSO> OnThemeSelected;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Configuration")]
        #endif
        [SerializeField] private List<GameThemeSO> availableThemes = new List<GameThemeSO>();
        [SerializeField] private int canvasSortOrder = 100;
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
        [SerializeField] private Sprite cardFrameSprite;
        [SerializeField] private Sprite notesSprite;
        [SerializeField] private Sprite titleBannerSprite;
        [SerializeField] private Sprite arrowSprite;

        // -------------------------------------------------------------------------
        // Style
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Style")]
        #endif
        [SerializeField] private Color panelBgColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        [SerializeField] private Color cardBorderColor = new Color(0.35f, 0.32f, 0.28f, 1f);
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
        private GameThemeSO selectedTheme;
        public GameThemeSO SelectedTheme => selectedTheme;

        // Video players for each card
        private List<VideoPlayer> cardVideoPlayers = new List<VideoPlayer>();
        private List<RenderTexture> cardRenderTextures = new List<RenderTexture>();
        private List<RawImage> cardVideoImages = new List<RawImage>();
        private List<GameObject> cardObjects = new List<GameObject>();
        private GameObject selectedHighlight;

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
            // Auto-show on play if canvas exists and we have themes
            if (canvasRoot != null && availableThemes.Count > 0)
                Show();
        }

        private void OnDestroy()
        {
            CleanupVideoPlayers();
        }

        private void CleanupVideoPlayers()
        {
            foreach (var rt in cardRenderTextures)
            {
                if (rt != null)
                {
                    rt.Release();
                    DestroyImmediate(rt);
                }
            }
            cardRenderTextures.Clear();
            cardVideoPlayers.Clear();
            cardVideoImages.Clear();
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
            canvasRoot = UIBuilderUtils.CreateCanvasRoot(transform, "ThemeSelectCanvas", canvasSortOrder);
            UIBuilderUtils.EnsureEventSystem();

            // Main panel (full screen)
            panel = UIBuilderUtils.CreatePanel(canvasRoot.transform, "ThemeSelectPanel", panelBgColor);

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

            // ---- Title Banner ----
            BuildTitleBanner(panel.transform);

            // ---- Card Grid ----
            BuildCardGrid(panel.transform);

            // ---- Confirm Button ----
            BuildConfirmButton(panel.transform);

            if (enableDebugLogs) Debug.Log("[ThemeSelectUI] Auto Setup complete.");

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

            // Centered at top
            RectTransform rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.88f);
            rect.anchorMax = new Vector2(0.85f, 0.98f);
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
            text.text = "SCENARIO SELECTION";
            text.fontSize = 48;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 24;
            text.fontSizeMax = 56;
            if (titleFont != null) text.font = titleFont;
        }

        private void BuildCardGrid(Transform parent)
        {
            // Grid container — below title, above confirm button
            GameObject gridObj = new GameObject("CardGrid");
            gridObj.transform.SetParent(parent, false);

            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.03f, 0.08f);
            gridRect.anchorMax = new Vector2(0.97f, 0.86f);
            gridRect.offsetMin = Vector2.zero;
            gridRect.offsetMax = Vector2.zero;

            // GridLayoutGroup with calculated cell sizes
            // Reference canvas: 1920x1080. Grid area ~94% x 78% = ~1804 x 842
            // 2 cols: (1804 - 40 padding - 30 spacing) / 2 = ~867 per cell width
            // 2 rows: (842 - 20 padding - 30 spacing) / 2 = ~396 per cell height
            GridLayoutGroup grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.cellSize = new Vector2(860, 390);
            grid.spacing = new Vector2(30, 30);
            grid.padding = new RectOffset(20, 20, 10, 10);
            grid.childAlignment = TextAnchor.MiddleCenter;

            // Build individual cards for each theme
            for (int i = 0; i < availableThemes.Count; i++)
            {
                BuildThemeCardInGrid(gridObj.transform, availableThemes[i], i);
            }
        }

        private void BuildThemeCardInGrid(Transform gridParent, GameThemeSO theme, int index)
        {
            // Card wrapper — the clickable card (sized by GridLayoutGroup)
            GameObject cardObj = new GameObject($"ThemeCard_{index}");
            cardObj.transform.SetParent(gridParent, false);

            RectTransform cardRect = cardObj.AddComponent<RectTransform>();

            // Card frame background
            Image cardBorder = cardObj.AddComponent<Image>();
            if (cardFrameSprite != null)
            {
                cardBorder.sprite = cardFrameSprite;
                cardBorder.type = Image.Type.Sliced;
                cardBorder.color = Color.white;
            }
            else
            {
                cardBorder.color = cardBorderColor;
            }

            // Make it clickable
            Button cardButton = cardObj.AddComponent<Button>();
            var btnColors = cardButton.colors;
            btnColors.highlightedColor = new Color(0.9f, 0.9f, 0.85f, 1f);
            btnColors.pressedColor = new Color(0.7f, 0.7f, 0.65f, 1f);
            cardButton.colors = btnColors;

            // --- Media area (video/image inside card frame) ---
            GameObject mediaArea = new GameObject("MediaArea");
            mediaArea.transform.SetParent(cardObj.transform, false);

            RectTransform mediaRect = mediaArea.AddComponent<RectTransform>();
            mediaRect.anchorMin = new Vector2(0.04f, 0.18f);
            mediaRect.anchorMax = new Vector2(0.96f, 0.96f);
            mediaRect.offsetMin = Vector2.zero;
            mediaRect.offsetMax = Vector2.zero;

            // Static image fallback
            Image mediaImg = mediaArea.AddComponent<Image>();
            mediaImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);
            mediaImg.type = Image.Type.Simple;
            mediaImg.preserveAspect = false;
            if (theme != null && theme.ThemeIcon != null)
            {
                mediaImg.sprite = theme.ThemeIcon;
                mediaImg.color = Color.white;
            }

            // Video overlay (RawImage)
            GameObject videoObj = new GameObject("VideoDisplay");
            videoObj.transform.SetParent(mediaArea.transform, false);

            RectTransform videoRect = videoObj.AddComponent<RectTransform>();
            videoRect.anchorMin = Vector2.zero;
            videoRect.anchorMax = Vector2.one;
            videoRect.offsetMin = Vector2.zero;
            videoRect.offsetMax = Vector2.zero;

            RawImage videoImg = videoObj.AddComponent<RawImage>();
            videoImg.color = Color.white;
            videoImg.enabled = false;

            // --- Scenario name at bottom of card ---
            GameObject titleArea = new GameObject("CardTitle");
            titleArea.transform.SetParent(cardObj.transform, false);

            RectTransform titleRect = titleArea.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.04f, 0.01f);
            titleRect.anchorMax = new Vector2(0.96f, 0.18f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // No black bg — let the card frame show through, just text
            GameObject titleTextObj = new GameObject("Text");
            titleTextObj.transform.SetParent(titleArea.transform, false);

            RectTransform ttRect = titleTextObj.AddComponent<RectTransform>();
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;
            ttRect.offsetMin = new Vector2(5, 0);
            ttRect.offsetMax = new Vector2(-5, 0);

            string themeName = theme != null ? theme.ThemeName.ToUpper() : "UNKNOWN";
            var cardTitle = titleTextObj.AddComponent<TextMeshProUGUI>();
            cardTitle.text = themeName;
            cardTitle.fontSize = 32;
            cardTitle.alignment = TextAlignmentOptions.Center;
            cardTitle.color = detailTextColor;
            cardTitle.fontStyle = FontStyles.Bold;
            cardTitle.enableAutoSizing = true;
            cardTitle.fontSizeMin = 16;
            cardTitle.fontSizeMax = 40;
            if (titleFont != null) cardTitle.font = titleFont;

            // --- Selection highlight (hidden by default) ---
            GameObject highlight = new GameObject("SelectionHighlight");
            highlight.transform.SetParent(cardObj.transform, false);

            RectTransform hlRect = highlight.AddComponent<RectTransform>();
            hlRect.anchorMin = Vector2.zero;
            hlRect.anchorMax = Vector2.one;
            hlRect.offsetMin = new Vector2(-4, -4);
            hlRect.offsetMax = new Vector2(4, 4);

            Image hlImg = highlight.AddComponent<Image>();
            hlImg.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            hlImg.type = Image.Type.Sliced;
            hlImg.raycastTarget = false;
            highlight.SetActive(false);

            // Ensure highlight renders behind card content
            highlight.transform.SetAsFirstSibling();
        }

        private void BuildConfirmButton(Transform parent)
        {
            GameObject btnObj = new GameObject("ConfirmButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.01f);
            rect.anchorMax = new Vector2(0.65f, 0.06f);
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

        // -------------------------------------------------------------------------
        // Show / Hide
        // -------------------------------------------------------------------------
        public void Show()
        {
            if (canvasRoot == null) return;
            canvasRoot.SetActive(true);
            selectedTheme = null;

            SetupAllVideoPlayers();
            WireCardButtons();

            // Start all videos playing
            for (int i = 0; i < availableThemes.Count; i++)
            {
                if (i < cardVideoPlayers.Count && availableThemes[i] != null)
                {
                    PlayCardVideo(i, availableThemes[i].PreviewVideo);
                }
            }

            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", false);

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Shown with {availableThemes.Count} themes.");
        }

        public void Hide()
        {
            StopAllVideos();
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Video Player Setup (one per card)
        // -------------------------------------------------------------------------
        private void SetupAllVideoPlayers()
        {
            CleanupVideoPlayers();
            cardObjects.Clear();

            if (panel == null) return;

            Transform gridTransform = panel.transform.Find("CardGrid");
            if (gridTransform == null) return;

            for (int i = 0; i < availableThemes.Count; i++)
            {
                Transform cardTransform = gridTransform.Find($"ThemeCard_{i}");
                if (cardTransform == null) continue;

                cardObjects.Add(cardTransform.gameObject);

                Transform mediaArea = cardTransform.Find("MediaArea");
                Transform videoDisplay = mediaArea?.Find("VideoDisplay");
                RawImage videoImg = videoDisplay?.GetComponent<RawImage>();

                // Create RenderTexture + VideoPlayer for this card
                RenderTexture rt = new RenderTexture(832, 464, 0);
                rt.Create();
                cardRenderTextures.Add(rt);

                VideoPlayer vp = gameObject.AddComponent<VideoPlayer>();
                vp.playOnAwake = false;
                vp.renderMode = VideoRenderMode.RenderTexture;
                vp.targetTexture = rt;
                vp.isLooping = true;
                vp.audioOutputMode = VideoAudioOutputMode.None;
                cardVideoPlayers.Add(vp);

                if (videoImg != null)
                {
                    videoImg.texture = rt;
                    cardVideoImages.Add(videoImg);
                }
                else
                {
                    cardVideoImages.Add(null);
                }
            }
        }

        private void PlayCardVideo(int index, VideoClip clip)
        {
            if (index >= cardVideoPlayers.Count || clip == null) return;

            VideoPlayer vp = cardVideoPlayers[index];
            vp.clip = clip;
            vp.isLooping = true;
            vp.Play();

            if (index < cardVideoImages.Count && cardVideoImages[index] != null)
                cardVideoImages[index].enabled = true;
        }

        private void StopAllVideos()
        {
            foreach (var vp in cardVideoPlayers)
            {
                if (vp != null && vp.isPlaying)
                    vp.Stop();
            }

            foreach (var img in cardVideoImages)
            {
                if (img != null)
                    img.enabled = false;
            }
        }

        // -------------------------------------------------------------------------
        // Button Wiring
        // -------------------------------------------------------------------------
        private void WireCardButtons()
        {
            if (panel == null) return;

            Transform gridTransform = panel.transform.Find("CardGrid");
            if (gridTransform == null) return;

            for (int i = 0; i < availableThemes.Count; i++)
            {
                Transform cardTransform = gridTransform.Find($"ThemeCard_{i}");
                if (cardTransform == null) continue;

                Button cardBtn = cardTransform.GetComponent<Button>();
                if (cardBtn != null)
                {
                    int capturedIndex = i;
                    cardBtn.onClick.RemoveAllListeners();
                    cardBtn.onClick.AddListener(() => OnCardSelected(capturedIndex));
                }
            }

            // Confirm button
            Button confirmBtn = UIBuilderUtils.FindButton(panel, "ConfirmButton");
            if (confirmBtn != null)
            {
                confirmBtn.onClick.RemoveAllListeners();
                confirmBtn.onClick.AddListener(OnConfirm);
            }
        }

        // -------------------------------------------------------------------------
        // Card Selection
        // -------------------------------------------------------------------------
        private void OnCardSelected(int index)
        {
            if (index < 0 || index >= availableThemes.Count) return;

            selectedTheme = availableThemes[index];
            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", true);

            // Update selection highlight
            UpdateSelectionHighlight(index);

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Selected: {selectedTheme.ThemeName}");
        }

        private void UpdateSelectionHighlight(int selectedIndex)
        {
            if (panel == null) return;

            Transform gridTransform = panel.transform.Find("CardGrid");
            if (gridTransform == null) return;

            for (int i = 0; i < availableThemes.Count; i++)
            {
                Transform cardTransform = gridTransform.Find($"ThemeCard_{i}");
                if (cardTransform == null) continue;

                Transform highlight = cardTransform.Find("SelectionHighlight");
                if (highlight != null)
                    highlight.gameObject.SetActive(i == selectedIndex);
            }
        }

        // -------------------------------------------------------------------------
        // Confirm
        // -------------------------------------------------------------------------
        private void OnConfirm()
        {
            if (selectedTheme == null) return;

            if (selectedTheme.EventSchedule != null)
            {
                PreScriptedEventScheduleSO.SetInstance(selectedTheme.EventSchedule);
                if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Applied event schedule: {selectedTheme.EventSchedule.name}");
            }

            Hide();
            OnThemeSelected?.Invoke(selectedTheme);

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Theme confirmed: {selectedTheme.ThemeName}");
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
