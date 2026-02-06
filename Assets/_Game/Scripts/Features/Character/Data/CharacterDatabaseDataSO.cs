using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject holding all character definitions in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterDatabaseDataSO", menuName = "TheBunkerGames/Character Database Data")]
    public class CharacterDatabaseDataSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static CharacterDatabaseDataSO instance;
        public static CharacterDatabaseDataSO Instance => instance;

        public static void SetInstance(CharacterDatabaseDataSO database)
        {
            if (database == null)
            {
                Debug.LogError("[CharacterDatabaseDataSO] Attempted to set null instance!");
                return;
            }
            instance = database;
        }

        // -------------------------------------------------------------------------
        // Character List
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("All Characters")]
        [Searchable]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<CharacterDefinitionSO> allCharacters = new List<CharacterDefinitionSO>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<CharacterDefinitionSO> AllCharacters => allCharacters;

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public CharacterDefinitionSO GetCharacter(string name)
        {
            for (int i = 0; i < allCharacters.Count; i++)
            {
                if (allCharacters[i] != null && allCharacters[i].CharacterName == name)
                {
                    return allCharacters[i];
                }
            }
            Debug.LogWarning($"[CharacterDatabaseDataSO] Character not found: {name}");
            return null;
        }

        public List<CharacterDefinitionSO> GetCharactersByRole(CharacterRole role)
        {
            List<CharacterDefinitionSO> result = new List<CharacterDefinitionSO>();
            for (int i = 0; i < allCharacters.Count; i++)
            {
                if (allCharacters[i] != null && allCharacters[i].Role == role)
                {
                    result.Add(allCharacters[i]);
                }
            }
            return result;
        }

        public void AddCharacter(CharacterDefinitionSO character)
        {
            if (character != null && !allCharacters.Contains(character))
            {
                allCharacters.Add(character);
            }
        }

        public int RemoveNullEntries()
        {
            int removed = allCharacters.RemoveAll(c => c == null);
            if (removed > 0)
            {
                Debug.Log($"[CharacterDatabaseDataSO] Removed {removed} null/missing entries.");
            }
            return removed;
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Log All Characters", ButtonSizes.Medium)]
        private void Debug_LogAllCharacters()
        {
            Debug.Log($"[CharacterDatabaseDataSO] Total characters: {allCharacters.Count}");
            foreach (var character in allCharacters)
            {
                if (character != null)
                {
                    Debug.Log($"  - {character.CharacterName}: {character.Role} - {character.Description}");
                }
            }
        }

        [Button("Find and Add All Character Assets", ButtonSizes.Large)]
        private void Debug_FindAndAddAll()
        {
#if UNITY_EDITOR
            RemoveNullEntries();
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CharacterDefinitionSO");
            int count = 0;
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                CharacterDefinitionSO character = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDefinitionSO>(path);
                if (character != null && !allCharacters.Contains(character))
                {
                    allCharacters.Add(character);
                    count++;
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[CharacterDatabaseDataSO] Added {count} new characters to the database.");
#endif
        }

        [Button("Clean Up Missing Characters", ButtonSizes.Medium)]
        private void Debug_CleanUpMissing()
        {
#if UNITY_EDITOR
            int removed = RemoveNullEntries();
            UnityEditor.EditorUtility.SetDirty(this);
            if (removed == 0)
            {
                Debug.Log("[CharacterDatabaseDataSO] No missing characters to clean up.");
            }
#endif
        }
        #endif
    }
}
