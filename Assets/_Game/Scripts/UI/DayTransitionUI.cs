using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles the day transition screen with fade and event display.
    /// Shows "Processing..." or custom messages during day advancement,
    /// then fades to the next day.
    /// </summary>
    public class DayTransitionUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static DayTransitionUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Canvas References")]
        #endif
        [Header("Canvas")]
        [SerializeField] private Canvas transitionCanvas;
        [SerializeField] private CanvasGroup canvasGroup;

        #if ODIN_INSPECTOR
        [Title("UI Elements")]
        #endif
        [Header("Display Elements")]
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text eventText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject loadingIndicator;

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset bodyFont;

        #if ODIN_INSPECTOR
        [Title("Animation Settings")]
        #endif
        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float displayDuration = 2.0f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float minimumDisplayTime = 1.5f;

        #if ODIN_INSPECTOR
        [Title("Visual Settings")]
        #endif
        [Header("Colors")]
        [SerializeField] private Color backgroundColor = Color.black;  // Pure black
        [SerializeField] private Color textColor = new Color(0.9f, 0.85f, 0.7f, 1f);  // Warm off-white
        [SerializeField] private Color accentColor = new Color(0.8f, 0.3f, 0.2f, 1f);  // Warning red
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private bool isTransitioning;
        private Action onTransitionComplete;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public bool IsTransitioning => isTransitioning;

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
            if (transitionCanvas == null)
                transitionCanvas = GetComponent<Canvas>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Start hidden
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            if (transitionCanvas != null)
                transitionCanvas.enabled = false;
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Show the transition screen with fade animation.
        /// </summary>
        /// <param name="fromDay">The day we're transitioning FROM</param>
        /// <param name="toDay">The day we're transitioning TO</param>
        /// <param name="statusMessage">Optional status message (e.g., "Processing actions...")</param>
        /// <param name="onComplete">Callback when transition is complete</param>
        public void ShowTransition(int fromDay, int toDay, string statusMessage = null, Action onComplete = null)
        {
            if (isTransitioning)
            {
                if (enableDebugLogs) Debug.LogWarning("[DayTransitionUI] Already transitioning!");
                return;
            }

            // Ensure the GameObject is active
            gameObject.SetActive(true);

            onTransitionComplete = onComplete;
            StartCoroutine(TransitionSequence(fromDay, toDay, statusMessage));
        }

        /// <summary>
        /// Update the status text during transition (e.g., when LLM responds).
        /// </summary>
        public void UpdateStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        /// <summary>
        /// Update the event text to show what's happening.
        /// </summary>
        public void UpdateEvent(string eventMessage)
        {
            if (eventText != null)
            {
                eventText.text = eventMessage;
                eventText.gameObject.SetActive(!string.IsNullOrEmpty(eventMessage));
            }
        }

        /// <summary>
        /// Complete the transition immediately (call after LLM response is received).
        /// </summary>
        public void CompleteTransition()
        {
            if (!isTransitioning) return;
            
            // Signal to end the wait early
            StopAllCoroutines();
            StartCoroutine(FadeOut());
        }

        // -------------------------------------------------------------------------
        // Transition Sequence
        // -------------------------------------------------------------------------
        private IEnumerator TransitionSequence(int fromDay, int toDay, string statusMessage)
        {
            isTransitioning = true;

            if (enableDebugLogs)
                Debug.Log($"[DayTransitionUI] Starting transition: Day {fromDay} -> Day {toDay}");

            // Setup UI
            SetupUI(fromDay, toDay, statusMessage);

            // Enable canvas
            if (transitionCanvas != null)
                transitionCanvas.enabled = true;

            // Fade in
            yield return StartCoroutine(FadeIn());

            // Wait for minimum display time
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            yield return StartCoroutine(FadeOut());
        }

        private void SetupUI(int fromDay, int toDay, string statusMessage)
        {
            if (dayText != null)
                dayText.text = $"DAY {toDay}";

            if (statusText != null)
                statusText.text = statusMessage ?? "Day is transitioning...";

            if (eventText != null)
                eventText.gameObject.SetActive(false);

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

        private IEnumerator FadeOut()
        {
            if (canvasGroup == null) yield break;

            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            if (transitionCanvas != null)
                transitionCanvas.enabled = false;

            isTransitioning = false;

            if (enableDebugLogs)
                Debug.Log("[DayTransitionUI] Transition complete.");

            onTransitionComplete?.Invoke();
            onTransitionComplete = null;
        }

        // -------------------------------------------------------------------------
        // Debug / Editor
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Actions")]
        [Button("Test Transition (Day 1 -> 2)", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        private void Debug_TestTransition()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test.");
                return;
            }
            ShowTransition(1, 2, "Testing transition...", () => Debug.Log("[Test] Transition callback fired!"));
        }

        [Button("Auto Setup", ButtonSizes.Medium)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        public void AutoSetup()
        {
            gameObject.name = "[DayTransitionUI]";

            // Get or add Canvas
            transitionCanvas = GetComponent<Canvas>();
            if (transitionCanvas == null)
                transitionCanvas = gameObject.AddComponent<Canvas>();
            transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            transitionCanvas.sortingOrder = 999; // On top of everything

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

            // --- CREATE OR FIND: DayText ---
            var dayTextTransform = transform.Find("DayText");
            if (dayTextTransform == null)
            {
                var dayGO = new GameObject("DayText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                dayGO.transform.SetParent(transform, false);
                dayTextTransform = dayGO.transform;
            }
            dayText = dayTextTransform.GetComponent<TMP_Text>();
            if (dayText != null)
            {
                dayText.text = "DAY 1";
                dayText.fontSize = 72;
                dayText.alignment = TextAlignmentOptions.Center;
                dayText.color = textColor;
                if (titleFont != null) dayText.font = titleFont;
                var rt = dayText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 100);
                rt.sizeDelta = new Vector2(600, 100);
            }

            // --- CREATE OR FIND: StatusText ---
            var statusTransform = transform.Find("StatusText");
            if (statusTransform == null)
            {
                var statusGO = new GameObject("StatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                statusGO.transform.SetParent(transform, false);
                statusTransform = statusGO.transform;
            }
            statusText = statusTransform.GetComponent<TMP_Text>();
            if (statusText != null)
            {
                statusText.text = "Day is transitioning...";
                statusText.fontSize = 28;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.color = textColor;
                if (bodyFont != null) statusText.font = bodyFont;
                var rt = statusText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 0);
                rt.sizeDelta = new Vector2(800, 50);
            }

            // --- CREATE OR FIND: EventText ---
            var eventTransform = transform.Find("EventText");
            if (eventTransform == null)
            {
                var eventGO = new GameObject("EventText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                eventGO.transform.SetParent(transform, false);
                eventTransform = eventGO.transform;
            }
            eventText = eventTransform.GetComponent<TMP_Text>();
            if (eventText != null)
            {
                eventText.text = "";
                eventText.fontSize = 24;
                eventText.alignment = TextAlignmentOptions.Center;
                eventText.color = accentColor;
                if (bodyFont != null) eventText.font = bodyFont;
                var rt = eventText.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, -60);
                rt.sizeDelta = new Vector2(800, 50);
                eventTransform.gameObject.SetActive(false);
            }

            // --- CREATE OR FIND: LoadingIndicator (hidden by default) ---
            var loadingTransform = transform.Find("LoadingIndicator");
            if (loadingTransform == null)
            {
                var loadingGO = new GameObject("LoadingIndicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                loadingGO.transform.SetParent(transform, false);
                loadingTransform = loadingGO.transform;
            }
            loadingIndicator = loadingTransform.gameObject;
            var loadingImage = loadingTransform.GetComponent<Image>();
            if (loadingImage != null)
            {
                var rt = loadingImage.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, -100);
                rt.sizeDelta = new Vector2(48, 48);
                loadingImage.color = new Color(textColor.r, textColor.g, textColor.b, 0.6f);
            }
            loadingIndicator.SetActive(false);  // Hidden by default

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif

            Debug.Log("[DayTransitionUI] Auto Setup Complete. All UI elements created and wired.");
        }
    }
}
