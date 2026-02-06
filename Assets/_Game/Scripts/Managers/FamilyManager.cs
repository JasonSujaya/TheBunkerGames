using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Manages the family members in the bunker.
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
        [SerializeField] private List<Character> familyMembers = new List<Character>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<Character> FamilyMembers => familyMembers;
        public int AliveCount => familyMembers.FindAll(c => c.IsAlive).Count;

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

        private void OnEnable()
        {
            GameManager.OnNightStart += ApplyDailyDecay;
        }

        private void OnDisable()
        {
            GameManager.OnNightStart -= ApplyDailyDecay;
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void AddCharacter(string name, float hunger = 100f, float sanity = 100f)
        {
            var character = new Character(name, hunger, sanity);
            familyMembers.Add(character);
            Debug.Log($"[FamilyManager] Added character: {name}");
        }

        public void AddCharacter(CharacterData data)
        {
            if (data == null) return;
            var character = data.CreateCharacter();
            familyMembers.Add(character);
            Debug.Log($"[FamilyManager] Added character from data: {data.CharacterName}");
        }

        public Character GetCharacter(string name)
        {
            return familyMembers.Find(c => c.Name == name);
        }

        public void ApplyDailyDecay()
        {
            var config = GameConfig.Instance;
            if (config == null) return;

            foreach (var character in familyMembers)
            {
                if (!character.IsAlive) continue;
                
                character.ModifyHunger(-config.HungerDecayPerDay);
                character.ModifySanity(-config.SanityDecayPerDay);
                Debug.Log($"[FamilyManager] {character.Name} - Hunger: {character.Hunger:F0}, Sanity: {character.Sanity:F0}");
            }
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Add Test Character", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_AddTestCharacter()
        {
            if (Application.isPlaying)
            {
                AddCharacter($"Survivor_{familyMembers.Count + 1}");
            }
        }

        [Button("Apply Daily Decay", ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.5f)]
        private void Debug_ApplyDecay()
        {
            if (Application.isPlaying)
            {
                ApplyDailyDecay();
            }
        }

        [Button("Feed First Character (+20 Hunger)", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_FeedFirst()
        {
            if (Application.isPlaying && familyMembers.Count > 0)
            {
                familyMembers[0].ModifyHunger(20f);
                Debug.Log($"[FamilyManager] Fed {familyMembers[0].Name}, Hunger: {familyMembers[0].Hunger:F0}");
            }
        }

        [Button("Scare First Character (-20 Sanity)", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.5f, 1f)]
        private void Debug_ScareFirst()
        {
            if (Application.isPlaying && familyMembers.Count > 0)
            {
                familyMembers[0].ModifySanity(-20f);
                Debug.Log($"[FamilyManager] Scared {familyMembers[0].Name}, Sanity: {familyMembers[0].Sanity:F0}");
            }
        }
        #endif
    }
}
