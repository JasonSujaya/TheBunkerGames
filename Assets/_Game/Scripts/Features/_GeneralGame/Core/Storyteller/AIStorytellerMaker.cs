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
    /// Generates AI-driven story events for a bunker survival game.
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
        [SerializeField] private int totalDays = 30;
        [SerializeField] private int eventsPerDay = 2;
        [SerializeField] private bool useJsonMode = true;
        [SerializeField] private bool enableDebugLogs = true;

        #if ODIN_INSPECTOR
        [Title("Data Sources")]
        [InfoBox("Assign a StoryLogSO asset. All generated events are saved here, organized by day.", InfoMessageType.Info)]
        [Required("Create a StoryLogSO via Create > TheBunkerGames > Story Log")]
        #endif
        [SerializeField] private StoryLogSO storyLog;

        #if ODIN_INSPECTOR
        [InfoBox("Optional: Pre-scripted events scheduled for specific days. The AI will incorporate these into its story.", InfoMessageType.Info)]
        #endif
        [SerializeField] private PreScriptedEventScheduleSO preScriptedSchedule;

        #if ODIN_INSPECTOR
        [Title("Current Day State")]
        [ReadOnly]
        #endif
        [Header("State")]
        [SerializeField] private int currentDay = 1;
        [SerializeField] private int eventsGeneratedToday = 0;
        [SerializeField, TextArea(2, 4)] private string lastPlayerAction = "";
        [SerializeField, TextArea(3, 6)] private string lastGeneratedEvent = "";

        #if ODIN_INSPECTOR
        [Title("Today's Scheduled Events")]
        [InfoBox("Shows pre-scripted events scheduled for the current day. These get passed to the AI as context.")]
        [ReadOnly]
        [ListDrawerSettings(IsReadOnly = true)]
        #endif
        [SerializeField] private List<string> todayScheduledEvents = new List<string>();

        // -------------------------------------------------------------------------
        // Story Memory - tracks what happened so the AI stays consistent
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Story Log")]
        [ListDrawerSettings(IsReadOnly = true)]
        #endif
        [SerializeField] private List<StoryBeat> storyHistory = new List<StoryBeat>();
        private bool isGenerating;
        private HashSet<string> usedEventTitles = new HashSet<string>();

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
        /// Generate a story event for the current day based on a player action.
        /// The AI considers current game state, family status, day number, and past story.
        /// </summary>
        public void GenerateStoryEvent(string playerAction, Action<LLMStoryEventData> onComplete = null)
        {
            if (isGenerating)
            {
                Debug.LogWarning("[AIStorytellerMaker] Already generating a story event. Please wait.");
                return;
            }

            if (currentDay > totalDays)
            {
                Debug.LogWarning($"[AIStorytellerMaker] All {totalDays} days completed! Story is finished. Use ResetHistory() to start over.");
                onComplete?.Invoke(null);
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
            RefreshTodayScheduleDisplay();

            string systemPrompt = BuildSystemPrompt();
            string userPrompt = BuildUserPrompt(playerAction);

            if (enableDebugLogs)
            {
                var debugSb = new StringBuilder();
                debugSb.AppendLine($"[AIStorytellerMaker] === GENERATING Day {currentDay} | Action: {playerAction} ===");
                debugSb.AppendLine($"--- SYSTEM PROMPT ---\n{systemPrompt}");
                debugSb.AppendLine($"--- USER PROMPT ---\n{userPrompt}");
                Debug.Log(debugSb.ToString());
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
        /// Advance to the next day and immediately generate a story event for it.
        /// Call this when the player clicks "Next Day".
        /// </summary>
        public void GenerateNextDay(string playerAction, Action<LLMStoryEventData> onComplete = null)
        {
            if (currentDay >= totalDays)
            {
                Debug.LogWarning($"[AIStorytellerMaker] Already on final day ({totalDays}). Cannot advance further.");
                onComplete?.Invoke(null);
                return;
            }

            AdvanceDay();
            GenerateStoryEvent(playerAction, onComplete);
        }

        /// <summary>
        /// Set the current day manually (useful for testing or overriding GameSessionData).
        /// </summary>
        public void SetDay(int day)
        {
            currentDay = Mathf.Clamp(day, 1, totalDays);
            eventsGeneratedToday = 0;
        }

        /// <summary>
        /// Advance to the next day manually.
        /// </summary>
        public void AdvanceDay()
        {
            if (currentDay >= totalDays)
            {
                Debug.LogWarning($"[AIStorytellerMaker] Already on final day ({totalDays}). Cannot advance further.");
                return;
            }

            currentDay++;
            eventsGeneratedToday = 0;

            // Also advance GameSessionData if available
            if (GameManager.Instance != null && GameManager.Instance.SessionData != null)
            {
                GameManager.Instance.SessionData.CurrentDay = currentDay;
            }

            RefreshTodayScheduleDisplay();
            Debug.Log($"[AIStorytellerMaker] Advanced to Day {currentDay}");
        }

        /// <summary>
        /// Clear all story history (for new game).
        /// </summary>
        public void ResetHistory()
        {
            storyHistory.Clear();
            usedEventTitles.Clear();
            currentDay = 1;
            eventsGeneratedToday = 0;
            lastPlayerAction = "";
            lastGeneratedEvent = "";
            if (storyLog != null)
                storyLog.Clear();
            Debug.Log("[AIStorytellerMaker] Story history cleared.");
        }

        // -------------------------------------------------------------------------
        // Prompt Building
        // -------------------------------------------------------------------------
        private string BuildSystemPrompt()
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are the story engine for a government fallout bunker survival game.");
            sb.AppendLine($"The game lasts {totalDays} days. A family must survive in their underground bunker after a government-triggered catastrophe.");
            sb.AppendLine();
            sb.AppendLine("SETTING:");
            sb.AppendLine("- A government fallout has devastated the country. Martial law, failed evacuations, collapsed infrastructure.");
            sb.AppendLine("- The family is sheltering in an underground bunker. Resources are scarce: food, water, medicine, tools.");
            sb.AppendLine("- Threats include government patrols, raiders, contamination, disease, structural damage, and psychological stress.");
            sb.AppendLine("- Occasional government broadcasts provide unreliable information.");
            sb.AppendLine($"- The family must survive all {totalDays} days until rescue or evacuation.");
            sb.AppendLine();
            sb.AppendLine("YOUR ROLE:");
            sb.AppendLine("- Generate a story event based on the player's action, scheduled events, and current game state.");
            sb.AppendLine("- Events should have consequences (effects) and present the player with meaningful choices.");
            sb.AppendLine("- Escalate tension as days progress.");
            sb.AppendLine("- If SCHEDULED EVENTS are provided, you MUST weave them into your story.");
            sb.AppendLine("- Be creative but consistent with prior events.");
            sb.AppendLine();

            // Day-based pacing guidance (scaled for 30 days)
            sb.AppendLine("PACING:");
            if (currentDay <= 5)
                sb.AppendLine("- Days 1-5: Settling in. Minor issues, resource discovery, learning the bunker. Low danger.");
            else if (currentDay <= 12)
                sb.AppendLine("- Days 6-12: Rising tension. Supplies dwindling, first outside threats, government broadcasts.");
            else if (currentDay <= 20)
                sb.AppendLine("- Days 13-20: Crisis. Major shortages, raids, disease outbreaks, hard moral choices.");
            else if (currentDay <= 27)
                sb.AppendLine("- Days 21-27: Desperation. Resources critical, casualties likely, hope fading. High danger.");
            else
                sb.AppendLine($"- Days 28-{totalDays}: Endgame. Final push for survival, evacuation rumors, maximum stakes.");
            sb.AppendLine();

            // Character names the AI must use
            sb.AppendLine("CHARACTERS:");
            string characterNames = GetCharacterNameList();
            sb.AppendLine($"- The family members are: {characterNames}");
            sb.AppendLine("- You MUST use these exact names as the \"target\" in effects. Do NOT invent names.");
            sb.AppendLine();

            // Effect types the AI can use
            sb.AppendLine("AVAILABLE EFFECT TYPES:");
            sb.AppendLine("Character stats: AddHP, ReduceHP, AddSanity, ReduceSanity, AddHunger, ReduceHunger, AddThirst, ReduceThirst");
            sb.AppendLine("Resources: AddFood, ReduceFood, AddWater, ReduceWater, AddSupplies, ReduceSupplies");
            sb.AppendLine("Character: InjureCharacter, HealCharacter, KillCharacter");
            sb.AppendLine("Intensity is 1-10 (1=minor, 10=extreme). Use KillCharacter sparingly and only in extreme cases.");
            sb.AppendLine($"Target must be one of: {characterNames}");
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
            sb.AppendLine($"      \"effects\": [{{\"effectType\": \"ReduceHP\", \"intensity\": 3, \"target\": \"{GetFirstCharacterName()}\"}}]");
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
            sb.AppendLine("- IMPORTANT: Each event MUST have a UNIQUE title. Never reuse a title from a previous event.");
            sb.AppendLine("- Be creative and varied. Each event should feel different from the last.");
            sb.AppendLine("- Vary the types of effects used. Don't always target the same character.");

            // Add used titles so AI avoids repeats
            if (usedEventTitles.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("TITLES ALREADY USED (do NOT reuse any of these):");
                foreach (var title in usedEventTitles)
                {
                    sb.AppendLine($"- \"{title}\"");
                }
            }

            return sb.ToString();
        }

        private string BuildUserPrompt(string playerAction)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"=== DAY {currentDay} of {totalDays} (Event {eventsGeneratedToday + 1} of {eventsPerDay}) ===");
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

            // Past story context - send full narrative so AI stays consistent
            if (storyHistory.Count > 0)
            {
                sb.AppendLine("STORY SO FAR:");
                int prevDay = -1;
                foreach (var beat in storyHistory)
                {
                    if (beat.Day != prevDay)
                    {
                        sb.AppendLine($"--- Day {beat.Day} ---");
                        prevDay = beat.Day;
                    }
                    sb.AppendLine($"  \"{beat.EventTitle}\": {beat.EventDescription}");
                }
                sb.AppendLine();
            }

            // Pre-scripted events for today (designer-authored narrative beats)
            if (preScriptedSchedule != null && preScriptedSchedule.HasEventsForDay(currentDay))
            {
                var scheduled = preScriptedSchedule.GetEventsForDay(currentDay);
                sb.AppendLine("SCHEDULED EVENTS FOR TODAY (you MUST incorporate these into your story):");
                for (int i = 0; i < scheduled.Count; i++)
                {
                    sb.AppendLine($"- [{scheduled[i].Category}] \"{scheduled[i].Title}\": {scheduled[i].Description}");
                }
                sb.AppendLine("Weave these scheduled events into your response. They are mandatory narrative beats.");
                sb.AppendLine();
            }

            // Player action
            sb.AppendLine($"PLAYER ACTION: {playerAction}");
            sb.AppendLine();

            // Add randomized flavor to prevent identical outputs
            string[] flavorPrompts = new string[]
            {
                "Generate a surprising story event that reacts to this action. Introduce an unexpected twist.",
                "Generate a story event with emotional weight. Show how this action affects the family's morale.",
                "Generate a tense story event. Something goes wrong or an opportunity appears.",
                "Generate a story event that reveals something new about the bunker or the world outside.",
                "Generate a story event that creates conflict between family members about what to do next.",
                "Generate a story event where the action has unintended consequences.",
                "Generate a story event that forces a moral dilemma upon the family.",
                "Generate a story event that changes the family's situation significantly."
            };
            string flavor = flavorPrompts[UnityEngine.Random.Range(0, flavorPrompts.Length)];
            sb.AppendLine(flavor);
            sb.AppendLine("Consider the family's current state and what has happened before. Make this event UNIQUE.");

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

            if (enableDebugLogs)
                Debug.Log($"[AIStorytellerMaker] --- RAW LLM RESPONSE ---\n{response}");

            // Extract and parse JSON
            string json = LLMJsonParser.ExtractJson(response);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("[AIStorytellerMaker] Could not extract JSON from LLM response.");
                OnGenerationFailed?.Invoke("Could not extract JSON from response.");
                onComplete?.Invoke(null);
                return;
            }

            var storyEvent = LLMStoryEventData.FromJson(json);
            if (storyEvent == null)
            {
                Debug.LogError("[AIStorytellerMaker] Failed to parse story event from JSON.");
                OnGenerationFailed?.Invoke("Failed to parse story event JSON.");
                onComplete?.Invoke(null);
                return;
            }

            if (enableDebugLogs)
            {
                var parsedSb = new StringBuilder();
                parsedSb.AppendLine($"[AIStorytellerMaker] --- PARSED EVENT (Day {currentDay}) ---");
                parsedSb.AppendLine($"  Title: {storyEvent.Title}");
                parsedSb.AppendLine($"  Description: {storyEvent.Description}");
                if (storyEvent.Effects != null && storyEvent.Effects.Count > 0)
                {
                    parsedSb.AppendLine($"  Effects ({storyEvent.Effects.Count}):");
                    foreach (var fx in storyEvent.Effects)
                        parsedSb.AppendLine($"    - {fx.EffectType} intensity={fx.Intensity} target=\"{fx.Target}\"");
                }
                if (storyEvent.Choices != null && storyEvent.Choices.Count > 0)
                {
                    parsedSb.AppendLine($"  Choices ({storyEvent.Choices.Count}):");
                    for (int i = 0; i < storyEvent.Choices.Count; i++)
                    {
                        var c = storyEvent.Choices[i];
                        parsedSb.AppendLine($"    [{i}] \"{c.Text}\"");
                        if (c.Effects != null)
                            foreach (var cfx in c.Effects)
                                parsedSb.AppendLine($"         -> {cfx.EffectType} intensity={cfx.Intensity} target=\"{cfx.Target}\"");
                    }
                }
                Debug.Log(parsedSb.ToString());
            }

            lastGeneratedEvent = storyEvent.ToJson(true);

            // Track title for uniqueness
            if (!string.IsNullOrEmpty(storyEvent.Title))
                usedEventTitles.Add(storyEvent.Title);

            // Record in story history (runtime memory - used as context for future LLM calls)
            storyHistory.Add(new StoryBeat
            {
                Day = currentDay,
                EventTitle = storyEvent.Title,
                EventDescription = storyEvent.Description,
                PlayerAction = playerAction
            });

            // Record in StoryLogSO (persists in editor)
            if (storyLog != null)
                storyLog.RecordEvent(currentDay, playerAction, storyEvent);

            eventsGeneratedToday++;

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
            Debug.LogError($"[AIStorytellerMaker] LLM Error: {error}");
            OnGenerationFailed?.Invoke(error);
            onComplete?.Invoke(null);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        /// <summary>
        /// Refreshes the inspector display of what's scheduled for the current day.
        /// </summary>
        private void RefreshTodayScheduleDisplay()
        {
            todayScheduledEvents.Clear();
            if (preScriptedSchedule != null)
            {
                var events = preScriptedSchedule.GetEventsForDay(currentDay);
                for (int i = 0; i < events.Count; i++)
                    todayScheduledEvents.Add($"[{events[i].Category}] {events[i].Title}");
            }
        }

        /// <summary>
        /// Gets a comma-separated list of actual character names from FamilyManager.
        /// Falls back to default role names if no family is loaded.
        /// </summary>
        private string GetCharacterNameList()
        {
            if (FamilyManager.Instance != null)
            {
                var family = FamilyManager.Instance.FamilyMembers;
                if (family != null && family.Count > 0)
                {
                    var names = new List<string>();
                    foreach (var member in family)
                    {
                        if (!string.IsNullOrEmpty(member.Name) && member.IsAlive)
                            names.Add($"\"{member.Name}\"");
                    }
                    if (names.Count > 0)
                        return string.Join(", ", names);
                }
            }
            return "\"Father\", \"Mother\", \"Son\", \"Daughter\"";
        }

        /// <summary>
        /// Gets the first alive character name for use in JSON examples.
        /// </summary>
        private string GetFirstCharacterName()
        {
            if (FamilyManager.Instance != null)
            {
                var family = FamilyManager.Instance.FamilyMembers;
                if (family != null)
                {
                    foreach (var member in family)
                    {
                        if (!string.IsNullOrEmpty(member.Name) && member.IsAlive)
                            return member.Name;
                    }
                }
            }
            return "Father";
        }

        // -------------------------------------------------------------------------
        // Data
        // -------------------------------------------------------------------------
        [Serializable]
        private class StoryBeat
        {
            #if ODIN_INSPECTOR
            [HorizontalGroup("Row", Width = 40)]
            [HideLabel]
            #endif
            public int Day;

            #if ODIN_INSPECTOR
            [HorizontalGroup("Row")]
            [HideLabel]
            #endif
            public string EventTitle;

            public string EventDescription;
            public string PlayerAction;

            public override string ToString() => $"Day {Day}: {EventTitle}";
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
                    Debug.Log($"[AIStorytellerMaker] Test complete! Day {currentDay}, Event: {result.Title}");
                else
                    Debug.LogWarning("[AIStorytellerMaker] Test failed - no event generated.");
            });
        }

        [Button("Next Day + Generate", ButtonSizes.Large)]
        [GUIColor(1f, 0.6f, 0.2f)]
        private void Debug_NextDay()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode."); return; }
            if (isGenerating) { Debug.LogWarning("Already generating."); return; }

            string action = testPlayerAction;
            if (string.IsNullOrEmpty(action)) action = "Start the new day";

            GenerateNextDay(action, (result) =>
            {
                if (result != null)
                    Debug.Log($"[AIStorytellerMaker] Next day complete! Day {currentDay}, Event: {result.Title}");
                else
                    Debug.LogWarning("[AIStorytellerMaker] Next day failed.");
            });
        }

        [Button("Reset History")]
        [GUIColor(1f, 0.3f, 0.3f)]
        private void Debug_ResetHistory()
        {
            ResetHistory();
        }
        #endif
    }
}
