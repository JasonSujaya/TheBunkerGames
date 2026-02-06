using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using Neocortex;
using Neocortex.Data;

namespace TheBunkerGames
{
    /// <summary>
    /// Simple integrator script to test Neocortex AI connection.
    /// Provides Odin Inspector buttons for sending messages and logs responses.
    /// </summary>
    public class NeocortexIntegrator : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Neocortex Settings")]
        #endif
        [SerializeField] private NeocortexSmartAgent smartAgent;

        #if ODIN_INSPECTOR
        [Title("Test Message")]
        [TextArea(2, 5)]
        #endif
        [SerializeField] private string messageToSend = "Hello, Neocortex!";

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void OnEnable()
        {
            if (smartAgent == null)
            {
                smartAgent = GetComponent<NeocortexSmartAgent>();
            }

            if (smartAgent != null)
            {
                smartAgent.OnChatResponseReceived.AddListener(OnResponseReceived);
                smartAgent.OnRequestFailed.AddListener(OnRequestFailed);
            }
            else
            {
                Debug.LogWarning("[NeocortexIntegrator] No NeocortexSmartAgent found. Please assign one.");
            }
        }

        private void OnDisable()
        {
            if (smartAgent != null)
            {
                smartAgent.OnChatResponseReceived.RemoveListener(OnResponseReceived);
                smartAgent.OnRequestFailed.RemoveListener(OnRequestFailed);
            }
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void SendNeocortexMessage(string message)
        {
            if (smartAgent == null)
            {
                Debug.LogError("[NeocortexIntegrator] Cannot send message: NeocortexSmartAgent is not assigned.");
                return;
            }

            Debug.Log($"[NeocortexIntegrator] Sending message: {message}");
            smartAgent.TextToText(message);
        }

        // -------------------------------------------------------------------------
        // Event Callbacks
        // -------------------------------------------------------------------------
        private void OnResponseReceived(ChatResponse response)
        {
            Debug.Log($"[NeocortexIntegrator] Neocortex Response: {response.message}");
            if (!string.IsNullOrEmpty(response.action))
            {
                Debug.Log($"[NeocortexIntegrator] Action: {response.action}");
            }
        }

        private void OnRequestFailed(string error)
        {
            Debug.LogError($"[NeocortexIntegrator] Request Failed: {error}");
        }

        // -------------------------------------------------------------------------
        // Debug / Editor Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Send Test Message", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        private void Debug_SendTestMessage()
        {
            if (Application.isPlaying)
            {
                SendNeocortexMessage(messageToSend);
            }
            else
            {
                Debug.LogWarning("[NeocortexIntegrator] Please enter Play Mode to test this action.");
            }
        }
        #endif
    }
}
