using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Manages all characters in the game (Family, Enemies, Survivors, Neutral, etc.).
    /// This is the global entity database for the session.
    /// </summary>
    public class CharacterManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static CharacterManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Database Reference
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        [Required("Character Database is required")]
        #endif
        [SerializeField] private CharacterDatabaseDataSO characterDatabase;

        // -------------------------------------------------------------------------
        // Character Data
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("All Characters (Mobs, Family, etc.)")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<CharacterData> allCharacters = new List<CharacterData>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<CharacterData> AllCharacters => allCharacters;
        public CharacterDatabaseDataSO Database => characterDatabase;

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

            // Initialize database singleton
            if (characterDatabase != null)
            {
                CharacterDatabaseDataSO.SetInstance(characterDatabase);
            }
            else
            {
                // Try to load from Resources
                characterDatabase = Resources.Load<CharacterDatabaseDataSO>("CharacterDatabaseDataSO");
                if (characterDatabase != null)
                {
                    CharacterDatabaseDataSO.SetInstance(characterDatabase);
                }
                else
                {
                    Debug.LogWarning("[CharacterManager] CharacterDatabaseDataSO not assigned and not found in Resources!");
                }
            }
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void AddCharacter(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f, CharacterSubtype subtype = CharacterSubtype.Family)
        {
            var character = new CharacterData(name, hunger, thirst, sanity, health, subtype);
            allCharacters.Add(character);
            Debug.Log($"[CharacterManager] Added {subtype}: {name}");
        }

        public void AddCharacter(CharacterDefinitionSO data)
        {
            if (data == null) return;
            var character = data.CreateCharacter();
            allCharacters.Add(character);
            Debug.Log($"[CharacterManager] Added character from data: {data.CharacterName} ({data.Subtype})");
        }

        public CharacterData GetCharacter(string name)
        {
            return allCharacters.Find(c => c.Name == name);
        }

        public List<CharacterData> GetCharactersBySubtype(CharacterSubtype subtype)
        {
            return allCharacters.FindAll(c => c.Subtype == subtype);
        }

        public void ClearAllCharacters()
        {
            allCharacters.Clear();
            Debug.Log("[CharacterManager] All characters cleared.");
        }

        public void LoadCharacters(List<CharacterData> characters)
        {
            allCharacters.Clear();
            if (characters != null)
            {
                allCharacters.AddRange(characters);
            }
            Debug.Log($"[CharacterManager] Loaded {allCharacters.Count} character(s).");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [HorizontalGroup("Debug Controls/AddSO")]
        [ValueDropdown("GetAllCharacterProfileList")]
        [SerializeField] private CharacterDefinitionSO debugCharacterProfile;

        [TitleGroup("Debug Controls")]
        [HorizontalGroup("Debug Controls/AddSO")]
        [Button("Add Character From SO", ButtonSizes.Medium)]
        private void Debug_AddCharacterFromSO()
        {
            if (debugCharacterProfile != null)
            {
                AddCharacter(debugCharacterProfile);
            }
            else
            {
                Debug.LogWarning("[CharacterManager] No Character Data SO selected.");
            }
        }

        private IEnumerable<ValueDropdownItem<CharacterDefinitionSO>> GetAllCharacterProfileList()
        {
            var list = new ValueDropdownList<CharacterDefinitionSO>();

            if (characterDatabase != null && characterDatabase.AllCharacters != null)
            {
                foreach (var charDef in characterDatabase.AllCharacters)
                {
                    if (charDef != null)
                        list.Add($"[P] {charDef.CharacterName} ({charDef.Subtype})", charDef);
                }
            }

            if (CharacterCreator.Instance != null && CharacterCreator.Instance.SessionCharacters != null)
            {
                foreach (var charData in CharacterCreator.Instance.SessionCharacters)
                {
                    if (charData != null)
                        list.Add($"[S] {charData.Name} ({charData.Subtype})", null);
                }
            }

            return list;
        }

        [TitleGroup("Debug Controls")]
        [HorizontalGroup("Debug Controls/Manual")]
        [SerializeField] private CharacterSubtype debugSubtype = CharacterSubtype.Family;

        [TitleGroup("Debug Controls")]
        [HorizontalGroup("Debug Controls/Manual")]
        [Button("Add Test Character", ButtonSizes.Medium)]
        private void Debug_AddTestCharacter(string name = "Survivor", float hunger = 100, float thirst = 100, float sanity = 100, float health = 100)
        {
            AddCharacter(name, hunger, thirst, sanity, health, debugSubtype);
        }

        [TitleGroup("Debug Controls")]
        [Button("Clear All Characters", ButtonSizes.Medium)]
        private void Debug_ClearAll()
        {
            ClearAllCharacters();
        }

        [TitleGroup("Debug Controls")]
        [Button("Log All Characters", ButtonSizes.Medium)]
        private void Debug_LogAllStats()
        {
            foreach (var c in allCharacters)
            {
                Debug.Log($"[CharacterManager] {c.Name} ({c.Subtype}) - H:{c.Hunger:F0} T:{c.Thirst:F0} S:{c.Sanity:F0} HP:{c.Health:F0}");
            }
        }
        #endif
    }
}
