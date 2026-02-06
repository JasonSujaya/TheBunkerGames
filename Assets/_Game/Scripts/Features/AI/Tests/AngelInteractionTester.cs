using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for AngelInteractionController: mood changes, processing degradation,
    /// interaction limits, mock responses, resource granting, events.
    /// </summary>
    public class AngelInteractionTester : BaseTester
    {
        public override string TesterName => "AngelInteraction";

        private AngelInteractionController angel;

        protected override void Setup()
        {
            angel = AngelInteractionController.Instance;
            AssertNotNull(angel, "AngelInteractionController.Instance");
        }

        // -------------------------------------------------------------------------
        // AngelResponseData / ResourceGrantData
        // -------------------------------------------------------------------------
        [TestMethod("AngelResponseData has correct defaults")]
        private void Test_AngelResponseData_Defaults()
        {
            var response = new AngelResponseData();
            AssertEqual("", response.Message, "Default message");
            AssertNotNull(response.GrantedItems, "GrantedItems initialized");
            AssertEqual("Neutral", response.EmotionalTag, "Default emotional tag");
        }

        [TestMethod("ResourceGrantData default constructor")]
        private void Test_ResourceGrantData_Default()
        {
            var grant = new ResourceGrantData();
            AssertNull(grant.ItemId, "Default ItemId");
            AssertEqual(0, grant.Quantity, "Default Quantity");
        }

        [TestMethod("ResourceGrantData parameterized constructor")]
        private void Test_ResourceGrantData_Params()
        {
            var grant = new ResourceGrantData("food_can", 3);
            AssertEqual("food_can", grant.ItemId, "ItemId");
            AssertEqual(3, grant.Quantity, "Quantity");
        }

        // -------------------------------------------------------------------------
        // Mood
        // -------------------------------------------------------------------------
        [TestMethod("SetMood changes the current mood")]
        private void Test_SetMood()
        {
            angel.SetMood(AngelMood.Cooperative);
            AssertEqual(AngelMood.Cooperative, angel.CurrentMood, "Mood after set");

            angel.SetMood(AngelMood.Hostile);
            AssertEqual(AngelMood.Hostile, angel.CurrentMood, "Mood after second set");
        }

        [TestMethod("SetMood fires OnMoodChanged event")]
        private void Test_SetMood_FiresEvent()
        {
            angel.SetMood(AngelMood.Cooperative);  // Reset to known state

            AngelMood? received = null;
            Action<AngelMood> handler = m => received = m;
            AngelInteractionController.OnMoodChanged += handler;

            angel.SetMood(AngelMood.Mocking);
            AssertNotNull(received, "OnMoodChanged should fire");
            AssertEqual(AngelMood.Mocking, received.Value, "Received mood");

            AngelInteractionController.OnMoodChanged -= handler;
        }

        [TestMethod("SetMood does not fire event for same mood")]
        private void Test_SetMood_SameMood_NoEvent()
        {
            angel.SetMood(AngelMood.Neutral);

            bool fired = false;
            Action<AngelMood> handler = m => fired = true;
            AngelInteractionController.OnMoodChanged += handler;

            angel.SetMood(AngelMood.Neutral);
            AssertFalse(fired, "Should not fire for same mood");

            AngelInteractionController.OnMoodChanged -= handler;
        }

        // -------------------------------------------------------------------------
        // Processing Degradation
        // -------------------------------------------------------------------------
        [TestMethod("DegradeProcessing reduces processing level")]
        private void Test_DegradeProcessing()
        {
            // Reset to high processing
            angel.BeginInteractionPhase();
            float levelBefore = angel.ProcessingLevel;

            angel.DegradeProcessing(10f);
            AssertLessThan(angel.ProcessingLevel, levelBefore, "Processing should decrease");
        }

        [TestMethod("DegradeProcessing clamps to 0")]
        private void Test_DegradeProcessing_ClampsTo0()
        {
            angel.DegradeProcessing(9999f);
            AssertApproxEqual(0f, angel.ProcessingLevel, 0.01f, "Should clamp to 0");
        }

        [TestMethod("DegradeProcessing sets Glitching mood when <= 20")]
        private void Test_DegradeProcessing_Glitching()
        {
            angel.SetMood(AngelMood.Cooperative);
            angel.DegradeProcessing(9999f);  // Force to 0
            AssertEqual(AngelMood.Glitching, angel.CurrentMood, "Should be Glitching at 0%");
        }

        // -------------------------------------------------------------------------
        // Interaction Limits
        // -------------------------------------------------------------------------
        [TestMethod("BeginInteractionPhase resets interaction count")]
        private void Test_BeginPhase_ResetsCount()
        {
            angel.BeginInteractionPhase();
            AssertTrue(angel.CanInteract, "Should be able to interact after phase begin");
        }

        [TestMethod("CanInteract is false after max interactions")]
        private void Test_CanInteract_MaxReached()
        {
            angel.BeginInteractionPhase();

            // Request until we can't anymore
            int safetyLimit = 20;
            while (angel.CanInteract && safetyLimit > 0)
            {
                angel.RequestResources("Give me food");
                safetyLimit--;
            }
            AssertFalse(angel.CanInteract, "Should not be able to interact after max");
        }

        // -------------------------------------------------------------------------
        // Process Response
        // -------------------------------------------------------------------------
        [TestMethod("ProcessAngelResponse fires OnAngelResponse event")]
        private void Test_ProcessResponse_FiresEvent()
        {
            AngelResponseData received = null;
            Action<AngelResponseData> handler = r => received = r;
            AngelInteractionController.OnAngelResponse += handler;

            var response = new AngelResponseData
            {
                Message = "Test response",
                GrantedItems = new List<ResourceGrantData>()
            };
            angel.ProcessAngelResponse(response);

            AssertNotNull(received, "OnAngelResponse should fire");
            AssertEqual("Test response", received.Message, "Response message");

            AngelInteractionController.OnAngelResponse -= handler;
        }

        [TestMethod("ProcessAngelResponse adds granted items to inventory")]
        private void Test_ProcessResponse_AddsToInventory()
        {
            var inv = InventoryManager.Instance;
            if (inv == null) return;

            inv.ClearInventory();

            var response = new AngelResponseData
            {
                Message = "Resources approved",
                GrantedItems = new List<ResourceGrantData>
                {
                    new ResourceGrantData("test_food", 3),
                    new ResourceGrantData("test_meds", 1)
                }
            };
            angel.ProcessAngelResponse(response);

            AssertTrue(inv.HasItem("test_food", 3), "Should have 3 test_food");
            AssertTrue(inv.HasItem("test_meds", 1), "Should have 1 test_meds");

            inv.ClearInventory();
        }

        // -------------------------------------------------------------------------
        // Complete Phase
        // -------------------------------------------------------------------------
        [TestMethod("CompleteInteraction fires OnInteractionComplete")]
        private void Test_CompleteInteraction_FiresEvent()
        {
            bool fired = false;
            Action handler = () => fired = true;
            AngelInteractionController.OnInteractionComplete += handler;

            angel.CompleteInteraction();
            AssertTrue(fired, "OnInteractionComplete should fire");

            AngelInteractionController.OnInteractionComplete -= handler;
        }

        // -------------------------------------------------------------------------
        // Mood Enum Coverage
        // -------------------------------------------------------------------------
        [TestMethod("All AngelMood values can be set")]
        private void Test_AllMoods()
        {
            foreach (AngelMood mood in Enum.GetValues(typeof(AngelMood)))
            {
                // Set to a different mood first to ensure change
                angel.SetMood(mood == AngelMood.Neutral ? AngelMood.Cooperative : AngelMood.Neutral);
                angel.SetMood(mood);
                AssertEqual(mood, angel.CurrentMood, $"Mood should be {mood}");
            }
        }
    }
}
