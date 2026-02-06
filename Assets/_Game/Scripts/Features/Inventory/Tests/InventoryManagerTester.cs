using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for InventoryManager: add/remove items, quantity stacking,
    /// HasItem, GetItemCount, ClearInventory, edge cases.
    /// </summary>
    public class InventoryManagerTester : BaseTester
    {
        public override string TesterName => "InventoryManager";

        private InventoryManager inv;

        protected override void Setup()
        {
            inv = InventoryManager.Instance;
            AssertNotNull(inv, "InventoryManager.Instance");
            inv.ClearInventory();
        }

        protected override void TearDown()
        {
            inv.ClearInventory();
        }

        // -------------------------------------------------------------------------
        // Add Items
        // -------------------------------------------------------------------------
        [TestMethod("AddItem creates a new slot")]
        private void Test_AddItem_NewSlot()
        {
            inv.AddItem("food_can", 1);
            AssertEqual(1, inv.Items.Count, "Should have 1 slot");
            AssertEqual(1, inv.GetItemCount("food_can"), "Quantity");
        }

        [TestMethod("AddItem stacks quantity on existing slot")]
        private void Test_AddItem_Stacks()
        {
            inv.AddItem("food_can", 3);
            inv.AddItem("food_can", 2);
            AssertEqual(1, inv.Items.Count, "Should still have 1 slot");
            AssertEqual(5, inv.GetItemCount("food_can"), "Quantity should be 5");
        }

        [TestMethod("AddItem with different IDs creates separate slots")]
        private void Test_AddItem_DifferentSlots()
        {
            inv.AddItem("food_can", 2);
            inv.AddItem("bandages", 3);
            AssertEqual(2, inv.Items.Count, "Should have 2 slots");
        }

        [TestMethod("AddItem ignores null or empty ID")]
        private void Test_AddItem_NullId()
        {
            inv.AddItem(null, 1);
            inv.AddItem("", 1);
            AssertEqual(0, inv.Items.Count, "Should have 0 slots for null/empty IDs");
        }

        [TestMethod("AddItem ignores zero or negative quantity")]
        private void Test_AddItem_ZeroQuantity()
        {
            inv.AddItem("food_can", 0);
            inv.AddItem("bandages", -5);
            AssertEqual(0, inv.Items.Count, "Should have 0 slots for 0 or negative qty");
        }

        // -------------------------------------------------------------------------
        // Remove Items
        // -------------------------------------------------------------------------
        [TestMethod("RemoveItem reduces quantity")]
        private void Test_RemoveItem()
        {
            inv.AddItem("food_can", 5);
            bool result = inv.RemoveItem("food_can", 2);
            AssertTrue(result, "RemoveItem should return true");
            AssertEqual(3, inv.GetItemCount("food_can"), "Remaining quantity");
        }

        [TestMethod("RemoveItem removes slot when quantity reaches 0")]
        private void Test_RemoveItem_RemovesSlot()
        {
            inv.AddItem("food_can", 3);
            inv.RemoveItem("food_can", 3);
            AssertEqual(0, inv.Items.Count, "Slot should be removed");
            AssertEqual(0, inv.GetItemCount("food_can"), "Count should be 0");
        }

        [TestMethod("RemoveItem fails when not enough quantity")]
        private void Test_RemoveItem_NotEnough()
        {
            inv.AddItem("food_can", 2);
            bool result = inv.RemoveItem("food_can", 5);
            AssertFalse(result, "Should return false when not enough");
            AssertEqual(2, inv.GetItemCount("food_can"), "Quantity should be unchanged");
        }

        [TestMethod("RemoveItem fails for nonexistent item")]
        private void Test_RemoveItem_NotFound()
        {
            bool result = inv.RemoveItem("nonexistent", 1);
            AssertFalse(result, "Should return false for nonexistent item");
        }

        [TestMethod("RemoveItem ignores null or empty ID")]
        private void Test_RemoveItem_NullId()
        {
            bool result = inv.RemoveItem(null, 1);
            AssertFalse(result, "Should return false for null ID");
        }

        [TestMethod("RemoveItem ignores zero or negative quantity")]
        private void Test_RemoveItem_ZeroQty()
        {
            inv.AddItem("food_can", 5);
            bool result = inv.RemoveItem("food_can", 0);
            AssertFalse(result, "Should return false for 0 qty");
            AssertEqual(5, inv.GetItemCount("food_can"), "Quantity unchanged");
        }

        // -------------------------------------------------------------------------
        // HasItem / GetItemCount
        // -------------------------------------------------------------------------
        [TestMethod("HasItem returns true when enough quantity")]
        private void Test_HasItem_True()
        {
            inv.AddItem("food_can", 5);
            AssertTrue(inv.HasItem("food_can", 3), "Should have at least 3");
            AssertTrue(inv.HasItem("food_can", 5), "Should have exactly 5");
        }

        [TestMethod("HasItem returns false when not enough quantity")]
        private void Test_HasItem_NotEnough()
        {
            inv.AddItem("food_can", 2);
            AssertFalse(inv.HasItem("food_can", 3), "Should not have 3");
        }

        [TestMethod("HasItem returns false for nonexistent item")]
        private void Test_HasItem_NotFound()
        {
            AssertFalse(inv.HasItem("nonexistent"), "Should not have nonexistent item");
        }

        [TestMethod("GetItemCount returns 0 for nonexistent item")]
        private void Test_GetItemCount_NotFound()
        {
            AssertEqual(0, inv.GetItemCount("nonexistent"), "Count should be 0");
        }

        // -------------------------------------------------------------------------
        // TotalItemCount
        // -------------------------------------------------------------------------
        [TestMethod("TotalItemCount sums all slot quantities")]
        private void Test_TotalItemCount()
        {
            inv.AddItem("food_can", 3);
            inv.AddItem("bandages", 2);
            inv.AddItem("tools", 5);
            AssertEqual(10, inv.TotalItemCount, "TotalItemCount");
        }

        [TestMethod("TotalItemCount is 0 when empty")]
        private void Test_TotalItemCount_Empty()
        {
            AssertEqual(0, inv.TotalItemCount, "Should be 0 when empty");
        }

        // -------------------------------------------------------------------------
        // Clear
        // -------------------------------------------------------------------------
        [TestMethod("ClearInventory removes everything")]
        private void Test_ClearInventory()
        {
            inv.AddItem("food_can", 5);
            inv.AddItem("bandages", 3);
            inv.ClearInventory();
            AssertEqual(0, inv.Items.Count, "Slots after clear");
            AssertEqual(0, inv.TotalItemCount, "TotalItemCount after clear");
        }
    }
}
