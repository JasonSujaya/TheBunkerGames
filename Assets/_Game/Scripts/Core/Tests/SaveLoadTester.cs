using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for SaveLoadManager: save, load, delete, data integrity.
    /// </summary>
    public class SaveLoadTester : BaseTester
    {
        public override string TesterName => "SaveLoad";

        private SaveLoadManager saveLoad;

        protected override void Setup()
        {
            saveLoad = SaveLoadManager.Instance;
            AssertNotNull(saveLoad, "SaveLoadManager.Instance");

            // Clean up any existing save before running tests
            saveLoad.DeleteSave();
        }

        protected override void TearDown()
        {
            // Clean up after tests
            saveLoad.DeleteSave();
        }

        // -------------------------------------------------------------------------
        // Save Path
        // -------------------------------------------------------------------------
        [TestMethod("SaveFilePath is not empty")]
        private void Test_SaveFilePath_NotEmpty()
        {
            AssertTrue(!string.IsNullOrEmpty(saveLoad.SaveFilePath), "SaveFilePath should not be empty");
        }

        [TestMethod("SaveExists is false when no save file")]
        private void Test_SaveExists_FalseInitially()
        {
            saveLoad.DeleteSave();
            AssertFalse(saveLoad.SaveExists, "SaveExists should be false after delete");
        }

        // -------------------------------------------------------------------------
        // Save / Load Cycle
        // -------------------------------------------------------------------------
        [TestMethod("SaveGame creates a save file")]
        private void Test_SaveGame_CreatesFile()
        {
            // Set up some game state
            var gm = GameManager.Instance;
            if (gm != null) gm.StartNewGame();

            saveLoad.SaveGame();
            AssertTrue(saveLoad.SaveExists, "Save file should exist after SaveGame");
        }

        [TestMethod("SaveGame fires OnGameSaved event")]
        private void Test_SaveGame_FiresEvent()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.StartNewGame();

            bool fired = false;
            System.Action handler = () => fired = true;
            SaveLoadManager.OnGameSaved += handler;

            saveLoad.SaveGame();
            AssertTrue(fired, "OnGameSaved should fire");

            SaveLoadManager.OnGameSaved -= handler;
        }

        [TestMethod("LoadGame fires OnGameLoaded event")]
        private void Test_LoadGame_FiresEvent()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.StartNewGame();

            saveLoad.SaveGame();

            bool fired = false;
            System.Action handler = () => fired = true;
            SaveLoadManager.OnGameLoaded += handler;

            saveLoad.LoadGame();
            AssertTrue(fired, "OnGameLoaded should fire");

            SaveLoadManager.OnGameLoaded -= handler;
        }

        [TestMethod("LoadGame does nothing when no save file exists")]
        private void Test_LoadGame_NoFile()
        {
            saveLoad.DeleteSave();

            bool fired = false;
            System.Action handler = () => fired = true;
            SaveLoadManager.OnGameLoaded += handler;

            saveLoad.LoadGame();
            AssertFalse(fired, "OnGameLoaded should NOT fire when no save exists");

            SaveLoadManager.OnGameLoaded -= handler;
        }

        // -------------------------------------------------------------------------
        // Delete
        // -------------------------------------------------------------------------
        [TestMethod("DeleteSave removes the save file")]
        private void Test_DeleteSave()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.StartNewGame();

            saveLoad.SaveGame();
            AssertTrue(saveLoad.SaveExists, "Save should exist before delete");

            saveLoad.DeleteSave();
            AssertFalse(saveLoad.SaveExists, "Save should not exist after delete");
        }

        // -------------------------------------------------------------------------
        // Data Integrity
        // -------------------------------------------------------------------------
        [TestMethod("Save/Load preserves family member data")]
        private void Test_SaveLoad_PreservesFamily()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.StartNewGame();

            var fm = FamilyManager.Instance;
            if (fm == null) return;

            fm.ClearFamily();
            fm.AddCharacter("TestSurvivor", 75f, 60f, 80f, 90f);

            saveLoad.SaveGame();

            // Modify family to ensure load actually restores
            fm.ClearFamily();
            AssertEqual(0, fm.FamilyMembers.Count, "Family should be empty after clear");

            saveLoad.LoadGame();
            AssertEqual(1, fm.FamilyMembers.Count, "Family should have 1 member after load");

            var loaded = fm.GetCharacter("TestSurvivor");
            AssertNotNull(loaded, "TestSurvivor");
            AssertApproxEqual(75f, loaded.Hunger, 0.1f, "Hunger");
            AssertApproxEqual(60f, loaded.Thirst, 0.1f, "Thirst");
            AssertApproxEqual(80f, loaded.Sanity, 0.1f, "Sanity");
            AssertApproxEqual(90f, loaded.Health, 0.1f, "Health");
        }

        [TestMethod("Save/Load preserves inventory data")]
        private void Test_SaveLoad_PreservesInventory()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.StartNewGame();

            var inv = InventoryManager.Instance;
            if (inv == null) return;

            inv.ClearInventory();
            inv.AddItem("test_food", 5);
            inv.AddItem("test_meds", 3);

            saveLoad.SaveGame();

            inv.ClearInventory();
            AssertEqual(0, inv.TotalItemCount, "Inventory should be empty after clear");

            saveLoad.LoadGame();
            AssertTrue(inv.HasItem("test_food", 5), "Should have 5 test_food after load");
            AssertTrue(inv.HasItem("test_meds", 3), "Should have 3 test_meds after load");
        }

        [TestMethod("Save/Load preserves quest data")]
        private void Test_SaveLoad_PreservesQuests()
        {
            var gm = GameManager.Instance;
            if (gm != null) gm.StartNewGame();

            var qm = QuestManager.Instance;
            if (qm == null) return;

            // Clear existing quests
            while (qm.Quests.Count > 0)
                qm.RemoveQuest(qm.Quests[0].Id);

            qm.AddQuest("TestQuest1", "Find water");
            qm.AddQuest("TestQuest2", "Fix filter");
            qm.UpdateQuest("TestQuest2", QuestState.Completed);

            saveLoad.SaveGame();

            // Clear quests
            while (qm.Quests.Count > 0)
                qm.RemoveQuest(qm.Quests[0].Id);

            saveLoad.LoadGame();

            var q1 = qm.GetQuest("TestQuest1");
            AssertNotNull(q1, "TestQuest1");
            AssertTrue(q1.IsActive, "TestQuest1 should be active");

            var q2 = qm.GetQuest("TestQuest2");
            AssertNotNull(q2, "TestQuest2");
            AssertTrue(q2.IsCompleted, "TestQuest2 should be completed");
        }
    }
}
