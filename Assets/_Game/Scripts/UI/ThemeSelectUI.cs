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
    /// Displays a single large polaroid-style card with looping video on the left,
    /// and a notebook-style detail panel on the right showing Name, Traits, and Bio.
    /// Left/right arrows navigate between available themes.
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
        private int currentIndex = 0;

        // Single video player for the displayed card
        private VideoPlayer videoPlayer;
        private RenderTexture renderTexture;
        private RawImage videoImage;

        // Runtime text references for updating when navigating
        private TextMeshProUGUI scenarioNameText;
        private TextMeshProUGUI detailNameText;
        private TextMeshProUGUI detailTraitsText;
        private TextMeshProUGUI detailBioText;
        private Image cardMediaImage;

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
            CleanupVideoPlayer();
        }

        private void CleanupVideoPlayer()
        {
            if (renderTexture != null)
            {
                renderTexture.Release();
                DestroyImmediate(renderTexture);
                renderTexture = null;
            }
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                Destroy(videoPlayer);
                videoPlayer = null;
            }
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

            // ---- Theme Card (left side, large) ----
            BuildThemeCard(panel.transform);

            // ---- Detail Panel / Notes (right side) ----
            BuildDetailPanel(panel.transform);

            // ---- Navigation Arrows ----
            BuildNavigationArrows(panel.transform);

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

            // Positioned above the card, left-aligned
            RectTransform rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.88f);
            rect.anchorMax = new Vector2(0.48f, 0.98f);
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

        private void BuildThemeCard(Transform parent)
        {
            // The card sits on the left ~60% of the screen
            // Reference layout: card takes roughly left 62% of width, ~10%-85% height
            GameObject cardObj = new GameObject("ThemeCard");
            cardObj.transform.SetParent(parent, false);

            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.04f, 0.04f);
            cardRect.anchorMax = new Vector2(0.58f, 0.87f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

            // Card frame (polaroid style white border)
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

            // --- Media area (video/image fills most of the card) ---
            // In the reference: image takes up ~top 80% of card, name at bottom ~20%
            GameObject mediaArea = new GameObject("MediaArea");
            mediaArea.transform.SetParent(cardObj.transform, false);

            RectTransform mediaRect = mediaArea.AddComponent<RectTransform>();
            mediaRect.anchorMin = new Vector2(0.04f, 0.15f);
            mediaRect.anchorMax = new Vector2(0.96f, 0.96f);
            mediaRect.offsetMin = Vector2.zero;
            mediaRect.offsetMax = Vector2.zero;

            // Static image fallback (dark bg)
            Image mediaImg = mediaArea.AddComponent<Image>();
            mediaImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            mediaImg.type = Image.Type.Simple;
            mediaImg.preserveAspect = false;

            // Video overlay (RawImage) — fills the media area
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
            titleRect.anchorMax = new Vector2(0.96f, 0.15f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            GameObject titleTextObj = new GameObject("Text");
            titleTextObj.transform.SetParent(titleArea.transform, false);

            RectTransform ttRect = titleTextObj.AddComponent<RectTransform>();
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;
            ttRect.offsetMin = new Vector2(5, 0);
            ttRect.offsetMax = new Vector2(-5, 0);

            var cardTitle = titleTextObj.AddComponent<TextMeshProUGUI>();
            cardTitle.text = "SCENARIO NAME";
            cardTitle.fontSize = 44;
            cardTitle.alignment = TextAlignmentOptions.Center;
            cardTitle.color = detailTextColor;
            cardTitle.fontStyle = FontStyles.Bold;
            cardTitle.enableAutoSizing = true;
            cardTitle.fontSizeMin = 24;
            cardTitle.fontSizeMax = 52;
            if (titleFont != null) cardTitle.font = titleFont;
        }

        private void BuildDetailPanel(Transform parent)
        {
            // Notes panel on the right side — notebook paper style
            // Reference: right ~35% of screen, slightly overlapping vertically
            GameObject detailObj = new GameObject("DetailPanel");
            detailObj.transform.SetParent(parent, false);

            RectTransform detailRect = detailObj.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.60f, 0.06f);
            detailRect.anchorMax = new Vector2(0.97f, 0.92f);
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = Vector2.zero;

            Image detailBg = detailObj.AddComponent<Image>();
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

            // Content container with padding
            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(detailObj.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.08f, 0.05f);
            contentRect.anchorMax = new Vector2(0.92f, 0.95f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            // Vertical layout for the text sections
            VerticalLayoutGroup vlg = contentObj.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(5, 5, 10, 10);

            // --- NAME Section ---
            BuildDetailLabel(contentObj.transform, "NameLabel", "NAME:", 32);
            BuildDetailValue(contentObj.transform, "NameValue", "ZOMBIE APOCALYPSE", 28, 45);

            // Spacer
            BuildSpacer(contentObj.transform, 10);

            // --- TRAITS Section ---
            BuildDetailLabel(contentObj.transform, "TraitsLabel", "TRAITS:", 32);
            BuildDetailValue(contentObj.transform, "TraitsValue", "SCAVENGER\nMELEE EXPERT", 26, 100);

            // Spacer
            BuildSpacer(contentObj.transform, 10);

            // --- BIO Section ---
            BuildDetailLabel(contentObj.transform, "BioLabel", "BIO:", 32);
            BuildDetailValue(contentObj.transform, "BioValue", "Theme description goes here.", 24, 250);
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
            text.textWrappingMode = TextWrappingModes.Normal;
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

        private void BuildNavigationArrows(Transform parent)
        {
            // Left arrow — overlaid on left edge of the card
            BuildArrow(parent, "LeftArrow", new Vector2(0.04f, 0.38f), new Vector2(0.08f, 0.56f), true);

            // Right arrow — overlaid on right edge of the card
            BuildArrow(parent, "RightArrow", new Vector2(0.54f, 0.38f), new Vector2(0.58f, 0.56f), false);
        }

        private void BuildArrow(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, bool flipHorizontal)
        {
            GameObject arrowObj = new GameObject(name);
            arrowObj.transform.SetParent(parent, false);

            RectTransform rect = arrowObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image arrowImg = arrowObj.AddComponent<Image>();
            if (arrowSprite != null)
            {
                arrowImg.sprite = arrowSprite;
                arrowImg.type = Image.Type.Simple;
                arrowImg.preserveAspect = true;
                arrowImg.color = Color.white;
            }
            else
            {
                arrowImg.color = new Color(0.9f, 0.9f, 0.9f, 0.8f);
            }

            // Flip left arrow horizontally
            if (flipHorizontal)
            {
                rect.localScale = new Vector3(-1f, 1f, 1f);
            }

            Button btn = arrowObj.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            btn.colors = colors;
        }

        private void BuildConfirmButton(Transform parent)
        {
            GameObject btnObj = new GameObject("ConfirmButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.78f, 0.01f);
            rect.anchorMax = new Vector2(0.97f, 0.07f);
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
            currentIndex = 0;

            SetupVideoPlayer();
            WireCardButtons();
            DisplayCurrentTheme();

            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", false);

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Shown with {availableThemes.Count} themes.");
        }

        public void Hide()
        {
            StopVideo();
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Video Player Setup (single player for the displayed card)
        // -------------------------------------------------------------------------
        private void SetupVideoPlayer()
        {
            CleanupVideoPlayer();

            if (panel == null) return;

            Transform cardTransform = panel.transform.Find("ThemeCard");
            if (cardTransform == null) return;

            Transform mediaArea = cardTransform.Find("MediaArea");
            Transform videoDisplay = mediaArea?.Find("VideoDisplay");
            videoImage = videoDisplay?.GetComponent<RawImage>();
            cardMediaImage = mediaArea?.GetComponent<Image>();

            // Find text references
            Transform titleTextTransform = cardTransform.Find("CardTitle/Text");
            scenarioNameText = titleTextTransform?.GetComponent<TextMeshProUGUI>();

            Transform detailPanel = panel.transform.Find("DetailPanel/Content");
            if (detailPanel != null)
            {
                Transform nameVal = detailPanel.Find("NameValue");
                detailNameText = nameVal?.GetComponent<TextMeshProUGUI>();

                Transform traitsVal = detailPanel.Find("TraitsValue");
                detailTraitsText = traitsVal?.GetComponent<TextMeshProUGUI>();

                Transform bioVal = detailPanel.Find("BioValue");
                detailBioText = bioVal?.GetComponent<TextMeshProUGUI>();
            }

            // Create RenderTexture + VideoPlayer
            renderTexture = new RenderTexture(832, 464, 0);
            renderTexture.Create();

            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            if (videoImage != null)
                videoImage.texture = renderTexture;
        }

        private void PlayVideo(VideoClip clip)
        {
            if (videoPlayer == null || clip == null) return;

            videoPlayer.clip = clip;
            videoPlayer.isLooping = true;
            videoPlayer.Play();

            if (videoImage != null)
                videoImage.enabled = true;
        }

        private void StopVideo()
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
                videoPlayer.Stop();

            if (videoImage != null)
                videoImage.enabled = false;
        }

        // -------------------------------------------------------------------------
        // Display Current Theme
        // -------------------------------------------------------------------------
        private void DisplayCurrentTheme()
        {
            if (availableThemes.Count == 0) return;
            if (currentIndex < 0 || currentIndex >= availableThemes.Count)
                currentIndex = 0;

            GameThemeSO theme = availableThemes[currentIndex];
            if (theme == null) return;

            // Update card title
            if (scenarioNameText != null)
                scenarioNameText.text = theme.ThemeName.ToUpper();

            // Update card media (static image fallback)
            if (cardMediaImage != null)
            {
                if (theme.ThemeIcon != null)
                {
                    cardMediaImage.sprite = theme.ThemeIcon;
                    cardMediaImage.color = Color.white;
                    cardMediaImage.preserveAspect = false;
                }
                else
                {
                    cardMediaImage.sprite = null;
                    cardMediaImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                }
            }

            // Update detail panel
            if (detailNameText != null)
                detailNameText.text = theme.ThemeName.ToUpper();

            if (detailTraitsText != null)
            {
                if (theme.Traits != null && theme.Traits.Length > 0)
                    detailTraitsText.text = string.Join("\n", theme.Traits).ToUpper();
                else
                    detailTraitsText.text = "NONE";
            }

            if (detailBioText != null)
                detailBioText.text = !string.IsNullOrEmpty(theme.Description)
                    ? theme.Description
                    : "No description available.";

            // Play video
            StopVideo();
            if (theme.PreviewVideo != null)
                PlayVideo(theme.PreviewVideo);

            // Auto-select the displayed theme
            selectedTheme = theme;
            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", true);

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Displaying: {theme.ThemeName} ({currentIndex + 1}/{availableThemes.Count})");
        }

        // -------------------------------------------------------------------------
        // Navigation
        // -------------------------------------------------------------------------
        private void NavigateLeft()
        {
            if (availableThemes.Count == 0) return;
            currentIndex--;
            if (currentIndex < 0)
                currentIndex = availableThemes.Count - 1;
            DisplayCurrentTheme();
        }

        private void NavigateRight()
        {
            if (availableThemes.Count == 0) return;
            currentIndex++;
            if (currentIndex >= availableThemes.Count)
                currentIndex = 0;
            DisplayCurrentTheme();
        }

        // -------------------------------------------------------------------------
        // Button Wiring
        // -------------------------------------------------------------------------
        private void WireCardButtons()
        {
            if (panel == null) return;

            // Left arrow
            Button leftBtn = UIBuilderUtils.FindButton(panel, "LeftArrow");
            if (leftBtn != null)
            {
                leftBtn.onClick.RemoveAllListeners();
                leftBtn.onClick.AddListener(NavigateLeft);
            }

            // Right arrow
            Button rightBtn = UIBuilderUtils.FindButton(panel, "RightArrow");
            if (rightBtn != null)
            {
                rightBtn.onClick.RemoveAllListeners();
                rightBtn.onClick.AddListener(NavigateRight);
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
