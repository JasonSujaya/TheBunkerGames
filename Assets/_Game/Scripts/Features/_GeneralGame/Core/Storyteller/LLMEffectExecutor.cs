using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Interprets LLMStoryEffectData and executes the actual game effects.
    /// Maps intensity 1-10 to actual game values based on configuration.
    /// </summary>
    public class LLMEffectExecutor : MonoBehaviour
    {
        public static LLMEffectExecutor Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration - Maps intensity to actual values
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Intensity Mappings")]
        [InfoBox("Intensity 1 = first value, Intensity 10 = last value. Values are interpolated.")]
        #endif
        [Header("Health Effects (intensity 1-10 -> actual HP change)")]
        [SerializeField] private float minHealthChange = 5f;
        [SerializeField] private float maxHealthChange = 50f;
        
        [Header("Sanity Effects")]
        [SerializeField] private float minSanityChange = 3f;
        [SerializeField] private float maxSanityChange = 30f;
        
        [Header("Hunger Effects")]
        [SerializeField] private float minHungerChange = 5f;
        [SerializeField] private float maxHungerChange = 40f;
        
        [Header("Thirst Effects")]
        [SerializeField] private float minThirstChange = 5f;
        [SerializeField] private float maxThirstChange = 40f;
        
        [Header("Resource Effects")]  
        [SerializeField] private int minResourceChange = 1;
        [SerializeField] private int maxResourceChange = 10;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void ExecuteEffect(LLMStoryEffectData effect)
        {
            if (effect == null) return;
            
            Debug.Log($"[LLMEffectExecutor] Executing: {effect}");
            
            switch (effect.EffectType)
            {
                // Health Effects
                case "AddHP":
                    ApplyHealthChange(effect.Intensity, effect.Target, positive: true);
                    break;
                case "ReduceHP":
                    ApplyHealthChange(effect.Intensity, effect.Target, positive: false);
                    break;
                case "AddSanity":
                    ApplySanityChange(effect.Intensity, effect.Target, positive: true);
                    break;
                case "ReduceSanity":
                    ApplySanityChange(effect.Intensity, effect.Target, positive: false);
                    break;
                case "AddHunger":
                    ApplyHungerChange(effect.Intensity, effect.Target, positive: true);
                    break;
                case "ReduceHunger":
                    ApplyHungerChange(effect.Intensity, effect.Target, positive: false);
                    break;
                case "AddThirst":
                    ApplyThirstChange(effect.Intensity, effect.Target, positive: true);
                    break;
                case "ReduceThirst":
                    ApplyThirstChange(effect.Intensity, effect.Target, positive: false);
                    break;
                    
                // Resource Effects
                case "AddFood":
                case "ReduceFood":
                case "AddWater":
                case "ReduceWater":
                case "AddSupplies":
                case "ReduceSupplies":
                    ApplyResourceEffect(effect);
                    break;
                    
                // Character Effects
                case "InjureCharacter":
                    ApplyInjury(effect.Intensity, effect.Target);
                    break;
                case "HealCharacter":
                    ApplyHealing(effect.Intensity, effect.Target);
                    break;
                case "KillCharacter":
                    KillCharacter(effect.Target);
                    break;
                    
                // Special Effects
                case "TriggerEvent":
                case "UnlockArea":
                case "SpawnItem":
                    Debug.Log($"[LLMEffectExecutor] Special effect '{effect.EffectType}' - implement as needed");
                    break;
                    
                default:
                    Debug.LogWarning($"[LLMEffectExecutor] Unknown effect type: {effect.EffectType}");
                    break;
            }
        }

        public void ExecuteEffects(List<LLMStoryEffectData> effects)
        {
            if (effects == null) return;
            
            foreach (var effect in effects)
            {
                ExecuteEffect(effect);
            }
        }

        /// <summary>
        /// Execute effects directly from LLM JSON output.
        /// Expects either a full LLMStoryEventData JSON or a JSON array of effects.
        /// </summary>
        public void ExecuteFromLLMOutput(string llmOutput)
        {
            if (string.IsNullOrEmpty(llmOutput)) return;

            // Try parsing as full event first
            var storyEvent = LLMStoryEventData.FromJson(llmOutput);
            if (storyEvent != null && storyEvent.Effects != null)
            {
                ExecuteEffects(storyEvent.Effects);
                return;
            }

            // Fall back to parsing as single effect (legacy format)
            var singleEffect = LLMStoryEffectData.FromJson(llmOutput);
            if (singleEffect != null)
            {
                ExecuteEffect(singleEffect);
            }
            else
            {
                Debug.LogWarning($"[LLMEffectExecutor] Could not parse LLM output: {llmOutput}");
            }
        }

        // -------------------------------------------------------------------------
        // Private Effect Handlers
        // -------------------------------------------------------------------------
        private float IntensityToValue(int intensity, float min, float max)
        {
            // intensity 1-10 maps to min-max
            float t = (intensity - 1) / 9f;
            return Mathf.Lerp(min, max, t);
        }

        private int IntensityToValue(int intensity, int min, int max)
        {
            float t = (intensity - 1) / 9f;
            return Mathf.RoundToInt(Mathf.Lerp(min, max, t));
        }

        private void ApplyHealthChange(int intensity, string target, bool positive)
        {
            float amount = IntensityToValue(intensity, minHealthChange, maxHealthChange);
            if (!positive) amount = -amount;
            
            // TODO: Connect to CharacterManager
            Debug.Log($"[LLMEffectExecutor] Health change: {amount:+0.0} to '{target}'");
            
            // Example integration:
            // if (CharacterManager.Instance != null)
            // {
            //     var character = CharacterManager.Instance.GetCharacter(target);
            //     character?.ModifyHealth(amount);
            // }
        }

        private void ApplySanityChange(int intensity, string target, bool positive)
        {
            float amount = IntensityToValue(intensity, minSanityChange, maxSanityChange);
            if (!positive) amount = -amount;
            
            Debug.Log($"[LLMEffectExecutor] Sanity change: {amount:+0.0} to '{target}'");
        }

        private void ApplyHungerChange(int intensity, string target, bool positive)
        {
            float amount = IntensityToValue(intensity, minHungerChange, maxHungerChange);
            if (!positive) amount = -amount;
            
            Debug.Log($"[LLMEffectExecutor] Hunger change: {amount:+0.0} to '{target}'");
        }

        private void ApplyThirstChange(int intensity, string target, bool positive)
        {
            float amount = IntensityToValue(intensity, minThirstChange, maxThirstChange);
            if (!positive) amount = -amount;
            
            Debug.Log($"[LLMEffectExecutor] Thirst change: {amount:+0.0} to '{target}'");
        }

        private void ApplyResourceEffect(LLMStoryEffectData effect)
        {
            int amount = IntensityToValue(effect.Intensity, minResourceChange, maxResourceChange);
            bool positive = effect.EffectType.StartsWith("Add");
            if (!positive) amount = -amount;
            
            string resourceType = effect.EffectType.Replace("Add", "").Replace("Reduce", "");
            Debug.Log($"[LLMEffectExecutor] Resource '{resourceType}' change: {amount:+0}");
        }

        private void ApplyInjury(int intensity, string target)
        {
            float damage = IntensityToValue(intensity, minHealthChange, maxHealthChange);
            Debug.Log($"[LLMEffectExecutor] Injure '{target}' for {damage:0.0} damage");
        }

        private void ApplyHealing(int intensity, string target)
        {
            float heal = IntensityToValue(intensity, minHealthChange, maxHealthChange);
            Debug.Log($"[LLMEffectExecutor] Heal '{target}' for {heal:0.0} HP");
        }

        private void KillCharacter(string target)
        {
            Debug.Log($"[LLMEffectExecutor] KILL CHARACTER: '{target}'");
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug - Quick Test")]
        #endif
        [Header("Single Effect Test")]
        [SerializeField] private LLMEffectType testEffectType = LLMEffectType.ReduceHP;
        [SerializeField, Range(1, 10)] private int testIntensity = 5;
        [SerializeField] private string testTarget = "Marcus";
        
        [Header("LLM Output Test")]
        [SerializeField] private string testLLMOutput = "ReduceHP:7, AddSanity:3";
        
        #if ODIN_INSPECTOR
        [Button("Execute Single Effect", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        private void Debug_ExecuteSingleEffect()
        {
            var effect = new LLMStoryEffectData(testEffectType, testIntensity, testTarget);
            ExecuteEffect(effect);
        }
        
        [Button("Execute LLM Output String")]
        [GUIColor(0.8f, 0.8f, 1f)]
        private void Debug_ExecuteLLMOutput()
        {
            ExecuteFromLLMOutput(testLLMOutput);
        }

        [Button("Test All Effects")]
        [GUIColor(1f, 0.5f, 0)]
        private void Debug_TestAllEffects()
        {
            Debug.Log("[LLMEffectExecutor] Testing ALL effect types with intensity 5...");
            foreach (LLMEffectType type in System.Enum.GetValues(typeof(LLMEffectType)))
            {
                var effect = new LLMStoryEffectData(type, 5, "TestTarget");
                ExecuteEffect(effect);
            }
            Debug.Log("[LLMEffectExecutor] All effects tested!");
        }
        #endif
    }
}
