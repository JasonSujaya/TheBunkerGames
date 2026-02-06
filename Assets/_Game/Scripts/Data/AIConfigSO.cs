using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Which LLM provider to use for API calls.
    /// </summary>
    public enum LLMProvider
    {
        OpenRouter,
        Mistral
    }

    /// <summary>
    /// Model quality tier - affects cost and capability.
    /// </summary>
    public enum ModelTier
    {
        High,   // Best quality, highest cost
        Mid,    // Balanced quality/cost
        Low     // Fast & cheap, lower quality
    }

    /// <summary>
    /// ScriptableObject for AI/LLM API configuration.
    /// Store your API keys here instead of on the LLMManager directly.
    /// </summary>
    [CreateAssetMenu(fileName = "AIConfigSO", menuName = "TheBunkerGames/AI Config")]
    public class AIConfigSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Active Provider Selection
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Active Provider")]
        [InfoBox("Select which LLM provider to use for all AI calls.")]
        #endif
        [SerializeField] private LLMProvider activeProvider = LLMProvider.OpenRouter;
        
        #if ODIN_INSPECTOR
        [InfoBox("Select model quality tier. High = best quality, Low = fast & cheap.")]
        #endif
        [SerializeField] private ModelTier activeModelTier = ModelTier.Mid;

        // -------------------------------------------------------------------------
        // OpenRouter
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("OpenRouter")]
        [InfoBox("Get your API key from https://openrouter.ai/")]
        #endif
        [SerializeField] private string openRouterApiKey = "";
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("OpenRouter Models")]
        #endif
        [SerializeField] private string openRouterHighModel = "openai/gpt-4o";
        #if ODIN_INSPECTOR
        [FoldoutGroup("OpenRouter Models")]
        #endif
        [SerializeField] private string openRouterMidModel = "openai/gpt-4o-mini";
        #if ODIN_INSPECTOR
        [FoldoutGroup("OpenRouter Models")]
        #endif
        [SerializeField] private string openRouterLowModel = "meta-llama/llama-3.2-3b-instruct:free";

        // -------------------------------------------------------------------------
        // Mistral
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Mistral")]
        [InfoBox("Get your API key from https://console.mistral.ai/")]
        #endif
        [SerializeField] private string mistralApiKey = "";
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Mistral Models")]
        #endif
        [SerializeField] private string mistralHighModel = "mistral-large-latest";
        #if ODIN_INSPECTOR
        [FoldoutGroup("Mistral Models")]
        #endif
        [SerializeField] private string mistralMidModel = "mistral-medium-latest";
        #if ODIN_INSPECTOR
        [FoldoutGroup("Mistral Models")]
        #endif
        [SerializeField] private string mistralLowModel = "mistral-small-latest";

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
        public LLMProvider ActiveProvider => activeProvider;
        public ModelTier ActiveModelTier => activeModelTier;
        
        public string OpenRouterApiKey => openRouterApiKey;
        public string MistralApiKey => mistralApiKey;
        public float RequestTimeout => requestTimeout;
        public bool EnableDebugLogs => enableDebugLogs;

        /// <summary>
        /// Gets the model string for the active provider and tier.
        /// </summary>
        public string GetActiveModel()
        {
            if (activeProvider == LLMProvider.OpenRouter)
            {
                return activeModelTier switch
                {
                    ModelTier.High => openRouterHighModel,
                    ModelTier.Mid => openRouterMidModel,
                    ModelTier.Low => openRouterLowModel,
                    _ => openRouterMidModel
                };
            }
            else
            {
                return activeModelTier switch
                {
                    ModelTier.High => mistralHighModel,
                    ModelTier.Mid => mistralMidModel,
                    ModelTier.Low => mistralLowModel,
                    _ => mistralMidModel
                };
            }
        }

        /// <summary>
        /// Gets the API key for the active provider.
        /// </summary>
        public string GetActiveApiKey()
        {
            return activeProvider == LLMProvider.OpenRouter ? openRouterApiKey : mistralApiKey;
        }

        /// <summary>
        /// Gets the API URL for the active provider.
        /// </summary>
        public string GetActiveApiUrl()
        {
            return activeProvider == LLMProvider.OpenRouter
                ? "https://openrouter.ai/api/v1/chat/completions"
                : "https://api.mistral.ai/v1/chat/completions";
        }

        // -------------------------------------------------------------------------
        // Validation
        // -------------------------------------------------------------------------
        public bool HasOpenRouterKey => !string.IsNullOrEmpty(openRouterApiKey);
        public bool HasMistralKey => !string.IsNullOrEmpty(mistralApiKey);
        public bool HasActiveKey => activeProvider == LLMProvider.OpenRouter ? HasOpenRouterKey : HasMistralKey;
    }
}
