using UnityEngine;
using TMPro;

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
        [SerializeField] private bool autoToggleVisibility = true;

        private void Awake()
        {
            // Start hidden
            if (contentParent != null) contentParent.SetActive(false);
            ClearText();
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
                    r.anchorMin = new Vector2(0.05f, 0.05f);
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
            
            Debug.Log("[StorytellerUI] Auto-Setup Complete.");
        }
#endif
    }
}

