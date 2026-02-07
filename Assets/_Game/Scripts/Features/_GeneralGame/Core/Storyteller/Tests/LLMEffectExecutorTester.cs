using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Test component for LLMEffectExecutor.
    /// Provides dropdown selection for easy effect testing in the Inspector.
    /// </summary>
    public class LLMEffectExecutorTester : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Test Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Effect Tester")]
        [InfoBox("Use this to test individual effects via dropdown selection.")]
        #endif
        [Header("Single Effect Test")]
        [SerializeField] private LLMEffectType effectType = LLMEffectType.ReduceHP;
        
        [SerializeField, Range(1, 10)] private int intensity = 5;
        
        [SerializeField] private string target = "";

        #if ODIN_INSPECTOR
        [Title("Batch Test")]
        #endif
        [Header("LLM Output Test")]
        [SerializeField, TextArea(2, 5)] 
        private string llmOutputTest = "ReduceHP:7, AddSanity:3";

        #if ODIN_INSPECTOR
        [Title("Startup Test")]
        #endif
        [Header("Auto-Test on Start")]
        [SerializeField] private bool runTestOnStart = true;
        [SerializeField] private LLMEffectType startupTestEffect = LLMEffectType.AddHP;
        [SerializeField] private int startupTestIntensity = 3;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Start()
        {
            if (runTestOnStart)
            {
                Debug.Log("[LLMEffectExecutorTester] Running startup test...");
                RunStartupTest();
            }
        }

        // -------------------------------------------------------------------------
        // Test Methods
        // -------------------------------------------------------------------------
        private void RunStartupTest()
        {
            if (LLMEffectExecutor.Instance == null)
            {
                Debug.LogError("[LLMEffectExecutorTester] FAIL - LLMEffectExecutor.Instance is null!");
                return;
            }

            var effect = new LLMStoryEffectData(startupTestEffect.ToString(), startupTestIntensity, "StartupTest");
            LLMEffectExecutor.Instance.ExecuteEffect(effect);
            
            Debug.Log($"[LLMEffectExecutorTester] SUCCESS - Startup test executed: {effect}");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Execute Selected Effect", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        #endif
        public void Debug_ExecuteSelectedEffect()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test effects.");
                return;
            }

            if (LLMEffectExecutor.Instance == null)
            {
                Debug.LogError("LLMEffectExecutor not found in scene!");
                return;
            }

            var effect = new LLMStoryEffectData(effectType.ToString(), intensity, target);
            LLMEffectExecutor.Instance.ExecuteEffect(effect);
            
            Debug.Log($"[Tester] Executed: {effect}");
        }

        #if ODIN_INSPECTOR
        [Button("Execute LLM Output String")]
        [GUIColor(0.8f, 0.8f, 1f)]
        #endif
        public void Debug_ExecuteLLMOutput()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test effects.");
                return;
            }

            if (LLMEffectExecutor.Instance == null)
            {
                Debug.LogError("LLMEffectExecutor not found in scene!");
                return;
            }

            LLMEffectExecutor.Instance.ExecuteFromLLMOutput(llmOutputTest);
            Debug.Log($"[Tester] Executed LLM output: {llmOutputTest}");
        }

        #if ODIN_INSPECTOR
        [Button("Test All Effect Types")]
        [GUIColor(1f, 0.5f, 0)]
        #endif
        public void Debug_TestAllEffects()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test effects.");
                return;
            }

            if (LLMEffectExecutor.Instance == null)
            {
                Debug.LogError("LLMEffectExecutor not found in scene!");
                return;
            }

            Debug.Log("[Tester] Testing ALL effect types with intensity 5...");
            
            foreach (LLMEffectType type in System.Enum.GetValues(typeof(LLMEffectType)))
            {
                var effect = new LLMStoryEffectData(type.ToString(), 5, "TestTarget");
                LLMEffectExecutor.Instance.ExecuteEffect(effect);
            }
            
            Debug.Log("[Tester] All effects tested!");
        }
    }
}
