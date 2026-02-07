using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// A single row in the inbox-style task list.
    /// Displays category name, challenge title, and saved/draft status.
    /// The entire row is clickable to open the detail view.
    /// </summary>
    public class PlayerActionListItem : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // References
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("List Item References")]
        #endif
        [Header("Display")]
        [SerializeField] private TMP_Text categoryLabel;
        [SerializeField] private TMP_Text challengeTitleText;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Image backgroundImage;

        [Header("Interaction")]
        [SerializeField] private Button selectButton;

        // -------------------------------------------------------------------------
        // Runtime State
        // -------------------------------------------------------------------------
        private PlayerActionCategory category;
        private bool isSaved;
        private bool isSelected;
        private Action<PlayerActionCategory> onClickedCallback;

        // -------------------------------------------------------------------------
        // Colors
        // -------------------------------------------------------------------------
        private static readonly Color DraftBgColor = new Color(0.12f, 0.12f, 0.18f, 1f);
        private static readonly Color SavedBgColor = new Color(0.08f, 0.18f, 0.08f, 1f);
        private static readonly Color SelectedBgColor = new Color(0.15f, 0.15f, 0.25f, 1f);
        private static readonly Color DraftStatusColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        private static readonly Color SavedStatusColor = new Color(0.4f, 1f, 0.4f, 1f);
        private static readonly Color ProcessingStatusColor = new Color(1f, 0.8f, 0.2f, 1f);

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public PlayerActionCategory Category => category;
        public bool IsSaved => isSaved;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(OnClicked);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Initialize the list item for a category.
        /// </summary>
        public void Initialize(PlayerActionCategory cat, string displayName,
                               string challengeTitle, Action<PlayerActionCategory> onClicked)
        {
            category = cat;
            onClickedCallback = onClicked;
            isSaved = false;
            isSelected = false;

            if (categoryLabel != null)
                categoryLabel.text = displayName;

            if (challengeTitleText != null)
                challengeTitleText.text = challengeTitle ?? "No Challenge";

            SetSaved(false);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Update the saved/draft visual state.
        /// </summary>
        public void SetSaved(bool saved)
        {
            isSaved = saved;
            UpdateVisuals();
        }

        /// <summary>
        /// Highlight when this item's detail panel is being viewed.
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            UpdateVisuals();
        }

        /// <summary>
        /// Show processing state (after Submit All).
        /// </summary>
        public void SetProcessing()
        {
            if (statusLabel != null)
            {
                statusLabel.text = "Processing...";
                statusLabel.color = ProcessingStatusColor;
            }
        }

        /// <summary>
        /// Show completed state (after LLM result received).
        /// </summary>
        public void SetComplete()
        {
            if (statusLabel != null)
            {
                statusLabel.text = "Complete";
                statusLabel.color = SavedStatusColor;
            }

            if (backgroundImage != null)
                backgroundImage.color = SavedBgColor;
        }

        /// <summary>
        /// Hide this list item (for inactive categories).
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------------
        private void OnClicked()
        {
            onClickedCallback?.Invoke(category);
        }

        private void UpdateVisuals()
        {
            // Status label
            if (statusLabel != null)
            {
                if (isSaved)
                {
                    statusLabel.text = "Saved \u2713";
                    statusLabel.color = SavedStatusColor;
                }
                else
                {
                    statusLabel.text = "Draft";
                    statusLabel.color = DraftStatusColor;
                }
            }

            // Background color
            if (backgroundImage != null)
            {
                if (isSelected)
                    backgroundImage.color = SelectedBgColor;
                else if (isSaved)
                    backgroundImage.color = SavedBgColor;
                else
                    backgroundImage.color = DraftBgColor;
            }
        }
    }
}
