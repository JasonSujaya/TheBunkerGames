using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles the ending screen (Game Over / Victory) with fade and restart options.
    /// </summary>
    public class EndingUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static EndingUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Canvas References")]
        #endif
        [Header("Canvas")]
        [SerializeField] private Canvas endingCanvas;
        [SerializeField] private CanvasGroup canvasGroup;

        #if ODIN_INSPECTOR
        [Title("UI Elements")]
        #endif
        [Header("Display Elements")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitButton;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset bodyFont;

        #if ODIN_INSPECTOR
        [Title("Animation Settings")]
        #endif
        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 1.0f;

        #if ODIN_INSPECTOR
        [Title("Visual Settings")]
        #endif
        [Header("Colors")]
        [SerializeField] private Color backgroundColor = Color.black;
        [SerializeField] private Color victoryColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Green
        [SerializeField] private Color defeatColor = new Color(0.8f, 0.2f, 0.2f, 1f);  // Red
        [SerializeField] private Color messageColor = new Color(0.9f, 0.9f, 0.9f, 1f); // White

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private bool isTransitioning;

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

            // Auto-get references if not assigned
            if (endingCanvas == null)
                endingCanvas = GetComponent<Canvas>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Start hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (endingCanvas != null)
                endingCanvas.enabled = false;
        }

        private void Start()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuitClicked);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Show the ending screen.
        /// </summary>
        /// <param name="survived">True if the player won, false if they lost.</param>
        /// <param name="message">Optional message to display below the title.</param>
        public void ShowEnding(bool survived, string message = "")
        {
            if (isTransitioning) return;

            // Ensure the GameObject is active
            gameObject.SetActive(true);
            StartCoroutine(ShowSequence(survived, message));
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
            if (endingCanvas != null)
                endingCanvas.enabled = false;
        }

        // -------------------------------------------------------------------------
        // Private Methods
        // -------------------------------------------------------------------------
        private IEnumerator ShowSequence(bool survived, string message)
        {
            isTransitioning = true;

            if (enableDebugLogs)
                Debug.Log($"[EndingUI] Showing Ending: Survived={survived}");

            // Setup UI
            SetupUI(survived, message);

            // Enable canvas
            if (endingCanvas != null)
                endingCanvas.enabled = true;

            // Fade in
            yield return StartCoroutine(FadeIn());

            isTransitioning = false;
        }

        private void SetupUI(bool survived, string message)
        {
            if (titleText != null)
            {
                titleText.text = survived ? "YOU SURVIVED" : "YOU PERISHED";
                titleText.color = survived ? victoryColor : defeatColor;
            }

            if (messageText != null)
            {
                messageText.text = message;
                messageText.color = messageColor;
            }

            if (backgroundImage != null)
                backgroundImage.color = backgroundColor;
        }

        private IEnumerator FadeIn()
        {
            if (canvasGroup == null) yield break;

            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;
        }

        private void OnRestartClicked()
        {
            if (enableDebugLogs) Debug.Log("[EndingUI] Restart clicked. Reloading scene...");
            // Reload the current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnQuitClicked()
        {
            if (enableDebugLogs) Debug.Log("[EndingUI] Quit clicked. Quitting application...");
            Application.Quit();
        }

        // -------------------------------------------------------------------------
        // Debug / Editor
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Actions")]
        [Button("Test Win", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        private void Debug_TestWin()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test.");
                return;
            }
            ShowEnding(true, "Congratulations! You survived another day in the bunker.");
        }

        [Button("Test Loss", ButtonSizes.Large)]
        [GUIColor(1, 0, 0)]
        private void Debug_TestLoss()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test.");
                return;
            }
            ShowEnding(false, "The bunker claims another soul.");
        }

        [Button("Auto Setup", ButtonSizes.Medium)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        public void AutoSetup()
        {
            gameObject.name = "[EndingUI]";

            // Get or add Canvas
            endingCanvas = GetComponent<Canvas>();
            if (endingCanvas == null)
                endingCanvas = gameObject.AddComponent<Canvas>();
            endingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            endingCanvas.sortingOrder = 1000; // On top of everything, even transition UI

            // Get or add CanvasGroup
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Ensure CanvasScaler
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Ensure GraphicRaycaster
            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            // Load fonts from Resources if not assigned
            if (titleFont == null)
                titleFont = Resources.Load<TMP_FontAsset>("StoryScript-Regular SDF");
            if (bodyFont == null)
                bodyFont = Resources.Load<TMP_FontAsset>("Barriecito-Regular SDF");

            // --- CREATE OR FIND: Background ---
            var backgroundTransform = transform.Find("Background");
            if (backgroundTransform == null)
            {
                var bgGO = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgGO.transform.SetParent(transform, false);
                bgGO.transform.SetAsFirstSibling();
                backgroundTransform = bgGO.transform;
            }
            backgroundImage = backgroundTransform.GetComponent<Image>();
            if (backgroundImage != null)
            {
                var rt = backgroundImage.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                backgroundImage.color = backgroundColor;
            }

            // --- CREATE OR FIND: TitleText ---
            var titleTransform = transform.Find("TitleText");
            if (titleTransform == null)
            {
                var titleGO = new GameObject("TitleText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                titleGO.transform.SetParent(transform, false);
                titleTransform = titleGO.transform;
            }
            titleText = titleTransform.GetComponent<TMP_Text>();
            if (titleText != null)
            {
                titleText.text = "YOU SURVIVED";
                titleText.fontSize = 96;
                titleText.alignment = TextAlignmentOptions.Center;
                titleText.color = victoryColor;
                if (titleFont != null) titleText.font = titleFont;
                var rt = titleText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 150);
                rt.sizeDelta = new Vector2(1000, 150);
            }

            // --- CREATE OR FIND: MessageText ---
            var messageTransform = transform.Find("MessageText");
            if (messageTransform == null)
            {
                var messageGO = new GameObject("MessageText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                messageGO.transform.SetParent(transform, false);
                messageTransform = messageGO.transform;
            }
            messageText = messageTransform.GetComponent<TMP_Text>();
            if (messageText != null)
            {
                messageText.text = "Congratulations...";
                messageText.fontSize = 32;
                messageText.alignment = TextAlignmentOptions.Center;
                messageText.color = messageColor;
                if (bodyFont != null) messageText.font = bodyFont;
                var rt = messageText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 0);
                rt.sizeDelta = new Vector2(800, 100);
            }

            // --- Buttons Panel ---
            var buttonsPanelTransform = transform.Find("ButtonsPanel");
            if (buttonsPanelTransform == null)
            {
                var panelGO = new GameObject("ButtonsPanel", typeof(RectTransform));
                panelGO.transform.SetParent(transform, false);
                buttonsPanelTransform = panelGO.transform;
                var rt = panelGO.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, -200);
                rt.sizeDelta = new Vector2(600, 100);
            }

            // --- Restart Button ---
            CreateButton("RestartButton", "RESTART", buttonsPanelTransform, new Vector2(-150, 0), out restartButton);

            // --- Quit Button ---
            CreateButton("QuitButton", "QUIT", buttonsPanelTransform, new Vector2(150, 0), out quitButton);

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif

            Debug.Log("[EndingUI] Auto Setup Complete. All UI elements created and wired.");
        }

        private void CreateButton(string name, string text, Transform parent, Vector2 anchoredPos, out Button buttonRef)
        {
            var btnTransform = parent.Find(name);
            if (btnTransform == null)
            {
                var btnGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnGO.transform.SetParent(parent, false);
                btnTransform = btnGO.transform;
            }
            
            buttonRef = btnTransform.GetComponent<Button>();
            var img = btnTransform.GetComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            var rt = btnTransform.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(240, 60);

            // Text
            var txtTransform = btnTransform.Find("Text");
            if (txtTransform == null)
            {
                var txtGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(btnTransform, false);
                txtTransform = txtGO.transform;
            }
            var tmp = txtTransform.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            if (bodyFont != null) tmp.font = bodyFont;
            
            var txtRt = txtTransform.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
        }
    }
}
