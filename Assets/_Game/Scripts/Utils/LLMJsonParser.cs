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
        /// Handles both flat JSON and nested structures like {"item": {...}}.
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

                // First try direct deserialization
                result = JsonConvert.DeserializeObject<GeneratedItemData>(extracted);
                if (result != null && !string.IsNullOrEmpty(result.itemName))
                    return true;

                // Try to find nested "item" property
                var wrapper = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(extracted);
                if (wrapper != null)
                {
                    // Check for "item" wrapper
                    if (wrapper.ContainsKey("item"))
                    {
                        result = JsonConvert.DeserializeObject<GeneratedItemData>(wrapper["item"].ToString());
                        if (result != null && !string.IsNullOrEmpty(result.itemName))
                        {
                            ApplyFuzzyMapping(result);
                            return true;
                        }
                    }
                    // Try direct mapping with different field names
                    if (wrapper.ContainsKey("name") || wrapper.ContainsKey("itemName"))
                    {
                        string name = wrapper.ContainsKey("itemName") ? wrapper["itemName"]?.ToString() : wrapper["name"]?.ToString();
                        string desc = wrapper.ContainsKey("description") ? wrapper["description"]?.ToString() : "";
                        string type = wrapper.ContainsKey("itemType") ? wrapper["itemType"]?.ToString() : 
                                      wrapper.ContainsKey("type") ? wrapper["type"]?.ToString() : "Junk";
                        
                        // Fuzzy mapping for type
                        if (type == "Medicine") type = "Meds";
                        if (type == "Tool") type = "Tools";
                        if (type == "Resource") type = "Junk";

                        result = new GeneratedItemData { itemName = name, description = desc, itemType = type };
                        return !string.IsNullOrEmpty(result.itemName);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMJsonParser] Failed to parse item: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse JSON into a GeneratedItemBatch object.
        /// Handles flat arrays or wrapped lists like {"items": [...]}.
        /// </summary>
        public static bool TryParseItemBatch(string json, out GeneratedItemBatch result)
        {
            result = new GeneratedItemBatch();
            try
            {
                string extracted = ExtractJson(json);
                if (string.IsNullOrEmpty(extracted)) return false;

                // 1. Try direct array deserialization [{}, {}]
                var arrayList = JsonConvert.DeserializeObject<System.Collections.Generic.List<GeneratedItemData>>(extracted);
                if (arrayList != null && arrayList.Count > 0)
                {
                    result.items = arrayList;
                }
                else
                {
                    // 2. Try wrapped object {"items": [...]}
                    var wrapper = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(extracted);
                    if (wrapper != null && wrapper.ContainsKey("items"))
                    {
                        result.items = JsonConvert.DeserializeObject<System.Collections.Generic.List<GeneratedItemData>>(wrapper["items"].ToString());
                    }
                    else if (wrapper != null && wrapper.ContainsKey("item_list"))
                    {
                        result.items = JsonConvert.DeserializeObject<System.Collections.Generic.List<GeneratedItemData>>(wrapper["item_list"].ToString());
                    }
                }

                // Apply fuzzy mapping and validation
                if (result.items != null && result.items.Count > 0)
                {
                    foreach (var item in result.items)
                    {
                        ApplyFuzzyMapping(item);
                    }
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMJsonParser] Failed to parse item batch: {ex.Message}");
                return false;
            }
        }

        private static void ApplyFuzzyMapping(GeneratedItemData data)
        {
            if (data == null) return;
            if (data.itemType == "Medicine" || data.itemType == "Medical") data.itemType = "Meds";
            if (data.itemType == "Tool" || data.itemType == "Equipment") data.itemType = "Tools";
            if (data.itemType == "Resource" || data.itemType == "Material") data.itemType = "Junk";
            if (string.IsNullOrEmpty(data.itemType)) data.itemType = "Junk";
        }

        /// <summary>
        /// Attempts to parse JSON into a GeneratedCharacterData object.
        /// Handles both flat JSON and nested structures like {"character": {...}}.
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

                // First try direct deserialization
                result = JsonConvert.DeserializeObject<GeneratedCharacterData>(extracted);
                if (result != null && !string.IsNullOrEmpty(result.name))
                    return true;

                // Try to find nested property
                var wrapper = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(extracted);
                if (wrapper != null)
                {
                    foreach (var key in new[] { "character", "survivor", "enemy", "npc" })
                    {
                        if (wrapper.ContainsKey(key))
                        {
                            result = JsonConvert.DeserializeObject<GeneratedCharacterData>(wrapper[key].ToString());
                            if (result != null && !string.IsNullOrEmpty(result.name))
                                return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMJsonParser] Failed to parse character: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse JSON into a GeneratedPlaceData object.
        /// Handles both flat JSON and nested structures like {"place": {...}}.
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

                // First try direct deserialization
                result = JsonConvert.DeserializeObject<GeneratedPlaceData>(extracted);
                if (result != null && !string.IsNullOrEmpty(result.placeName))
                    return true;

                // Try to find nested property
                var wrapper = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(extracted);
                if (wrapper != null)
                {
                    foreach (var key in new[] { "place", "location", "area" })
                    {
                        if (wrapper.ContainsKey(key))
                        {
                            result = JsonConvert.DeserializeObject<GeneratedPlaceData>(wrapper[key].ToString());
                            if (result != null && !string.IsNullOrEmpty(result.placeName))
                                return true;
                        }
                    }
                    // Try name -> placeName mapping
                    if (wrapper.ContainsKey("name") && !wrapper.ContainsKey("placeName"))
                    {
                        result = new GeneratedPlaceData 
                        { 
                            placeName = wrapper["name"]?.ToString(),
                            description = wrapper.ContainsKey("description") ? wrapper["description"]?.ToString() : "",
                            placeId = wrapper.ContainsKey("id") ? wrapper["id"]?.ToString() : wrapper.ContainsKey("placeId") ? wrapper["placeId"]?.ToString() : System.Guid.NewGuid().ToString()
                        };
                        return !string.IsNullOrEmpty(result.placeName);
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMJsonParser] Failed to parse place: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to parse JSON into a GeneratedQuestData object.
        /// Handles both flat JSON and nested structures like {"quest": {...}}.
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

                // First try direct deserialization
                result = JsonConvert.DeserializeObject<GeneratedQuestData>(extracted);
                if (result != null && !string.IsNullOrEmpty(result.id))
                    return true;

                // Try to find nested property
                var wrapper = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, object>>(extracted);
                if (wrapper != null)
                {
                    foreach (var key in new[] { "quest", "mission", "objective", "task" })
                    {
                        if (wrapper.ContainsKey(key))
                        {
                            result = JsonConvert.DeserializeObject<GeneratedQuestData>(wrapper[key].ToString());
                            if (result != null && !string.IsNullOrEmpty(result.id))
                                return true;
                        }
                    }
                }

                return false;
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
