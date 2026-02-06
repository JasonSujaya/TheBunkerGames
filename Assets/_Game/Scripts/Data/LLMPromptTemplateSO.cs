using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject holding prompt templates for LLM generation.
    /// Each template defines the system prompt, user prompt template, and expected JSON schema.
    /// </summary>
    [CreateAssetMenu(fileName = "LLMPromptTemplate", menuName = "TheBunkerGames/LLM Prompt Template")]
    public class LLMPromptTemplateSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Template Info")]
        #endif
        [SerializeField] private string templateName;
        [TextArea(2, 4)]
        [SerializeField] private string templateDescription;

        #if ODIN_INSPECTOR
        [Title("System Prompt")]
        [InfoBox("This prompt sets the AI's behavior and JSON format expectations.")]
        #endif
        [TextArea(5, 15)]
        [SerializeField] private string systemPrompt;

        #if ODIN_INSPECTOR
        [Title("User Prompt Template")]
        [InfoBox("Use {0}, {1}, etc. for string.Format placeholders.")]
        #endif
        [TextArea(3, 10)]
        [SerializeField] private string userPromptTemplate;

        #if ODIN_INSPECTOR
        [Title("Expected JSON Schema")]
        [InfoBox("Document the expected JSON structure for reference.")]
        #endif
        [TextArea(5, 15)]
        [SerializeField] private string jsonSchemaExample;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string TemplateName => templateName;
        public string TemplateDescription => templateDescription;
        public string SystemPrompt => systemPrompt;
        public string UserPromptTemplate => userPromptTemplate;
        public string JsonSchemaExample => jsonSchemaExample;

        /// <summary>
        /// Builds the final user prompt by formatting the template with provided arguments.
        /// </summary>
        public string BuildUserPrompt(params object[] args)
        {
            if (args == null || args.Length == 0)
                return userPromptTemplate;
            return string.Format(userPromptTemplate, args);
        }
    }
}
