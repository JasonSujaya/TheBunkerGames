using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Test scene UI controller for exercising LLM API calls.
    /// Wire up UI references in the Inspector.
    /// </summary>
    public class LLMTestUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // UI References
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("UI References")]
        #endif
        [SerializeField] private TMP_Dropdown providerDropdown;
        [SerializeField] private TMP_InputField modelOverrideField;
        [SerializeField] private TMP_InputField promptInputField;
        [SerializeField] private TMP_InputField systemPromptField;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private TMP_Text responseText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private ScrollRect responseScrollRect;
        [SerializeField] private RawImage generatedImage;

        // -------------------------------------------------------------------------
        // Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private string defaultSystemPrompt = "You are a helpful assistant.";

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private bool isWaiting;
        private float requestStartTime;

        private enum TestMode
        {
            OpenRouterChat,
            OpenRouterImage,
            MistralChat
        }

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Start()
        {
            EnsureLLMServiceExists();
            SetupDropdown();
            WireButtons();
            SetStatus("Ready");

            if (systemPromptField != null)
            {
                systemPromptField.text = defaultSystemPrompt;
            }

            // Check config
            var config = LLMConfigDataSO.Instance;
            if (config == null)
            {
                SetStatus("ERROR: LLMConfigDataSO not found in Resources/LLM/");
                return;
            }

            if (!config.HasOpenRouterKey && !config.HasMistralKey)
            {
                SetStatus("Warning: No API keys configured in LLMConfigDataSO");
            }

            // Hide image display by default
            if (generatedImage != null)
            {
                generatedImage.gameObject.SetActive(false);
            }
        }

        // -------------------------------------------------------------------------
        // Setup
        // -------------------------------------------------------------------------

        private void SetupDropdown()
        {
            if (providerDropdown == null) return;

            providerDropdown.ClearOptions();
            providerDropdown.AddOptions(new List<string>
            {
                "OpenRouter Chat",
                "OpenRouter Image",
                "Mistral Chat"
            });
        }

        private void WireButtons()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendClicked);
            if (clearButton != null)
                clearButton.onClick.AddListener(OnClearClicked);
        }

        // -------------------------------------------------------------------------
        // Button Handlers
        // -------------------------------------------------------------------------

        private void OnSendClicked()
        {
            if (isWaiting) return;

            string prompt = promptInputField != null ? promptInputField.text : "";
            if (string.IsNullOrWhiteSpace(prompt))
            {
                SetStatus("Please enter a prompt.");
                return;
            }

            var mode = (TestMode)(providerDropdown != null ? providerDropdown.value : 0);
            string modelOverride = modelOverrideField != null && !string.IsNullOrWhiteSpace(modelOverrideField.text)
                ? modelOverrideField.text
                : null;
            string systemPrompt = systemPromptField != null ? systemPromptField.text : null;

            isWaiting = true;
            requestStartTime = Time.realtimeSinceStartup;
            SetStatus("Sending...");
            SetSendButtonInteractable(false);

            switch (mode)
            {
                case TestMode.OpenRouterChat:
                    SendChatRequest(LLMProvider.OpenRouter, prompt, systemPrompt, modelOverride);
                    break;

                case TestMode.OpenRouterImage:
                    SendImageRequest(prompt, modelOverride);
                    break;

                case TestMode.MistralChat:
                    SendChatRequest(LLMProvider.Mistral, prompt, systemPrompt, modelOverride);
                    break;
            }
        }

        private void OnClearClicked()
        {
            if (responseText != null) responseText.text = "";
            if (generatedImage != null) generatedImage.gameObject.SetActive(false);
            SetStatus("Ready");
        }

        // -------------------------------------------------------------------------
        // Request Methods
        // -------------------------------------------------------------------------

        private void SendChatRequest(LLMProvider provider, string prompt, string systemPrompt, string modelOverride)
        {
            var messages = new List<LLMMessage>();

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(LLMMessage.System(systemPrompt));
            }

            messages.Add(LLMMessage.User(prompt));

            LLMService.Instance.SendChat(provider, messages, OnChatResponse, modelOverride);
        }

        private void SendImageRequest(string prompt, string modelOverride)
        {
            LLMService.Instance.SendOpenRouterImageRequest(prompt, OnImageResponse, modelOverride);
        }

        // -------------------------------------------------------------------------
        // Callbacks
        // -------------------------------------------------------------------------

        private void OnChatResponse(LLMResult<LLMChatResponse> result)
        {
            float elapsed = Time.realtimeSinceStartup - requestStartTime;
            isWaiting = false;
            SetSendButtonInteractable(true);

            if (generatedImage != null)
            {
                generatedImage.gameObject.SetActive(false);
            }

            if (result.Success)
            {
                string content = result.Data.FirstMessageContent ?? "(empty response)";
                if (responseText != null) responseText.text = content;

                string usage = result.Data.usage != null
                    ? $"Tokens: {result.Data.usage.prompt_tokens}/{result.Data.usage.completion_tokens}/{result.Data.usage.total_tokens}"
                    : "Tokens: N/A";
                string model = result.Data.model ?? "unknown";

                SetStatus($"OK | {model} | {usage} | {elapsed:F2}s");
            }
            else
            {
                if (responseText != null)
                {
                    responseText.text = $"<color=red>Error:</color>\n{result.Error}";
                }
                SetStatus($"Error (HTTP {result.HttpStatusCode}) | {elapsed:F2}s");
            }

            ScrollToTop();
        }

        private void OnImageResponse(LLMResult<LLMChatResponse> result)
        {
            float elapsed = Time.realtimeSinceStartup - requestStartTime;
            isWaiting = false;
            SetSendButtonInteractable(true);

            if (result.Success)
            {
                string content = result.Data.FirstMessageContent ?? "";
                if (responseText != null) responseText.text = content;

                // Try to extract and display an image URL from the response
                string imageUrl = ExtractImageUrl(content);
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    StartCoroutine(DownloadAndDisplayImage(imageUrl));
                }

                string model = result.Data.model ?? "unknown";
                SetStatus($"OK | {model} | {elapsed:F2}s");
            }
            else
            {
                if (responseText != null)
                {
                    responseText.text = $"<color=red>Error:</color>\n{result.Error}";
                }
                if (generatedImage != null) generatedImage.gameObject.SetActive(false);
                SetStatus($"Error (HTTP {result.HttpStatusCode}) | {elapsed:F2}s");
            }

            ScrollToTop();
        }

        // -------------------------------------------------------------------------
        // Image Helpers
        // -------------------------------------------------------------------------

        private string ExtractImageUrl(string content)
        {
            if (string.IsNullOrEmpty(content)) return null;

            // Try markdown image: ![...](url)
            int mdStart = content.IndexOf("](");
            if (mdStart >= 0)
            {
                int urlStart = mdStart + 2;
                int urlEnd = content.IndexOf(')', urlStart);
                if (urlEnd > urlStart)
                {
                    return content.Substring(urlStart, urlEnd - urlStart);
                }
            }

            // Try raw URL starting with http
            foreach (string line in content.Split('\n'))
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("http://") || trimmed.StartsWith("https://"))
                {
                    return trimmed;
                }
            }

            return null;
        }

        private IEnumerator DownloadAndDisplayImage(string url)
        {
            if (generatedImage == null) yield break;

            SetStatus("Downloading image...");

            using var request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                generatedImage.texture = texture;
                generatedImage.gameObject.SetActive(true);
                SetStatus("Image loaded");
            }
            else
            {
                Debug.LogError($"[LLMTestUI] Failed to download image: {request.error}");
                SetStatus($"Image download failed: {request.error}");
            }
        }

        // -------------------------------------------------------------------------
        // UI Helpers
        // -------------------------------------------------------------------------

        private void SetStatus(string text)
        {
            if (statusText != null) statusText.text = text;
        }

        private void SetSendButtonInteractable(bool interactable)
        {
            if (sendButton != null) sendButton.interactable = interactable;
        }

        private void ScrollToTop()
        {
            if (responseScrollRect != null)
            {
                responseScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void EnsureLLMServiceExists()
        {
            if (LLMService.Instance == null)
            {
                var go = new GameObject("[LLMService]");
                go.AddComponent<LLMService>();
                Debug.Log("[LLMTestUI] Created LLMService instance.");
            }
        }
    }
}
