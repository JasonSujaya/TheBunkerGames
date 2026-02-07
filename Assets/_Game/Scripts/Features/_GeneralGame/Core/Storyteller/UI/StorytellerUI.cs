using UnityEngine;
using TMPro;

namespace TheBunkerGames
{
    public class StorytellerUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private GameObject contentParent;

        private void Awake()
        {
            // Start hidden
            if (contentParent != null) contentParent.SetActive(false);
            ClearText();
        }

        private void OnEnable()
        {
            StorytellerManager.OnStoryEventTriggered += UpdateUI;
        }

        private void OnDisable()
        {
            StorytellerManager.OnStoryEventTriggered -= UpdateUI;
        }

        private void UpdateUI(StoryEventSO storyEvent)
        {
            if (storyEvent != null)
            {
                if (titleText != null) titleText.text = storyEvent.Title;
                if (descriptionText != null) descriptionText.text = storyEvent.Description;
                if (contentParent != null) contentParent.SetActive(true);
            }
            else
            {
                ClearText();
                if (contentParent != null) contentParent.SetActive(false);
            }
        }

        private void ClearText()
        {
            if (titleText != null) titleText.text = "";
            if (descriptionText != null) descriptionText.text = "";
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("GameObject/TheBunkerGames/UI/Storyteller Panel", false, 10)]
        public static void CreateStorytellerUI()
        {
            // 1. Find or Create Canvas
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // 2. Create Panel
            var panelGO = new GameObject("Storyteller_Panel");
            panelGO.transform.SetParent(canvas.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.3f);
            panelRect.anchorMax = new Vector2(0.7f, 0.7f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            
            // Add Background
            var bgImage = panelGO.AddComponent<UnityEngine.UI.Image>();
            bgImage.color = new Color(0, 0, 0, 0.9f);

            // 3. Create Title
            var titleGO = new GameObject("Title_Text");
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "Event Title";
            titleText.fontSize = 24;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontStyle = FontStyles.Bold;
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // 4. Create Description
            var descGO = new GameObject("Description_Text");
            descGO.transform.SetParent(panelGO.transform, false);
            var descText = descGO.AddComponent<TextMeshProUGUI>();
            descText.text = "Event Description...";
            descText.fontSize = 18;
            descText.alignment = TextAlignmentOptions.TopLeft;
            var descRect = descText.rectTransform;
            descRect.anchorMin = new Vector2(0.05f, 0.05f);
            descRect.anchorMax = new Vector2(0.95f, 0.8f);
            descRect.offsetMin = Vector2.zero;
            descRect.offsetMax = Vector2.zero;

            // 5. Add Component
            // Note: In Editor, we can't AddComponent if the script is not compiled yet or found.
            // But since this code IS inside the component script, it's fine.
            var ui = panelGO.AddComponent<StorytellerUI>();
            ui.titleText = titleText;
            ui.descriptionText = descText;
            ui.contentParent = panelGO;

            // Select it
            UnityEditor.Selection.activeGameObject = panelGO;
            Debug.Log("[StorytellerUI] Created UI hierarchy.");
        }
#endif
    }
}
