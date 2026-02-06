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
        public void AddItem(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return;

            var existingSlot = items.Find(s => s.ItemId == itemId);
            if (existingSlot != null)
            {
                existingSlot.Quantity += quantity;
            }
            else
            {
                items.Add(new InventorySlotData(itemId, quantity));
            }

            var itemData = ItemDatabaseDataSO.Instance?.GetItem(itemId);
            string displayName = itemData != null ? itemData.DisplayName : itemId;
            Debug.Log($"[InventoryManager] Added {quantity}x {displayName}");
        }

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

            var itemData = ItemDatabaseDataSO.Instance?.GetItem(itemId);
            string displayName = itemData != null ? itemData.DisplayName : itemId;
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
        [Title("Debug Controls")]
        [HorizontalGroup("Add")]
        [ValueDropdown("GetAllItemIds")]
        [SerializeField] private string debugItemId = "can_of_beans";

        [HorizontalGroup("Add")]
        [Button("Add Item", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_AddItem()
        {
            AddItem(debugItemId, 1);
        }

        [HorizontalGroup("Add")]
        [Button("Remove Item", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void Debug_RemoveItem()
        {
            RemoveItem(debugItemId, 1);
        }

        [Button("Add 5 Random Items", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_AddRandomItems()
        {
            var allIds = GetAllItemIds().ToList();
            if (allIds.Count == 0)
            {
                Debug.LogWarning("[InventoryManager] No items found in database.");
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                string randomId = allIds[Random.Range(0, allIds.Count)];
                AddItem(randomId, Random.Range(1, 3));
            }
        }

        [Button("Clear All", ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.3f)]
        private void Debug_ClearAll()
        {
            ClearInventory();
        }

        [Button("Log All Items", ButtonSizes.Medium)]
        private void Debug_LogItems()
        {
            Debug.Log($"[InventoryManager] Total slots: {items.Count}, Total items: {TotalItemCount}");
            foreach (var slot in items)
            {
                var data = slot.GetItemData();
                string name = data != null ? data.DisplayName : slot.ItemId;
                Debug.Log($"  - {name}: {slot.Quantity}");
            }
        }

        private IEnumerable<string> GetAllItemIds()
        {
            var db = ItemDatabaseDataSO.Instance;
            if (db != null && db.AllItems != null)
            {
                return db.AllItems.Where(i => i != null).Select(i => i.Id);
            }
            return new string[] { "can_of_beans", "bandages", "broken_radio" }; // Fallback
        }

        [Title("Auto Setup")]
        [Button("Auto Setup Dependencies", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void AutoSetupDependencies()
        {
            #if UNITY_EDITOR
            // Ensure Tester exists
            var testerType = System.Type.GetType("TheBunkerGames.Tests.InventoryManagerTester");
            if (testerType != null && GetComponent(testerType) == null)
            {
                gameObject.AddComponent(testerType);
                Debug.Log("[InventoryManager] Added InventoryManagerTester.");
            }
            else if (testerType == null)
            {
                Debug.LogWarning("[InventoryManager] Could not find InventoryManagerTester type. Ensure it is in TheBunkerGames.Tests namespace.");
            }
            #endif
        }
        #endif
    }
}
