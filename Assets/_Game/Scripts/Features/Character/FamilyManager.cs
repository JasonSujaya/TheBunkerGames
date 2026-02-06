using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Manages the family members in the bunker.
    /// Stat decay is now handled by NightCycleController.
    /// </summary>
    public class FamilyManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static FamilyManager Instance { get; private set; }

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
        public void AddCharacter(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f)
        {
            var character = new CharacterData(name, hunger, thirst, sanity, health);
            familyMembers.Add(character);
            Debug.Log($"[FamilyManager] Added character: {name}");
        }

        public void AddCharacter(CharacterDataSO data)
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
        [Button("Add Test CharacterData", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_AddTestCharacter()
        {
            if (Application.isPlaying)
            {
                AddCharacter($"Survivor_{familyMembers.Count + 1}");
            }
        }

        [Button("Add Family (Father, Mother, Child)", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_AddFamily()
        {
            if (Application.isPlaying)
            {
                AddCharacter("Father", 90f, 85f, 70f, 100f);
                AddCharacter("Mother", 95f, 90f, 80f, 100f);
                AddCharacter("Child", 80f, 80f, 90f, 100f);
            }
        }

        [Button("Feed First CharacterData (+20 Hunger)", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_FeedFirst()
        {
            if (Application.isPlaying && familyMembers.Count > 0)
            {
                familyMembers[0].ModifyHunger(20f);
                Debug.Log($"[FamilyManager] Fed {familyMembers[0].Name}, Hunger: {familyMembers[0].Hunger:F0}");
            }
        }

        [Button("Scare First CharacterData (-20 Sanity)", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.5f, 1f)]
        private void Debug_ScareFirst()
        {
            if (Application.isPlaying && familyMembers.Count > 0)
            {
                familyMembers[0].ModifySanity(-20f);
                Debug.Log($"[FamilyManager] Scared {familyMembers[0].Name}, Sanity: {familyMembers[0].Sanity:F0}");
            }
        }

        [Button("Log All Stats", ButtonSizes.Medium)]
        private void Debug_LogAllStats()
        {
            foreach (var c in familyMembers)
            {
                Debug.Log($"[FamilyManager] {c.Name} - H:{c.Hunger:F0} T:{c.Thirst:F0} S:{c.Sanity:F0} HP:{c.Health:F0} | Alive:{c.IsAlive} Injured:{c.IsInjured}");
            }
        }
        #endif
    }
}
