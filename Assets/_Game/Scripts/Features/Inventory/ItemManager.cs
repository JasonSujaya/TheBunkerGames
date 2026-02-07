using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Global manager for all item definitions in the game.
    /// Acts as the central library/database for looking up "What is this item?".
    /// </summary>
    public class ItemManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static ItemManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Database")]
        [Required("Item Database is required for creating/finding items")]
        #endif
        [SerializeField] private ItemDatabaseDataSO itemDatabase;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public ItemDatabaseDataSO Database => itemDatabase;

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

            // Initialize the ItemDatabase singleton if needed
            if (itemDatabase != null)
            {
                ItemDatabaseDataSO.SetInstance(itemDatabase);
            }
            else
            {
                Debug.LogError("[ItemManager] ItemDatabase reference is not assigned in Inspector!");
            }
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public ItemData GetItem(string itemId)
        {
            if (itemDatabase == null) return null;
            return itemDatabase.GetItem(itemId);
        }

        public bool TryGetItem(string itemId, out ItemData itemData)
        {
            itemData = GetItem(itemId);
            return itemData != null;
        }

        /// <summary>
        /// Registers a new item into the database if it doesn't already exist.
        /// Useful for runtime-generated items.
        /// </summary>
        public void RegisterItem(ItemData itemData)
        {
            if (itemDatabase == null || itemData == null) return;

            if (!itemDatabase.AllItems.Contains(itemData))
            {
                Debug.Log($"[ItemManager] Registering new item '{itemData.ItemName}' to database.");
                itemDatabase.AddItem(itemData);
            }
        }

        public List<ItemData> GetAllItems()
        {
             return itemDatabase != null ? itemDatabase.AllItems : new List<ItemData>();
        }
    }
}
