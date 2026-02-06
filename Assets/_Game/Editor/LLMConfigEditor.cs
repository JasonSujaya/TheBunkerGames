using UnityEngine;
using UnityEditor;

namespace TheBunkerGames.Editor
{
    [CustomEditor(typeof(LLMConfigDataSO))]
    public class LLMConfigDataSOEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);
            EditorGUILayout.LabelField("API Validation", EditorStyles.boldLabel);

            var config = (LLMConfigDataSO)target;

            // OpenRouter validation
            EditorGUILayout.BeginHorizontal();
            DrawStatusIcon(config.HasOpenRouterKey);
            EditorGUILayout.LabelField(config.HasOpenRouterKey
                ? "OpenRouter key configured"
                : "OpenRouter key missing");
            EditorGUILayout.EndHorizontal();

            // Mistral validation
            EditorGUILayout.BeginHorizontal();
            DrawStatusIcon(config.HasMistralKey);
            EditorGUILayout.LabelField(config.HasMistralKey
                ? "Mistral key configured"
                : "Mistral key missing");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Enter Play Mode and use the LLMTestScene to test API connectivity.",
                MessageType.Info);
        }

        private void DrawStatusIcon(bool isValid)
        {
            var color = GUI.color;
            GUI.color = isValid ? Color.green : Color.red;
            GUILayout.Label(isValid ? "\u2713" : "\u2717", GUILayout.Width(20));
            GUI.color = color;
        }
    }
}
