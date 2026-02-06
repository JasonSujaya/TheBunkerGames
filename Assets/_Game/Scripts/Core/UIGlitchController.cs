using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Manages UI shader effects globally.
    /// Scales glitch intensity based on A.N.G.E.L.'s Processing Level.
    /// Provides methods for dramatic momentary glitch bursts.
    /// </summary>
    public class UIGlitchController : MonoBehaviour
    {
        public static UIGlitchController Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private Material uiGlitchMaterial;
        [SerializeField] private AnimationCurve processingToIntensityCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        #if ODIN_INSPECTOR
        [Title("Debug")]
        #endif
        [SerializeField] private bool enableDebugBypass = false;
        [Range(0, 1)] 
        [SerializeField] private float debugIntensity = 0f;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private float currentBaseIntensity = 0f;
        private float burstIntensity = 0f;
        private static readonly int GlitchIntensityID = Shader.PropertyToID("_GlitchIntensity");

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
            
            if (uiGlitchMaterial == null)
            {
                Debug.LogWarning("[UIGlitchController] UI Glitch Material not assigned!");
            }
        }

        private void Update()
        {
            UpdateIntensity();
            ApplyToMaterial();
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        
        /// <summary>
        /// Trigger a sudden localized glitch burst for a duration.
        /// </summary>
        public void TriggerBurst(float intensity, float duration)
        {
            StopAllCoroutines();
            StartCoroutine(BurstCoroutine(intensity, duration));
        }

        // -------------------------------------------------------------------------
        // Internal Logic
        // -------------------------------------------------------------------------
        
        private void UpdateIntensity()
        {
            if (enableDebugBypass)
            {
                currentBaseIntensity = debugIntensity;
                return;
            }

            // Sync with A.N.G.E.L.'s processing level
            var angel = AngelInteractionController.Instance;
            if (angel != null)
            {
                // Normalize processing level (0-100) to (0-1)
                float normalizedProcessing = angel.ProcessingLevel / 100f;
                currentBaseIntensity = processingToIntensityCurve.Evaluate(normalizedProcessing);
            }
        }

        private void ApplyToMaterial()
        {
            if (uiGlitchMaterial == null) return;
            
            float totalIntensity = Mathf.Clamp01(currentBaseIntensity + burstIntensity);
            uiGlitchMaterial.SetFloat(GlitchIntensityID, totalIntensity);
        }

        private IEnumerator BurstCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                burstIntensity = Mathf.Lerp(intensity, 0f, t);
                yield return null;
            }
            burstIntensity = 0f;
        }

        // -------------------------------------------------------------------------
        // Debug Tools
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Test Burst (0.8)", ButtonSizes.Medium)]
        [GUIColor(1, 0.5f, 0)]
        private void Debug_TestBurst()
        {
            if (Application.isPlaying) TriggerBurst(0.8f, 0.5f);
        }
        #endif
    }
}
