using UnityEngine;
using System.Collections;
using TMPro;
using DG.Tweening;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Whole-text transitions for TMP: fade in/out, scale punch, and per-character staggered fade.
    /// Uses DOTween for smooth animation. Runs after TMPVertexEffects via execution order.
    /// </summary>
    [DefaultExecutionOrder(1)]
    [RequireComponent(typeof(TMP_Text))]
    public class TMPTextAnimator : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Fade Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Fade Settings")]
        #endif
        [SerializeField] private float fadeDuration = 0.5f;
        [SerializeField] private Ease fadeEase = Ease.InOutQuad;

        // -------------------------------------------------------------------------
        // Scale Punch Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Scale Punch Settings")]
        #endif
        [SerializeField] private float punchStrength = 0.3f;
        [SerializeField] private float punchDuration = 0.4f;
        [SerializeField] private int punchVibrato = 6;
        [SerializeField] private float punchElasticity = 0.5f;

        // -------------------------------------------------------------------------
        // Per-CharacterData Fade Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Per-CharacterData Fade")]
        #endif
        [SerializeField] private float charFadeDelay = 0.02f;
        [SerializeField] private float charFadeDuration = 0.2f;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public float FadeDuration => fadeDuration;
        public float PunchDuration => punchDuration;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private TMP_Text textComponent;
        private CanvasGroup canvasGroup;
        private Vector3 originalScale;
        private Tween activeFadeTween;
        private Tween activePunchTween;
        private Coroutine perCharFadeCoroutine;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
            originalScale = transform.localScale;

            // Auto-add CanvasGroup for whole-text fade
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        private void OnDestroy()
        {
            activeFadeTween?.Kill();
            activePunchTween?.Kill();
        }

        // -------------------------------------------------------------------------
        // Public API — Fade
        // -------------------------------------------------------------------------

        /// <summary>Fade text from transparent to fully visible.</summary>
        #if ODIN_INSPECTOR
        [Button("Fade In")]
        #endif
        public void FadeIn(System.Action onComplete = null)
        {
            activeFadeTween?.Kill();
            canvasGroup.alpha = 0f;
            activeFadeTween = canvasGroup
                .DOFade(1f, fadeDuration)
                .SetEase(fadeEase)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>Fade text from fully visible to transparent.</summary>
        #if ODIN_INSPECTOR
        [Button("Fade Out")]
        #endif
        public void FadeOut(System.Action onComplete = null)
        {
            activeFadeTween?.Kill();
            activeFadeTween = canvasGroup
                .DOFade(0f, fadeDuration)
                .SetEase(fadeEase)
                .OnComplete(() => onComplete?.Invoke());
        }

        /// <summary>Fade to a specific alpha value.</summary>
        public void FadeTo(float targetAlpha, System.Action onComplete = null)
        {
            activeFadeTween?.Kill();
            activeFadeTween = canvasGroup
                .DOFade(targetAlpha, fadeDuration)
                .SetEase(fadeEase)
                .OnComplete(() => onComplete?.Invoke());
        }

        // -------------------------------------------------------------------------
        // Public API — Scale Punch
        // -------------------------------------------------------------------------

        /// <summary>Play a punchy scale pop effect.</summary>
        #if ODIN_INSPECTOR
        [Button("Punch Scale")]
        #endif
        public void PunchScale(System.Action onComplete = null)
        {
            activePunchTween?.Kill();
            transform.localScale = originalScale;
            activePunchTween = transform
                .DOPunchScale(Vector3.one * punchStrength, punchDuration, punchVibrato, punchElasticity)
                .OnComplete(() =>
                {
                    transform.localScale = originalScale;
                    onComplete?.Invoke();
                });
        }

        // -------------------------------------------------------------------------
        // Public API — Per-CharacterData Fade
        // -------------------------------------------------------------------------

        /// <summary>
        /// Fade in each character with a stagger delay, using vertex color alpha.
        /// Works independently of the CanvasGroup fade.
        /// </summary>
        #if ODIN_INSPECTOR
        [Button("Fade In Per CharacterData")]
        #endif
        public void FadeInPerCharacter(System.Action onComplete = null)
        {
            if (perCharFadeCoroutine != null)
            {
                StopCoroutine(perCharFadeCoroutine);
            }
            perCharFadeCoroutine = StartCoroutine(PerCharFadeRoutine(onComplete));
        }

        private IEnumerator PerCharFadeRoutine(System.Action onComplete)
        {
            textComponent.ForceMeshUpdate();
            TMP_TextInfo textInfo = textComponent.textInfo;
            int charCount = textInfo.characterCount;

            if (charCount == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Initialize all characters to transparent
            SetAllCharactersAlpha(0);

            float totalDuration = (charCount - 1) * charFadeDelay + charFadeDuration;
            float startTime = Time.time;

            while (Time.time - startTime < totalDuration)
            {
                textComponent.ForceMeshUpdate();
                textInfo = textComponent.textInfo;

                float elapsed = Time.time - startTime;

                for (int i = 0; i < charCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    float charStart = i * charFadeDelay;
                    float t = Mathf.Clamp01((elapsed - charStart) / charFadeDuration);

                    byte alpha = (byte)(t * 255);
                    SetCharacterAlpha(textInfo, i, alpha);
                }

                UpdateMeshColors(textInfo);
                yield return null;
            }

            // Ensure all fully visible at end
            textComponent.ForceMeshUpdate();
            SetAllCharactersAlpha(255);
            UpdateMeshColors(textComponent.textInfo);

            perCharFadeCoroutine = null;
            onComplete?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------
        private void SetCharacterAlpha(TMP_TextInfo textInfo, int charIndex, byte alpha)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
            if (!charInfo.isVisible) return;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

            for (int v = 0; v < 4; v++)
            {
                colors[vertexIndex + v].a = alpha;
            }
        }

        private void SetAllCharactersAlpha(byte alpha)
        {
            textComponent.ForceMeshUpdate();
            TMP_TextInfo textInfo = textComponent.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                SetCharacterAlpha(textInfo, i, alpha);
            }

            UpdateMeshColors(textInfo);
        }

        private void UpdateMeshColors(TMP_TextInfo textInfo)
        {
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
    }
}
