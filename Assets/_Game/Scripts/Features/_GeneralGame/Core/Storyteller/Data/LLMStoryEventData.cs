using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TheBunkerGames
{
    /// <summary>
    /// Complete story event data from LLM output.
    /// Designed for JSON serialization/deserialization.
    /// </summary>
    [Serializable]
    public class LLMStoryEventData
    {
        // -------------------------------------------------------------------------
        // Fields (JSON keys)
        // -------------------------------------------------------------------------
        [JsonProperty("title")]
        public string Title;
        
        [JsonProperty("description")]
        public string Description;
        
        [JsonProperty("effects")]
        public List<LLMStoryEffectData> Effects = new List<LLMStoryEffectData>();
        
        [JsonProperty("choices")]
        public List<LLMStoryChoice> Choices = new List<LLMStoryChoice>();

        // -------------------------------------------------------------------------
        // Parsing
        // -------------------------------------------------------------------------
        /// <summary>
        /// Parse a JSON string into LLMStoryEventData.
        /// </summary>
        public static LLMStoryEventData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            
            try
            {
                return JsonConvert.DeserializeObject<LLMStoryEventData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMStoryEventData] Failed to parse JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Serialize to JSON string.
        /// </summary>
        public string ToJson(bool prettyPrint = false)
        {
            return JsonConvert.SerializeObject(this, prettyPrint ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Create a sample event for testing.
        /// </summary>
        public static LLMStoryEventData CreateSample()
        {
            return new LLMStoryEventData
            {
                Title = "The Pipe Bursts",
                Description = "A pipe in the water filtration system has burst! Water is spraying everywhere.",
                Effects = new List<LLMStoryEffectData>
                {
                    new LLMStoryEffectData("ReduceWater", 7),
                    new LLMStoryEffectData("ReduceSanity", 3, "Marcus")
                },
                Choices = new List<LLMStoryChoice>
                {
                    new LLMStoryChoice
                    {
                        Text = "Fix it yourself (risky)",
                        Effects = new List<LLMStoryEffectData>
                        {
                            new LLMStoryEffectData("ReduceHP", 4)
                        }
                    },
                    new LLMStoryChoice
                    {
                        Text = "Send Marcus to fix it",
                        Effects = new List<LLMStoryEffectData>
                        {
                            new LLMStoryEffectData("InjureCharacter", 5, "Marcus")
                        }
                    }
                }
            };
        }

        public override string ToString()
        {
            return $"[{Title}] Effects:{Effects?.Count ?? 0} Choices:{Choices?.Count ?? 0}";
        }
    }

    /// <summary>
    /// A choice the player can make in a story event.
    /// </summary>
    [Serializable]
    public class LLMStoryChoice
    {
        [JsonProperty("text")]
        public string Text;
        
        [JsonProperty("effects")]
        public List<LLMStoryEffectData> Effects = new List<LLMStoryEffectData>();

        public override string ToString()
        {
            return $"[{Text}] ({Effects?.Count ?? 0} effects)";
        }
    }
}
