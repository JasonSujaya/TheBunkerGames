using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Specialized manager for the family members in the bunker.
    /// Exposes high-level family logic and queries CharacterManager for data.
    /// </summary>
    public class FamilyManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static FamilyManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        [SerializeField] private FamilyListSO defaultFamilyProfile;

        // -------------------------------------------------------------------------
        // Public Properties (Filtering CharacterManager)
        // -------------------------------------------------------------------------
        public List<CharacterData> FamilyMembers => CharacterManager.Instance != null 
            ? CharacterManager.Instance.GetCharactersBySubtype(CharacterSubtype.Family) 
            : new List<CharacterData>();

        public int AliveCount => FamilyMembers.FindAll(c => c.IsAlive).Count;
        public List<CharacterData> AvailableExplorers => FamilyMembers.FindAll(c => c.IsAvailableForExploration);

        public void SpawnDefaultFamily()
        {
            if (defaultFamilyProfile == null)
            {
                Debug.LogWarning("[FamilyManager] No Default Family Profile assigned! Cannot spawn default family.");
                return;
            }

            Debug.Log("[FamilyManager] Spawning Default Family...");
            ClearFamily();

            foreach (var memberDef in defaultFamilyProfile.DefaultFamilyMembers)
            {
                if (memberDef != null)
                {
                    AddCharacter(memberDef);
                }
            }
            
            Debug.Log($"[FamilyManager] Spawned {AliveCount} family members.");
        }

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
        // Public Methods (Wrappers for CharacterManager)
        // -------------------------------------------------------------------------
        public void AddCharacter(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f)
        {
            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.AddCharacter(name, hunger, thirst, sanity, health, CharacterSubtype.Family);
            }
        }

        public void AddCharacter(CharacterDefinitionSO data)
        {
            if (CharacterManager.Instance != null && data != null)
            {
                CharacterManager.Instance.AddCharacter(data);
            }
        }

        public CharacterData GetCharacter(string name)
        {
            return FamilyMembers.Find(c => c.Name == name);
        }

        public void ClearFamily()
        {
            if (CharacterManager.Instance != null)
            {
                var family = FamilyMembers;
                foreach (var member in family)
                {
                    CharacterManager.Instance.AllCharacters.Remove(member);
                }
                Debug.Log("[FamilyManager] Family members removed from CharacterManager.");
            }
        }

        public void LoadCharacters(List<CharacterData> characters)
        {
            if (CharacterManager.Instance == null) return;

            // Clear existing family first
            ClearFamily();

            if (characters != null)
            {
                foreach (var c in characters)
                {
                    c.Subtype = CharacterSubtype.Family; // Ensure they are marked as family
                    CharacterManager.Instance.AllCharacters.Add(c);
                }
            }
            Debug.Log($"[FamilyManager] Loaded {characters?.Count ?? 0} family member(s).");
        }

        public bool IsFamilyDead()
        {
            return AliveCount == 0;
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [Button("Add Default Family", ButtonSizes.Medium)]
        private void Debug_AddFamily()
        {
            if (CharacterManager.Instance != null)
            {
                CharacterManager.Instance.AddCharacter("Father", 90f, 85f, 70f, 100f, CharacterSubtype.Family);
                CharacterManager.Instance.AddCharacter("Mother", 95f, 90f, 80f, 100f, CharacterSubtype.Family);
                CharacterManager.Instance.AddCharacter("Child", 80f, 80f, 90f, 100f, CharacterSubtype.Family);
            }
            else
            {
                Debug.LogWarning("[FamilyManager] CharacterManager.Instance is null!");
            }
        }

        [TitleGroup("Debug Controls")]
        [Button("Log Family Stats", ButtonSizes.Medium)]
        private void Debug_LogFamilyStats()
        {
            var family = FamilyMembers;
            Debug.Log($"[FamilyManager] Total Family: {family.Count} | Alive: {AliveCount}");
            foreach (var c in family)
            {
                Debug.Log($"  - {c.Name}: HP:{c.Health:F0} H:{c.Hunger:F0} T:{c.Thirst:F0} S:{c.Sanity:F0}");
            }
        }
        #endif
    }
}
