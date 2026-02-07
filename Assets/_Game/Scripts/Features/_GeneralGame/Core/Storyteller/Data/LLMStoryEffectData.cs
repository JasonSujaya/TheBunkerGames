using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Lightweight data class for LLM-generated story effects.
    /// The LLM outputs simple string commands with intensity (1-10).
    /// Unity interprets these and triggers the actual game effects.
    /// </summary>
    [Serializable]
    public class LLMStoryEffectData
    {
        // -------------------------------------------------------------------------
        // Effect Types the LLM Can Use
        // -------------------------------------------------------------------------
        public static readonly string[] AvailableEffects = new[]
        {
            // Health Effects
            "AddHP",
            "ReduceHP",
            "AddSanity",
            "ReduceSanity",
            "AddHunger",
            "ReduceHunger",
            "AddThirst",
            "ReduceThirst",
            
            // Resource Effects
            "AddFood",
            "ReduceFood",
            "AddWater",
            "ReduceWater",
            "AddSupplies",
            "ReduceSupplies",
            
            // Character Effects
            "InjureCharacter",
            "HealCharacter",
            "KillCharacter",
            
            // Special Effects
            "TriggerEvent",
            "UnlockArea",
            "SpawnItem"
        };

        // -------------------------------------------------------------------------
        // Data Fields
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [ValueDropdown("AvailableEffects")]
        #endif
        [SerializeField] private string effectType;
        
        #if ODIN_INSPECTOR
        [InfoBox("1 = Minor effect, 10 = Extreme effect")]
        #endif
        [SerializeField, Range(1, 10)] private int intensity = 5;
        
        #if ODIN_INSPECTOR
        [Tooltip("Optional target (character name, item ID, etc.)")]
        #endif
        [SerializeField] private string target;

        // -------------------------------------------------------------------------
        // Properties
        // -------------------------------------------------------------------------
        public string EffectType => effectType;
        public int Intensity => intensity;
        public string Target => target;

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
        // LLM Parsing Helper
        // -------------------------------------------------------------------------
        /// <summary>
        /// Parse a simple LLM output format: "EffectType:Intensity" or "EffectType:Intensity:Target"
        /// Example: "ReduceHP:7" or "InjureCharacter:5:Marcus"
        /// </summary>
        public static LLMStoryEffectData Parse(string llmOutput)
        {
            if (string.IsNullOrEmpty(llmOutput)) return null;
            
            var parts = llmOutput.Split(':');
            if (parts.Length < 2) return null;
            
            string effectType = parts[0].Trim();
            if (!int.TryParse(parts[1].Trim(), out int intensity))
            {
                intensity = 5; // Default to medium
            }
            
            string target = parts.Length > 2 ? parts[2].Trim() : "";
            
            return new LLMStoryEffectData(effectType, intensity, target);
        }

        /// <summary>
        /// Parse multiple effects from LLM output (newline or comma separated).
        /// Example: "ReduceHP:7, AddSanity:3" or "ReduceHP:7\nAddSanity:3"
        /// </summary>
        public static List<LLMStoryEffectData> ParseMultiple(string llmOutput)
        {
            var results = new List<LLMStoryEffectData>();
            if (string.IsNullOrEmpty(llmOutput)) return results;
            
            // Split by newlines or commas
            var lines = llmOutput.Split(new[] { '\n', ',' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var effect = Parse(line.Trim());
                if (effect != null)
                {
                    results.Add(effect);
                }
            }
            
            return results;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(target) 
                ? $"{effectType}:{intensity}" 
                : $"{effectType}:{intensity}:{target}";
        }
    }
}
