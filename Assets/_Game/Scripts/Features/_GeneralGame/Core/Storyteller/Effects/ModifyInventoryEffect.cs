using UnityEngine;
using System;

namespace TheBunkerGames
{
    [Serializable]
    public class ModifyInventoryEffect : StoryEffect
    {
        [SerializeField] private string itemId;
        [SerializeField] private int amount;

        public string ItemId => itemId;
        public int Amount => amount;

        public ModifyInventoryEffect(string itemId, int amount)
        {
            this.itemId = itemId;
            this.amount = amount;
        }

        public override void Execute()
        {
            if (InventoryManager.Instance != null)
            {
                if (amount > 0)
                {
                    InventoryManager.Instance.AddItem(itemId, amount);
                }
                else if (amount < 0)
                {
                    InventoryManager.Instance.RemoveItem(itemId, Mathf.Abs(amount));
                }
            }
            else
            {
                Debug.LogWarning("[StoryEffect] Cannot execute ModifyInventory: InventoryManager missing.");
            }
        }
    }
}
