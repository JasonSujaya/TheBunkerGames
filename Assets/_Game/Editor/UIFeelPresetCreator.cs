using UnityEngine;
using UnityEditor;

namespace TheBunkerGames.Editor
{
    /// <summary>
    /// Editor utility that creates common UI Feel preset ScriptableObject assets.
    /// </summary>
    public static class UIFeelPresetCreator
    {
        private const string BasePath = "Assets/_Game/Resources/UIFeel";

        [MenuItem("TheBunkerGames/UI Feel/Create All Presets")]
        public static void CreateAllPresets()
        {
            EnsureFolder();

            // One-shot presets
            CreateButtonPress();
            CreateButtonRelease();
            CreateHoverEnter();
            CreateHoverExit();
            CreatePopIn();
            CreatePulse();
            CreatePunchClick();
            CreateShake();

            // Continuous / held presets
            CreateHeldPress();
            CreateHeldHover();
            CreateHeldPulsePress();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UIFeel] All presets created in " + BasePath);
        }

        // =====================================================================
        // One-Shot Presets
        // =====================================================================

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Button Press")]
        public static void CreateButtonPress()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.Scale);
            SetPrivateField(asset, "duration", 0.1f);
            SetPrivateField(asset, "targetScale", new Vector3(0.92f, 0.92f, 1f));
            SetPrivateField(asset, "scaleRelative", true);
            SetPrivateField(asset, "easeCurve", AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            SaveAsset(asset, "UIFeel_ButtonPress");
        }

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Button Release")]
        public static void CreateButtonRelease()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.Scale);
            SetPrivateField(asset, "duration", 0.15f);
            SetPrivateField(asset, "targetScale", new Vector3(1f, 1f, 1f));
            SetPrivateField(asset, "scaleRelative", false);

            // Overshoot curve
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.6f, 1.08f),
                new Keyframe(1f, 1f)
            );
            SetPrivateField(asset, "easeCurve", curve);
            SaveAsset(asset, "UIFeel_ButtonRelease");
        }

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Hover Enter")]
        public static void CreateHoverEnter()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.Scale);
            SetPrivateField(asset, "duration", 0.12f);
            SetPrivateField(asset, "targetScale", new Vector3(1.05f, 1.05f, 1f));
            SetPrivateField(asset, "scaleRelative", true);
            SetPrivateField(asset, "easeCurve", AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            SaveAsset(asset, "UIFeel_HoverEnter");
        }

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Hover Exit")]
        public static void CreateHoverExit()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.Scale);
            SetPrivateField(asset, "duration", 0.12f);
            SetPrivateField(asset, "targetScale", new Vector3(1f, 1f, 1f));
            SetPrivateField(asset, "scaleRelative", false);
            SetPrivateField(asset, "easeCurve", AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            SaveAsset(asset, "UIFeel_HoverExit");
        }

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Pop In")]
        public static void CreatePopIn()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.Scale);
            SetPrivateField(asset, "duration", 0.3f);
            SetPrivateField(asset, "targetScale", new Vector3(1f, 1f, 1f));
            SetPrivateField(asset, "scaleRelative", false);

            // Start from 0, overshoot, settle
            var curve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.5f, 1.15f),
                new Keyframe(0.75f, 0.95f),
                new Keyframe(1f, 1f)
            );
            SetPrivateField(asset, "easeCurve", curve);
            SaveAsset(asset, "UIFeel_PopIn");
        }

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Pulse")]
        public static void CreatePulse()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.Scale);
            SetPrivateField(asset, "duration", 0.5f);
            SetPrivateField(asset, "targetScale", new Vector3(1.08f, 1.08f, 1f));
            SetPrivateField(asset, "scaleRelative", true);
            SetPrivateField(asset, "loopType", UIFeelLoop.PingPong);
            SetPrivateField(asset, "loopCount", -1);
            SetPrivateField(asset, "easeCurve", AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            SaveAsset(asset, "UIFeel_Pulse");
        }

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Punch Click")]
        public static void CreatePunchClick()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.PunchScale);
            SetPrivateField(asset, "duration", 0.3f);
            SetPrivateField(asset, "targetScale", new Vector3(0.2f, 0.2f, 0f));
            SetPrivateField(asset, "scaleRelative", true);
            SetPrivateField(asset, "vibrato", 4);
            SetPrivateField(asset, "elasticity", 0.5f);
            SetPrivateField(asset, "easeCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            SaveAsset(asset, "UIFeel_PunchClick");
        }

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Shake")]
        public static void CreateShake()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.PunchMove);
            SetPrivateField(asset, "duration", 0.4f);
            SetPrivateField(asset, "moveOffset", new Vector3(8f, 4f, 0f));
            SetPrivateField(asset, "vibrato", 6);
            SetPrivateField(asset, "elasticity", 0.7f);
            SetPrivateField(asset, "easeCurve", AnimationCurve.Linear(0f, 0f, 1f, 1f));
            SaveAsset(asset, "UIFeel_Shake");
        }

        // =====================================================================
        // Continuous / Held Presets
        // =====================================================================

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Held Press")]
        public static void CreateHeldPress()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.Scale);
            SetPrivateField(asset, "duration", 0.1f);
            SetPrivateField(asset, "targetScale", new Vector3(0.92f, 0.92f, 1f));
            SetPrivateField(asset, "scaleRelative", true);
            SetPrivateField(asset, "easeCurve", AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            // No loop - just hold at target while pressed
            SaveAsset(asset, "UIFeel_HeldPress");
        }

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Held Hover")]
        public static void CreateHeldHover()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.Scale);
            SetPrivateField(asset, "duration", 0.12f);
            SetPrivateField(asset, "targetScale", new Vector3(1.05f, 1.05f, 1f));
            SetPrivateField(asset, "scaleRelative", true);
            SetPrivateField(asset, "easeCurve", AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            // No loop - just hold at target while hovered
            SaveAsset(asset, "UIFeel_HeldHover");
        }

        [MenuItem("TheBunkerGames/UI Feel/Create Presets/Held Pulse Press")]
        public static void CreateHeldPulsePress()
        {
            var asset = ScriptableObject.CreateInstance<UIFeelEffectData>();
            SetPrivateField(asset, "effectType", UIFeelType.Scale);
            SetPrivateField(asset, "duration", 0.35f);
            SetPrivateField(asset, "targetScale", new Vector3(0.95f, 0.95f, 1f));
            SetPrivateField(asset, "scaleRelative", true);
            SetPrivateField(asset, "loopType", UIFeelLoop.PingPong);
            SetPrivateField(asset, "loopCount", -1);
            SetPrivateField(asset, "easeCurve", AnimationCurve.EaseInOut(0f, 0f, 1f, 1f));
            // Pulses while held, reverses to original on release
            SaveAsset(asset, "UIFeel_HeldPulsePress");
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources"))
                AssetDatabase.CreateFolder("Assets/_Game", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/_Game/Resources/UIFeel"))
                AssetDatabase.CreateFolder("Assets/_Game/Resources", "UIFeel");
        }

        private static void SaveAsset(UIFeelEffectData asset, string name)
        {
            EnsureFolder();
            string path = $"{BasePath}/{name}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<UIFeelEffectData>(path);
            if (existing != null)
            {
                EditorUtility.CopySerialized(asset, existing);
                Object.DestroyImmediate(asset);
                Debug.Log($"[UIFeel] Updated: {path}");
            }
            else
            {
                AssetDatabase.CreateAsset(asset, path);
                Debug.Log($"[UIFeel] Created: {path}");
            }
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(obj, value);
        }
    }
}
