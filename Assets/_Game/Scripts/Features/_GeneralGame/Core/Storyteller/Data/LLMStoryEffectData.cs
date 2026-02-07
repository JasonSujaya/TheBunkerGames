using UnityEngine;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Lightweight data class for LLM-generated story effects.
    /// JSON-serializable for LLM input/output.
    /// </summary>
    [Serializable]
    public class LLMStoryEffectData
    {
        // -------------------------------------------------------------------------
        // Data Fields (JSON keys)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [ValueDropdown("GetAvailableEffects")]
        #endif
        [JsonProperty("effectType")]
        [SerializeField] private string effectType;
        
        #if ODIN_INSPECTOR
        [InfoBox("1 = Minor effect, 10 = Extreme effect")]
        #endif
        [JsonProperty("intensity")]
        [SerializeField, Range(1, 10)] private int intensity = 5;
        
        [JsonProperty("target")]
        [SerializeField] private string target;

        // -------------------------------------------------------------------------
        // Properties
        // -------------------------------------------------------------------------
        [JsonIgnore] public string EffectType => effectType;
        [JsonIgnore] public int Intensity => intensity;
        [JsonIgnore] public string Target => target;

        // -------------------------------------------------------------------------
        // Available Effects (for dropdown)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        private static string[] GetAvailableEffects() => new[]
        {
            "AddHP", "ReduceHP", "AddSanity", "ReduceSanity",
            "AddHunger", "ReduceHunger", "AddThirst", "ReduceThirst",
            "AddFood", "ReduceFood", "AddWater", "ReduceWater", "AddSupplies", "ReduceSupplies",
            "InjureCharacter", "HealCharacter", "KillCharacter",
            "TriggerEvent", "UnlockArea", "SpawnItem"
        };
        #endif

        // -------------------------------------------------------------------------
        // Constructors
        // -------------------------------------------------------------------------
        public LLMStoryEffectData() { }
        
        public LLMStoryEffectData(string effectType, int intensity, string target = "")
        {
            this.effectType = effectType;
            this.intensity = Mathf.Clamp(intensity, 1, 10);
            this.target = target;
        }

        public LLMStoryEffectData(LLMEffectType effectType, int intensity, string target = "")
            : this(effectType.ToString(), intensity, target) { }

        // -------------------------------------------------------------------------
        // JSON Helpers
        // -------------------------------------------------------------------------
        public static LLMStoryEffectData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonConvert.DeserializeObject<LLMStoryEffectData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LLMStoryEffectData] JSON parse error: {ex.Message}");
                return null;
            }
        }

        public string ToJson() => JsonConvert.SerializeObject(this);

        // -------------------------------------------------------------------------
        // Legacy String Parsing (for backward compatibility)
        // -------------------------------------------------------------------------
        /// <summary>
        /// Parse legacy format: "EffectType:Intensity" or "EffectType:Intensity:Target"
        /// </summary>
        public static LLMStoryEffectData ParseLegacy(string llmOutput)
        {
            if (string.IsNullOrEmpty(llmOutput)) return null;
            
            var parts = llmOutput.Split(':');
            if (parts.Length < 2) return null;
            
            string effectType = parts[0].Trim();
            if (!int.TryParse(parts[1].Trim(), out int intensity))
                intensity = 5;
            
            string target = parts.Length > 2 ? parts[2].Trim() : "";
            return new LLMStoryEffectData(effectType, intensity, target);
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(target) 
                ? $"{effectType}:{intensity}" 
                : $"{effectType}:{intensity}:{target}";
        }
    }
}
