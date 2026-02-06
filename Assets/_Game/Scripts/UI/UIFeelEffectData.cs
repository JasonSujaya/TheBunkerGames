using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject that defines a reusable UI feel effect preset.
    /// Drag onto any UIFeel component to share the same animation data across multiple UI elements.
    /// </summary>
    [CreateAssetMenu(fileName = "UIFeel_", menuName = "TheBunkerGames/UI Feel Effect")]
    public class UIFeelEffectData : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Effect Type
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Effect Type")]
        #endif
        [SerializeField] private UIFeelType effectType = UIFeelType.Scale;

        // -------------------------------------------------------------------------
        // Timing
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Timing")]
        #endif
        [SerializeField] private float duration = 0.15f;
        [SerializeField] private float delay = 0f;
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // -------------------------------------------------------------------------
        // Scale Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Scale")]
        [ShowIf("effectType", UIFeelType.Scale)]
        #endif
        [SerializeField] private Vector3 targetScale = new Vector3(1.1f, 1.1f, 1f);
        [SerializeField] private bool scaleRelative = true;

        // -------------------------------------------------------------------------
        // Rotation Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Rotation")]
        [ShowIf("effectType", UIFeelType.Rotation)]
        #endif
        [SerializeField] private Vector3 targetRotation = new Vector3(0f, 0f, 5f);

        // -------------------------------------------------------------------------
        // Move Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Move")]
        [ShowIf("effectType", UIFeelType.Move)]
        #endif
        [SerializeField] private Vector3 moveOffset = new Vector3(0f, 10f, 0f);

        // -------------------------------------------------------------------------
        // Fade Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Fade")]
        [ShowIf("effectType", UIFeelType.Fade)]
        #endif
        [SerializeField] private float targetAlpha = 0.5f;

        // -------------------------------------------------------------------------
        // Color Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Color")]
        [ShowIf("effectType", UIFeelType.Color)]
        #endif
        [SerializeField] private Color targetColor = Color.white;

        // -------------------------------------------------------------------------
        // Punch Settings (used for Punch variants)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Punch")]
        [ShowIf("IsPunchType")]
        #endif
        [SerializeField] private int vibrato = 4;
        [SerializeField] private float elasticity = 0.5f;

        // -------------------------------------------------------------------------
        // Loop Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Loop")]
        #endif
        [SerializeField] private UIFeelLoop loopType = UIFeelLoop.None;
        [SerializeField] private int loopCount = -1;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public UIFeelType EffectType => effectType;
        public float Duration => duration;
        public float Delay => delay;
        public AnimationCurve EaseCurve => easeCurve;
        public Vector3 TargetScale => targetScale;
        public bool ScaleRelative => scaleRelative;
        public Vector3 TargetRotation => targetRotation;
        public Vector3 MoveOffset => moveOffset;
        public float TargetAlpha => targetAlpha;
        public Color TargetColor => targetColor;
        public int Vibrato => vibrato;
        public float Elasticity => elasticity;
        public UIFeelLoop LoopType => loopType;
        public int LoopCount => loopCount;

#if ODIN_INSPECTOR
        private bool IsPunchType() =>
            effectType == UIFeelType.PunchScale ||
            effectType == UIFeelType.PunchRotation ||
            effectType == UIFeelType.PunchMove;
#endif
    }

    // -------------------------------------------------------------------------
    // Enums
    // -------------------------------------------------------------------------

    public enum UIFeelType
    {
        Scale,
        Rotation,
        Move,
        Fade,
        Color,
        PunchScale,
        PunchRotation,
        PunchMove
    }

    public enum UIFeelLoop
    {
        None,
        Restart,
        PingPong
    }

    public enum UIFeelTrigger
    {
        Manual,
        OnEnable,
        OnPointerDown,
        OnPointerUp,
        OnPointerEnter,
        OnPointerExit,
        OnClick
    }
}
