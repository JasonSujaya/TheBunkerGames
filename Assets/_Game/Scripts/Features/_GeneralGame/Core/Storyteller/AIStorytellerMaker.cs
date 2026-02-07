using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Generates AI-driven story events for a 7-day bunker survival game.
    /// Sends player actions + game state to an LLM (Mistral/OpenRouter) and
    /// feeds the resulting JSON into StorytellerManager → LLMEffectExecutor.
    /// </summary>
    public class AIStorytellerMaker : MonoBehaviour
    {
        public static AIStorytellerMaker Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Story Generation")]
        #endif
        [Header("Settings")]
        [SerializeField] private int totalDays = 7;
        [SerializeField] private int eventsPerDay = 2;
        [SerializeField] private bool useJsonMode = true;
        [SerializeField] private bool enableDebugLogs = true;

        #if ODIN_INSPECTOR
        [Title("Story History")]
        [ReadOnly]
        #endif
        [Header("State")]
        [SerializeField] private int currentDay = 1;
        [SerializeField, TextArea(2, 4)] private string lastPlayerAction = "";
        [SerializeField, TextArea(3, 6)] private string lastGeneratedEvent = "";

        // -------------------------------------------------------------------------
        // Story Memory - tracks what happened so the AI stays consistent
        // -------------------------------------------------------------------------
        private List<StoryBeat> storyHistory = new List<StoryBeat>();
        private bool isGenerating;

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<LLMStoryEventData> OnStoryGenerated;
        public static event Action<string> OnGenerationFailed;

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
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Generate a story event based on what the player just did.
        /// The AI considers current game state, family status, day number, and history.
        /// </summary>
        public void GenerateStoryEvent(string playerAction, Action<LLMStoryEventData> onComplete = null)
        {
            if (isGenerating)
            {
                Debug.LogWarning("[AIStorytellerMaker] Already generating a story event. Please wait.");
                return;
            }

            if (LLMManager.Instance == null)
            {
                Debug.LogError("[AIStorytellerMaker] LLMManager not found! Cannot generate story.");
                OnGenerationFailed?.Invoke("LLMManager not found.");
                return;
            }

            isGenerating = true;
            lastPlayerAction = playerAction;

            // Sync day from GameSessionData if available
            SyncCurrentDay();

            string systemPrompt = BuildSystemPrompt();
            string userPrompt = BuildUserPrompt(playerAction);

            Debug.Log($"[AIStorytellerMaker] Generating story for Day {currentDay}. Player action: {playerAction}");

            if (enableDebugLogs)
            {
                Debug.Log($"[AIStorytellerMaker] <color=cyan>===== SYSTEM PROMPT =====</color>\n{systemPrompt}");
                Debug.Log($"[AIStorytellerMaker] <color=yellow>===== USER PROMPT =====</color>\n{userPrompt}");
            }

            LLMManager.Instance.QuickChat(
                userPrompt,
                onSuccess: (response) => HandleLLMResponse(response, playerAction, onComplete),
                onError: (error) => HandleLLMError(error, onComplete),
                systemPrompt: systemPrompt,
                useJsonMode: useJsonMode
            );
        }

        /// <summary>
        /// Set the current day manually (useful for testing or overriding GameSessionData).
        /// </summary>
        public void SetDay(int day)
        {
            currentDay = Mathf.Clamp(day, 1, totalDays);
        }

        /// <summary>
        /// Clear all story history (for new game).
        /// </summary>
        public void ResetHistory()
        {
            storyHistory.Clear();
            currentDay = 1;
            lastPlayerAction = "";
            lastGeneratedEvent = "";
            Debug.Log("[AIStorytellerMaker] Story history cleared.");
        }

        // -------------------------------------------------------------------------
        // Prompt Building
        // -------------------------------------------------------------------------
        private string BuildSystemPrompt()
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are the story engine for a post-apocalyptic bunker survival game.");
            sb.AppendLine($"The game lasts {totalDays} days. A family must survive in their underground bunker.");
            sb.AppendLine();
            sb.AppendLine("SETTING:");
            sb.AppendLine("- The world above has been devastated. The family is sheltering in a bunker.");
            sb.AppendLine("- Resources are scarce: food, water, medicine, and tools are limited.");
            sb.AppendLine("- Threats include raiders, radiation, illness, structural damage, and psychological stress.");
            sb.AppendLine("- The family must make tough decisions to survive all 7 days.");
            sb.AppendLine();
            sb.AppendLine("YOUR ROLE:");
            sb.AppendLine("- Generate a story event based on the player's action and current game state.");
            sb.AppendLine("- Events should have consequences (effects) and present the player with meaningful choices.");
            sb.AppendLine("- Escalate tension as days progress. Early days are about settling in, later days get dangerous.");
            sb.AppendLine("- Be creative but consistent with prior events.");
            sb.AppendLine();

            // Day-based pacing guidance
            sb.AppendLine("PACING:");
            if (currentDay <= 2)
                sb.AppendLine("- Days 1-2: Introduction. Minor issues, resource discovery, settling in. Low danger.");
            else if (currentDay <= 4)
                sb.AppendLine("- Days 3-4: Rising tension. Resources running low, first real threats appear. Medium danger.");
            else if (currentDay <= 6)
                sb.AppendLine("- Days 5-6: Crisis. Major threats, hard choices, potential casualties. High danger.");
            else
                sb.AppendLine("- Day 7: Climax. Final day, everything comes to a head. Maximum stakes.");
            sb.AppendLine();

            // Effect types the AI can use
            sb.AppendLine("AVAILABLE EFFECT TYPES:");
            sb.AppendLine("Character stats: AddHP, ReduceHP, AddSanity, ReduceSanity, AddHunger, ReduceHunger, AddThirst, ReduceThirst");
            sb.AppendLine("Resources: AddFood, ReduceFood, AddWater, ReduceWater, AddSupplies, ReduceSupplies");
            sb.AppendLine("Character: InjureCharacter, HealCharacter, KillCharacter");
            sb.AppendLine("Intensity is 1-10 (1=minor, 10=extreme). Use KillCharacter sparingly and only in extreme cases.");
            sb.AppendLine("Target must be a character name (e.g. \"Father\", \"Mother\", \"Son\", \"Daughter\").");
            sb.AppendLine("For resource effects, target can be empty.");
            sb.AppendLine();

            // JSON format specification
            sb.AppendLine("RESPONSE FORMAT (strict JSON):");
            sb.AppendLine("You MUST respond with ONLY a JSON object in this exact format:");
            sb.AppendLine("{");
            sb.AppendLine("  \"title\": \"Short event title\",");
            sb.AppendLine("  \"description\": \"2-3 sentences describing what happens\",");
            sb.AppendLine("  \"effects\": [");
            sb.AppendLine("    {\"effectType\": \"ReduceWater\", \"intensity\": 5, \"target\": \"\"}");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"choices\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"text\": \"Choice description\",");
            sb.AppendLine("      \"effects\": [{\"effectType\": \"ReduceHP\", \"intensity\": 3, \"target\": \"Father\"}]");
            sb.AppendLine("    },");
            sb.AppendLine("    {");
            sb.AppendLine("      \"text\": \"Alternative choice\",");
            sb.AppendLine("      \"effects\": [{\"effectType\": \"ReduceFood\", \"intensity\": 4, \"target\": \"\"}]");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("RULES:");
            sb.AppendLine("- Always include 2-3 choices.");
            sb.AppendLine("- Each choice must have at least 1 effect.");
            sb.AppendLine("- The \"effects\" array at the top level contains immediate consequences.");
            sb.AppendLine("- Do NOT include any text outside the JSON object. No markdown, no explanation.");

            return sb.ToString();
        }

        private string BuildUserPrompt(string playerAction)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"=== DAY {currentDay} of {totalDays} ===");
            sb.AppendLine();

            // Current family status
            sb.AppendLine("FAMILY STATUS:");
            if (FamilyManager.Instance != null)
            {
                var family = FamilyManager.Instance.FamilyMembers;
                if (family != null && family.Count > 0)
                {
                    foreach (var member in family)
                    {
                        string status = member.IsDead ? "DEAD" : member.IsCritical ? "CRITICAL" : member.IsInjured ? "INJURED" : "OK";
                        sb.AppendLine($"- {member.Name}: HP={member.Health:F0} Hunger={member.Hunger:F0} Thirst={member.Thirst:F0} Sanity={member.Sanity:F0} [{status}]");
                    }
                }
                else
                {
                    sb.AppendLine("- No family data available.");
                }
            }
            else
            {
                sb.AppendLine("- Family status unknown.");
            }
            sb.AppendLine();

            // Story history summary (last few events for context)
            if (storyHistory.Count > 0)
            {
                sb.AppendLine("RECENT EVENTS:");
                int start = Mathf.Max(0, storyHistory.Count - 3);
                for (int i = start; i < storyHistory.Count; i++)
                {
                    var beat = storyHistory[i];
                    sb.AppendLine($"- Day {beat.Day}: \"{beat.EventTitle}\" (Player chose: {beat.PlayerAction})");
                }
                sb.AppendLine();
            }

            // Player action
            sb.AppendLine($"PLAYER ACTION: {playerAction}");
            sb.AppendLine();
            sb.AppendLine("Generate a story event that reacts to this action. Consider the family's current state and what has happened before.");

            return sb.ToString();
        }

        // -------------------------------------------------------------------------
        // LLM Response Handling
        // -------------------------------------------------------------------------
        private void HandleLLMResponse(string response, string playerAction, Action<LLMStoryEventData> onComplete)
        {
            isGenerating = false;

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[AIStorytellerMaker] Empty response from LLM.");
                OnGenerationFailed?.Invoke("Empty LLM response.");
                onComplete?.Invoke(null);
                return;
            }

            // Log raw LLM output
            if (enableDebugLogs)
            {
                Debug.Log($"[AIStorytellerMaker] <color=green>===== RAW LLM RESPONSE =====</color>\n{response}");
            }

            // Extract JSON from response (handles markdown code blocks etc.)
            string json = LLMJsonParser.ExtractJson(response);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[AIStorytellerMaker] <color=red>Could not extract JSON from LLM response:</color>\n{response}");
                OnGenerationFailed?.Invoke("Could not extract JSON from response.");
                onComplete?.Invoke(null);
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[AIStorytellerMaker] <color=green>===== EXTRACTED JSON =====</color>\n{json}");
            }

            // Parse into story event
            var storyEvent = LLMStoryEventData.FromJson(json);
            if (storyEvent == null)
            {
                Debug.LogError($"[AIStorytellerMaker] <color=red>Failed to parse story event from JSON:</color>\n{json}");
                OnGenerationFailed?.Invoke("Failed to parse story event JSON.");
                onComplete?.Invoke(null);
                return;
            }

            // Log detailed parsed breakdown
            if (enableDebugLogs)
            {
                var breakdown = new StringBuilder();
                breakdown.AppendLine($"[AIStorytellerMaker] <color=green>===== PARSED EVENT =====</color>");
                breakdown.AppendLine($"  Title: {storyEvent.Title}");
                breakdown.AppendLine($"  Description: {storyEvent.Description}");
                breakdown.AppendLine($"  Immediate Effects ({storyEvent.Effects?.Count ?? 0}):");
                if (storyEvent.Effects != null)
                {
                    foreach (var fx in storyEvent.Effects)
                        breakdown.AppendLine($"    - {fx.EffectType} | intensity={fx.Intensity} | target=\"{fx.Target}\"");
                }
                breakdown.AppendLine($"  Choices ({storyEvent.Choices?.Count ?? 0}):");
                if (storyEvent.Choices != null)
                {
                    for (int i = 0; i < storyEvent.Choices.Count; i++)
                    {
                        var choice = storyEvent.Choices[i];
                        breakdown.AppendLine($"    [{i}] \"{choice.Text}\"");
                        if (choice.Effects != null)
                        {
                            foreach (var cfx in choice.Effects)
                                breakdown.AppendLine($"         -> {cfx.EffectType} | intensity={cfx.Intensity} | target=\"{cfx.Target}\"");
                        }
                    }
                }
                Debug.Log(breakdown.ToString());
            }

            lastGeneratedEvent = storyEvent.ToJson(true);

            // Record in story history
            storyHistory.Add(new StoryBeat
            {
                Day = currentDay,
                EventTitle = storyEvent.Title,
                PlayerAction = playerAction
            });

            // Fire event
            OnStoryGenerated?.Invoke(storyEvent);

            // Feed into StorytellerManager which handles UI + effect execution
            if (StorytellerManager.Instance != null)
            {
                StorytellerManager.Instance.ProcessEvent(storyEvent);
            }
            else
            {
                Debug.LogWarning("[AIStorytellerMaker] StorytellerManager not found. Event generated but not processed.");
            }

            onComplete?.Invoke(storyEvent);
        }

        private void HandleLLMError(string error, Action<LLMStoryEventData> onComplete)
        {
            isGenerating = false;
            Debug.LogError($"[AIStorytellerMaker] <color=red>===== LLM ERROR =====</color>\n{error}");
            OnGenerationFailed?.Invoke(error);
            onComplete?.Invoke(null);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------
        private void SyncCurrentDay()
        {
            if (GameManager.Instance != null && GameManager.Instance.SessionData != null)
            {
                currentDay = GameManager.Instance.SessionData.CurrentDay;
            }
        }

        // -------------------------------------------------------------------------
        // Data
        // -------------------------------------------------------------------------
        [Serializable]
        private class StoryBeat
        {
            public int Day;
            public string EventTitle;
            public string PlayerAction;
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug - Test Generation")]
        #endif
        [Header("Test")]
        [SerializeField, TextArea(2, 4)] private string testPlayerAction = "Search the storage room for supplies";

        #if ODIN_INSPECTOR
        [Button("Generate Story Event", ButtonSizes.Large)]
        [GUIColor(0, 1, 0)]
        private void Debug_GenerateEvent()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode to test.");
                return;
            }

            GenerateStoryEvent(testPlayerAction, (result) =>
            {
                if (result != null)
                    Debug.Log($"[AIStorytellerMaker] Test complete! Event: {result.Title}");
                else
                    Debug.LogWarning("[AIStorytellerMaker] Test failed - no event generated.");
            });
        }

        [Button("Generate with Custom Action")]
        [GUIColor(0.8f, 0.8f, 1f)]
        private void Debug_GenerateCustom()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode."); return; }

            string action = testPlayerAction;
            if (string.IsNullOrEmpty(action)) action = "Look around the bunker";
            GenerateStoryEvent(action);
        }

        [Button("Show System Prompt")]
        [GUIColor(1f, 0.9f, 0.5f)]
        private void Debug_ShowSystemPrompt()
        {
            SyncCurrentDay();
            Debug.Log($"[AIStorytellerMaker] System Prompt:\n{BuildSystemPrompt()}");
        }

        [Button("Show User Prompt")]
        [GUIColor(1f, 0.9f, 0.5f)]
        private void Debug_ShowUserPrompt()
        {
            SyncCurrentDay();
            Debug.Log($"[AIStorytellerMaker] User Prompt:\n{BuildUserPrompt(testPlayerAction)}");
        }

        [Button("Reset History")]
        [GUIColor(1f, 0.3f, 0.3f)]
        private void Debug_ResetHistory()
        {
            ResetHistory();
        }

        [Title("Quick Day Test")]
        [Button("Day 1 - Settle In")]
        private void Debug_Day1() { if (!Application.isPlaying) return; currentDay = 1; GenerateStoryEvent("Explore the bunker and check supplies"); }

        [Button("Day 3 - Scavenge")]
        private void Debug_Day3() { if (!Application.isPlaying) return; currentDay = 3; GenerateStoryEvent("Send Father to scavenge the nearby buildings"); }

        [Button("Day 5 - Defend")]
        private void Debug_Day5() { if (!Application.isPlaying) return; currentDay = 5; GenerateStoryEvent("Barricade the entrance, raiders are approaching"); }

        [Button("Day 7 - Final")]
        private void Debug_Day7() { if (!Application.isPlaying) return; currentDay = 7; GenerateStoryEvent("Make a final stand and wait for rescue"); }
        #endif
    }
}
