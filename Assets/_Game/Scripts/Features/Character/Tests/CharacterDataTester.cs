using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for CharacterData: stat modifiers, derived states, clamping, constructors.
    /// </summary>
    public class CharacterDataTester : BaseTester
    {
        public override string TesterName => "CharacterData";

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------
        [TestMethod("Default constructor sets all stats to 100")]
        private void Test_DefaultConstructor()
        {
            var c = new CharacterData();
            AssertEqual("Unknown", c.Name, "Default name");
            AssertApproxEqual(100f, c.Hunger, 0.01f, "Default Hunger");
            AssertApproxEqual(100f, c.Thirst, 0.01f, "Default Thirst");
            AssertApproxEqual(100f, c.Sanity, 0.01f, "Default Sanity");
            AssertApproxEqual(100f, c.Health, 0.01f, "Default Health");
        }

        [TestMethod("Parameterized constructor sets name and stats")]
        private void Test_ParameterizedConstructor()
        {
            var c = new CharacterData("Alice", 80f, 70f, 60f, 50f);
            AssertEqual("Alice", c.Name, "Name");
            AssertApproxEqual(80f, c.Hunger, 0.01f, "Hunger");
            AssertApproxEqual(70f, c.Thirst, 0.01f, "Thirst");
            AssertApproxEqual(60f, c.Sanity, 0.01f, "Sanity");
            AssertApproxEqual(50f, c.Health, 0.01f, "Health");
        }

        [TestMethod("Constructor clamps stats above 100")]
        private void Test_Constructor_ClampsAbove100()
        {
            var c = new CharacterData("Over", 150f, 200f, 999f, 101f);
            AssertApproxEqual(100f, c.Hunger, 0.01f, "Hunger clamped");
            AssertApproxEqual(100f, c.Thirst, 0.01f, "Thirst clamped");
            AssertApproxEqual(100f, c.Sanity, 0.01f, "Sanity clamped");
            AssertApproxEqual(100f, c.Health, 0.01f, "Health clamped");
        }

        [TestMethod("Constructor clamps stats below 0")]
        private void Test_Constructor_ClampsBelow0()
        {
            var c = new CharacterData("Under", -10f, -50f, -1f, -999f);
            AssertApproxEqual(0f, c.Hunger, 0.01f, "Hunger clamped to 0");
            AssertApproxEqual(0f, c.Thirst, 0.01f, "Thirst clamped to 0");
            AssertApproxEqual(0f, c.Sanity, 0.01f, "Sanity clamped to 0");
            AssertApproxEqual(0f, c.Health, 0.01f, "Health clamped to 0");
        }

        // -------------------------------------------------------------------------
        // Stat Modifiers
        // -------------------------------------------------------------------------
        [TestMethod("ModifyHunger adds positive amount")]
        private void Test_ModifyHunger_Positive()
        {
            var c = new CharacterData("Test", 50f, 50f, 50f, 50f);
            c.ModifyHunger(20f);
            AssertApproxEqual(70f, c.Hunger, 0.01f, "Hunger after +20");
        }

        [TestMethod("ModifyHunger subtracts negative amount")]
        private void Test_ModifyHunger_Negative()
        {
            var c = new CharacterData("Test", 50f, 50f, 50f, 50f);
            c.ModifyHunger(-30f);
            AssertApproxEqual(20f, c.Hunger, 0.01f, "Hunger after -30");
        }

        [TestMethod("ModifyHunger clamps to 0")]
        private void Test_ModifyHunger_ClampMin()
        {
            var c = new CharacterData("Test", 10f, 50f, 50f, 50f);
            c.ModifyHunger(-50f);
            AssertApproxEqual(0f, c.Hunger, 0.01f, "Hunger should not go below 0");
        }

        [TestMethod("ModifyHunger clamps to 100")]
        private void Test_ModifyHunger_ClampMax()
        {
            var c = new CharacterData("Test", 90f, 50f, 50f, 50f);
            c.ModifyHunger(50f);
            AssertApproxEqual(100f, c.Hunger, 0.01f, "Hunger should not exceed 100");
        }

        [TestMethod("ModifyThirst works correctly")]
        private void Test_ModifyThirst()
        {
            var c = new CharacterData("Test", 50f, 50f, 50f, 50f);
            c.ModifyThirst(-25f);
            AssertApproxEqual(25f, c.Thirst, 0.01f, "Thirst after -25");
            c.ModifyThirst(10f);
            AssertApproxEqual(35f, c.Thirst, 0.01f, "Thirst after +10");
        }

        [TestMethod("ModifySanity works correctly")]
        private void Test_ModifySanity()
        {
            var c = new CharacterData("Test", 50f, 50f, 50f, 50f);
            c.ModifySanity(-50f);
            AssertApproxEqual(0f, c.Sanity, 0.01f, "Sanity after -50");
            c.ModifySanity(-10f);
            AssertApproxEqual(0f, c.Sanity, 0.01f, "Sanity stays at 0");
        }

        [TestMethod("ModifyHealth works correctly")]
        private void Test_ModifyHealth()
        {
            var c = new CharacterData("Test", 50f, 50f, 50f, 50f);
            c.ModifyHealth(-20f);
            AssertApproxEqual(30f, c.Health, 0.01f, "Health after -20");
            c.ModifyHealth(100f);
            AssertApproxEqual(100f, c.Health, 0.01f, "Health clamps to 100");
        }

        // -------------------------------------------------------------------------
        // Derived States
        // -------------------------------------------------------------------------
        [TestMethod("IsAlive is true when Health > 0 and Hunger > 0")]
        private void Test_IsAlive_True()
        {
            var c = new CharacterData("Alive", 50f, 50f, 50f, 50f);
            AssertTrue(c.IsAlive, "Should be alive");
        }

        [TestMethod("IsAlive is false when Health is 0")]
        private void Test_IsAlive_FalseHealth()
        {
            var c = new CharacterData("Dead", 50f, 50f, 50f, 0f);
            AssertFalse(c.IsAlive, "Should be dead (Health=0)");
        }

        [TestMethod("IsAlive is false when Hunger is 0")]
        private void Test_IsAlive_FalseHunger()
        {
            var c = new CharacterData("Starved", 0f, 50f, 50f, 50f);
            AssertFalse(c.IsAlive, "Should be dead (Hunger=0)");
        }

        [TestMethod("IsInsane is true when Sanity is 0")]
        private void Test_IsInsane()
        {
            var c = new CharacterData("Crazy", 50f, 50f, 0f, 50f);
            AssertTrue(c.IsInsane, "Should be insane");
        }

        [TestMethod("IsInsane is false when Sanity > 0")]
        private void Test_IsNotInsane()
        {
            var c = new CharacterData("Sane", 50f, 50f, 1f, 50f);
            AssertFalse(c.IsInsane, "Should not be insane");
        }

        [TestMethod("IsDehydrated when Thirst is 0")]
        private void Test_IsDehydrated()
        {
            var c = new CharacterData("Dry", 50f, 0f, 50f, 50f);
            AssertTrue(c.IsDehydrated, "Should be dehydrated");
        }

        [TestMethod("IsCritical when Health <= 20")]
        private void Test_IsCritical_LowHealth()
        {
            var c = new CharacterData("Critical", 50f, 50f, 50f, 20f);
            AssertTrue(c.IsCritical, "Should be critical (Health=20)");
        }

        [TestMethod("IsCritical when Hunger <= 10")]
        private void Test_IsCritical_LowHunger()
        {
            var c = new CharacterData("Starving", 10f, 50f, 50f, 50f);
            AssertTrue(c.IsCritical, "Should be critical (Hunger=10)");
        }

        [TestMethod("IsCritical when Thirst <= 10")]
        private void Test_IsCritical_LowThirst()
        {
            var c = new CharacterData("Parched", 50f, 10f, 50f, 50f);
            AssertTrue(c.IsCritical, "Should be critical (Thirst=10)");
        }

        [TestMethod("IsCritical is false when all stats are healthy")]
        private void Test_NotCritical()
        {
            var c = new CharacterData("Healthy", 50f, 50f, 50f, 50f);
            AssertFalse(c.IsCritical, "Should not be critical");
        }

        // -------------------------------------------------------------------------
        // Exploration Availability
        // -------------------------------------------------------------------------
        [TestMethod("IsAvailableForExploration when alive, not exploring, not injured")]
        private void Test_AvailableForExploration()
        {
            var c = new CharacterData("Explorer", 50f, 50f, 50f, 50f);
            AssertTrue(c.IsAvailableForExploration, "Should be available");
        }

        [TestMethod("Not available when exploring")]
        private void Test_NotAvailable_Exploring()
        {
            var c = new CharacterData("Busy", 50f, 50f, 50f, 50f);
            c.IsExploring = true;
            AssertFalse(c.IsAvailableForExploration, "Should not be available when exploring");
        }

        [TestMethod("Not available when injured")]
        private void Test_NotAvailable_Injured()
        {
            var c = new CharacterData("Hurt", 50f, 50f, 50f, 50f);
            c.IsInjured = true;
            AssertFalse(c.IsAvailableForExploration, "Should not be available when injured");
        }

        [TestMethod("Not available when dead")]
        private void Test_NotAvailable_Dead()
        {
            var c = new CharacterData("Gone", 50f, 50f, 50f, 0f);
            AssertFalse(c.IsAvailableForExploration, "Should not be available when dead");
        }
    }
}
