using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Manages the family members in the bunker.
    /// Stat decay is now handled by NightCycleManager.
    /// </summary>
    public class FamilyManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static FamilyManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Database Reference
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Database")]
        [Required("Character Database is required")]
        #endif
        [SerializeField] private CharacterDatabaseDataSO characterDatabase;

        // -------------------------------------------------------------------------
        // Family Data
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Family Members")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<CharacterData> familyMembers = new List<CharacterData>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<CharacterData> FamilyMembers => familyMembers;
        public int AliveCount => familyMembers.FindAll(c => c.IsAlive).Count;
        public List<CharacterData> AvailableExplorers => familyMembers.FindAll(c => c.IsAvailableForExploration);
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
                    Debug.LogWarning("[FamilyManager] CharacterDatabaseDataSO not assigned and not found in Resources!");
                }
            }
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void AddCharacter(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f)
        {
            var character = new CharacterData(name, hunger, thirst, sanity, health);
            familyMembers.Add(character);
            Debug.Log($"[FamilyManager] Added character: {name}");
        }

        public void AddCharacter(CharacterDefinitionSO data)
        {
            if (data == null) return;
            var character = data.CreateCharacter();
            familyMembers.Add(character);
            Debug.Log($"[FamilyManager] Added character from data: {data.CharacterName}");
        }

        public CharacterData GetCharacter(string name)
        {
            return familyMembers.Find(c => c.Name == name);
        }

        public void ClearFamily()
        {
            familyMembers.Clear();
            Debug.Log("[FamilyManager] Family cleared.");
        }

        public void LoadCharacters(List<CharacterData> characters)
        {
            familyMembers.Clear();
            if (characters != null)
            {
                familyMembers.AddRange(characters);
            }
            Debug.Log($"[FamilyManager] Loaded {familyMembers.Count} character(s).");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        
        [HorizontalGroup("AddSO")]
        [ValueDropdown("GetAllCharacterProfileList")]
        [SerializeField] private CharacterDefinitionSO debugCharacterProfile;

        [HorizontalGroup("AddSO")]
        [Button("Add Character From SO", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_AddCharacterFromSO()
        {
            if (debugCharacterProfile != null)
            {
                AddCharacter(debugCharacterProfile);
            }
            else
            {
                Debug.LogWarning("[FamilyManager] No Character Data SO selected.");
            }
        }

        private IEnumerable<ValueDropdownItem<CharacterDefinitionSO>> GetAllCharacterProfileList()
        {
            var list = new ValueDropdownList<CharacterDefinitionSO>();

            // 1. Persistent from Database
            if (characterDatabase != null && characterDatabase.AllCharacters != null)
            {
                foreach (var charDef in characterDatabase.AllCharacters)
                {
                    if (charDef != null)
                        list.Add($"[P] {charDef.CharacterName}", charDef);
                }
            }

            // 2. Session-Bound from Creator
            if (CharacterCreator.Instance != null && CharacterCreator.Instance.SessionCharacters != null)
            {
                foreach (var charDef in CharacterCreator.Instance.SessionCharacters)
                {
                    if (charDef != null)
                        list.Add($"[S] {charDef.CharacterName}", charDef);
                }
            }

            return list;
        }

        [Button("Add Test Character (Manual)", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.8f)]
        private void Debug_AddTestCharacter(string name = "Survivor", float hunger = 100, float thirst = 100, float sanity = 100, float health = 100)
        {
            AddCharacter(name, hunger, thirst, sanity, health);
        }

        [Button("Add Default Family", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_AddFamily()
        {
            AddCharacter("Father", 90f, 85f, 70f, 100f);
            AddCharacter("Mother", 95f, 90f, 80f, 100f);
            AddCharacter("Child", 80f, 80f, 90f, 100f);
        }

        [Button("Clear Family", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void Debug_ClearFamily()
        {
            ClearFamily();
        }

        [Button("Log All Stats", ButtonSizes.Medium)]
        private void Debug_LogAllStats()
        {
            foreach (var c in familyMembers)
            {
                Debug.Log($"[FamilyManager] {c.Name} - H:{c.Hunger:F0} T:{c.Thirst:F0} S:{c.Sanity:F0} HP:{c.Health:F0} | Alive:{c.IsAlive} Injured:{c.IsInjured}");
            }
        }

        [Title("Auto Setup")]
        [Button("Auto Setup Dependencies", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void AutoSetupDependencies()
        {
            #if UNITY_EDITOR
            // Ensure Tester exists
            var testerType = System.Type.GetType("TheBunkerGames.Tests.FamilyManagerTester");
            if (testerType != null && GetComponent(testerType) == null)
            {
                gameObject.AddComponent(testerType);
                Debug.Log("[FamilyManager] Added FamilyTester.");
            }
            else if (testerType == null)
            {
                Debug.LogWarning("[FamilyManager] Could not find FamilyTester type. Ensure it is in TheBunkerGames.Tests namespace.");
            }
            #endif
        }
        #endif
    }
}
