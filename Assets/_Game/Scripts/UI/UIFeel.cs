using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Drop this on any UI GameObject to add juice/feel effects.
    /// Assign UIFeelEffectDataSO ScriptableObjects for shared, reusable presets.
    /// Supports automatic triggers (hover, click, enable) and manual Play() calls.
    /// Zero dependencies - uses coroutines + AnimationCurves.
    /// </summary>
    [AddComponentMenu("TheBunkerGames/UI Feel")]
    public class UIFeel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        // -------------------------------------------------------------------------
        // Effect Entries
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Effects")]
        [ListDrawerSettings(ShowFoldout = true)]
        #endif
        [SerializeField] private List<UIFeelEntry> effects = new List<UIFeelEntry>();

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private readonly Dictionary<int, Coroutine> _runningEffects = new Dictionary<int, Coroutine>();
        private Vector3 _originalScale;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private float _originalAlpha;
        private Color _originalColor;

        private CanvasGroup _canvasGroup;
        private Graphic _graphic;
        private RectTransform _rectTransform;
        private bool _initialized;

        // -------------------------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------------------------

        private void Awake()
        {
            CacheOriginalValues();
        }

        private void OnEnable()
        {
            if (!_initialized) CacheOriginalValues();
            PlayByTrigger(UIFeelTrigger.OnEnable);
        }

        private void OnDisable()
        {
            StopAll();
        }

        // -------------------------------------------------------------------------
        // Pointer Events
        // -------------------------------------------------------------------------

        public void OnPointerDown(PointerEventData eventData) => PlayByTrigger(UIFeelTrigger.OnPointerDown);
        public void OnPointerUp(PointerEventData eventData) => PlayByTrigger(UIFeelTrigger.OnPointerUp);
        public void OnPointerEnter(PointerEventData eventData) => PlayByTrigger(UIFeelTrigger.OnPointerEnter);
        public void OnPointerExit(PointerEventData eventData) => PlayByTrigger(UIFeelTrigger.OnPointerExit);
        public void OnPointerClick(PointerEventData eventData) => PlayByTrigger(UIFeelTrigger.OnClick);

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Play all effects marked as Manual trigger.
        /// </summary>
        public void Play()
        {
            PlayByTrigger(UIFeelTrigger.Manual);
        }

        /// <summary>
        /// Play a specific effect entry by index.
        /// </summary>
        public void PlayAt(int index)
        {
            if (index < 0 || index >= effects.Count) return;
            StartEffect(index, effects[index]);
        }

        /// <summary>
        /// Stop all running effects and reset to original values.
        /// </summary>
        public void StopAll()
        {
            foreach (var kvp in _runningEffects)
            {
                if (kvp.Value != null) StopCoroutine(kvp.Value);
            }
            _runningEffects.Clear();
        }

        /// <summary>
        /// Stop all and snap back to cached original state.
        /// </summary>
        public void ResetToOriginal()
        {
            StopAll();
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _originalScale;
                _rectTransform.localPosition = _originalPosition;
                _rectTransform.localRotation = _originalRotation;
            }
            if (_canvasGroup != null) _canvasGroup.alpha = _originalAlpha;
            if (_graphic != null) _graphic.color = _originalColor;
        }

        // -------------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------------

        private void CacheOriginalValues()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _graphic = GetComponent<Graphic>();

            if (_rectTransform != null)
            {
                _originalScale = _rectTransform.localScale;
                _originalPosition = _rectTransform.localPosition;
                _originalRotation = _rectTransform.localRotation;
            }

            if (_canvasGroup != null) _originalAlpha = _canvasGroup.alpha;
            if (_graphic != null) _originalColor = _graphic.color;

            _initialized = true;
        }

        private void PlayByTrigger(UIFeelTrigger trigger)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i].Trigger == trigger && effects[i].Data != null)
                {
                    StartEffect(i, effects[i]);
                }
            }
        }

        private void StartEffect(int index, UIFeelEntry entry)
        {
            if (entry.Data == null) return;

            // Stop existing coroutine for this slot
            if (_runningEffects.TryGetValue(index, out var existing) && existing != null)
            {
                StopCoroutine(existing);
            }

            _runningEffects[index] = StartCoroutine(RunEffect(index, entry.Data));
        }

        private IEnumerator RunEffect(int index, UIFeelEffectDataSO data)
        {
            if (data.Delay > 0f) yield return new WaitForSeconds(data.Delay);

            bool loop = true;
            int loopsCompleted = 0;
            bool pingPongForward = true;

            while (loop)
            {
                yield return RunSinglePass(data, pingPongForward);

                loopsCompleted++;

                switch (data.LoopType)
                {
                    case UIFeelLoop.None:
                        loop = false;
                        break;
                    case UIFeelLoop.Restart:
                        if (data.LoopCount > 0 && loopsCompleted >= data.LoopCount) loop = false;
                        break;
                    case UIFeelLoop.PingPong:
                        pingPongForward = !pingPongForward;
                        if (data.LoopCount > 0 && loopsCompleted >= data.LoopCount * 2) loop = false;
                        break;
                }
            }

            _runningEffects.Remove(index);
        }

        private IEnumerator RunSinglePass(UIFeelEffectDataSO data, bool forward)
        {
            float elapsed = 0f;
            float duration = Mathf.Max(data.Duration, 0.001f);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float rawT = Mathf.Clamp01(elapsed / duration);
                float t = forward ? rawT : 1f - rawT;
                float curved = data.EaseCurve.Evaluate(t);

                ApplyEffect(data, curved);
                yield return null;
            }

            // Snap to final
            float finalT = forward ? 1f : 0f;
            ApplyEffect(data, data.EaseCurve.Evaluate(finalT));
        }

        private void ApplyEffect(UIFeelEffectDataSO data, float t)
        {
            if (_rectTransform == null) return;

            switch (data.EffectType)
            {
                case UIFeelType.Scale:
                    Vector3 targetScale = data.ScaleRelative
                        ? Vector3.Scale(_originalScale, data.TargetScale)
                        : data.TargetScale;
                    _rectTransform.localScale = Vector3.LerpUnclamped(_originalScale, targetScale, t);
                    break;

                case UIFeelType.Rotation:
                    Quaternion targetRot = _originalRotation * Quaternion.Euler(data.TargetRotation);
                    _rectTransform.localRotation = Quaternion.LerpUnclamped(_originalRotation, targetRot, t);
                    break;

                case UIFeelType.Move:
                    _rectTransform.localPosition = Vector3.LerpUnclamped(
                        _originalPosition, _originalPosition + data.MoveOffset, t);
                    break;

                case UIFeelType.Fade:
                    if (_canvasGroup != null)
                        _canvasGroup.alpha = Mathf.LerpUnclamped(_originalAlpha, data.TargetAlpha, t);
                    break;

                case UIFeelType.Color:
                    if (_graphic != null)
                        _graphic.color = UnityEngine.Color.LerpUnclamped(_originalColor, data.TargetColor, t);
                    break;

                case UIFeelType.PunchScale:
                    ApplyPunch(data, t, PunchTarget.Scale);
                    break;

                case UIFeelType.PunchRotation:
                    ApplyPunch(data, t, PunchTarget.Rotation);
                    break;

                case UIFeelType.PunchMove:
                    ApplyPunch(data, t, PunchTarget.Move);
                    break;
            }
        }

        private enum PunchTarget { Scale, Rotation, Move }

        private void ApplyPunch(UIFeelEffectDataSO data, float t, PunchTarget target)
        {
            // Damped sine wave for punch feel
            float decay = 1f - t;
            float frequency = data.Vibrato * Mathf.PI * 2f;
            float wave = Mathf.Sin(t * frequency) * decay * data.Elasticity;

            switch (target)
            {
                case PunchTarget.Scale:
                    Vector3 punchScale = data.ScaleRelative
                        ? Vector3.Scale(_originalScale, Vector3.one + data.TargetScale * wave)
                        : _originalScale + data.TargetScale * wave;
                    _rectTransform.localScale = punchScale;
                    break;

                case PunchTarget.Rotation:
                    _rectTransform.localRotation = _originalRotation *
                        Quaternion.Euler(data.TargetRotation * wave);
                    break;

                case PunchTarget.Move:
                    _rectTransform.localPosition = _originalPosition + data.MoveOffset * wave;
                    break;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Serializable entry linking data + trigger
    // -------------------------------------------------------------------------

    [Serializable]
    public class UIFeelEntry
    {
        #if ODIN_INSPECTOR
        [HorizontalGroup("Row", Width = 120)]
        #endif
        [SerializeField] private UIFeelTrigger trigger = UIFeelTrigger.OnClick;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Row")]
        #endif
        [SerializeField] private UIFeelEffectDataSO data;

        public UIFeelTrigger Trigger => trigger;
        public UIFeelEffectDataSO Data => data;
    }
}
