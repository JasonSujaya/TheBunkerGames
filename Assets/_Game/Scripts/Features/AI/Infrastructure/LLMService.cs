using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Singleton service for sending LLM requests to OpenRouter and Mistral.
    /// Other game systems call LLMService.Instance methods to interact with AI APIs.
    /// </summary>
    public class LLMService : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static LLMService Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        [Required("LLM Config is required for this service to function")]
        #endif
        [SerializeField] private LLMConfigDataSO llmConfig;

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public event Action<LLMResult<LLMChatResponse>> OnChatResponseReceived;
        public event Action<LLMResult<LLMChatResponse>> OnImageResponseReceived;
        public event Action<string> OnRequestFailed;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize the LLMConfig singleton
            if (llmConfig != null)
            {
                LLMConfigDataSO.SetInstance(llmConfig);
            }
            else
            {
                Debug.LogError("[LLMService] LLMConfig reference is not assigned in Inspector!");
            }
        }

        // -------------------------------------------------------------------------
        // Public Methods — OpenRouter
        // -------------------------------------------------------------------------

        /// <summary>
        /// Send a chat completion request to OpenRouter.
        /// </summary>
        public void SendOpenRouterChat(
            List<LLMMessage> messages,
            Action<LLMResult<LLMChatResponse>> callback = null,
            string modelOverride = null,
            float? temperature = null,
            int? maxTokens = null)
        {
            SendChat(LLMProvider.OpenRouter, messages, callback, modelOverride, temperature, maxTokens);
        }

        /// <summary>
        /// Send an image generation request to OpenRouter.
        /// Uses the configured default image model.
        /// </summary>
        public void SendOpenRouterImageRequest(
            string prompt,
            Action<LLMResult<LLMChatResponse>> callback = null,
            string modelOverride = null)
        {
            var config = LLMConfigDataSO.Instance;
            if (config == null) return;

            string model = !string.IsNullOrEmpty(modelOverride)
                ? modelOverride
                : config.OpenRouterDefaultImageModel;

            var messages = new List<LLMMessage> { LLMMessage.User(prompt) };

            var request = new LLMChatRequest
            {
                model = model,
                messages = messages
            };

            StartCoroutine(SendRequestCoroutine(LLMProvider.OpenRouter, request, result =>
            {
                callback?.Invoke(result);
                OnImageResponseReceived?.Invoke(result);

                if (!result.Success)
                {
                    OnRequestFailed?.Invoke(result.Error);
                }
            }));
        }

        // -------------------------------------------------------------------------
        // Public Methods — Mistral
        // -------------------------------------------------------------------------

        /// <summary>
        /// Send a chat completion request to Mistral.
        /// </summary>
        public void SendMistralChat(
            List<LLMMessage> messages,
            Action<LLMResult<LLMChatResponse>> callback = null,
            string modelOverride = null,
            float? temperature = null,
            int? maxTokens = null)
        {
            SendChat(LLMProvider.Mistral, messages, callback, modelOverride, temperature, maxTokens);
        }

        // -------------------------------------------------------------------------
        // Public Methods — Generic
        // -------------------------------------------------------------------------

        /// <summary>
        /// Send a chat completion request to the specified provider.
        /// </summary>
        public void SendChat(
            LLMProvider provider,
            List<LLMMessage> messages,
            Action<LLMResult<LLMChatResponse>> callback = null,
            string modelOverride = null,
            float? temperature = null,
            int? maxTokens = null)
        {
            var config = LLMConfigDataSO.Instance;
            if (config == null) return;

            string apiKey = config.GetApiKey(provider);
            if (string.IsNullOrEmpty(apiKey))
            {
                string error = $"[LLMService] No API key configured for {provider}.";
                Debug.LogError(error);
                var fail = LLMResult<LLMChatResponse>.Fail(error);
                callback?.Invoke(fail);
                OnRequestFailed?.Invoke(error);
                return;
            }

            string model = !string.IsNullOrEmpty(modelOverride)
                ? modelOverride
                : config.GetDefaultChatModel(provider);

            var request = new LLMChatRequest
            {
                model = model,
                messages = messages,
                temperature = temperature,
                max_tokens = maxTokens
            };

            StartCoroutine(SendRequestCoroutine(provider, request, result =>
            {
                callback?.Invoke(result);
                OnChatResponseReceived?.Invoke(result);

                if (!result.Success)
                {
                    OnRequestFailed?.Invoke(result.Error);
                }
            }));
        }

        /// <summary>
        /// Convenience method for a single prompt with optional system prompt.
        /// Returns just the text content via callbacks.
        /// </summary>
        public void QuickChat(
            LLMProvider provider,
            string prompt,
            Action<string> onSuccess,
            Action<string> onError = null,
            string systemPrompt = null)
        {
            var messages = new List<LLMMessage>();

            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(LLMMessage.System(systemPrompt));
            }

            messages.Add(LLMMessage.User(prompt));

            SendChat(provider, messages, result =>
            {
                if (result.Success)
                {
                    onSuccess?.Invoke(result.Data.FirstMessageContent ?? "");
                }
                else
                {
                    onError?.Invoke(result.Error);
                }
            });
        }

        // -------------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------------

        private IEnumerator SendRequestCoroutine(
            LLMProvider provider,
            LLMChatRequest request,
            Action<LLMResult<LLMChatResponse>> callback)
        {
            var config = LLMConfigDataSO.Instance;
            if (config == null) yield break;

            string url = config.GetBaseUrl(provider).TrimEnd('/') + "/chat/completions";
            string jsonBody = JsonConvert.SerializeObject(request);
            var headers = BuildHeaders(provider, config);

            if (config.EnableDebugLogs)
            {
                Debug.Log($"[LLMService] {provider} request to {url}\nModel: {request.model}\nBody: {jsonBody}");
            }

            float startTime = Time.realtimeSinceStartup;

            yield return LLMWebRequestHelper.Post(
                url, jsonBody, headers, config.RequestTimeoutSeconds,
                (responseBody, statusCode, isError) =>
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;

                    if (config.EnableDebugLogs)
                    {
                        Debug.Log($"[LLMService] {provider} response ({elapsed:F2}s, HTTP {statusCode}):\n{responseBody}");
                    }

                    if (isError)
                    {
                        string errorMsg = ParseErrorMessage(responseBody, statusCode);
                        Debug.LogError($"[LLMService] {provider} error: {errorMsg}");
                        callback?.Invoke(LLMResult<LLMChatResponse>.Fail(errorMsg, statusCode));
                    }
                    else
                    {
                        try
                        {
                            var response = JsonConvert.DeserializeObject<LLMChatResponse>(responseBody);
                            callback?.Invoke(LLMResult<LLMChatResponse>.Ok(response, statusCode));
                        }
                        catch (JsonException ex)
                        {
                            string errorMsg = $"Failed to parse response: {ex.Message}\nRaw: {responseBody}";
                            Debug.LogError($"[LLMService] {errorMsg}");
                            callback?.Invoke(LLMResult<LLMChatResponse>.Fail(errorMsg, statusCode));
                        }
                    }
                });
        }

        private Dictionary<string, string> BuildHeaders(LLMProvider provider, LLMConfigDataSO config)
        {
            var headers = new Dictionary<string, string>
            {
                { "Authorization", $"Bearer {config.GetApiKey(provider)}" }
            };

            if (provider == LLMProvider.OpenRouter)
            {
                if (!string.IsNullOrEmpty(config.HttpReferer))
                {
                    headers["HTTP-Referer"] = config.HttpReferer;
                }
                if (!string.IsNullOrEmpty(config.AppTitle))
                {
                    headers["X-Title"] = config.AppTitle;
                }
            }

            return headers;
        }

        private string ParseErrorMessage(string responseBody, long statusCode)
        {
            try
            {
                var errorResponse = JsonConvert.DeserializeObject<LLMErrorResponse>(responseBody);
                if (errorResponse?.error != null)
                {
                    return $"HTTP {statusCode}: {errorResponse.error.message} ({errorResponse.error.type})";
                }
            }
            catch
            {
                // Could not parse as error response
            }

            if (string.IsNullOrEmpty(responseBody))
            {
                return $"HTTP {statusCode}: Request failed with no response body.";
            }

            return $"HTTP {statusCode}: {responseBody}";
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug")]
        [TextArea(2, 5)]
        [SerializeField] private string debugPrompt = "Hello! What model are you?";

        [Button("Test OpenRouter Chat", ButtonSizes.Large)]
        private void Debug_TestOpenRouter()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[LLMService] Please enter Play Mode to test.");
                return;
            }
            QuickChat(LLMProvider.OpenRouter, debugPrompt,
                response => Debug.Log($"[LLMService] OpenRouter: {response}"),
                error => Debug.LogError($"[LLMService] OpenRouter error: {error}"));
        }

        [Button("Test Mistral Chat", ButtonSizes.Large)]
        private void Debug_TestMistral()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[LLMService] Please enter Play Mode to test.");
                return;
            }
            QuickChat(LLMProvider.Mistral, debugPrompt,
                response => Debug.Log($"[LLMService] Mistral: {response}"),
                error => Debug.LogError($"[LLMService] Mistral error: {error}"));
        }
        #endif
    }
}
