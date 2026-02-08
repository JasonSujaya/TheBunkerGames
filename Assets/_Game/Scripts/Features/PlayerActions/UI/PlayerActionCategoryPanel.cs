using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Data holder and UI bridge for a single player action category.
    /// Stores the challenge, player input, selected items, and saved/submitted state.
    /// In the inbox UI, this acts as the data backing for the shared detail panel.
    /// </summary>
    public class PlayerActionCategoryPanel : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Panel Config")]
        #endif
        [Header("Category")]
        [SerializeField] private PlayerActionCategory category;

        #if ODIN_INSPECTOR
        [Title("UI References")]
        #endif
        [Header("Challenge Display")]
        [SerializeField] private TMP_Text categoryLabel;
        [SerializeField] private TMP_Text challengeTitleText;
        [SerializeField] private TMP_Text challengeDescriptionText;

        [Header("Player Input")]
        [SerializeField] private TMP_InputField playerInputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private TMP_Text submitButtonText;

        #if ODIN_INSPECTOR
        [Title("Player Input (Live)")]
        [InfoBox("This field is synced with the TMP_InputField at runtime. Edit here or in the game UI.")]
        [MultiLineProperty(4)]
        [OnValueChanged("SyncInputToField")]
        #endif
        [TextArea(2, 6)]
        [SerializeField] private string playerInputText = "";

        [Header("Item Selection (Optional)")]
        [SerializeField] private Transform itemToggleContainer;
        [SerializeField] private GameObject itemTogglePrefab;

        [Header("Result Display")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultTitleText;
        [SerializeField] private TMP_Text resultDescriptionText;
        [SerializeField] private TMP_Text resultEffectsText;

        [Header("Status")]
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private TMP_Text statusText;

        // -------------------------------------------------------------------------
        // Runtime State
        // -------------------------------------------------------------------------
        private PlayerActionChallenge currentChallenge;
        private string familyTarget;
        private List<string> selectedItems = new List<string>();
        private bool isSaved;
        private bool isSubmitted;
        private bool hasResult;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public PlayerActionCategory Category => category;
        public bool IsSaved => isSaved;
        public bool IsSubmitted => isSubmitted;
        public bool HasResult => hasResult;
        public string PlayerInput => playerInputText ?? "";
        public PlayerActionChallenge CurrentChallenge => currentChallenge;
        public string FamilyTarget => familyTarget;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (submitButton != null)
                submitButton.onClick.AddListener(OnSubmitClicked);

            if (playerInputField != null)
                playerInputField.onValueChanged.AddListener(SyncInputFromField);

            // Push any pre-filled editor text into the input field
            if (playerInputField != null && !string.IsNullOrEmpty(playerInputText))
                playerInputField.text = playerInputText;

            HideResult();
            HideLoading();
        }

        private void OnDestroy()
        {
            if (submitButton != null)
                submitButton.onClick.RemoveListener(OnSubmitClicked);

            if (playerInputField != null)
                playerInputField.onValueChanged.RemoveListener(SyncInputFromField);
        }

        // -------------------------------------------------------------------------
        // Input Sync (Inspector ↔ TMP_InputField)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Called when the TMP_InputField value changes at runtime.
        /// Syncs the value back to the serialized inspector field and manager.
        /// </summary>
        private void SyncInputFromField(string newValue)
        {
            playerInputText = newValue;

            // Sync to PlayerActionManager so its inspector stays up to date
            if (PlayerActionManager.Instance != null)
                PlayerActionManager.Instance.SetInput(category, newValue);
        }

        /// <summary>
        /// Called when the inspector field changes (Odin OnValueChanged).
        /// Pushes the value to the TMP_InputField and manager.
        /// </summary>
        private void SyncInputToField()
        {
            if (playerInputField != null && playerInputField.text != playerInputText)
                playerInputField.text = playerInputText ?? "";

            if (PlayerActionManager.Instance != null)
                PlayerActionManager.Instance.SetInput(category, playerInputText ?? "");
        }

        /// <summary>
        /// Set the input text programmatically (e.g., from the shared detail panel).
        /// </summary>
        public void SetInputText(string text)
        {
            playerInputText = text ?? "";
            if (playerInputField != null)
                playerInputField.text = playerInputText;
            if (PlayerActionManager.Instance != null)
                PlayerActionManager.Instance.SetInput(category, playerInputText);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Setup the panel with a challenge for the day.
        /// </summary>
        public void Setup(PlayerActionChallenge challenge, string target = null)
        {
            currentChallenge = challenge;
            familyTarget = target;
            isSaved = false;
            isSubmitted = false;
            hasResult = false;
            selectedItems.Clear();

            // Category label
            if (categoryLabel != null)
            {
                switch (category)
                {
                    case PlayerActionCategory.Exploration:
                        categoryLabel.text = "EXPLORATION";
                        break;
                    case PlayerActionCategory.Dilemma:
                        categoryLabel.text = "DILEMMA";
                        break;
                    case PlayerActionCategory.FamilyRequest:
                        categoryLabel.text = $"FAMILY REQUEST ({target ?? "?"})";
                        break;
                }
            }

            // Challenge text
            if (challengeTitleText != null)
                challengeTitleText.text = challenge != null ? challenge.Title : "No Challenge";

            if (challengeDescriptionText != null)
            {
                string desc = challenge != null ? challenge.GetDescription(target) : "";
                challengeDescriptionText.text = desc;
            }

            // Reset input
            playerInputText = "";
            if (playerInputField != null)
            {
                playerInputField.text = "";
                playerInputField.interactable = true;
            }

            // Reset button
            if (submitButton != null)
                submitButton.interactable = true;
            if (submitButtonText != null)
                submitButtonText.text = "Save";

            // Status
            SetStatus("Type your response...");
            HideResult();
            HideLoading();

            // Populate item toggles
            PopulateItemToggles();

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Get the category display name.
        /// </summary>
        public string GetCategoryDisplayName()
        {
            switch (category)
            {
                case PlayerActionCategory.Exploration: return "EXPLORATION";
                case PlayerActionCategory.Dilemma: return "DILEMMA";
                case PlayerActionCategory.FamilyRequest:
                    return string.IsNullOrEmpty(familyTarget)
                        ? "FAMILY REQUEST"
                        : $"FAMILY REQUEST ({familyTarget})";
                default: return category.ToString().ToUpper();
            }
        }

        // -------------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------------

        /// <summary>
        /// Validate the current input. Returns true if valid.
        /// Sets status text with error message if invalid.
        /// </summary>
        public bool ValidateInput()
        {
            string input = (playerInputText ?? "").Trim();

            if (category == PlayerActionCategory.Dilemma && string.IsNullOrEmpty(input))
            {
                SetStatus("You must respond to the dilemma!");
                return false;
            }

            if (category == PlayerActionCategory.Exploration && string.IsNullOrEmpty(input))
            {
                SetStatus("Describe your approach!");
                return false;
            }

            return true;
        }

        // -------------------------------------------------------------------------
        // Save (locks input, does NOT submit to LLM)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Save the current input. Locks the input field and marks as saved.
        /// Does NOT submit to LLM — that happens via PlayerActionManager.SubmitAllActions().
        /// </summary>
        public void SaveInput()
        {
            if (isSaved) 
            {
                Debug.LogWarning($"[PlayerActionCategoryPanel] SaveInput called but panel is already saved! Ignoring.");
                return;
            }

            isSaved = true;

            // Lock input
            if (playerInputField != null)
                playerInputField.interactable = false;
            if (submitButton != null)
                submitButton.interactable = false;
            if (submitButtonText != null)
                submitButtonText.text = "Saved";

            SetStatus("Response saved and locked.");

            // Sync to manager
            if (PlayerActionManager.Instance != null)
            {
                Debug.Log($"[PlayerActionCategoryPanel] Saving to Manager: {category}");
                PlayerActionManager.Instance.SaveAction(category, (playerInputText ?? "").Trim(), GetSelectedItems());
            }
            else
            {
                Debug.LogError("[PlayerActionCategoryPanel] PlayerActionManager instance is null!");
            }
        }

        /// <summary>
        /// Show the result from the LLM.
        /// </summary>
        public void ShowResult(PlayerActionResult result)
        {
            hasResult = true;
            HideLoading();

            if (result == null) return;

            if (result.HasError)
            {
                SetStatus($"Error: {result.Error}");
                return;
            }

            if (resultPanel != null)
                resultPanel.SetActive(true);

            if (result.StoryEvent != null)
            {
                if (resultTitleText != null)
                    resultTitleText.text = result.StoryEvent.Title;

                if (resultDescriptionText != null)
                    resultDescriptionText.text = result.StoryEvent.Description;

                if (resultEffectsText != null)
                {
                    var effectStrings = new List<string>();
                    if (result.StoryEvent.Effects != null)
                    {
                        foreach (var effect in result.StoryEvent.Effects)
                        {
                            string target = string.IsNullOrEmpty(effect.Target) ? "All" : effect.Target;
                            effectStrings.Add($"{effect.EffectType} ({effect.Intensity}) -> {target}");
                        }
                    }
                    resultEffectsText.text = effectStrings.Count > 0
                        ? string.Join("\n", effectStrings)
                        : "No effects.";
                }
            }

            SetStatus("Complete!");
        }

        /// <summary>
        /// Hide this panel.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Reset the panel to its default state.
        /// </summary>
        public void ResetPanel()
        {
            isSaved = false;
            isSubmitted = false;
            hasResult = false;
            selectedItems.Clear();

            playerInputText = "";
            if (playerInputField != null)
            {
                playerInputField.text = "";
                playerInputField.interactable = true;
            }
            if (submitButton != null)
                submitButton.interactable = true;
            if (submitButtonText != null)
                submitButtonText.text = "Save";

            HideResult();
            HideLoading();
            SetStatus("");
        }

        // -------------------------------------------------------------------------
        // Item Toggles
        // -------------------------------------------------------------------------
        private void PopulateItemToggles()
        {
            // Clear existing toggles
            if (itemToggleContainer != null)
            {
                for (int i = itemToggleContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(itemToggleContainer.GetChild(i).gameObject);
                }
            }

            selectedItems.Clear();

            if (itemToggleContainer == null || itemTogglePrefab == null) return;
            if (InventoryManager.Instance == null) return;

            var inventory = InventoryManager.Instance.Items;
            if (inventory == null || inventory.Count == 0) return;

            foreach (var slot in inventory)
            {
                if (slot.Quantity <= 0) continue;

                var toggleObj = Instantiate(itemTogglePrefab, itemToggleContainer);
                var toggle = toggleObj.GetComponent<Toggle>();
                var label = toggleObj.GetComponentInChildren<TMP_Text>();

                string itemName = slot.ItemId;
                if (ItemManager.Instance != null)
                {
                    var itemData = ItemManager.Instance.GetItem(slot.ItemId);
                    if (itemData != null)
                        itemName = $"{itemData.ItemName} ({itemData.Type}) x{slot.Quantity}";
                }

                if (label != null)
                    label.text = itemName;

                if (toggle != null)
                {
                    toggle.isOn = false;
                    string itemId = slot.ItemId;
                    toggle.onValueChanged.AddListener((isOn) =>
                    {
                        if (isOn && !selectedItems.Contains(itemId))
                            selectedItems.Add(itemId);
                        else if (!isOn)
                            selectedItems.Remove(itemId);
                    });
                }
            }
        }

        /// <summary>
        /// Get the currently selected item IDs.
        /// </summary>
        public List<string> GetSelectedItems()
        {
            return new List<string>(selectedItems);
        }

        // -------------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------------

        /// <summary>
        /// Legacy submit handler — kept for backward compatibility.
        /// Now triggers save flow through the parent UI instead of direct LLM submission.
        /// </summary>
        private void OnSubmitClicked()
        {
            if (isSaved || isSubmitted) return;

            if (!ValidateInput()) return;

            // In the new inbox flow, the parent PlayerActionUI handles the
            // save confirmation popup. But if no parent is managing us,
            // fall back to direct save.
            SaveInput();
        }

        private void ShowLoading()
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(true);
        }

        private void HideLoading()
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }

        private void HideResult()
        {
            if (resultPanel != null)
                resultPanel.SetActive(false);
        }

        public void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text;
        }
    }
}
