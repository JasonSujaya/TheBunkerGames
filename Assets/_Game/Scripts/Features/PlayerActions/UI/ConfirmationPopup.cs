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
    /// Reusable confirmation popup with Confirm/Cancel buttons.
    /// Shows a full-screen overlay to block input behind it.
    /// Usage: confirmationPopup.Show("Are you sure?", onConfirm, onCancel);
    /// </summary>
    public class ConfirmationPopup : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // References
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Popup References")]
        #endif
        [Header("Popup")]
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Image overlayBackground;

        [Header("Content")]
        [SerializeField] private TMP_Text messageText;

        [Header("Buttons")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text confirmButtonText;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text cancelButtonText;

        // -------------------------------------------------------------------------
        // Runtime State
        // -------------------------------------------------------------------------
        private Action onConfirmCallback;
        private Action onCancelCallback;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public bool IsVisible => popupRoot != null && popupRoot.activeSelf;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClicked);

            Hide();
        }

        private void OnDestroy()
        {
            if (confirmButton != null)
                confirmButton.onClick.RemoveListener(OnConfirmClicked);
            if (cancelButton != null)
                cancelButton.onClick.RemoveListener(OnCancelClicked);
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Show the confirmation popup with a message and callbacks.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="onConfirm">Called when the user clicks Confirm.</param>
        /// <param name="onCancel">Called when the user clicks Cancel (optional).</param>
        /// <param name="confirmLabel">Text for the confirm button (default "Confirm").</param>
        /// <param name="cancelLabel">Text for the cancel button (default "Cancel").</param>
        public void Show(string message, Action onConfirm, Action onCancel = null,
                         string confirmLabel = "Confirm", string cancelLabel = "Cancel")
        {
            onConfirmCallback = onConfirm;
            onCancelCallback = onCancel;

            if (messageText != null)
                messageText.text = message;

            if (confirmButtonText != null)
                confirmButtonText.text = confirmLabel;

            if (cancelButtonText != null)
                cancelButtonText.text = cancelLabel;

            if (popupRoot != null)
                popupRoot.SetActive(true);
        }

        /// <summary>
        /// Hide the popup and clear callbacks.
        /// </summary>
        public void Hide()
        {
            if (popupRoot != null)
                popupRoot.SetActive(false);

            onConfirmCallback = null;
            onCancelCallback = null;
        }

        // -------------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------------
        private void OnConfirmClicked()
        {
            var callback = onConfirmCallback;
            Hide();
            callback?.Invoke();
        }

        private void OnCancelClicked()
        {
            var callback = onCancelCallback;
            Hide();
            callback?.Invoke();
        }
    }
}
