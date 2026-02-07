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
    /// Builds category-specific prompts and sends them to LLMManager.
    /// Each category (Exploration, Dilemma, FamilyRequest) gets its own
    /// tailored system prompt and user prompt, then a separate LLM API call.
    /// </summary>
    public class PlayerActionLLMBridge : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static PlayerActionLLMBridge Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private bool useJsonMode = true;
        [SerializeField] private bool enableDebugLogs = true;

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
        /// Send a player action for a specific category to the LLM.
        /// Builds category-specific system + user prompts, calls LLMManager.QuickChat.
        /// </summary>
        public void SendCategoryAction(
            PlayerActionCategory category,
            PlayerActionChallenge challenge,
            string playerInput,
            List<string> itemsUsed,
            int currentDay,
            int totalDays,
            string familyTarget,
            Action<PlayerActionResult> onComplete)
        {
            if (LLMManager.Instance == null)
            {
                Debug.LogError("[PlayerActionLLMBridge] LLMManager not found!");
                var errorResult = CreateErrorResult(category, playerInput, itemsUsed, "LLMManager not found.");
                onComplete?.Invoke(errorResult);
                return;
            }

            string systemPrompt = BuildSystemPrompt(category, currentDay, totalDays);
            string userPrompt = BuildUserPrompt(category, challenge, playerInput, itemsUsed, currentDay, totalDays, familyTarget);

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionLLMBridge] Sending [{category}] to LLM...\n--- SYSTEM ---\n{systemPrompt.Substring(0, Mathf.Min(200, systemPrompt.Length))}...\n--- USER ---\n{userPrompt.Substring(0, Mathf.Min(200, userPrompt.Length))}...");

            LLMManager.Instance.QuickChat(
                userPrompt,
                (response) => HandleLLMResponse(response, category, playerInput, itemsUsed, onComplete),
                (error) => HandleLLMError(error, category, playerInput, itemsUsed, onComplete),
                systemPrompt,
                useJsonMode
            );
        }

        // -------------------------------------------------------------------------
        // System Prompt Building (Category-Specific)
        // -------------------------------------------------------------------------
        private string BuildSystemPrompt(PlayerActionCategory category, int currentDay, int totalDays)
        {
            var sb = new StringBuilder();

            // Shared context
            sb.AppendLine("You are the story engine for a government fallout bunker survival game.");
            sb.AppendLine($"The game lasts {totalDays} days. A family must survive in their underground bunker after a government-triggered catastrophe.");
            sb.AppendLine();

            // Category-specific role
            switch (category)
            {
                case PlayerActionCategory.Exploration:
                    sb.AppendLine("CATEGORY: EXPLORATION");
                    sb.AppendLine("The player is attempting to solve a challenge in the bunker or during an expedition.");
                    sb.AppendLine("Evaluate the player's typed solution. Be fair but realistic.");
                    sb.AppendLine("- Creative, clever solutions should be rewarded with positive effects.");
                    sb.AppendLine("- Reckless or poorly thought-out solutions should have negative consequences.");
                    sb.AppendLine("- Partial solutions can have mixed results (some good, some bad).");
                    sb.AppendLine("- If the player uses items, factor them into the outcome (tools help with repairs, meds help with medical issues, etc).");
                    break;

                case PlayerActionCategory.Dilemma:
                    sb.AppendLine("CATEGORY: DILEMMA");
                    sb.AppendLine("The player is responding to a moral or practical dilemma with no single correct answer.");
                    sb.AppendLine("Every choice should have BOTH positive AND negative consequences.");
                    sb.AppendLine("- There is no perfect answer. Reward thoughtfulness, but show tradeoffs.");
                    sb.AppendLine("- Short-term gains may cause long-term problems.");
                    sb.AppendLine("- Helping one person may hurt another.");
                    sb.AppendLine("- If the player uses items, it can soften negative consequences but not eliminate them.");
                    break;

                case PlayerActionCategory.FamilyRequest:
                    sb.AppendLine("CATEGORY: FAMILY REQUEST");
                    sb.AppendLine("A family member needs help with a personal issue (sickness, injury, fear, conflict).");
                    sb.AppendLine("Focus on the emotional and physical impact on the target character.");
                    sb.AppendLine("- Good, attentive help should improve the character's condition.");
                    sb.AppendLine("- Neglect or dismissive responses should worsen their state.");
                    sb.AppendLine("- If the player uses items (especially meds for sickness), it should significantly help.");
                    sb.AppendLine("- The response should feel personal and character-driven.");
                    break;
            }
            sb.AppendLine();

            // Pacing
            sb.AppendLine("PACING:");
            if (currentDay <= 5)
                sb.AppendLine("- Days 1-5: Settling in. Minor issues, resource discovery. Low danger.");
            else if (currentDay <= 12)
                sb.AppendLine("- Days 6-12: Rising tension. Supplies dwindling, first outside threats.");
            else if (currentDay <= 20)
                sb.AppendLine("- Days 13-20: Crisis. Major shortages, raids, disease, hard moral choices.");
            else if (currentDay <= 27)
                sb.AppendLine("- Days 21-27: Desperation. Resources critical, casualties likely.");
            else
                sb.AppendLine($"- Days 28-{totalDays}: Endgame. Final push for survival, maximum stakes.");
            sb.AppendLine();

            // Character names
            sb.AppendLine("CHARACTERS:");
            string characterNames = GetCharacterNameList();
            sb.AppendLine($"- The family members are: {characterNames}");
            sb.AppendLine("- You MUST use these exact names as the \"target\" in effects. Do NOT invent names.");
            sb.AppendLine();

            // Effect types
            sb.AppendLine("AVAILABLE EFFECT TYPES:");
            sb.AppendLine("Character stats: AddHP, ReduceHP, AddSanity, ReduceSanity, AddHunger, ReduceHunger, AddThirst, ReduceThirst");
            sb.AppendLine("Resources: AddFood, ReduceFood, AddWater, ReduceWater, AddSupplies, ReduceSupplies");
            sb.AppendLine("Character: InjureCharacter, HealCharacter, KillCharacter");
            sb.AppendLine("Sickness: InfectCharacter (intensity determines severity/type), CureCharacter");
            sb.AppendLine("Intensity is 1-10 (1=minor, 10=extreme). Use KillCharacter sparingly.");
            sb.AppendLine("InfectCharacter intensity: 1-2=Flu, 3-4=FoodPoisoning, 5=Fever, 6=Infection, 7=Dysentery, 8=Pneumonia, 9=RadiationPoisoning, 10=Plague");
            sb.AppendLine($"Target must be one of: {characterNames}");
            sb.AppendLine("For resource effects, target can be empty.");
            sb.AppendLine();

            // JSON format
            sb.AppendLine("RESPONSE FORMAT (strict JSON):");
            sb.AppendLine("You MUST respond with ONLY a JSON object in this exact format:");
            sb.AppendLine("{");
            sb.AppendLine("  \"title\": \"Short event title\",");
            sb.AppendLine("  \"description\": \"2-3 sentences describing what happens as a result of the player's action\",");
            sb.AppendLine("  \"effects\": [");
            sb.AppendLine("    {\"effectType\": \"ReduceWater\", \"intensity\": 5, \"target\": \"\"}");
            sb.AppendLine("  ],");
            sb.AppendLine("  \"choices\": [");
            sb.AppendLine("    {");
            sb.AppendLine("      \"text\": \"Follow-up choice description\",");
            sb.AppendLine($"      \"effects\": [{{\"effectType\": \"ReduceHP\", \"intensity\": 3, \"target\": \"{GetFirstCharacterName()}\"}}]");
            sb.AppendLine("    },");
            sb.AppendLine("    {");
            sb.AppendLine("      \"text\": \"Alternative follow-up\",");
            sb.AppendLine("      \"effects\": [{\"effectType\": \"ReduceFood\", \"intensity\": 4, \"target\": \"\"}]");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            sb.AppendLine();

            sb.AppendLine("RULES:");
            sb.AppendLine("- Always include 2-3 choices for follow-up actions.");
            sb.AppendLine("- Each choice must have at least 1 effect.");
            sb.AppendLine("- The \"effects\" array at the top level contains immediate consequences of the player's action.");
            sb.AppendLine("- Do NOT include any text outside the JSON object.");
            sb.AppendLine("- Be creative and varied. Each event should feel different.");
            sb.AppendLine("- Vary the types of effects used. Don't always target the same character.");

            return sb.ToString();
        }

        // -------------------------------------------------------------------------
        // User Prompt Building (Category-Specific)
        // -------------------------------------------------------------------------
        private string BuildUserPrompt(
            PlayerActionCategory category,
            PlayerActionChallenge challenge,
            string playerInput,
            List<string> itemsUsed,
            int currentDay,
            int totalDays,
            string familyTarget)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"=== DAY {currentDay} of {totalDays} | CATEGORY: {category.ToString().ToUpper()} ===");
            sb.AppendLine();

            // Family status
            sb.AppendLine("FAMILY STATUS:");
            if (FamilyManager.Instance != null)
            {
                var family = FamilyManager.Instance.FamilyMembers;
                if (family != null && family.Count > 0)
                {
                    foreach (var member in family)
                    {
                        sb.AppendLine($"- {member.Name}: HP={member.Health:F0} Hunger={member.Hunger:F0} Thirst={member.Thirst:F0} Sanity={member.Sanity:F0} [{member.GetStatusSummary()}]");
                    }
                }
                else
                {
                    sb.AppendLine("- No family data available.");
                }
            }
            sb.AppendLine();

            // Challenge context
            if (challenge != null)
            {
                sb.AppendLine("CHALLENGE PRESENTED TO PLAYER:");
                sb.AppendLine($"  Title: {challenge.Title}");
                string desc = category == PlayerActionCategory.FamilyRequest && !string.IsNullOrEmpty(familyTarget)
                    ? challenge.GetDescription(familyTarget)
                    : challenge.Description;
                sb.AppendLine($"  Description: {desc}");
                sb.AppendLine();
            }

            // Family target for family requests
            if (category == PlayerActionCategory.FamilyRequest && !string.IsNullOrEmpty(familyTarget))
            {
                sb.AppendLine($"TARGET FAMILY MEMBER: {familyTarget}");
                if (FamilyManager.Instance != null)
                {
                    var target = FamilyManager.Instance.GetCharacter(familyTarget);
                    if (target != null)
                    {
                        sb.AppendLine($"  Status: HP={target.Health:F0} Hunger={target.Hunger:F0} Sanity={target.Sanity:F0} [{target.GetStatusSummary()}]");
                    }
                }
                sb.AppendLine();
            }

            // Items being used
            if (itemsUsed != null && itemsUsed.Count > 0)
            {
                sb.AppendLine("ITEMS PLAYER IS USING:");
                foreach (var itemId in itemsUsed)
                {
                    string itemName = itemId;
                    string itemType = "Unknown";
                    if (ItemManager.Instance != null)
                    {
                        var itemData = ItemManager.Instance.GetItem(itemId);
                        if (itemData != null)
                        {
                            itemName = itemData.ItemName;
                            itemType = itemData.Type.ToString();
                        }
                    }
                    sb.AppendLine($"  - {itemName} ({itemType})");
                }
                sb.AppendLine("Consider these items in your response. They should influence the outcome.");
                sb.AppendLine();
            }

            // Player's response
            sb.AppendLine($"PLAYER'S RESPONSE: \"{playerInput}\"");
            sb.AppendLine();

            // Category-specific evaluation instructions
            switch (category)
            {
                case PlayerActionCategory.Exploration:
                    sb.AppendLine("Evaluate how well the player's response solves the challenge. Generate consequences accordingly.");
                    sb.AppendLine("Consider creativity, practicality, and risk level of their approach.");
                    break;
                case PlayerActionCategory.Dilemma:
                    sb.AppendLine("Evaluate the player's stance on this dilemma. Show both positive and negative consequences.");
                    sb.AppendLine("There should be tradeoffs no matter what they chose.");
                    break;
                case PlayerActionCategory.FamilyRequest:
                    sb.AppendLine($"Evaluate how the player's response helps (or fails to help) {familyTarget}.");
                    sb.AppendLine("Focus effects primarily on the target family member.");
                    break;
            }

            return sb.ToString();
        }

        // -------------------------------------------------------------------------
        // LLM Response Handling
        // -------------------------------------------------------------------------
        private void HandleLLMResponse(
            string response,
            PlayerActionCategory category,
            string playerInput,
            List<string> itemsUsed,
            Action<PlayerActionResult> onComplete)
        {
            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError($"[PlayerActionLLMBridge] Empty response from LLM for [{category}].");
                var errorResult = CreateErrorResult(category, playerInput, itemsUsed, "Empty LLM response.");
                onComplete?.Invoke(errorResult);
                return;
            }

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionLLMBridge] [{category}] RAW RESPONSE:\n{response}");

            // Extract and parse JSON
            string json = LLMJsonParser.ExtractJson(response);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError($"[PlayerActionLLMBridge] Could not extract JSON for [{category}].");
                var errorResult = CreateErrorResult(category, playerInput, itemsUsed, "Could not extract JSON from response.");
                onComplete?.Invoke(errorResult);
                return;
            }

            var storyEvent = LLMStoryEventData.FromJson(json);
            if (storyEvent == null)
            {
                Debug.LogError($"[PlayerActionLLMBridge] Failed to parse story event for [{category}].");
                var errorResult = CreateErrorResult(category, playerInput, itemsUsed, "Failed to parse story event JSON.");
                onComplete?.Invoke(errorResult);
                return;
            }

            if (enableDebugLogs)
                Debug.Log($"[PlayerActionLLMBridge] [{category}] SUCCESS: \"{storyEvent.Title}\" with {storyEvent.Effects?.Count ?? 0} effect(s)");

            var result = new PlayerActionResult
            {
                Category = category,
                PlayerInput = playerInput,
                ItemsUsed = itemsUsed ?? new List<string>(),
                StoryEvent = storyEvent,
                IsComplete = true
            };

            onComplete?.Invoke(result);
        }

        private void HandleLLMError(
            string error,
            PlayerActionCategory category,
            string playerInput,
            List<string> itemsUsed,
            Action<PlayerActionResult> onComplete)
        {
            Debug.LogError($"[PlayerActionLLMBridge] [{category}] LLM Error: {error}");
            var errorResult = CreateErrorResult(category, playerInput, itemsUsed, error);
            onComplete?.Invoke(errorResult);
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------
        private PlayerActionResult CreateErrorResult(
            PlayerActionCategory category,
            string playerInput,
            List<string> itemsUsed,
            string error)
        {
            return new PlayerActionResult
            {
                Category = category,
                PlayerInput = playerInput,
                ItemsUsed = itemsUsed ?? new List<string>(),
                StoryEvent = null,
                IsComplete = true,
                Error = error
            };
        }

        private string GetCharacterNameList()
        {
            if (FamilyManager.Instance == null) return "Unknown";
            var family = FamilyManager.Instance.FamilyMembers;
            if (family == null || family.Count == 0) return "Unknown";

            var names = new List<string>();
            foreach (var member in family)
            {
                if (member.IsAlive)
                    names.Add(member.Name);
            }
            return names.Count > 0 ? string.Join(", ", names) : "Unknown";
        }

        private string GetFirstCharacterName()
        {
            if (FamilyManager.Instance == null) return "Father";
            var family = FamilyManager.Instance.FamilyMembers;
            if (family == null || family.Count == 0) return "Father";
            return family[0].Name;
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug")]
        [Button("Test Exploration Prompt", ButtonSizes.Medium)]
        private void Debug_TestExplorationPrompt()
        {
            string sys = BuildSystemPrompt(PlayerActionCategory.Exploration, 5, 30);
            string usr = BuildUserPrompt(
                PlayerActionCategory.Exploration,
                new PlayerActionChallenge(),
                "I'll use the crowbar to pry open the door",
                new List<string> { "Crowbar" },
                5, 30, null);
            Debug.Log($"[Debug] EXPLORATION SYSTEM PROMPT:\n{sys}\n\nUSER PROMPT:\n{usr}");
        }

        [Button("Test Dilemma Prompt", ButtonSizes.Medium)]
        private void Debug_TestDilemmaPrompt()
        {
            string sys = BuildSystemPrompt(PlayerActionCategory.Dilemma, 10, 30);
            string usr = BuildUserPrompt(
                PlayerActionCategory.Dilemma,
                new PlayerActionChallenge(),
                "We should share the water equally",
                null,
                10, 30, null);
            Debug.Log($"[Debug] DILEMMA SYSTEM PROMPT:\n{sys}\n\nUSER PROMPT:\n{usr}");
        }

        [Button("Test Family Prompt", ButtonSizes.Medium)]
        private void Debug_TestFamilyPrompt()
        {
            string sys = BuildSystemPrompt(PlayerActionCategory.FamilyRequest, 15, 30);
            string usr = BuildUserPrompt(
                PlayerActionCategory.FamilyRequest,
                new PlayerActionChallenge(),
                "I'll give them medicine and stay by their side",
                new List<string> { "Antibiotics" },
                15, 30, "Mother");
            Debug.Log($"[Debug] FAMILY SYSTEM PROMPT:\n{sys}\n\nUSER PROMPT:\n{usr}");
        }
        #endif
    }
}
