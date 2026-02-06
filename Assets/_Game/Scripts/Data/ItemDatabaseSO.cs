using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Singleton ScriptableObject holding all items in the game.
    /// The AI calls GetItem(id) to look up item data.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabaseSO", menuName = "TheBunkerGames/Item Database")]
    public class ItemDatabaseSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static ItemDatabaseSO instance;
        public static ItemDatabaseSO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<ItemDatabaseSO>("ItemDatabaseSO");
                    if (instance == null)
                    {
                        Debug.LogError("[ItemDatabaseSO] No ItemDatabaseSO found in Resources folder!");
                    }
                }
                return instance;
            }
        }

        // -------------------------------------------------------------------------
        // Item List
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("All Items")]
        [Searchable]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<ItemDataSO> allItems = new List<ItemDataSO>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<ItemDataSO> AllItems => allItems;

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        /// <summary>
        /// Look up an item by its ID.
        /// </summary>
        public ItemDataSO GetItem(string id)
        {
            for (int i = 0; i < allItems.Count; i++)
            {
                if (allItems[i] != null && allItems[i].Id == id)
                {
                    return allItems[i];
                }
            }
            Debug.LogWarning($"[ItemDatabaseSO] Item not found: {id}");
            return null;
        }

        /// <summary>
        /// Get all items of a specific type.
        /// </summary>
        public List<ItemDataSO> GetItemsByType(ItemType type)
        {
            List<ItemDataSO> result = new List<ItemDataSO>();
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
        public void AddItem(ItemDataSO item)
        {
            if (item != null && !allItems.Contains(item))
            {
                allItems.Add(item);
            }
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Log All Items", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_LogAllItems()
        {
            Debug.Log($"[ItemDatabaseSO] Total items: {allItems.Count}");
            foreach (var item in allItems)
            {
                if (item != null)
                {
                    Debug.Log($"  - {item.Id}: {item.DisplayName} ({item.Type})");
                }
            }
        }
        #endif
    }
}
