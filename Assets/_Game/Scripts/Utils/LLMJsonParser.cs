using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace TheBunkerGames
{
    /// <summary>
    /// Utility class for parsing LLM JSON responses.
    /// Handles extraction of JSON from markdown code blocks and type-specific parsing.
    /// </summary>
    public static class LLMJsonParser
    {
        /// <summary>
        /// Extracts JSON content from an LLM response, handling markdown code blocks.
        /// </summary>
        public static string ExtractJson(string llmResponse)
        {
            if (string.IsNullOrEmpty(llmResponse))
                return null;

            // Try to find JSON in markdown code block
            var codeBlockMatch = Regex.Match(llmResponse, @"```(?:json)?\s*([\s\S]*?)```");
            if (codeBlockMatch.Success)
            {
                return codeBlockMatch.Groups[1].Value.Trim();
            }

            // Try to find raw JSON object
            var jsonObjectMatch = Regex.Match(llmResponse, @"\{[\s\S]*\}");
            if (jsonObjectMatch.Success)
            {
                return jsonObjectMatch.Value.Trim();
            }

            // Try to find raw JSON array
            var jsonArrayMatch = Regex.Match(llmResponse, @"\[[\s\S]*\]");
            if (jsonArrayMatch.Success)
            {
                return jsonArrayMatch.Value.Trim();
            }

            return null;
        }

        /// <summary>
        /// Attempts to parse JSON into a GeneratedItemData object.
        /// </summary>
        public static bool TryParseItem(string json, out GeneratedItemData result)
        {
            result = null;
            try
            {
                string extracted = ExtractJson(json);
                if (string.IsNullOrEmpty(extracted))
                {
                    Debug.LogWarning("[LLMJsonParser] Could not extract JSON from response.");
                    return false;
                }
                result = JsonConvert.DeserializeObject<GeneratedItemData>(extracted);
                return result != null && !string.IsNullOrEmpty(result.itemName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMJsonParser] Failed to parse item: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse JSON into a GeneratedCharacterData object.
        /// </summary>
        public static bool TryParseCharacter(string json, out GeneratedCharacterData result)
        {
            result = null;
            try
            {
                string extracted = ExtractJson(json);
                if (string.IsNullOrEmpty(extracted))
                {
                    Debug.LogWarning("[LLMJsonParser] Could not extract JSON from response.");
                    return false;
                }
                result = JsonConvert.DeserializeObject<GeneratedCharacterData>(extracted);
                return result != null && !string.IsNullOrEmpty(result.name);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMJsonParser] Failed to parse character: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse JSON into a GeneratedPlaceData object.
        /// </summary>
        public static bool TryParsePlace(string json, out GeneratedPlaceData result)
        {
            result = null;
            try
            {
                string extracted = ExtractJson(json);
                if (string.IsNullOrEmpty(extracted))
                {
                    Debug.LogWarning("[LLMJsonParser] Could not extract JSON from response.");
                    return false;
                }
                result = JsonConvert.DeserializeObject<GeneratedPlaceData>(extracted);
                return result != null && !string.IsNullOrEmpty(result.placeName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMJsonParser] Failed to parse place: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse JSON into a GeneratedQuestData object.
        /// </summary>
        public static bool TryParseQuest(string json, out GeneratedQuestData result)
        {
            result = null;
            try
            {
                string extracted = ExtractJson(json);
                if (string.IsNullOrEmpty(extracted))
                {
                    Debug.LogWarning("[LLMJsonParser] Could not extract JSON from response.");
                    return false;
                }
                result = JsonConvert.DeserializeObject<GeneratedQuestData>(extracted);
                return result != null && !string.IsNullOrEmpty(result.id);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMJsonParser] Failed to parse quest: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Generic JSON parsing with type parameter.
        /// </summary>
        public static bool TryParse<T>(string json, out T result) where T : class
        {
            result = null;
            try
            {
                string extracted = ExtractJson(json);
                if (string.IsNullOrEmpty(extracted))
                {
                    Debug.LogWarning("[LLMJsonParser] Could not extract JSON from response.");
                    return false;
                }
                result = JsonConvert.DeserializeObject<T>(extracted);
                return result != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMJsonParser] Failed to parse {typeof(T).Name}: {ex.Message}");
                return false;
            }
        }
    }
}
