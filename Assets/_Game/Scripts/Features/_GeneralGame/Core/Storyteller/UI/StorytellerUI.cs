using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace TheBunkerGames
{
    /// <summary>
    /// UI component for displaying story events.
    /// Works with LLMStoryEventData from the simplified storyteller system.
    /// </summary>
    public class StorytellerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private GameObject contentParent;
        
        [Header("Interaction")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private TMP_InputField inputField;
        
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
        }

        private void OnDisable()
        {
            StorytellerManager.OnStoryEventReceived -= ShowEvent;
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
            if (inputField != null) inputField.text = "";
            
            if (contentParent != null && autoToggleVisibility) 
                contentParent.SetActive(false);
        }

        private void ClearText()
        {
            if (titleText != null) titleText.text = "";
            if (descriptionText != null) descriptionText.text = "";
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
                    img.color = new Color(0, 0, 0, 0.9f);
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
                    txt.color = Color.white;
                    
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
                    txt.color = Color.white;
                    
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
                    img.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    
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
                    text.color = Color.white;
                    
                    // Placeholder
                    var placeholderGO = new GameObject("Placeholder");
                    placeholderGO.transform.SetParent(textArea.transform, false);
                    var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
                    placeholder.text = "Enter response...";
                    placeholder.fontSize = 14;
                    placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    placeholder.fontStyle = FontStyles.Italic;

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
                    img.color = Color.green;
                    
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
                    text.color = Color.black;
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
                    img.color = Color.red;
                    
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
                    text.color = Color.white;
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
            
            Debug.Log("[StorytellerUI] Auto-Setup Complete.");
        }
#endif
    }
}

