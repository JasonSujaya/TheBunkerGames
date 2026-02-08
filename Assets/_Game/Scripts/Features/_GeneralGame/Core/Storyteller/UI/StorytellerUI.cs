using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// UI component for displaying story events.
    /// Works with LLMStoryEventData from the simplified storyteller system.
    /// </summary>
    public class StorytellerUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Visual Assets (drag & drop in Inspector)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Visual Assets – Fonts")]
        #endif
        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset bodyFont;

        #if ODIN_INSPECTOR
        [Title("Visual Assets – Sprites")]
        #endif
        [Header("Sprites – Backgrounds & Frames")]
        [SerializeField] private Sprite panelBackgroundSprite;    // main overlay bg
        [SerializeField] private Sprite titleBackgroundSprite;    // title area bg
        [SerializeField] private Sprite descriptionFrameSprite;   // description area frame
        [SerializeField] private Sprite inputFieldSprite;         // text input bg
        [SerializeField] private Sprite effectsContainerSprite;   // effects area bg

        [Header("Sprites – Buttons")]
        [SerializeField] private Sprite buttonSprite;             // general button bg
        [SerializeField] private Sprite continueButtonSprite;     // continue button bg
        [SerializeField] private Sprite closeButtonSprite;        // close (X) button bg

        [Header("Sprites – Icons")]
        [SerializeField] private Sprite iconClose;                // X icon
        [SerializeField] private Sprite iconContinue;             // continue/arrow icon

        #if ODIN_INSPECTOR
        [Title("Visual Assets – Colors")]
        #endif
        [Header("Colors")]
        [SerializeField] private Color panelBgColor          = new Color(0f, 0f, 0f, 0.9f);
        [SerializeField] private Color titleColor            = Color.white;
        [SerializeField] private Color descriptionColor      = Color.white;
        [SerializeField] private Color inputBgColor          = new Color(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private Color textColor             = Color.white;
        [SerializeField] private Color placeholderColor      = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color continueButtonColor   = Color.green;
        [SerializeField] private Color closeButtonColor      = Color.red;
        [SerializeField] private Color buttonTextColor       = Color.black;

        // -------------------------------------------------------------------------
        // UI References
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("UI References")]
        #endif
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private GameObject contentParent;

        #if ODIN_INSPECTOR
        [Title("Interaction")]
        #endif
        [Header("Interaction")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_InputField inputField;

        #if ODIN_INSPECTOR
        [Title("Effect Display")]
        #endif
        [Header("Effect Display")]
        [SerializeField] private Transform effectsContainer;
        [SerializeField] private EffectDisplayEntry effectEntryPrefab;
        [SerializeField] private EffectIconDatabaseSO effectIconDatabase;

        [Header("Settings")]
        [SerializeField] private bool autoToggleVisibility = true;

        private void Awake()
        {
            // Start hidden
            if (contentParent != null) contentParent.SetActive(false);
            ClearText();
            
            // Setup default listeners if needed (optional, logic might be handled by controller)
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            StorytellerManager.OnStoryEventReceived += ShowEvent;
            LLMEffectExecutor.OnEffectExecuted += ShowEffectEntry;
        }

        private void OnDisable()
        {
            StorytellerManager.OnStoryEventReceived -= ShowEvent;
            LLMEffectExecutor.OnEffectExecuted -= ShowEffectEntry;
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        /// <summary>
        /// Show a story event in the UI.
        /// </summary>
        public void ShowEvent(LLMStoryEventData storyEvent)
        {
            if (storyEvent != null)
            {
                // Clear previous effect entries when a new event arrives
                ClearEffectEntries();

                if (titleText != null)
                    titleText.text = storyEvent.Title;
                else
                    Debug.LogWarning("[StorytellerUI] TitleText is missing!");

                if (descriptionText != null)
                    descriptionText.text = storyEvent.Description;
                else
                    Debug.LogWarning("[StorytellerUI] DescriptionText is missing!");

                if (contentParent != null && autoToggleVisibility)
                    contentParent.SetActive(true);
            }
            else
            {
                Hide();
            }
        }

        /// <summary>
        /// Hide the story UI.
        /// </summary>
        public void Hide()
        {
            ClearText();
            ClearEffectEntries();
            if (inputField != null) inputField.text = "";

            if (contentParent != null && autoToggleVisibility)
                contentParent.SetActive(false);
        }

        private void ClearText()
        {
            if (titleText != null) titleText.text = "";
            if (descriptionText != null) descriptionText.text = "";
        }

        // -------------------------------------------------------------------------
        // Effect Display
        // -------------------------------------------------------------------------
        private void ShowEffectEntry(EffectDisplayData data)
        {
            if (effectEntryPrefab == null || effectsContainer == null) return;

            // Look up character portrait
            Sprite portrait = null;
            if (!string.IsNullOrEmpty(data.Target) && CharacterDatabaseDataSO.Instance != null)
            {
                var charDef = CharacterDatabaseDataSO.Instance.GetCharacter(data.Target);
                if (charDef != null) portrait = charDef.Portrait;
            }

            // Look up effect icon and color
            var iconDb = effectIconDatabase != null ? effectIconDatabase : EffectIconDatabaseSO.Instance;
            Sprite icon = null;
            Color tintColor = data.IsPositive ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);

            if (iconDb != null)
            {
                string category = EffectIconDatabaseSO.EffectTypeToCategory(data.EffectType);
                var entry = iconDb.GetEntry(category);
                if (entry != null)
                {
                    icon = entry.icon;
                    tintColor = data.IsPositive ? entry.positiveColor : entry.negativeColor;
                }
                else
                {
                    icon = iconDb.FallbackIcon;
                }
            }

            // Spawn entry
            var instance = Instantiate(effectEntryPrefab, effectsContainer);
            instance.Setup(data, portrait, icon, tintColor);
        }

        private void ClearEffectEntries()
        {
            if (effectsContainer == null) return;
            for (int i = effectsContainer.childCount - 1; i >= 0; i--)
                Destroy(effectsContainer.GetChild(i).gameObject);
        }

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button("Auto-Setup UI Hierarchy", Sirenix.OdinInspector.ButtonSizes.Large)]
        [Sirenix.OdinInspector.GUIColor(0f, 1f, 0f)]
        private void Debug_AutoSetup()
        {
            // 1. Ensure Panel
            if (contentParent == null)
            {
                var panelTransform = transform.Find("Panel");
                if (panelTransform == null)
                {
                    var panelGO = new GameObject("Panel");
                    panelGO.transform.SetParent(transform, false);
                    panelTransform = panelGO.transform;
                    
                    var rect = panelGO.AddComponent<RectTransform>();
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    
                    var img = panelGO.AddComponent<UnityEngine.UI.Image>();
                    ApplySprite(img, panelBackgroundSprite, panelBgColor);
                }
                contentParent = panelTransform.gameObject;
            }

            // 2. Ensure Title
            if (titleText == null)
            {
                var titleTransform = contentParent.transform.Find("Title_Text");
                if (titleTransform == null)
                {
                    var go = new GameObject("Title_Text");
                    go.transform.SetParent(contentParent.transform, false);
                    var txt = go.AddComponent<TextMeshProUGUI>();
                    txt.text = "Event Title";
                    txt.fontSize = 24;
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.fontStyle = FontStyles.Bold;
                    txt.color = titleColor;
                    if (titleFont != null) txt.font = titleFont;
                    
                    var r = txt.rectTransform;
                    r.anchorMin = new Vector2(0, 0.85f);
                    r.anchorMax = new Vector2(1, 1);
                    r.offsetMin = Vector2.zero;
                    r.offsetMax = Vector2.zero;
                    
                    titleText = txt;
                }
                else
                {
                    titleText = titleTransform.GetComponent<TextMeshProUGUI>();
                }
            }

            // 3. Ensure Description
            if (descriptionText == null)
            {
                var descTransform = contentParent.transform.Find("Description_Text");
                if (descTransform == null)
                {
                    var go = new GameObject("Description_Text");
                    go.transform.SetParent(contentParent.transform, false);
                    var txt = go.AddComponent<TextMeshProUGUI>();
                    txt.text = "Event Description...";
                    txt.fontSize = 18;
                    txt.alignment = TextAlignmentOptions.TopLeft;
                    txt.color = descriptionColor;
                    if (bodyFont != null) txt.font = bodyFont;
                    
                    var r = txt.rectTransform;
                    r.anchorMin = new Vector2(0.05f, 0.2f); // Adjusted for buttons
                    r.anchorMax = new Vector2(0.95f, 0.8f);
                    r.offsetMin = Vector2.zero;
                    r.offsetMax = Vector2.zero;
                    
                    descriptionText = txt;
                }
                else
                {
                    descriptionText = descTransform.GetComponent<TextMeshProUGUI>();
                }
            }
            
            // 4. Ensure Input Field
            if (inputField == null)
            {
                var inputTransform = contentParent.transform.Find("Input_Field");
                if (inputTransform == null)
                {
                    // Create standard TMP Input Field structure could be complex, 
                    // simplifying for direct component addition, usually you want a prefab.
                    // Accessing default resources or creating basic structure.
                    var go = new GameObject("Input_Field");
                    go.transform.SetParent(contentParent.transform, false);
                    var img = go.AddComponent<Image>();
                    ApplySprite(img, inputFieldSprite, inputBgColor);

                    var input = go.AddComponent<TMP_InputField>();
                    var r = go.GetComponent<RectTransform>();
                    r.anchorMin = new Vector2(0.1f, 0.1f);
                    r.anchorMax = new Vector2(0.7f, 0.18f); // Bottom left area
                    r.offsetMin = Vector2.zero;
                    r.offsetMax = Vector2.zero;

                    // TextArea
                    var textArea = new GameObject("Text Area");
                    textArea.transform.SetParent(go.transform, false);
                    var textAreaRect = textArea.AddComponent<RectTransform>();
                    textAreaRect.anchorMin = Vector2.zero; 
                    textAreaRect.anchorMax = Vector2.one;
                    textAreaRect.offsetMin = new Vector2(10, 6);
                    textAreaRect.offsetMax = new Vector2(-10, -7);
                    
                    // Text
                    var textGO = new GameObject("Text");
                    textGO.transform.SetParent(textArea.transform, false);
                    var text = textGO.AddComponent<TextMeshProUGUI>();
                    text.text = "";
                    text.fontSize = 14;
                    text.color = textColor;
                    if (bodyFont != null) text.font = bodyFont;

                    // Placeholder
                    var placeholderGO = new GameObject("Placeholder");
                    placeholderGO.transform.SetParent(textArea.transform, false);
                    var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
                    placeholder.text = "Enter response...";
                    placeholder.fontSize = 14;
                    placeholder.color = placeholderColor;
                    placeholder.fontStyle = FontStyles.Italic;
                    if (bodyFont != null) placeholder.font = bodyFont;

                    input.textViewport = textAreaRect;
                    input.textComponent = text;
                    input.placeholder = placeholder;
                    
                    inputField = input;
                }
                else
                {
                    inputField = inputTransform.GetComponent<TMP_InputField>();
                }
            }

            // 5. Ensure Continue Button
            if (continueButton == null)
            {
                var btnTransform = contentParent.transform.Find("Continue_Button");
                if (btnTransform == null)
                {
                    var go = new GameObject("Continue_Button");
                    go.transform.SetParent(contentParent.transform, false);
                    var img = go.AddComponent<Image>();
                    ApplySprite(img, continueButtonSprite, continueButtonColor);

                    var btn = go.AddComponent<Button>();

                    var r = go.GetComponent<RectTransform>();
                    r.anchorMin = new Vector2(0.75f, 0.1f);
                    r.anchorMax = new Vector2(0.9f, 0.18f);
                    r.offsetMin = Vector2.zero;
                    r.offsetMax = Vector2.zero;

                    // Text
                    var textGO = new GameObject("Text");
                    textGO.transform.SetParent(go.transform, false);
                    var text = textGO.AddComponent<TextMeshProUGUI>();
                    text.text = "Continue";
                    text.fontSize = 14;
                    text.color = buttonTextColor;
                    if (titleFont != null) text.font = titleFont;
                    text.alignment = TextAlignmentOptions.Center;
                    text.rectTransform.anchorMin = Vector2.zero;
                    text.rectTransform.anchorMax = Vector2.one;
                    text.rectTransform.offsetMin = Vector2.zero;
                    text.rectTransform.offsetMax = Vector2.zero;

                    continueButton = btn;
                }
                else
                {
                    continueButton = btnTransform.GetComponent<Button>();
                }
            }

            // 6. Ensure Close Button
            if (closeButton == null)
            {
                var btnTransform = contentParent.transform.Find("Close_Button");
                if (btnTransform == null)
                {
                    var go = new GameObject("Close_Button");
                    go.transform.SetParent(contentParent.transform, false);
                    var img = go.AddComponent<Image>();
                    ApplySprite(img, closeButtonSprite, closeButtonColor);
                    
                    var btn = go.AddComponent<Button>();
                    
                    var r = go.GetComponent<RectTransform>();
                    r.anchorMin = new Vector2(0.92f, 0.92f);
                    r.anchorMax = new Vector2(0.98f, 0.98f);
                    r.offsetMin = Vector2.zero;
                    r.offsetMax = Vector2.zero;
                    
                    // Text
                    var textGO = new GameObject("Text");
                    textGO.transform.SetParent(go.transform, false);
                    var text = textGO.AddComponent<TextMeshProUGUI>();
                    text.text = "X";
                    text.fontSize = 14;
                    text.color = textColor;
                    if (titleFont != null) text.font = titleFont;
                    text.alignment = TextAlignmentOptions.Center;
                    text.rectTransform.anchorMin = Vector2.zero;
                    text.rectTransform.anchorMax = Vector2.one;
                    text.rectTransform.offsetMin = Vector2.zero;
                    text.rectTransform.offsetMax = Vector2.zero;

                    closeButton = btn;
                }
                else
                {
                    closeButton = btnTransform.GetComponent<Button>();
                }
            }
            
            // 7. Ensure Effects Container
            if (effectsContainer == null)
            {
                var containerTransform = contentParent.transform.Find("Effects_Container");
                if (containerTransform == null)
                {
                    var go = new GameObject("Effects_Container");
                    go.transform.SetParent(contentParent.transform, false);
                    go.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0f);

                    var vlg = go.AddComponent<VerticalLayoutGroup>();
                    vlg.padding = new RectOffset(8, 8, 4, 4);
                    vlg.spacing = 4;
                    vlg.childAlignment = TextAnchor.UpperLeft;
                    vlg.childControlWidth = true;
                    vlg.childControlHeight = false;
                    vlg.childForceExpandWidth = true;
                    vlg.childForceExpandHeight = false;

                    var csf = go.AddComponent<ContentSizeFitter>();
                    csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

                    var r = go.GetComponent<RectTransform>();
                    r.anchorMin = new Vector2(0.05f, 0.2f);
                    r.anchorMax = new Vector2(0.95f, 0.45f);
                    r.offsetMin = Vector2.zero;
                    r.offsetMax = Vector2.zero;

                    containerTransform = go.transform;
                }
                effectsContainer = containerTransform;
            }

            // 8. Auto-load EffectIconDatabase from Resources if not assigned
            if (effectIconDatabase == null)
            {
                effectIconDatabase = UnityEngine.Resources.Load<EffectIconDatabaseSO>("EffectIconDatabaseSO");
                if (effectIconDatabase != null)
                    Debug.Log("[StorytellerUI] Auto-loaded EffectIconDatabaseSO from Resources.");
            }

            Debug.Log("[StorytellerUI] Auto-Setup Complete.");
        }
#endif

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------
        private void ApplySprite(Image img, Sprite sprite, Color fallback)
        {
            if (sprite != null)
            { img.sprite = sprite; img.type = Image.Type.Sliced; img.color = Color.white; }
            else
            { img.color = fallback; }
        }
    }
}

