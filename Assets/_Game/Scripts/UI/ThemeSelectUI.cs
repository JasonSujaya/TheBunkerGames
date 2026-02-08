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
    /// Displays one full-screen theme card at a time with a 9:16 preview video,
    /// plus a detail panel (name, traits, bio) on the right side.
    /// Left/Right arrows cycle through available themes.
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
        private int currentIndex = 0;
        private GameThemeSO selectedTheme;
        public GameThemeSO SelectedTheme => selectedTheme;

        // Runtime UI references
        private Image cardImage;
        private RawImage videoImage;
        private VideoPlayer videoPlayer;
        private RenderTexture videoRenderTexture;
        private TMP_Text cardTitleText;
        private TMP_Text detailNameText;
        private TMP_Text detailTraitsText;
        private TMP_Text detailBioText;
        private GameObject detailPanel;

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
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
                DestroyImmediate(videoRenderTexture);
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

            // ---- Theme Card (left/center, big) ----
            BuildThemeCard(panel.transform);

            // ---- Detail Panel (right side) ----
            BuildDetailPanel(panel.transform);

            // ---- Navigation Arrows (inside the card) ----
            Transform cardTransform = panel.transform.Find("ThemeCard");
            if (cardTransform != null)
                BuildNavigationArrows(cardTransform);

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

            // Positioned at top-center above the card area
            RectTransform rect = bannerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.85f);
            rect.anchorMax = new Vector2(0.58f, 0.95f);
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
            text.fontSize = 36;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 20;
            text.fontSizeMax = 42;
            if (titleFont != null) text.font = titleFont;
        }

        private void BuildThemeCard(Transform parent)
        {
            // Card border / frame (polaroid style — white weathered card)
            // Image fills most of the card, title bar at the bottom
            GameObject cardObj = new GameObject("ThemeCard");
            cardObj.transform.SetParent(parent, false);

            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.04f, 0.06f);
            cardRect.anchorMax = new Vector2(0.62f, 0.84f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;

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

            // Video / Image area — fills nearly the entire card interior
            // Thin inset so the card frame border peeks through around edges
            GameObject mediaArea = new GameObject("MediaArea");
            mediaArea.transform.SetParent(cardObj.transform, false);

            RectTransform mediaRect = mediaArea.AddComponent<RectTransform>();
            mediaRect.anchorMin = new Vector2(0.03f, 0.14f);
            mediaRect.anchorMax = new Vector2(0.97f, 0.97f);
            mediaRect.offsetMin = Vector2.zero;
            mediaRect.offsetMax = Vector2.zero;

            // Fallback static image — stretch to fill the entire media area
            cardImage = mediaArea.AddComponent<Image>();
            cardImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            cardImage.type = Image.Type.Simple;
            cardImage.preserveAspect = false;

            // Video overlay (RawImage on top of the static image)
            GameObject videoObj = new GameObject("VideoDisplay");
            videoObj.transform.SetParent(mediaArea.transform, false);

            RectTransform videoRect = videoObj.AddComponent<RectTransform>();
            videoRect.anchorMin = Vector2.zero;
            videoRect.anchorMax = Vector2.one;
            videoRect.offsetMin = Vector2.zero;
            videoRect.offsetMax = Vector2.zero;

            videoImage = videoObj.AddComponent<RawImage>();
            videoImage.color = Color.white;
            videoImage.enabled = false;

            // AspectRatioFitter — envelope parent so video fills the area
            // Videos are 832x464 (landscape ~16:9), EnvelopeParent crops to fill
            AspectRatioFitter aspect = videoObj.AddComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspect.aspectRatio = 16f / 9f;

            // Theme title — bottom strip of the card with dark background
            GameObject titleArea = new GameObject("CardTitle");
            titleArea.transform.SetParent(cardObj.transform, false);

            RectTransform titleRect = titleArea.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.03f, 0.01f);
            titleRect.anchorMax = new Vector2(0.97f, 0.14f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Image titleBg = titleArea.AddComponent<Image>();
            titleBg.color = new Color(0, 0, 0, 0.85f);

            GameObject titleTextObj = new GameObject("Text");
            titleTextObj.transform.SetParent(titleArea.transform, false);

            RectTransform ttRect = titleTextObj.AddComponent<RectTransform>();
            ttRect.anchorMin = Vector2.zero;
            ttRect.anchorMax = Vector2.one;
            ttRect.offsetMin = new Vector2(10, 0);
            ttRect.offsetMax = new Vector2(-10, 0);

            var cardTitle = titleTextObj.AddComponent<TextMeshProUGUI>();
            cardTitle.text = "SCENARIO NAME";
            cardTitle.fontSize = 40;
            cardTitle.alignment = TextAlignmentOptions.Center;
            cardTitle.color = Color.white;
            cardTitle.fontStyle = FontStyles.Bold;
            cardTitle.enableAutoSizing = true;
            cardTitle.fontSizeMin = 22;
            cardTitle.fontSizeMax = 48;
            if (titleFont != null) cardTitle.font = titleFont;
            cardTitleText = cardTitle;
        }

        private void BuildDetailPanel(Transform parent)
        {
            detailPanel = new GameObject("DetailPanel");
            detailPanel.transform.SetParent(parent, false);

            // Notes panel on the right side — matches reference layout
            RectTransform detailRect = detailPanel.AddComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.64f, 0.06f);
            detailRect.anchorMax = new Vector2(0.98f, 0.92f);
            detailRect.offsetMin = Vector2.zero;
            detailRect.offsetMax = Vector2.zero;

            Image bg = detailPanel.AddComponent<Image>();
            if (notesSprite != null)
            {
                bg.sprite = notesSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = detailPanelBg;
            }

            // NAME: section — top of notes, large bold header + value below
            GameObject nameObj = new GameObject("NameLabel");
            nameObj.transform.SetParent(detailPanel.transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.08f, 0.80f);
            nameRect.anchorMax = new Vector2(0.92f, 0.96f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "NAME:";
            nameText.fontSize = 26;
            nameText.alignment = TextAlignmentOptions.TopLeft;
            nameText.color = detailTextColor;
            nameText.fontStyle = FontStyles.Bold;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 16;
            nameText.fontSizeMax = 32;
            if (titleFont != null) nameText.font = titleFont;
            detailNameText = nameText;

            // TRAITS: section — middle area
            GameObject traitsObj = new GameObject("TraitsLabel");
            traitsObj.transform.SetParent(detailPanel.transform, false);

            RectTransform traitsRect = traitsObj.AddComponent<RectTransform>();
            traitsRect.anchorMin = new Vector2(0.08f, 0.50f);
            traitsRect.anchorMax = new Vector2(0.92f, 0.78f);
            traitsRect.offsetMin = Vector2.zero;
            traitsRect.offsetMax = Vector2.zero;

            var traitsText = traitsObj.AddComponent<TextMeshProUGUI>();
            traitsText.text = "TRAITS:";
            traitsText.fontSize = 22;
            traitsText.alignment = TextAlignmentOptions.TopLeft;
            traitsText.color = detailTextColor;
            traitsText.fontStyle = FontStyles.Bold;
            if (titleFont != null) traitsText.font = titleFont;
            detailTraitsText = traitsText;

            // BIO: header
            GameObject bioHeaderObj = new GameObject("BioHeader");
            bioHeaderObj.transform.SetParent(detailPanel.transform, false);

            RectTransform bioHeaderRect = bioHeaderObj.AddComponent<RectTransform>();
            bioHeaderRect.anchorMin = new Vector2(0.08f, 0.42f);
            bioHeaderRect.anchorMax = new Vector2(0.92f, 0.50f);
            bioHeaderRect.offsetMin = Vector2.zero;
            bioHeaderRect.offsetMax = Vector2.zero;

            var bioHeader = bioHeaderObj.AddComponent<TextMeshProUGUI>();
            bioHeader.text = "BIO:";
            bioHeader.fontSize = 22;
            bioHeader.alignment = TextAlignmentOptions.MidlineLeft;
            bioHeader.color = detailTextColor;
            bioHeader.fontStyle = FontStyles.Bold;
            if (titleFont != null) bioHeader.font = titleFont;

            // BIO content — lower section with wrapping text
            GameObject bioObj = new GameObject("BioContent");
            bioObj.transform.SetParent(detailPanel.transform, false);

            RectTransform bioRect = bioObj.AddComponent<RectTransform>();
            bioRect.anchorMin = new Vector2(0.08f, 0.06f);
            bioRect.anchorMax = new Vector2(0.92f, 0.42f);
            bioRect.offsetMin = Vector2.zero;
            bioRect.offsetMax = Vector2.zero;

            var bioText = bioObj.AddComponent<TextMeshProUGUI>();
            bioText.text = "";
            bioText.fontSize = 20;
            bioText.alignment = TextAlignmentOptions.TopLeft;
            bioText.color = detailTextColor;
            bioText.enableAutoSizing = true;
            bioText.fontSizeMin = 12;
            bioText.fontSizeMax = 26;
            if (subtitleFont != null) bioText.font = subtitleFont;
            detailBioText = bioText;
        }

        private void BuildNavigationArrows(Transform cardParent)
        {
            // Arrows are children of ThemeCard so they sit inside/on the card edges

            // Left arrow — left edge of card, vertically centered in media area
            GameObject leftBtn = new GameObject("LeftArrow");
            leftBtn.transform.SetParent(cardParent, false);

            RectTransform leftRect = leftBtn.AddComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0.01f, 0.45f);
            leftRect.anchorMax = new Vector2(0.08f, 0.65f);
            leftRect.offsetMin = Vector2.zero;
            leftRect.offsetMax = Vector2.zero;

            Image leftBg = leftBtn.AddComponent<Image>();
            leftBg.color = new Color(0.3f, 0.28f, 0.25f, 0.8f);
            leftBg.preserveAspect = true;
            if (arrowSprite != null) { leftBg.sprite = arrowSprite; leftBg.type = Image.Type.Simple; leftBg.color = Color.white; leftBg.transform.localScale = new Vector3(-1, 1, 1); }

            Button leftButton = leftBtn.AddComponent<Button>();
            var lColors = leftButton.colors;
            lColors.highlightedColor = new Color(0.45f, 0.4f, 0.35f, 1f);
            lColors.pressedColor = new Color(0.2f, 0.18f, 0.15f, 1f);
            leftButton.colors = lColors;

            GameObject leftText = new GameObject("Text");
            leftText.transform.SetParent(leftBtn.transform, false);
            RectTransform ltRect = leftText.AddComponent<RectTransform>();
            ltRect.anchorMin = Vector2.zero;
            ltRect.anchorMax = Vector2.one;
            ltRect.offsetMin = Vector2.zero;
            ltRect.offsetMax = Vector2.zero;
            var lt = leftText.AddComponent<TextMeshProUGUI>();
            lt.text = arrowSprite == null ? "<" : "";
            lt.fontSize = 36;
            lt.alignment = TextAlignmentOptions.Center;
            lt.color = Color.white;
            lt.fontStyle = FontStyles.Bold;
            if (titleFont != null) lt.font = titleFont;

            // Right arrow — right edge of card, vertically centered in media area
            GameObject rightBtn = new GameObject("RightArrow");
            rightBtn.transform.SetParent(cardParent, false);

            RectTransform rightRect = rightBtn.AddComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(0.92f, 0.45f);
            rightRect.anchorMax = new Vector2(0.99f, 0.65f);
            rightRect.offsetMin = Vector2.zero;
            rightRect.offsetMax = Vector2.zero;

            Image rightBg = rightBtn.AddComponent<Image>();
            rightBg.color = new Color(0.3f, 0.28f, 0.25f, 0.8f);
            rightBg.preserveAspect = true;
            if (arrowSprite != null) { rightBg.sprite = arrowSprite; rightBg.type = Image.Type.Simple; rightBg.color = Color.white; }

            Button rightButton = rightBtn.AddComponent<Button>();
            var rColors = rightButton.colors;
            rColors.highlightedColor = new Color(0.45f, 0.4f, 0.35f, 1f);
            rColors.pressedColor = new Color(0.2f, 0.18f, 0.15f, 1f);
            rightButton.colors = rColors;

            GameObject rightText = new GameObject("Text");
            rightText.transform.SetParent(rightBtn.transform, false);
            RectTransform rtRect = rightText.AddComponent<RectTransform>();
            rtRect.anchorMin = Vector2.zero;
            rtRect.anchorMax = Vector2.one;
            rtRect.offsetMin = Vector2.zero;
            rtRect.offsetMax = Vector2.zero;
            var rt = rightText.AddComponent<TextMeshProUGUI>();
            rt.text = arrowSprite == null ? ">" : "";
            rt.fontSize = 36;
            rt.alignment = TextAlignmentOptions.Center;
            rt.color = Color.white;
            rt.fontStyle = FontStyles.Bold;
            if (titleFont != null) rt.font = titleFont;
        }

        private void BuildConfirmButton(Transform parent)
        {
            GameObject btnObj = new GameObject("ConfirmButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.64f, 0.02f);
            rect.anchorMax = new Vector2(0.97f, 0.08f);
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

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = "CONFIRM";
            text.fontSize = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16;
            text.fontSizeMax = 32;
            if (titleFont != null) text.font = titleFont;
        }

        // -------------------------------------------------------------------------
        // Show / Hide
        // -------------------------------------------------------------------------
        public void Show()
        {
            if (canvasRoot == null) return;
            canvasRoot.SetActive(true);
            currentIndex = 0;
            selectedTheme = null;

            CacheUIReferences();
            SetupVideoPlayer();
            WireButtons();

            if (availableThemes.Count > 0)
                DisplayTheme(0);
            else
                ClearDisplay();

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Shown with {availableThemes.Count} themes.");
        }

        public void Hide()
        {
            StopVideo();
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Cache UI References
        // -------------------------------------------------------------------------
        private void CacheUIReferences()
        {
            if (panel == null) return;

            // Card elements
            Transform cardTransform = panel.transform.Find("ThemeCard");
            if (cardTransform != null)
            {
                Transform mediaArea = cardTransform.Find("MediaArea");
                if (mediaArea != null)
                {
                    cardImage = mediaArea.GetComponent<Image>();
                    Transform videoDisplay = mediaArea.Find("VideoDisplay");
                    if (videoDisplay != null)
                        videoImage = videoDisplay.GetComponent<RawImage>();
                }

                Transform cardTitle = cardTransform.Find("CardTitle/Text");
                if (cardTitle != null)
                    cardTitleText = cardTitle.GetComponent<TMP_Text>();
            }

            // Detail panel
            detailPanel = panel.transform.Find("DetailPanel")?.gameObject;
            if (detailPanel != null)
            {
                detailNameText = detailPanel.transform.Find("NameLabel")?.GetComponent<TMP_Text>();
                detailTraitsText = detailPanel.transform.Find("TraitsLabel")?.GetComponent<TMP_Text>();
                detailBioText = detailPanel.transform.Find("BioContent")?.GetComponent<TMP_Text>();
            }
        }

        // -------------------------------------------------------------------------
        // Video Player Setup
        // -------------------------------------------------------------------------
        private void SetupVideoPlayer()
        {
            // Create RenderTexture for video output
            if (videoRenderTexture == null)
            {
                videoRenderTexture = new RenderTexture(832, 464, 0); // 16:9 landscape
                videoRenderTexture.Create();
            }

            // Get or create VideoPlayer on this GameObject
            videoPlayer = GetComponent<VideoPlayer>();
            if (videoPlayer == null)
                videoPlayer = gameObject.AddComponent<VideoPlayer>();

            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            if (videoImage != null)
                videoImage.texture = videoRenderTexture;
        }

        private void PlayVideo(VideoClip clip)
        {
            if (videoPlayer == null || clip == null)
            {
                StopVideo();
                return;
            }

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
        // Button Wiring
        // -------------------------------------------------------------------------
        private void WireButtons()
        {
            // Left arrow (inside ThemeCard)
            Transform card = panel.transform.Find("ThemeCard");
            Button leftBtn = card?.Find("LeftArrow")?.GetComponent<Button>();
            if (leftBtn != null)
            {
                leftBtn.onClick.RemoveAllListeners();
                leftBtn.onClick.AddListener(PreviousTheme);
            }

            // Right arrow (inside ThemeCard)
            Button rightBtn = card?.Find("RightArrow")?.GetComponent<Button>();
            if (rightBtn != null)
            {
                rightBtn.onClick.RemoveAllListeners();
                rightBtn.onClick.AddListener(NextTheme);
            }

            // Confirm
            Button confirmBtn = UIBuilderUtils.FindButton(panel, "ConfirmButton");
            if (confirmBtn != null)
            {
                confirmBtn.onClick.RemoveAllListeners();
                confirmBtn.onClick.AddListener(OnConfirm);
            }
            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", false);
        }

        // -------------------------------------------------------------------------
        // Navigation
        // -------------------------------------------------------------------------
        private void NextTheme()
        {
            if (availableThemes.Count == 0) return;
            currentIndex = (currentIndex + 1) % availableThemes.Count;
            DisplayTheme(currentIndex);
        }

        private void PreviousTheme()
        {
            if (availableThemes.Count == 0) return;
            currentIndex = (currentIndex - 1 + availableThemes.Count) % availableThemes.Count;
            DisplayTheme(currentIndex);
        }

        // -------------------------------------------------------------------------
        // Display Theme
        // -------------------------------------------------------------------------
        private void DisplayTheme(int index)
        {
            if (index < 0 || index >= availableThemes.Count) return;

            GameThemeSO theme = availableThemes[index];
            if (theme == null) return;

            selectedTheme = theme;
            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", true);

            // Card image (fallback when no video — stretches to fill)
            if (cardImage != null)
            {
                if (theme.ThemeIcon != null)
                {
                    cardImage.sprite = theme.ThemeIcon;
                    cardImage.type = Image.Type.Simple;
                    cardImage.preserveAspect = false;
                    cardImage.color = Color.white;
                }
                else
                {
                    cardImage.sprite = null;
                    cardImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
                }
            }

            // Video
            if (theme.PreviewVideo != null)
            {
                PlayVideo(theme.PreviewVideo);
            }
            else
            {
                StopVideo();
            }

            // Card title
            if (cardTitleText != null)
                cardTitleText.text = theme.ThemeName.ToUpper();

            // Detail panel
            if (detailNameText != null)
                detailNameText.text = $"NAME:\n{theme.ThemeName.ToUpper()}";

            if (detailTraitsText != null)
            {
                string traits = "TRAITS:\n";
                if (theme.Traits != null)
                {
                    foreach (var trait in theme.Traits)
                    {
                        if (!string.IsNullOrEmpty(trait))
                            traits += trait.ToUpper() + "\n";
                    }
                }
                detailTraitsText.text = traits.TrimEnd('\n');
            }

            if (detailBioText != null)
                detailBioText.text = !string.IsNullOrEmpty(theme.Description) ? theme.Description : "No description available.";

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Displaying: {theme.ThemeName} ({index + 1}/{availableThemes.Count})");
        }

        private void ClearDisplay()
        {
            if (cardImage != null) { cardImage.sprite = null; cardImage.color = new Color(0.3f, 0.3f, 0.3f, 1f); }
            if (cardTitleText != null) cardTitleText.text = "NO SCENARIOS";
            if (detailNameText != null) detailNameText.text = "NAME:";
            if (detailTraitsText != null) detailTraitsText.text = "TRAITS:";
            if (detailBioText != null) detailBioText.text = "No scenarios available.";
            StopVideo();
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

        [Button("Next", ButtonSizes.Small)]
        private void Debug_Next() => NextTheme();

        [Button("Previous", ButtonSizes.Small)]
        private void Debug_Previous() => PreviousTheme();
        #endif
    }
}
