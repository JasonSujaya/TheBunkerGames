using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles runtime creation of characters for AI-native gameplay.
    /// A.N.G.E.L. can generate dynamic survivors on the fly.
    /// Characters created here are SESSION-BOUND and do not persist between sessions.
    /// </summary>
    public class CharacterCreator : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static CharacterCreator Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private CharacterManager characterManager;
        
        #if ODIN_INSPECTOR
        [Title("LLM Settings")]
        [InfoBox("When enabled, characters are generated via LLM. When disabled, uses static fallback data.")]
        #endif
        [SerializeField] private bool useLLM = true;
        [SerializeField] private LLMPromptTemplateSO characterPromptTemplate;


        // -------------------------------------------------------------------------
        // Session-Bound Runtime Characters (NOT persisted)
        // -------------------------------------------------------------------------
        private List<CharacterData> sessionCharacters = new List<CharacterData>();

        public IReadOnlyList<CharacterData> SessionCharacters => sessionCharacters;

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

            if (characterManager == null)
            {
                characterManager = CharacterManager.Instance;
            }
        }

        private void OnDestroy()
        {
            ClearSessionCharacters();
        }

        // -------------------------------------------------------------------------
        // Public Methods - Character Creation (Session-Bound)
        // -------------------------------------------------------------------------
        public CharacterData CreateRuntimeCharacter(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f, CharacterSubtype subtype = CharacterSubtype.Family)
        {
            var newCharacter = new CharacterData(name, hunger, thirst, sanity, health, subtype);
            sessionCharacters.Add(newCharacter);

            Debug.Log($"[CharacterCreator] Created session character: {name} ({subtype}) (Total: {sessionCharacters.Count})");
            return newCharacter;
        }

        public CharacterData CreateAndAdd(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f, CharacterSubtype subtype = CharacterSubtype.Family)
        {
            var newCharacter = CreateRuntimeCharacter(name, hunger, thirst, sanity, health, subtype);
            
            if (characterManager != null)
            {
                characterManager.AllCharacters.Add(newCharacter);
                Debug.Log($"[CharacterCreator] Added {name} to CharacterManager as {subtype}.");
            }
            else
            {
                Debug.LogError("[CharacterCreator] CharacterManager is null! Cannot add character.");
            }

            return newCharacter;
        }

        public void GenerateRandomSurvivor(System.Action<CharacterData> onComplete = null)
        {
            if (useLLM && LLMManager.Instance != null && characterPromptTemplate != null)
            {
                Debug.Log("[CharacterCreator] <color=cyan>[LLM]</color> Generating survivor via AI...");
                string userPrompt = characterPromptTemplate.BuildUserPrompt("Survivor", "desperate survivor found in the wasteland");
                
                LLMManager.Instance.QuickChat(
                    userPrompt,
                    onSuccess: (response) => {
                        if (LLMJsonParser.TryParseCharacter(response, out var data))
                        {
                            CharacterSubtype subtype = CharacterSubtype.Survivor;
                            System.Enum.TryParse(data.subtype, true, out subtype);
                            var character = CreateAndAdd(data.name, data.hunger, data.thirst, data.sanity, data.health, subtype);
                            Debug.Log($"[CharacterCreator] <color=cyan>[LLM]</color> Created: {data.name}");
                            onComplete?.Invoke(character);
                        }
                        else
                        {
                            Debug.LogWarning("[CharacterCreator] <color=yellow>[LLM]</color> Failed to parse, using fallback.");
                            var fallback = GenerateFallbackSurvivor();
                            onComplete?.Invoke(fallback);
                        }
                    },
                    onError: (err) => {
                        Debug.LogError($"[CharacterCreator] <color=red>[LLM ERROR]</color> {err}");
                        var fallback = GenerateFallbackSurvivor();
                        onComplete?.Invoke(fallback);
                    },
                    systemPrompt: characterPromptTemplate.SystemPrompt
                );
            }
            else
            {
                Debug.Log("[CharacterCreator] <color=orange>[STATIC]</color> LLM disabled, using fallback data.");
                var character = GenerateFallbackSurvivor();
                onComplete?.Invoke(character);
            }
        }

        private CharacterData GenerateFallbackSurvivor()
        {
            string[] survivors = { "Marcus|85|90|60|100", "Elena|95|85|75|90", "Old Tom|70|65|80|60" };
            var data = survivors[Random.Range(0, survivors.Length)].Split('|');
            return CreateAndAdd(data[0], float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]), float.Parse(data[4]), CharacterSubtype.Survivor);
        }

        public void GenerateRandomEnemy(System.Action<CharacterData> onComplete = null)
        {
            if (useLLM && LLMManager.Instance != null && characterPromptTemplate != null)
            {
                Debug.Log("[CharacterCreator] <color=cyan>[LLM]</color> Generating enemy via AI...");
                string userPrompt = characterPromptTemplate.BuildUserPrompt("Hostile", "dangerous enemy in the wasteland");
                
                LLMManager.Instance.QuickChat(
                    userPrompt,
                    onSuccess: (response) => {
                        if (LLMJsonParser.TryParseCharacter(response, out var data))
                        {
                            var character = CreateAndAdd(data.name, data.hunger, data.thirst, data.sanity, data.health, CharacterSubtype.Enemy);
                            Debug.Log($"[CharacterCreator] <color=cyan>[LLM]</color> Created enemy: {data.name}");
                            onComplete?.Invoke(character);
                        }
                        else
                        {
                            Debug.LogWarning("[CharacterCreator] <color=yellow>[LLM]</color> Failed to parse, using fallback.");
                            var fallback = GenerateFallbackEnemy();
                            onComplete?.Invoke(fallback);
                        }
                    },
                    onError: (err) => {
                        Debug.LogError($"[CharacterCreator] <color=red>[LLM ERROR]</color> {err}");
                        var fallback = GenerateFallbackEnemy();
                        onComplete?.Invoke(fallback);
                    },
                    systemPrompt: characterPromptTemplate.SystemPrompt
                );
            }
            else
            {
                Debug.Log("[CharacterCreator] <color=orange>[STATIC]</color> LLM disabled, using fallback data.");
                var character = GenerateFallbackEnemy();
                onComplete?.Invoke(character);
            }
        }

        private CharacterData GenerateFallbackEnemy()
        {
            string[] enemies = { "Raider Scavenger|60|50|30|100", "Feral Dog|40|40|10|50" };
            var data = enemies[Random.Range(0, enemies.Length)].Split('|');
            return CreateAndAdd(data[0], float.Parse(data[1]), float.Parse(data[2]), float.Parse(data[3]), float.Parse(data[4]), CharacterSubtype.Enemy);
        }

        public CharacterData GetSessionCharacter(string name)
        {
            return sessionCharacters.FirstOrDefault(c => c != null && c.Name == name);
        }

        public void ClearSessionCharacters()
        {
            sessionCharacters.Clear();
            Debug.Log("[CharacterCreator] Cleared all session characters.");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [ShowInInspector, ReadOnly]
        private int SessionCharacterCount => sessionCharacters?.Count ?? 0;

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Generate 1 Random Survivor")]
        private void Debug_CreateRandomSurvivor()
        {
            GenerateRandomSurvivor();
        }

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Generate 3 Random Survivors")]
        private void Debug_Generate3Survivors()
        {
            for (int i = 0; i < 3; i++)
            {
                GenerateRandomSurvivor();
            }
        }

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Generate 1 Random Enemy")]
        private void Debug_CreateRandomEnemy()
        {
            GenerateRandomEnemy();
        }

        [SerializeField] private string customName = "New Entity";
        [SerializeField] private CharacterSubtype customSubtype = CharacterSubtype.Family;

        [Range(0, 100)]
        [SerializeField] private float customHealth = 100f;

        [Range(0, 100)]
        [SerializeField] private float customHunger = 100f;
        
        [Range(0, 100)]
        [SerializeField] private float customThirst = 100f;
        
        [Range(0, 100)]
        [SerializeField] private float customSanity = 100f;

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Create Custom Entity")]
        private void Debug_CreateCustomEntity()
        {
            CreateAndAdd(customName, customHunger, customThirst, customSanity, customHealth, customSubtype);
        }

        [Button("Clear All Session Characters")]
        private void Debug_ClearSession()
        {
            ClearSessionCharacters();
        }

        [Button("Log Session Characters")]
        private void Debug_LogSessionCharacters()
        {
            Debug.Log($"[CharacterCreator] Session characters ({sessionCharacters.Count}):");
            foreach (var c in sessionCharacters)
            {
                if (c != null)
                {
                    Debug.Log($"  - {c.Name}: H:{c.Hunger:F0} T:{c.Thirst:F0} S:{c.Sanity:F0} HP:{c.Health:F0}");
                }
            }
        }
        #endif
    }
}
