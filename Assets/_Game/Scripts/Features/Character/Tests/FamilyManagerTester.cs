using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for FamilyManager: add/get/clear characters, alive count, explorers.
    /// </summary>
    public class FamilyManagerTester : BaseTester
    {
        public override string TesterName => "FamilyManager";

        private FamilyManager fm;

        protected override void Setup()
        {
            fm = FamilyManager.Instance;
            AssertNotNull(fm, "FamilyManager.Instance");
            fm.ClearFamily();
        }

        protected override void TearDown()
        {
            fm.ClearFamily();
        }

        // -------------------------------------------------------------------------
        // Add Characters
        // -------------------------------------------------------------------------
        [TestMethod("AddCharacter adds a character to the family")]
        private void Test_AddCharacter()
        {
            fm.AddCharacter("Father", 90f, 85f, 70f, 100f);
            AssertEqual(1, fm.FamilyMembers.Count, "Should have 1 member");
        }

        [TestMethod("AddCharacter multiple times increases count")]
        private void Test_AddMultiple()
        {
            fm.AddCharacter("Father");
            fm.AddCharacter("Mother");
            fm.AddCharacter("Child");
            AssertEqual(3, fm.FamilyMembers.Count, "Should have 3 members");
        }

        [TestMethod("AddCharacter sets correct stats")]
        private void Test_AddCharacter_Stats()
        {
            fm.AddCharacter("Alice", 80f, 70f, 60f, 50f);
            var c = fm.GetCharacter("Alice");
            AssertNotNull(c, "Alice");
            AssertApproxEqual(80f, c.Hunger, 0.01f, "Hunger");
            AssertApproxEqual(70f, c.Thirst, 0.01f, "Thirst");
            AssertApproxEqual(60f, c.Sanity, 0.01f, "Sanity");
            AssertApproxEqual(50f, c.Health, 0.01f, "Health");
        }

        // -------------------------------------------------------------------------
        // Get Character
        // -------------------------------------------------------------------------
        [TestMethod("GetCharacter returns correct character by name")]
        private void Test_GetCharacter()
        {
            fm.AddCharacter("Father");
            fm.AddCharacter("Mother");
            var father = fm.GetCharacter("Father");
            AssertNotNull(father, "Father");
            AssertEqual("Father", father.Name, "Name");
        }

        [TestMethod("GetCharacter returns null for nonexistent name")]
        private void Test_GetCharacter_NotFound()
        {
            fm.AddCharacter("Father");
            var result = fm.GetCharacter("Nobody");
            AssertNull(result, "Should return null for nonexistent name");
        }

        // -------------------------------------------------------------------------
        // Clear
        // -------------------------------------------------------------------------
        [TestMethod("ClearFamily removes all members")]
        private void Test_ClearFamily()
        {
            fm.AddCharacter("Father");
            fm.AddCharacter("Mother");
            fm.ClearFamily();
            AssertEqual(0, fm.FamilyMembers.Count, "Should have 0 members after clear");
        }

        // -------------------------------------------------------------------------
        // Alive Count
        // -------------------------------------------------------------------------
        [TestMethod("AliveCount returns correct count")]
        private void Test_AliveCount()
        {
            fm.AddCharacter("Alive1", 50f, 50f, 50f, 50f);
            fm.AddCharacter("Alive2", 50f, 50f, 50f, 50f);
            fm.AddCharacter("Dead", 0f, 50f, 50f, 50f);  // Hunger=0 -> not alive
            AssertEqual(2, fm.AliveCount, "AliveCount should be 2");
        }

        [TestMethod("AliveCount is 0 when all are dead")]
        private void Test_AliveCount_AllDead()
        {
            fm.AddCharacter("Dead1", 0f, 0f, 0f, 0f);
            fm.AddCharacter("Dead2", 50f, 50f, 50f, 0f);
            AssertEqual(0, fm.AliveCount, "AliveCount should be 0");
        }

        // -------------------------------------------------------------------------
        // Available Explorers
        // -------------------------------------------------------------------------
        [TestMethod("AvailableExplorers returns only available characters")]
        private void Test_AvailableExplorers()
        {
            fm.AddCharacter("Available");
            fm.AddCharacter("Exploring");
            fm.AddCharacter("Injured");
            fm.AddCharacter("Dead", 0f, 0f, 0f, 0f);

            fm.GetCharacter("Exploring").IsExploring = true;
            fm.GetCharacter("Injured").IsInjured = true;

            var explorers = fm.AvailableExplorers;
            AssertEqual(1, explorers.Count, "Only 1 should be available");
            AssertEqual("Available", explorers[0].Name, "Available name");
        }

        // -------------------------------------------------------------------------
        // Load Characters
        // -------------------------------------------------------------------------
        [TestMethod("LoadCharacters replaces entire family")]
        private void Test_LoadCharacters()
        {
            fm.AddCharacter("OldMember");
            AssertEqual(1, fm.FamilyMembers.Count, "Before load");

            var newFamily = new List<CharacterData>
            {
                new CharacterData("New1"),
                new CharacterData("New2"),
                new CharacterData("New3")
            };
            fm.LoadCharacters(newFamily);

            AssertEqual(3, fm.FamilyMembers.Count, "After load");
            AssertNotNull(fm.GetCharacter("New1"), "New1 exists");
            AssertNull(fm.GetCharacter("OldMember"), "OldMember should be gone");
        }

        [TestMethod("LoadCharacters with null clears family")]
        private void Test_LoadCharacters_Null()
        {
            fm.AddCharacter("Survivor");
            fm.LoadCharacters(null);
            AssertEqual(0, fm.FamilyMembers.Count, "Should be empty after loading null");
        }
    }
}
