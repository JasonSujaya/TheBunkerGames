using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Manages the player's inventory.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static InventoryManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        // Removed direct ItemDatabase reference. Uses ItemManager.Instance now.

        // -------------------------------------------------------------------------
        // Inventory Data
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Inventory")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<InventorySlotData> items = new List<InventorySlotData>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<InventorySlotData> Items => items;
        public int TotalItemCount
        {
            get
            {
                int count = 0;
                foreach (var slot in items) count += slot.Quantity;
                return count;
            }
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
        // Public Methods
        // -------------------------------------------------------------------------
        /// <summary>
        /// Add an item to the inventory using a ScriptableObject reference.
        /// Preferred for AI-native games where items are created at runtime.
        /// </summary>
        /// <param name="itemData">The ItemData SO to add</param>
        /// <param name="quantity">Amount to add</param>
        /// <returns>True if item was added successfully</returns>
        public bool AddItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null || quantity <= 0)
            {
                Debug.LogWarning($"[InventoryManager] Invalid item or quantity");
                return false;
            }

            // Ensure the item exists in the global database via ItemManager
            if (ItemManager.Instance != null)
            {
                ItemManager.Instance.RegisterItem(itemData);
            }
            else
            {
                 Debug.LogWarning("[InventoryManager] ItemManager instance not found! Item might not be registered.");
            }

            // Add to existing slot or create new slot
            var existingSlot = items.Find(s => s.ItemId == itemData.ItemName);
            if (existingSlot != null)
            {
                existingSlot.Quantity += quantity;
            }
            else
            {
                items.Add(new InventorySlotData(itemData.ItemName, quantity));
            }

            Debug.Log($"[InventoryManager] Added {quantity}x {itemData.ItemName} ({itemData.Type})");
            return true;
        }

        /// <summary>
        /// Add an item to the inventory by ID (legacy support).
        /// </summary>
        public bool AddItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            
            // Use ItemManager to find the item definition
            if (ItemManager.Instance == null)
            {
                 Debug.LogError("[InventoryManager] Cannot add item by ID - ItemManager is missing!");
                 return false;
            }

            var itemData = ItemManager.Instance.GetItem(itemId);
            if (itemData == null)
            {
                Debug.LogError($"[InventoryManager] Cannot add item '{itemId}' - not found in ItemManager database!");
                return false;
            }

            return AddItem(itemData, quantity);
        }

        /// <summary>
        /// Remove an item using a ScriptableObject reference.
        /// </summary>
        public bool RemoveItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null || quantity <= 0) return false;
            return RemoveItem(itemData.ItemName, quantity);
        }

        /// <summary>
        /// Remove an item by ID.
        /// </summary>
        public bool RemoveItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;

            var slot = items.Find(s => s.ItemId == itemId);
            if (slot == null || slot.Quantity < quantity)
            {
                Debug.LogWarning($"[InventoryManager] Not enough {itemId} to remove");
                return false;
            }

            slot.Quantity -= quantity;
            if (slot.Quantity <= 0)
            {
                items.Remove(slot);
            }

            var itemData = ItemManager.Instance != null ? ItemManager.Instance.GetItem(itemId) : null;
            string displayName = itemData != null ? itemData.ItemName : itemId;
            Debug.Log($"[InventoryManager] Removed {quantity}x {displayName}");
            return true;
        }

        public bool HasItem(string itemId, int quantity = 1)
        {
            var slot = items.Find(s => s.ItemId == itemId);
            return slot != null && slot.Quantity >= quantity;
        }

        public int GetItemCount(string itemId)
        {
            var slot = items.Find(s => s.ItemId == itemId);
            return slot?.Quantity ?? 0;
        }

        public void ClearInventory()
        {
            items.Clear();
            Debug.Log("[InventoryManager] Inventory cleared");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [ValueDropdown("GetAllItemDataList")]
        [SerializeField] private ItemData debugSelectedItem;

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Add Item")]
        private void Debug_AddItem()
        {
            if (debugSelectedItem != null)
                AddItem(debugSelectedItem, 1);
        }

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Remove Item")]
        private void Debug_RemoveItem()
        {
            if (debugSelectedItem != null)
                RemoveItem(debugSelectedItem, 1);
        }

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Add 5 Random Items")]
        private void Debug_AddRandomItems()
        {
            if (ItemManager.Instance == null) return;
            var allItems = ItemManager.Instance.GetAllItems();

            if (allItems == null || allItems.Count == 0)
            {
                Debug.LogWarning("[InventoryManager] No items found in ItemManager.");
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                var randomItem = allItems[Random.Range(0, allItems.Count)];
                if (randomItem != null)
                    AddItem(randomItem, Random.Range(1, 3));
            }
        }

        [Button("Clear All")]
        private void Debug_ClearAll()
        {
            ClearInventory();
        }

        [Button("Log All Items")]
        private void Debug_LogItems()
        {
            Debug.Log($"[InventoryManager] Total slots: {items.Count}, Total items: {TotalItemCount}");
            foreach (var slot in items)
            {
                // Note: GetItemData might need updating if it relied on passed database, 
                // but usually slots store IDs. We can look up via ItemManager.
                ItemData data = null;
                if (ItemManager.Instance != null)
                {
                    data = ItemManager.Instance.GetItem(slot.ItemId);
                }
                
                string name = data != null ? data.ItemName : slot.ItemId;
                Debug.Log($"  - {name}: {slot.Quantity}");
            }
        }

        private IEnumerable<ValueDropdownItem<ItemData>> GetAllItemDataList()
        {
            var list = new ValueDropdownList<ItemData>();

            // 1. Add Persistent Items from Database
            if (ItemManager.Instance != null)
            {
                var allItems = ItemManager.Instance.GetAllItems();
                foreach (var item in allItems)
                {
                    if (item != null)
                    {
                        list.Add($"[P] {item.ItemName}", item);
                    }
                }
            }

            // 2. Add Session-Bound Items from AIItemCreator
            if (AIItemCreator.Instance != null && AIItemCreator.Instance.SessionItems != null)
            {
                foreach (var sessionItem in AIItemCreator.Instance.SessionItems)
                {
                    if (sessionItem != null)
                    {
                        // Add if not already in list (though session items usually differ)
                        list.Add($"[S] {sessionItem.ItemName}", sessionItem);
                    }
                }
            }

            return list;
        }
        #endif
    }
}
