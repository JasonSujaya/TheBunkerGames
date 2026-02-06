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
        [SerializeField] private FamilyManager familyManager;

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

            if (familyManager == null)
            {
                familyManager = FamilyManager.Instance;
            }
        }

        private void OnDestroy()
        {
            ClearSessionCharacters();
        }

        // -------------------------------------------------------------------------
        // Public Methods - Character Creation (Session-Bound)
        // -------------------------------------------------------------------------
        public CharacterData CreateRuntimeCharacter(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f)
        {
            var newCharacter = new CharacterData(name, hunger, thirst, sanity, health);
            sessionCharacters.Add(newCharacter);

            Debug.Log($"[CharacterCreator] Created session character: {name} (Total: {sessionCharacters.Count})");
            return newCharacter;
        }

        public CharacterData CreateAndAddToFamily(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f)
        {
            var newCharacter = CreateRuntimeCharacter(name, hunger, thirst, sanity, health);
            
            if (familyManager != null)
            {
                familyManager.FamilyMembers.Add(newCharacter);
                Debug.Log($"[CharacterCreator] Added {name} to family.");
            }
            else
            {
                Debug.LogError("[CharacterCreator] FamilyManager is null! Cannot add character to family.");
            }

            return newCharacter;
        }

        public CharacterData GenerateRandomSurvivor()
        {
            string[] survivors = new[] {
                "Marcus|85|90|60|100",
                "Elena|95|85|75|90",
                "Old Tom|70|65|80|60",
                "Little Lily|90|95|85|100",
                "Doc Rivera|80|80|90|85",
                "Silent Sam|75|70|40|95",
                "Hopeful Hannah|85|90|95|80",
                "Wounded Walker|50|60|50|45"
            };
            
            var data = survivors[Random.Range(0, survivors.Length)].Split('|');
            return CreateAndAddToFamily(
                data[0], 
                float.Parse(data[1]), 
                float.Parse(data[2]), 
                float.Parse(data[3]), 
                float.Parse(data[4])
            );
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
        [TitleGroup("Session Characters", "Characters created this session (not persisted)")]
        [ShowInInspector, ReadOnly]
        private int SessionCharacterCount => sessionCharacters?.Count ?? 0;

        [TitleGroup("AI Generation", "A.N.G.E.L. procedurally generates survivors")]
        [HorizontalGroup("AI Generation/Buttons")]
        [Button("Generate 1 Random Survivor")]
        private void Debug_CreateRandomSurvivor()
        {
            GenerateRandomSurvivor();
        }

        [HorizontalGroup("AI Generation/Buttons")]
        [Button("Generate 3 Random Survivors")]
        private void Debug_Generate3Survivors()
        {
            for (int i = 0; i < 3; i++)
            {
                GenerateRandomSurvivor();
            }
        }

        [TitleGroup("Custom Factory", "Manually define and create new characters")]
        [HorizontalGroup("Custom Factory/Row1")]
        [SerializeField] private string customName = "New Survivor";
        
        [HorizontalGroup("Custom Factory/Row1")]
        [Range(0, 100)]
        [SerializeField] private float customHealth = 100f;

        [HorizontalGroup("Custom Factory/Row2")]
        [Range(0, 100)]
        [SerializeField] private float customHunger = 100f;
        
        [HorizontalGroup("Custom Factory/Row2")]
        [Range(0, 100)]
        [SerializeField] private float customThirst = 100f;
        
        [HorizontalGroup("Custom Factory/Row2")]
        [Range(0, 100)]
        [SerializeField] private float customSanity = 100f;

        [Button("Create Custom Character", ButtonSizes.Medium)]
        private void Debug_CreateCustomCharacter()
        {
            CreateAndAddToFamily(customName, customHunger, customThirst, customSanity, customHealth);
        }

        [TitleGroup("Session Management")]
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
