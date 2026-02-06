using UnityEngine;
using System.Collections.Generic;
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
        [SerializeField] private string debugItemId = "can_of_beans";

        [HorizontalGroup("Add")]
        [Button("Add Item", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_AddItem()
        {
            if (Application.isPlaying)
            {
                AddItem(debugItemId, 1);
            }
        }

        [HorizontalGroup("Add")]
        [Button("Remove Item", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void Debug_RemoveItem()
        {
            if (Application.isPlaying)
            {
                RemoveItem(debugItemId, 1);
            }
        }

        [Button("Add 5 Random Items", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_AddRandomItems()
        {
            if (Application.isPlaying)
            {
                string[] testItems = { "can_of_beans", "bandages", "broken_radio" };
                foreach (var id in testItems)
                {
                    AddItem(id, Random.Range(1, 3));
                }
            }
        }

        [Button("Clear All", ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.3f)]
        private void Debug_ClearAll()
        {
            if (Application.isPlaying)
            {
                ClearInventory();
            }
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
        #endif
    }
}
