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

        public ItemData GetItemData()
        {
            return ItemDatabaseDataSO.Instance?.GetItem(ItemId);
        }
    }
}
