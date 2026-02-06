using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Configuration ScriptableObject for OpenRouter and Mistral API integration.
    /// Stores API keys, endpoints, and default model settings.
    /// </summary>
    [CreateAssetMenu(fileName = "LLMConfigDataSO", menuName = "TheBunkerGames/LLM Config")]
    public class LLMConfigDataSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static LLMConfigDataSO instance;
        public static LLMConfigDataSO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<LLMConfigDataSO>("LLM/LLMConfigDataSO");
                    
                    #if UNITY_EDITOR
                    if (instance == null)
                    {
                        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:LLMConfigDataSO");
                        if (guids.Length > 0)
                        {
                            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                            instance = UnityEditor.AssetDatabase.LoadAssetAtPath<LLMConfigDataSO>(path);
                        }
                    }
                    #endif

                    if (instance == null)
                    {
                        Debug.LogError("[LLMConfigDataSO] No LLMConfigDataSO found in Resources/LLM folder!");
                    }
                }
                return instance;
            }
        }

        // -------------------------------------------------------------------------
        // OpenRouter Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("OpenRouter")]
        [InfoBox("Get your API key at https://openrouter.ai/keys")]
        #endif
        [SerializeField] private string openRouterApiKey = "";
        [SerializeField] private string openRouterBaseUrl = "https://openrouter.ai/api/v1";
        [SerializeField] private string openRouterDefaultChatModel = "openai/gpt-4o-mini";
        [SerializeField] private string openRouterDefaultImageModel = "openai/dall-e-3";
        [SerializeField] private string httpReferer = "";
        [SerializeField] private string appTitle = "TheBunkerGames";

        // -------------------------------------------------------------------------
        // Mistral Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Mistral")]
        [InfoBox("Get your API key at https://console.mistral.ai/api-keys")]
        #endif
        [SerializeField] private string mistralApiKey = "";
        [SerializeField] private string mistralBaseUrl = "https://api.mistral.ai/v1";
        [SerializeField] private string mistralDefaultModel = "mistral-large-latest";

        // -------------------------------------------------------------------------
        // Debug Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug")]
        #endif
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private float requestTimeoutSeconds = 30f;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string OpenRouterApiKey => openRouterApiKey;
        public string OpenRouterBaseUrl => openRouterBaseUrl;
        public string OpenRouterDefaultChatModel => openRouterDefaultChatModel;
        public string OpenRouterDefaultImageModel => openRouterDefaultImageModel;
        public string HttpReferer => httpReferer;
        public string AppTitle => appTitle;

        public string MistralApiKey => mistralApiKey;
        public string MistralBaseUrl => mistralBaseUrl;
        public string MistralDefaultModel => mistralDefaultModel;

        public bool EnableDebugLogs => enableDebugLogs;
        public float RequestTimeoutSeconds => requestTimeoutSeconds;

        // -------------------------------------------------------------------------
        // Helper Methods
        // -------------------------------------------------------------------------
        public bool HasOpenRouterKey => !string.IsNullOrEmpty(openRouterApiKey);
        public bool HasMistralKey => !string.IsNullOrEmpty(mistralApiKey);

        public string GetApiKey(LLMProvider provider)
        {
            return provider switch
            {
                LLMProvider.OpenRouter => openRouterApiKey,
                LLMProvider.Mistral => mistralApiKey,
                _ => ""
            };
        }

        public string GetBaseUrl(LLMProvider provider)
        {
            return provider switch
            {
                LLMProvider.OpenRouter => openRouterBaseUrl,
                LLMProvider.Mistral => mistralBaseUrl,
                _ => ""
            };
        }

        public string GetDefaultChatModel(LLMProvider provider)
        {
            return provider switch
            {
                LLMProvider.OpenRouter => openRouterDefaultChatModel,
                LLMProvider.Mistral => mistralDefaultModel,
                _ => ""
            };
        }
    }
}
