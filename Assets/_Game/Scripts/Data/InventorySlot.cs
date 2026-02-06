using UnityEngine;
using System;

namespace TheBunkerGames
{
    /// <summary>
    /// Simple inventory slot holding an item ID and quantity.
    /// </summary>
    [Serializable]
    public class InventorySlot
    {
        public string ItemId;
        public int Quantity;

        public InventorySlot()
        {
            ItemId = string.Empty;
            Quantity = 0;
        }

        public InventorySlot(string itemId, int quantity = 1)
        {
            ItemId = itemId;
            Quantity = quantity;
        }

        public ItemDataSO GetItemData()
        {
            return ItemDatabaseSO.Instance?.GetItem(ItemId);
        }
    }
}
