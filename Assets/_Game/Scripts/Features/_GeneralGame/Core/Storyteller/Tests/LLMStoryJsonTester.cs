using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Test component for JSON-based story event parsing and execution.
    /// Use this to validate LLM JSON output format.
    /// </summary>
    public class LLMStoryJsonTester : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Test Input
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("JSON Input")]
        [InfoBox("Paste LLM JSON output here to test parsing and execution.")]
        #endif
        [Header("Test JSON")]
        [SerializeField, TextArea(8, 15)] 
        private string testJson = @"{
  ""title"": ""The Pipe Bursts"",
  ""description"": ""A pipe in the water filtration system has burst!"",
  ""effects"": [
    { ""effectType"": ""ReduceWater"", ""intensity"": 7 },
    { ""effectType"": ""ReduceSanity"", ""intensity"": 3, ""target"": ""Mother"" }
  ],
  ""choices"": [
    {
      ""text"": ""Fix it yourself (risky)"",
      ""effects"": [{ ""effectType"": ""ReduceHP"", ""intensity"": 4, ""target"": ""Daughter"" }]
    },
    {
      ""text"": ""Send Son to fix it"",
      ""effects"": [{ ""effectType"": ""InjureCharacter"", ""intensity"": 5, ""target"": ""Son"" }]
    }
  ]
}";

        #if ODIN_INSPECTOR
        [Title("Results")]
        [ReadOnly]
        #endif
        [Header("Parse Result")]
        [SerializeField] private string parseStatus = "Not tested";
        [SerializeField, TextArea(3, 5)] private string parsedEventInfo;

        // -------------------------------------------------------------------------
        // Test Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Actions")]
        [Button("Parse JSON", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.8f, 1f)]
        #endif
        public void ParseTestJson()
        {
            var storyEvent = LLMStoryEventData.FromJson(testJson);
            
            if (storyEvent == null)
            {
                parseStatus = "FAILED - Invalid JSON";
                parsedEventInfo = "";
                Debug.LogError("[LLMStoryJsonTester] Failed to parse JSON!");
                return;
            }

            parseStatus = "SUCCESS";
            parsedEventInfo = $"Title: {storyEvent.Title}\n" +
                             $"Effects: {storyEvent.Effects?.Count ?? 0}\n" +
                             $"Choices: {storyEvent.Choices?.Count ?? 0}";
            
            Debug.Log($"[LLMStoryJsonTester] Parsed: {storyEvent}");
            Debug.Log($"[LLMStoryJsonTester] Effects:");
            foreach (var effect in storyEvent.Effects)
            {
                Debug.Log($"  - {effect}");
            }
            Debug.Log($"[LLMStoryJsonTester] Choices:");
            foreach (var choice in storyEvent.Choices)
            {
                Debug.Log($"  - {choice}");
            }
        }

        #if ODIN_INSPECTOR
        [Button("Execute Effects", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        #endif
        public void ExecuteTestEffects()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[LLMStoryJsonTester] Enter Play Mode to execute effects.");
                return;
            }

            if (LLMEffectExecutor.Instance == null)
            {
                Debug.LogError("[LLMStoryJsonTester] LLMEffectExecutor not found in scene!");
                return;
            }

            var storyEvent = LLMStoryEventData.FromJson(testJson);
            if (storyEvent == null)
            {
                Debug.LogError("[LLMStoryJsonTester] Failed to parse JSON!");
                return;
            }

            Debug.Log($"[LLMStoryJsonTester] Executing {storyEvent.Effects.Count} effects...");
            LLMEffectExecutor.Instance.ExecuteEffects(storyEvent.Effects);
        }

        #if ODIN_INSPECTOR
        [Button("Full Test (Parse + Execute via StorytellerManager)")]
        [GUIColor(1f, 0.5f, 0)]
        #endif
        public void FullTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[LLMStoryJsonTester] Enter Play Mode for full test.");
                return;
            }

            if (StorytellerManager.Instance == null)
            {
                Debug.LogError("[LLMStoryJsonTester] StorytellerManager not found in scene!");
                return;
            }

            Debug.Log("[LLMStoryJsonTester] Running full JSON -> StorytellerManager test...");
            StorytellerManager.Instance.ProcessEventFromJson(testJson);
            Debug.Log("[LLMStoryJsonTester] Full test complete!");
        }

        #if ODIN_INSPECTOR
        [Button("Generate Sample JSON")]
        [GUIColor(0.8f, 0.8f, 0.8f)]
        #endif
        public void GenerateSampleJson()
        {
            var sample = LLMStoryEventData.CreateSample();
            testJson = sample.ToJson(prettyPrint: true);
            parseStatus = "Sample generated";
            Debug.Log("[LLMStoryJsonTester] Generated sample JSON.");
        }

        #if ODIN_INSPECTOR
        [Button("Test Round-Trip (Serialize -> Deserialize)")]
        #endif
        public void TestRoundTrip()
        {
            // Parse
            var original = LLMStoryEventData.FromJson(testJson);
            if (original == null)
            {
                Debug.LogError("[LLMStoryJsonTester] Round-trip FAILED - could not parse original JSON.");
                return;
            }

            // Serialize back
            string serialized = original.ToJson(prettyPrint: true);
            
            // Parse again
            var roundTripped = LLMStoryEventData.FromJson(serialized);
            if (roundTripped == null)
            {
                Debug.LogError("[LLMStoryJsonTester] Round-trip FAILED - could not parse serialized JSON.");
                return;
            }

            // Compare
            bool titlesMatch = original.Title == roundTripped.Title;
            bool effectsMatch = original.Effects.Count == roundTripped.Effects.Count;
            bool choicesMatch = original.Choices.Count == roundTripped.Choices.Count;

            if (titlesMatch && effectsMatch && choicesMatch)
            {
                Debug.Log("[LLMStoryJsonTester] Round-trip SUCCESS! JSON serialization is working correctly.");
                parseStatus = "Round-trip SUCCESS";
            }
            else
            {
                Debug.LogWarning($"[LLMStoryJsonTester] Round-trip MISMATCH - Titles:{titlesMatch} Effects:{effectsMatch} Choices:{choicesMatch}");
                parseStatus = "Round-trip MISMATCH";
            }
        }
    }
}
