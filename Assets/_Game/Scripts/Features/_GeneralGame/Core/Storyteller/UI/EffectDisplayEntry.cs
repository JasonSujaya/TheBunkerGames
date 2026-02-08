using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// UI component for a single effect display entry.
    /// Shows character portrait, effect icon, and value change.
    /// Spawned by StorytellerUI when effects are executed.
    /// </summary>
    public class EffectDisplayEntry : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image portraitImage;
        [SerializeField] private Image effectIconImage;
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image backgroundImage;

        [Header("Optional")]
        [SerializeField] private TextMeshProUGUI targetNameText;
        [SerializeField] private GameObject portraitContainer;

        /// <summary>
        /// Configure this entry with effect data, character portrait, and effect icon.
        /// </summary>
        public void Setup(EffectDisplayData data, Sprite portrait, Sprite effectIcon, Color tintColor)
        {
            // Portrait
            bool hasPortrait = portrait != null;
            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = hasPortrait;
            }
            if (portraitContainer != null)
                portraitContainer.SetActive(hasPortrait);

            // Effect icon
            if (effectIconImage != null)
            {
                effectIconImage.sprite = effectIcon;
                effectIconImage.enabled = effectIcon != null;
            }

            // Value text
            if (valueText != null)
            {
                string displayText = FormatValueText(data);
                valueText.text = displayText;
                valueText.color = tintColor;
            }

            // Target name
            if (targetNameText != null)
            {
                targetNameText.text = !string.IsNullOrEmpty(data.Target) ? data.Target : "";
                targetNameText.enabled = !string.IsNullOrEmpty(data.Target);
            }

            // Background tint (subtle)
            if (backgroundImage != null)
                backgroundImage.color = new Color(tintColor.r, tintColor.g, tintColor.b, 0.15f);
        }

        private string FormatValueText(EffectDisplayData data)
        {
            // Special cases with no numeric value
            if (data.DisplayLabel == "Death")
                return "KILLED";
            if (data.DisplayLabel == "Cure")
                return "Cured";

            // Sickness shows type info
            if (data.DisplayLabel == "Sickness" && !data.IsPositive)
            {
                string sign = data.ValueChange >= 0 ? "+" : "";
                return $"Infected ({sign}{data.ValueChange:F0} HP)";
            }

            // Standard numeric display
            string prefix = data.IsPositive ? "+" : "";
            return $"{prefix}{data.ValueChange:F0} {data.DisplayLabel}";
        }

        // -------------------------------------------------------------------------
        // Editor Auto-Setup
        // -------------------------------------------------------------------------
#if ODIN_INSPECTOR && UNITY_EDITOR
        [Button("Auto-Setup UI Hierarchy", ButtonSizes.Large)]
        [GUIColor(0f, 1f, 0f)]
        private void AutoSetupHierarchy()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320, 54);

            // Background
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
                if (backgroundImage == null) backgroundImage = gameObject.AddComponent<Image>();
            }
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);

            // HorizontalLayoutGroup
            var hlg = GetComponent<HorizontalLayoutGroup>();
            if (hlg == null) hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(6, 6, 4, 4);
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            // ContentSizeFitter
            var csf = GetComponent<ContentSizeFitter>();
            if (csf == null) csf = gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Portrait Container
            var portraitGO = EnsureChild("Portrait_Container");
            portraitContainer = portraitGO;
            var prt = portraitGO.GetComponent<RectTransform>();
            prt.sizeDelta = new Vector2(46, 46);

            var portraitImgGO = EnsureChild("Portrait_Image", portraitGO.transform);
            portraitImage = EnsureComponent<Image>(portraitImgGO);
            portraitImage.preserveAspect = true;
            var pirt = portraitImgGO.GetComponent<RectTransform>();
            pirt.anchorMin = Vector2.zero;
            pirt.anchorMax = Vector2.one;
            pirt.offsetMin = Vector2.zero;
            pirt.offsetMax = Vector2.zero;

            // Effect Icon
            var iconGO = EnsureChild("EffectIcon");
            effectIconImage = EnsureComponent<Image>(iconGO);
            effectIconImage.preserveAspect = true;
            var irt = iconGO.GetComponent<RectTransform>();
            irt.sizeDelta = new Vector2(32, 32);

            // Value Text
            var valueGO = EnsureChild("ValueText");
            valueText = EnsureComponent<TextMeshProUGUI>(valueGO);
            valueText.fontSize = 18;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.MidlineLeft;
            valueText.color = Color.white;
            valueText.text = "+15 HP";
            var vrt = valueGO.GetComponent<RectTransform>();
            vrt.sizeDelta = new Vector2(160, 46);

            // Target Name (small, below or beside value)
            var nameGO = EnsureChild("TargetName");
            targetNameText = EnsureComponent<TextMeshProUGUI>(nameGO);
            targetNameText.fontSize = 12;
            targetNameText.alignment = TextAlignmentOptions.MidlineLeft;
            targetNameText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            targetNameText.text = "Mother";
            var nrt = nameGO.GetComponent<RectTransform>();
            nrt.sizeDelta = new Vector2(80, 46);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
            Debug.Log("[EffectDisplayEntry] Auto-Setup Complete.");
        }

        private GameObject EnsureChild(string childName, Transform parent = null)
        {
            if (parent == null) parent = transform;
            var t = parent.Find(childName);
            if (t != null) return t.gameObject;

            var go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private T EnsureComponent<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null) comp = go.AddComponent<T>();
            return comp;
        }
#endif
    }
}
