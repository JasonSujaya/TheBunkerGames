using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles full-screen fading interactions (Fade In / Fade Out).
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreenFader : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private float defaultDuration = 0.5f;
        [SerializeField] private bool fadeOnStart = true;
        [SerializeField] private Color fadeColor = Color.black;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private CanvasGroup canvasGroup;
        private Image fadeImage;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            fadeImage = GetComponent<Image>();
            
            if (fadeImage != null)
                fadeImage.color = fadeColor;

            // Ensure we block raycasts when opaque
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false; // Allow clicks by default
                canvasGroup.alpha = 0f; // Start transparent
            }
        }

        private void Start()
        {
            if (fadeOnStart)
            {
                // Instant black then fade in
                SetAlpha(1f);
                FadeIn();
            }
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void FadeIn(Action onComplete = null) => FadeIn(defaultDuration, onComplete);
        public void FadeIn(float duration, Action onComplete = null)
        {
            StartCoroutine(FadeRoutine(1f, 0f, duration, onComplete));
        }

        public void FadeOut(Action onComplete = null) => FadeOut(defaultDuration, onComplete);
        public void FadeOut(float duration, Action onComplete = null)
        {
            StartCoroutine(FadeRoutine(0f, 1f, duration, onComplete));
        }

        public void InstantBlack()
        {
            SetAlpha(1f);
        }

        public void InstantClear()
        {
            SetAlpha(0f);
        }

        // -------------------------------------------------------------------------
        // Coroutine
        // -------------------------------------------------------------------------
        private IEnumerator FadeRoutine(float startAlpha, float endAlpha, float duration, Action onComplete)
        {
            float elapsed = 0f;
            SetAlpha(startAlpha);

            // Block input while fading out (going to black) or while opaque
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = (endAlpha > 0.5f); 

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                SetAlpha(Mathf.Lerp(startAlpha, endAlpha, t));
                yield return null;
            }

            SetAlpha(endAlpha);
            
            // Ensure input is unblocked if we faded to clear
            if (canvasGroup != null)
                canvasGroup.blocksRaycasts = (endAlpha > 0.5f);

            onComplete?.Invoke();
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
                // If practically invisible, disable blocksRaycasts
                canvasGroup.blocksRaycasts = (alpha > 0.1f);
            }
        }
        
        // -------------------------------------------------------------------------
        // Auto Setup / Editor
        // -------------------------------------------------------------------------
        #if UNITY_EDITOR
        [ContextMenu("Auto Setup Fader")]
        public void AutoSetupFader()
        {
             if (GetComponent<CanvasGroup>() == null) gameObject.AddComponent<CanvasGroup>();
             if (GetComponent<Image>() == null) gameObject.AddComponent<Image>().color = Color.black;
             
             // Ensure it stretches
             RectTransform rect = GetComponent<RectTransform>();
             if (rect != null)
             {
                 rect.anchorMin = Vector2.zero;
                 rect.anchorMax = Vector2.one;
                 rect.offsetMin = Vector2.zero;
                 rect.offsetMax = Vector2.zero;
             }
        }
        #endif
    }
}
