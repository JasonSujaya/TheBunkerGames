using UnityEngine;
using System.Collections.Generic;
using TMPro;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Per-character vertex effects for TMP text: wave, shake, and rainbow.
    /// Supports both global toggles (entire text) and inline custom tags.
    /// </summary>
    [DisallowMultipleComponent]
    public class TMPVertexEffects : TMPEffectBase
    {
        // -------------------------------------------------------------------------
        // Global Toggles (apply to ALL characters when enabled)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Global Effects")]
        #endif
        [SerializeField] private bool globalWave;
        [SerializeField] private bool globalShake;
        [SerializeField] private bool globalRainbow;

        // -------------------------------------------------------------------------
        // Wave Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Wave Settings")]
        #endif
        [SerializeField] private float waveAmplitude = 5f;
        [SerializeField] private float waveFrequency = 3f;
        [SerializeField] private float waveCharOffset = 0.5f;

        // -------------------------------------------------------------------------
        // Shake Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Shake Settings")]
        #endif
        [SerializeField] private float shakeIntensity = 2f;
        [SerializeField] private float shakeSpeed = 20f;

        // -------------------------------------------------------------------------
        // Rainbow Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Rainbow Settings")]
        #endif
        [SerializeField] private float rainbowSpeed = 1f;
        [SerializeField] private float rainbowSaturation = 0.8f;
        [SerializeField] private float rainbowBrightness = 1f;
        [SerializeField] private float rainbowCharOffset = 0.1f;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public bool GlobalWave { get => globalWave; set => globalWave = value; }
        public bool GlobalShake { get => globalShake; set => globalShake = value; }
        public bool GlobalRainbow { get => globalRainbow; set => globalRainbow = value; }

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private List<TagRange> tagRanges = new List<TagRange>();
        private bool tagsParsed;
        private TMPTypewriter siblingTypewriter;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        protected override void Awake()
        {
            base.Awake();
            siblingTypewriter = GetComponent<TMPTypewriter>();
        }

        private void OnEnable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            tagsParsed = false;
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
        }

        protected override void LateUpdate()
        {
            EnsureTagsParsed();
            base.LateUpdate();
        }

        // -------------------------------------------------------------------------
        // Tag Management
        // -------------------------------------------------------------------------

        /// <summary>
        /// Set tag ranges externally (e.g. from TMPTypewriter).
        /// </summary>
        public void SetTagRanges(List<TagRange> ranges)
        {
            tagRanges = ranges ?? new List<TagRange>();
            tagsParsed = true;
        }

        private void EnsureTagsParsed()
        {
            if (tagsParsed) return;

            // Check if sibling typewriter has already parsed tags
            if (siblingTypewriter != null && siblingTypewriter.ParsedTagRanges != null)
            {
                tagRanges = new List<TagRange>(siblingTypewriter.ParsedTagRanges);
                tagsParsed = true;
                return;
            }

            // Parse tags ourselves
            if (textComponent != null && !string.IsNullOrEmpty(textComponent.text))
            {
                string cleaned = TMPTagParser.Parse(textComponent.text, out var ranges);
                tagRanges = ranges;

                // Only update text if custom tags were actually found and stripped
                if (cleaned != textComponent.text)
                {
                    tagsParsed = true; // Set before changing text to avoid re-parse loop
                    textComponent.text = cleaned;
                }
            }

            tagsParsed = true;
        }

        private void OnTextChanged(Object obj)
        {
            if (obj == textComponent)
            {
                tagsParsed = false;
            }
        }

        // -------------------------------------------------------------------------
        // Effect Implementation
        // -------------------------------------------------------------------------
        protected override void ApplyEffect(
            int charIndex,
            int vertexIndex,
            Vector3[] vertices,
            Color32[] colors,
            TMP_CharacterInfo charInfo)
        {
            bool applyWave = globalWave || IsCharInRange(charIndex, TMPEffectType.Wave);
            bool applyShake = globalShake || IsCharInRange(charIndex, TMPEffectType.Shake);
            bool applyRainbow = globalRainbow || IsCharInRange(charIndex, TMPEffectType.Rainbow);

            if (applyWave)
            {
                float offset = Mathf.Sin(Time.time * waveFrequency * Mathf.PI * 2f + charIndex * waveCharOffset) * waveAmplitude;
                for (int v = 0; v < 4; v++)
                    vertices[vertexIndex + v] += new Vector3(0f, offset, 0f);
            }

            if (applyShake)
            {
                float x = (Mathf.PerlinNoise(charIndex * 100f, Time.time * shakeSpeed) - 0.5f) * 2f * shakeIntensity;
                float y = (Mathf.PerlinNoise(charIndex * 100f + 50f, Time.time * shakeSpeed) - 0.5f) * 2f * shakeIntensity;
                for (int v = 0; v < 4; v++)
                    vertices[vertexIndex + v] += new Vector3(x, y, 0f);
            }

            if (applyRainbow)
            {
                float hue = Mathf.Repeat(Time.time * rainbowSpeed + charIndex * rainbowCharOffset, 1f);
                Color32 c = Color.HSVToRGB(hue, rainbowSaturation, rainbowBrightness);
                for (int v = 0; v < 4; v++)
                    colors[vertexIndex + v] = c;
            }
        }

        private bool IsCharInRange(int charIndex, TMPEffectType effectType)
        {
            for (int i = 0; i < tagRanges.Count; i++)
            {
                if (tagRanges[i].EffectType == effectType && tagRanges[i].Contains(charIndex))
                    return true;
            }
            return false;
        }
    }
}
