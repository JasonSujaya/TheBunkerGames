using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject holding all items in the game.
    /// Initialize singleton via SetInstance() with a direct SerializeField reference.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabaseDataSO", menuName = "TheBunkerGames/Item Database Data")]
    public class ItemDatabaseDataSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static ItemDatabaseDataSO instance;
        public static ItemDatabaseDataSO Instance => instance;

        /// <summary>
        /// Initialize the singleton with a direct reference from a manager.
        /// </summary>
        public static void SetInstance(ItemDatabaseDataSO database)
        {
            if (database == null)
            {
                Debug.LogError("[ItemDatabaseDataSO] Attempted to set null instance!");
                return;
            }
            instance = database;
        }

        // -------------------------------------------------------------------------
        // Item List
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("All Items")]
        [Searchable]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<ItemData> allItems = new List<ItemData>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<ItemData> AllItems => allItems;

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        /// <summary>
        /// Look up an item by its ID.
        /// </summary>
        public ItemData GetItem(string name)
        {
            for (int i = 0; i < allItems.Count; i++)
            {
                // Assuming ItemName acts as the id
                if (allItems[i] != null && allItems[i].ItemName == name)
                {
                    return allItems[i];
                }
            }
            Debug.LogWarning($"[ItemDatabaseDataSO] Item not found: {name}");
            return null;
        }

        /// <summary>
        /// Get all items of a specific type.
        /// </summary>
        public List<ItemData> GetItemsByType(ItemType type)
        {
            List<ItemData> result = new List<ItemData>();
            for (int i = 0; i < allItems.Count; i++)
            {
                if (allItems[i] != null && allItems[i].Type == type)
                {
                    result.Add(allItems[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Add an item to the database (used by editor tools).
        /// </summary>
        public void AddItem(ItemData item)
        {
            if (item != null && !allItems.Contains(item))
            {
                allItems.Add(item);
            }
        }

        /// <summary>
        /// Remove all null/missing entries from the database.
        /// </summary>
        public int RemoveNullEntries()
        {
            int removed = allItems.RemoveAll(item => item == null);
            if (removed > 0)
            {
                Debug.Log($"[ItemDatabaseDataSO] Removed {removed} null/missing entries.");
            }
            return removed;
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Log All Items", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_LogAllItems()
        {
            Debug.Log($"[ItemDatabaseDataSO] Total items: {allItems.Count}");
            foreach (var item in allItems)
            {
                if (item != null)
                {
                    Debug.Log($"  - {item.ItemName}: {item.Description} ({item.Type})");
                }
            }
        }

        [Button("Find and Add All ItemData Assets", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void Debug_FindAndAddAll()
        {
#if UNITY_EDITOR
            // First clean up null entries
            RemoveNullEntries();
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
            int count = 0;
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                ItemData item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item != null && !allItems.Contains(item))
                {
                    allItems.Add(item);
                    count++;
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[ItemDatabaseDataSO] Added {count} new items to the database.");
#endif
        }

        [Button("Clean Up Missing Items", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void Debug_CleanUpMissingItems()
        {
#if UNITY_EDITOR
            int removed = RemoveNullEntries();
            UnityEditor.EditorUtility.SetDirty(this);
            if (removed == 0)
            {
                Debug.Log("[ItemDatabaseDataSO] No missing items to clean up.");
            }
#endif
        }
        #endif
    }
}

#if UNITY_EDITOR
namespace TheBunkerGames
{
    /// <summary>
    /// Editor utility to auto-clean all database SOs when exiting play mode.
    /// Handles ItemDatabaseDataSO, CharacterDatabaseDataSO, and QuestDatabaseDataSO.
    /// </summary>
    [UnityEditor.InitializeOnLoad]
    public static class DatabaseCleanupUtility
    {
        static DatabaseCleanupUtility()
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                CleanupAllDatabases();
            }
        }

        private static void CleanupAllDatabases()
        {
            bool anyChanges = false;

            // Clean ItemDatabaseDataSO
            string[] itemGuids = UnityEditor.AssetDatabase.FindAssets("t:ItemDatabaseDataSO");
            foreach (string guid in itemGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var db = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDatabaseDataSO>(path);
                if (db != null && db.RemoveNullEntries() > 0)
                {
                    UnityEditor.EditorUtility.SetDirty(db);
                    anyChanges = true;
                }
            }

            // Clean CharacterDatabaseDataSO
            string[] charGuids = UnityEditor.AssetDatabase.FindAssets("t:CharacterDatabaseDataSO");
            foreach (string guid in charGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var db = UnityEditor.AssetDatabase.LoadAssetAtPath<CharacterDatabaseDataSO>(path);
                if (db != null && db.RemoveNullEntries() > 0)
                {
                    UnityEditor.EditorUtility.SetDirty(db);
                    anyChanges = true;
                }
            }

            // Clean QuestDatabaseDataSO
            string[] questGuids = UnityEditor.AssetDatabase.FindAssets("t:QuestDatabaseDataSO");
            foreach (string guid in questGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var db = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestDatabaseDataSO>(path);
                if (db != null && db.RemoveNullEntries() > 0)
                {
                    UnityEditor.EditorUtility.SetDirty(db);
                    anyChanges = true;
                }
            }

            // Clean PlaceDatabaseDataSO
            string[] placeGuids = UnityEditor.AssetDatabase.FindAssets("t:PlaceDatabaseDataSO");
            foreach (string guid in placeGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var db = UnityEditor.AssetDatabase.LoadAssetAtPath<PlaceDatabaseDataSO>(path);
                if (db != null && db.RemoveNullEntries() > 0)
                {
                    UnityEditor.EditorUtility.SetDirty(db);
                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log("[DatabaseCleanupUtility] Cleaned up null entries from all databases.");
            }
        }
    }
}
#endif
