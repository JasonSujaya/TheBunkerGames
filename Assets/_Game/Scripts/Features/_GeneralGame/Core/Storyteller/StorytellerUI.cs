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
    }
}
