using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject for AI/LLM API configuration.
    /// Store your API keys here instead of on the LLMManager directly.
    /// </summary>
    [CreateAssetMenu(fileName = "AIConfigSO", menuName = "TheBunkerGames/AI Config")]
    public class AIConfigSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // OpenRouter
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("OpenRouter")]
        [InfoBox("Get your API key from https://openrouter.ai/")]
        #endif
        [SerializeField] private string openRouterApiKey = "";
        [SerializeField] private string openRouterModel = "openai/gpt-4o-mini";

        // -------------------------------------------------------------------------
        // Mistral
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Mistral")]
        [InfoBox("Get your API key from https://console.mistral.ai/")]
        #endif
        [SerializeField] private string mistralApiKey = "";
        [SerializeField] private string mistralModel = "mistral-large-latest";

        // -------------------------------------------------------------------------
        // General Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("General")]
        #endif
        [SerializeField] private float requestTimeout = 30f;
        [SerializeField] private bool enableDebugLogs = true;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string OpenRouterApiKey => openRouterApiKey;
        public string OpenRouterModel => openRouterModel;
        public string MistralApiKey => mistralApiKey;
        public string MistralModel => mistralModel;
        public float RequestTimeout => requestTimeout;
        public bool EnableDebugLogs => enableDebugLogs;

        // -------------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------------
        public bool HasOpenRouterKey => !string.IsNullOrEmpty(openRouterApiKey);
        public bool HasMistralKey => !string.IsNullOrEmpty(mistralApiKey);
    }
}
