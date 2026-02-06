using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for QuestManager and QuestData: add/update/remove quests,
    /// state transitions, filtering by state, duplicate handling.
    /// </summary>
    public class QuestManagerTester : BaseTester
    {
        public override string TesterName => "QuestManager";

        private QuestManager qm;

        protected override void Setup()
        {
            qm = QuestManager.Instance;
            AssertNotNull(qm, "QuestManager.Instance");
            ClearAllQuests();
        }

        protected override void TearDown()
        {
            ClearAllQuests();
        }

        private void ClearAllQuests()
        {
            while (qm.Quests.Count > 0)
                qm.RemoveQuest(qm.Quests[0].Id);
        }

        // -------------------------------------------------------------------------
        // QuestData Class Tests
        // -------------------------------------------------------------------------
        [TestMethod("QuestData default constructor sets Active state")]
        private void Test_QuestData_DefaultConstructor()
        {
            var q = new QuestData();
            AssertEqual(QuestState.Active, q.State, "Default state");
            AssertTrue(q.IsActive, "IsActive");
            AssertFalse(q.IsCompleted, "IsCompleted");
            AssertFalse(q.IsFailed, "IsFailed");
        }

        [TestMethod("QuestData parameterized constructor")]
        private void Test_QuestData_ParameterizedConstructor()
        {
            var q = new QuestData("FindWater", "Find clean water", QuestState.Active);
            AssertEqual("FindWater", q.Id, "Id");
            AssertEqual("Find clean water", q.Description, "Description");
            AssertTrue(q.IsActive, "IsActive");
        }

        [TestMethod("QuestData.SetState changes state correctly")]
        private void Test_QuestData_SetState()
        {
            var q = new QuestData("Test", "Test quest");
            q.SetState(QuestState.Completed);
            AssertTrue(q.IsCompleted, "Should be completed");
            AssertFalse(q.IsActive, "Should not be active");

            q.SetState(QuestState.Failed);
            AssertTrue(q.IsFailed, "Should be failed");
        }

        // -------------------------------------------------------------------------
        // Add Quest
        // -------------------------------------------------------------------------
        [TestMethod("AddQuest adds a new quest")]
        private void Test_AddQuest()
        {
            qm.AddQuest("FindWater", "Find clean water");
            AssertEqual(1, qm.Quests.Count, "Quest count");
            var q = qm.GetQuest("FindWater");
            AssertNotNull(q, "FindWater quest");
            AssertEqual("Find clean water", q.Description, "Description");
            AssertTrue(q.IsActive, "Should be active by default");
        }

        [TestMethod("AddQuest ignores null or empty ID")]
        private void Test_AddQuest_NullId()
        {
            qm.AddQuest(null, "Description");
            qm.AddQuest("", "Description");
            AssertEqual(0, qm.Quests.Count, "Should not add null/empty ID quests");
        }

        [TestMethod("AddQuest ignores duplicate ID")]
        private void Test_AddQuest_Duplicate()
        {
            qm.AddQuest("FindWater", "Find clean water");
            qm.AddQuest("FindWater", "Different description");
            AssertEqual(1, qm.Quests.Count, "Should not add duplicate");
            AssertEqual("Find clean water", qm.GetQuest("FindWater").Description, "Description unchanged");
        }

        // -------------------------------------------------------------------------
        // Update Quest
        // -------------------------------------------------------------------------
        [TestMethod("UpdateQuest changes quest state")]
        private void Test_UpdateQuest()
        {
            qm.AddQuest("FindWater", "Find clean water");
            qm.UpdateQuest("FindWater", QuestState.Completed);
            var q = qm.GetQuest("FindWater");
            AssertTrue(q.IsCompleted, "Should be completed");
        }

        [TestMethod("UpdateQuest does nothing for nonexistent ID")]
        private void Test_UpdateQuest_NotFound()
        {
            // Just ensure no exception
            qm.UpdateQuest("Nonexistent", QuestState.Completed);
            AssertEqual(0, qm.Quests.Count, "No quests should exist");
        }

        // -------------------------------------------------------------------------
        // Remove Quest
        // -------------------------------------------------------------------------
        [TestMethod("RemoveQuest removes the quest")]
        private void Test_RemoveQuest()
        {
            qm.AddQuest("FindWater", "Find clean water");
            qm.RemoveQuest("FindWater");
            AssertEqual(0, qm.Quests.Count, "Quest should be removed");
            AssertNull(qm.GetQuest("FindWater"), "Quest should not be found");
        }

        [TestMethod("RemoveQuest does nothing for nonexistent ID")]
        private void Test_RemoveQuest_NotFound()
        {
            qm.RemoveQuest("Nonexistent");
            AssertEqual(0, qm.Quests.Count, "No change");
        }

        // -------------------------------------------------------------------------
        // Get Quest
        // -------------------------------------------------------------------------
        [TestMethod("GetQuest returns null for nonexistent quest")]
        private void Test_GetQuest_NotFound()
        {
            AssertNull(qm.GetQuest("Nonexistent"), "Should return null");
        }

        // -------------------------------------------------------------------------
        // Filtered Lists
        // -------------------------------------------------------------------------
        [TestMethod("ActiveQuests returns only active quests")]
        private void Test_ActiveQuests()
        {
            qm.AddQuest("Q1", "Quest 1");
            qm.AddQuest("Q2", "Quest 2");
            qm.AddQuest("Q3", "Quest 3");
            qm.UpdateQuest("Q2", QuestState.Completed);
            qm.UpdateQuest("Q3", QuestState.Failed);

            var active = qm.ActiveQuests;
            AssertEqual(1, active.Count, "Only 1 active");
            AssertEqual("Q1", active[0].Id, "Active quest ID");
        }

        [TestMethod("CompletedQuests returns only completed quests")]
        private void Test_CompletedQuests()
        {
            qm.AddQuest("Q1", "Quest 1");
            qm.AddQuest("Q2", "Quest 2");
            qm.UpdateQuest("Q1", QuestState.Completed);

            var completed = qm.CompletedQuests;
            AssertEqual(1, completed.Count, "Only 1 completed");
            AssertEqual("Q1", completed[0].Id, "Completed quest ID");
        }

        [TestMethod("Multiple quest lifecycle: add, complete, fail")]
        private void Test_FullLifecycle()
        {
            qm.AddQuest("Q1", "Quest 1");
            qm.AddQuest("Q2", "Quest 2");
            qm.AddQuest("Q3", "Quest 3");

            AssertEqual(3, qm.ActiveQuests.Count, "All active initially");

            qm.UpdateQuest("Q1", QuestState.Completed);
            qm.UpdateQuest("Q3", QuestState.Failed);

            AssertEqual(1, qm.ActiveQuests.Count, "1 active");
            AssertEqual(1, qm.CompletedQuests.Count, "1 completed");
            AssertEqual(3, qm.Quests.Count, "3 total");

            qm.RemoveQuest("Q2");
            AssertEqual(2, qm.Quests.Count, "2 total after remove");
            AssertEqual(0, qm.ActiveQuests.Count, "0 active after remove");
        }
    }
}
