using UnityEngine;
using System;

namespace TheBunkerGames
{
    /// <summary>
    /// Simple inventory slot holding an item ID and quantity.
    /// </summary>
    [Serializable]
    public class InventorySlotData
    {
        public string ItemId;
        public int Quantity;

        public InventorySlotData()
        {
            ItemId = string.Empty;
            Quantity = 0;
        }

        public InventorySlotData(string itemId, int quantity = 1)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        /// <summary>
        /// Get the full ItemData from the database.
        /// </summary>
        /// <summary>
        /// Get the full ItemData from the database.
        /// </summary>
        public ItemData GetItemData(ItemDatabaseDataSO database)
        {
            return database?.GetItem(ItemId);
        }

        /// <summary>
        /// Get the full ItemData using the global ItemManager.
        /// </summary>
        public ItemData GetItemData()
        {
             if (ItemManager.Instance == null) return null;
             return ItemManager.Instance.GetItem(ItemId);
        }
    }
}
