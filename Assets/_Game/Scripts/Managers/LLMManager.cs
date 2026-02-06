using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Unified manager for LLM services (OpenRouter, Mistral).
    /// Handles API communication, configuration, and provides debugging tools.
    /// </summary>
    public class LLMManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
                // -------------------------------------------------------------------------
        // Configuration Asset
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Configuration")]
        #endif
        [SerializeField] private AIConfigSO configAsset;
        #if ODIN_INSPECTOR
        [Title("Editor Testing")]
        #endif
        [TextArea(3, 10)]
        [SerializeField] private string testPrompt = "Hello! Tell me a spooky bunker story.";
        
        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [TextArea(5, 15)]
        [SerializeField] private string lastResponse = "";


public static LLMManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        public enum Provider { OpenRouter, Mistral }

        /// <summary>
        /// Sends a quick chat request to the specified provider.
        /// </summary>
        public void QuickChat(
            Provider provider,
            string prompt,
            Action<string> onSuccess,
            Action<string> onError = null,
            string systemPrompt = null)
        {
            StartCoroutine(SendChatCoroutine(provider, prompt, systemPrompt, onSuccess, onError));
        }


        /// <summary>
                /// <summary>
        /// Checks OpenRouter credit balance. Mistral does NOT have a public credits API.
        /// </summary>
        public void CheckOpenRouterCredits(Action<OpenRouterCreditsInfo> onComplete = null)
        {
            StartCoroutine(CheckCreditsCoroutine(onComplete));
        }

        [Serializable]
        public class OpenRouterCreditsInfo
        {
            public float? limit_remaining;
            public float usage;
            public float usage_daily;
            public float usage_monthly;
            public bool is_free_tier;
        }

        private IEnumerator CheckCreditsCoroutine(Action<OpenRouterCreditsInfo> onComplete)
        {
            if (configAsset == null || string.IsNullOrEmpty(configAsset.OpenRouterApiKey))
            {
                Debug.LogWarning("[LLMManager] Cannot check credits - no OpenRouter API key configured.");
                onComplete?.Invoke(null);
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequest.Get("https://openrouter.ai/api/v1/key"))
            {
                request.SetRequestHeader("Authorization", $"Bearer {configAsset.OpenRouterApiKey}");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var wrapper = JsonConvert.DeserializeObject<Dictionary<string, OpenRouterCreditsInfo>>(request.downloadHandler.text);
                        if (wrapper != null && wrapper.TryGetValue("data", out var info))
                        {
                            string remaining = info.limit_remaining.HasValue ? $"${info.limit_remaining:F4}" : "Unlimited";
                            Debug.Log($"[LLMManager] <color=green>[CREDITS]</color> OpenRouter: Remaining={remaining}, UsedToday=${info.usage_daily:F4}, UsedTotal=${info.usage:F4}, FreeTier={info.is_free_tier}");
                            onComplete?.Invoke(info);
                        }
                        else
                        {
                            Debug.LogWarning("[LLMManager] Could not parse credits response.");
                            onComplete?.Invoke(null);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[LLMManager] Credits check failed: {ex.Message}");
                        onComplete?.Invoke(null);
                    }
                }
                else
                {
                    Debug.LogError($"[LLMManager] Credits check failed: {request.error}");
                    onComplete?.Invoke(null);
                }
            }
        }

/// Static wrapper for QuickChat. Access via LLMManager.QuickChatStatic(...).
        /// </summary>
        public static void QuickChatStatic(
            Provider provider,
            string prompt,
            Action<string> onSuccess,
            Action<string> onError = null,
            string systemPrompt = null)
        {
            if (Instance == null)
            {
                Debug.LogError("[LLMManager] No instance found! Make sure [LLMManager] is in your scene.");
                onError?.Invoke("No instance found.");
                return;
            }
            Instance.QuickChat(provider, prompt, onSuccess, onError, systemPrompt);
        }


        // -------------------------------------------------------------------------
        // Implementation
        // -------------------------------------------------------------------------

        private IEnumerator SendChatCoroutine(
            Provider provider,
            string prompt,
            string systemPrompt,
            Action<string> onSuccess,
            Action<string> onError)
        {
            if (configAsset == null)
            {
                string error = "[LLMManager] AIConfigSO is not assigned!";
                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            string url = "";
            string apiKey = "";
            string model = "";

            if (provider == Provider.OpenRouter)
            {
                url = "https://openrouter.ai/api/v1/chat/completions";
                apiKey = configAsset.OpenRouterApiKey;
                model = configAsset.OpenRouterModel;
            }
            else
            {
                url = "https://api.mistral.ai/v1/chat/completions";
                apiKey = configAsset.MistralApiKey;
                model = configAsset.MistralModel;
            }

            if (string.IsNullOrEmpty(apiKey))
            {
                string error = $"[LLMManager] API Key for {provider} is missing in AIConfigSO!";
                Debug.LogError(error);
                onError?.Invoke(error);
                yield break;
            }

            // Build payload
            var messages = new List<object>();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }
            messages.Add(new { role = "user", content = prompt });

            var payload = new
            {
                model = model,
                messages = messages
            };

            string json = JsonConvert.SerializeObject(payload);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                
                if (provider == Provider.OpenRouter)
                {
                    request.SetRequestHeader("HTTP-Referer", "https://github.com/JasonSujaya/TheBunkerGames");
                    request.SetRequestHeader("X-Title", "TheBunkerGames");
                }

                request.timeout = (int)configAsset.RequestTimeout;

                if (configAsset.EnableDebugLogs) Debug.Log($"[LLMManager] Sending {provider} request...\n{json}");

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string error = $"[LLMManager] Request failed: {request.error}\nResponse: {request.downloadHandler.text}";
                    Debug.LogError(error);
                    onError?.Invoke(error);
                }
                else
                {
                    try
                    {
                        var response = JsonConvert.DeserializeObject<ChatResponse>(request.downloadHandler.text);
                        string content = response?.choices?[0]?.message?.content ?? "";
                        if (configAsset.EnableDebugLogs) Debug.Log($"[LLMManager] {provider} Success: {content}");
                        onSuccess?.Invoke(content);
                    }
                    catch (Exception ex)
                    {
                        string error = $"[LLMManager] Failed to parse response: {ex.Message}";
                        Debug.LogError(error);
                        onError?.Invoke(error);
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // Data Classes
        // -------------------------------------------------------------------------
        [Serializable]
        private class ChatResponse
        {
            public Choice[] choices;
        }

        [Serializable]
        private class Choice
        {
            public Message message;
        }

        [Serializable]
        private class Message
        {
            public string role;
            public string content;
        }

        // -------------------------------------------------------------------------
        // Debug & Setup
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Tools")]
        [Button("Auto Setup LLM", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        public void AutoSetup()
        {
            gameObject.name = "[LLMManager]";
            
            // Auto-load config if missing
            if (configAsset == null)
            {
                configAsset = Resources.Load<AIConfigSO>("AI/AIConfigSO");
                if (configAsset == null)
                    Debug.LogWarning("[LLMManager] AIConfigSO not found in Resources/AI/. Please create one via Create > TheBunkerGames > AI Config.");
            }

            if (configAsset != null)
            {
                if (!configAsset.HasOpenRouterKey)
                    Debug.LogWarning("[LLMManager] OpenRouter API Key is empty in AIConfigSO.");
                if (!configAsset.HasMistralKey)
                    Debug.LogWarning("[LLMManager] Mistral API Key is empty in AIConfigSO.");
            }

            Debug.Log("[LLMManager] Auto Setup Complete.");
        }

        [Button("Test OpenRouter", ButtonSizes.Medium)]
        private void TestOpenRouter()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }
            lastResponse = "Waiting for response...";
            QuickChat(Provider.OpenRouter, testPrompt, 
                (res) => {
                    lastResponse = res;
                    if (configAsset.EnableDebugLogs) Debug.Log($"[Test] OpenRouter says: {res}");
                }, 
                (err) => {
                    lastResponse = $"ERROR: {err}";
                    Debug.LogError($"[Test] OpenRouter Error: {err}");
                });
        }

        [Button("Check OpenRouter Credits", ButtonSizes.Medium)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        private void Debug_CheckCredits()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to check credits."); return; }
            CheckOpenRouterCredits();
        }

        
[Button("Test Mistral", ButtonSizes.Medium)]
        private void TestMistral()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test."); return; }
            lastResponse = "Waiting for response...";
            QuickChat(Provider.Mistral, testPrompt, 
                (res) => {
                    lastResponse = res;
                    if (configAsset.EnableDebugLogs) Debug.Log($"[Test] Mistral says: {res}");
                }, 
                (err) => {
                    lastResponse = $"ERROR: {err}";
                    Debug.LogError($"[Test] Mistral Error: {err}");
                });
        }
        #endif
    }
}
