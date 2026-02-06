using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for CityExplorationManager: sending characters, resolving expeditions,
    /// validation, risk factors, loot generation.
    /// </summary>
    public class CityExplorationManagerTester : BaseTester
    {
        public override string TesterName => "Exploration";

        private CityExplorationManager exploration;
        private FamilyManager fm;

        protected override void Setup()
        {
            exploration = CityExplorationManager.Instance;
            AssertNotNull(exploration, "CityExplorationManager.Instance");

            fm = FamilyManager.Instance;
            AssertNotNull(fm, "FamilyManager.Instance");

            fm.ClearFamily();
            exploration.BeginExplorationPhase();
        }

        protected override void TearDown()
        {
            fm.ClearFamily();
        }

        private ExplorationLocation MakeLocation(string name, ExplorationRisk risk)
        {
            return new ExplorationLocation
            {
                LocationName = name,
                Description = $"Test location ({risk})",
                Risk = risk,
                IsDiscovered = true
            };
        }

        // -------------------------------------------------------------------------
        // ExplorationLocation Data
        // -------------------------------------------------------------------------
        [TestMethod("ExplorationLocation defaults are correct")]
        private void Test_LocationDefaults()
        {
            var loc = new ExplorationLocation();
            AssertEqual("Unknown", loc.LocationName, "Default name");
            AssertEqual(ExplorationRisk.Medium, loc.Risk, "Default risk");
            AssertTrue(loc.IsDiscovered, "Default discovered");
        }

        // -------------------------------------------------------------------------
        // Sending Characters
        // -------------------------------------------------------------------------
        [TestMethod("SendCharacterToExplore succeeds for available character")]
        private void Test_SendCharacter_Success()
        {
            fm.AddCharacter("Scout", 80f, 80f, 80f, 80f);
            var scout = fm.GetCharacter("Scout");
            var location = MakeLocation("Store", ExplorationRisk.Low);

            bool result = exploration.SendCharacterToExplore(scout, location);
            AssertTrue(result, "Should return true");
            AssertTrue(scout.IsExploring, "Character should be exploring");
            AssertEqual(1, exploration.ActiveExpeditions.Count, "1 active expedition");
        }

        [TestMethod("SendCharacterToExplore fails for null character")]
        private void Test_SendCharacter_NullCharacter()
        {
            var location = MakeLocation("Store", ExplorationRisk.Low);
            bool result = exploration.SendCharacterToExplore(null, location);
            AssertFalse(result, "Should return false for null character");
        }

        [TestMethod("SendCharacterToExplore fails for null location")]
        private void Test_SendCharacter_NullLocation()
        {
            fm.AddCharacter("Scout");
            var scout = fm.GetCharacter("Scout");
            bool result = exploration.SendCharacterToExplore(scout, null);
            AssertFalse(result, "Should return false for null location");
        }

        [TestMethod("SendCharacterToExplore fails for character already exploring")]
        private void Test_SendCharacter_AlreadyExploring()
        {
            fm.AddCharacter("Scout");
            var scout = fm.GetCharacter("Scout");
            scout.IsExploring = true;
            var location = MakeLocation("Store", ExplorationRisk.Low);

            bool result = exploration.SendCharacterToExplore(scout, location);
            AssertFalse(result, "Should return false for exploring character");
        }

        [TestMethod("SendCharacterToExplore fails for injured character")]
        private void Test_SendCharacter_Injured()
        {
            fm.AddCharacter("Scout");
            var scout = fm.GetCharacter("Scout");
            scout.IsInjured = true;
            var location = MakeLocation("Store", ExplorationRisk.Low);

            bool result = exploration.SendCharacterToExplore(scout, location);
            AssertFalse(result, "Should return false for injured character");
        }

        [TestMethod("SendCharacterToExplore fails for dead character")]
        private void Test_SendCharacter_Dead()
        {
            fm.AddCharacter("Ghost", 0f, 0f, 0f, 0f);
            var ghost = fm.GetCharacter("Ghost");
            var location = MakeLocation("Store", ExplorationRisk.Low);

            bool result = exploration.SendCharacterToExplore(ghost, location);
            AssertFalse(result, "Should return false for dead character");
        }

        [TestMethod("SendCharacterToExplore fires OnCharacterSentOut event")]
        private void Test_SendCharacter_FiresEvent()
        {
            fm.AddCharacter("Scout");
            var scout = fm.GetCharacter("Scout");
            var location = MakeLocation("Store", ExplorationRisk.Low);

            CharacterData eventChar = null;
            ExplorationLocation eventLoc = null;
            System.Action<CharacterData, ExplorationLocation> handler = (c, l) =>
            {
                eventChar = c;
                eventLoc = l;
            };
            CityExplorationManager.OnCharacterSentOut += handler;

            exploration.SendCharacterToExplore(scout, location);

            AssertNotNull(eventChar, "Event character");
            AssertEqual("Scout", eventChar.Name, "Event character name");
            AssertNotNull(eventLoc, "Event location");

            CityExplorationManager.OnCharacterSentOut -= handler;
        }

        // -------------------------------------------------------------------------
        // Multiple Expeditions
        // -------------------------------------------------------------------------
        [TestMethod("Multiple characters can be sent on expeditions")]
        private void Test_MultipleExpeditions()
        {
            fm.AddCharacter("Scout1");
            fm.AddCharacter("Scout2");
            var location1 = MakeLocation("Store", ExplorationRisk.Low);
            var location2 = MakeLocation("Hospital", ExplorationRisk.High);

            exploration.SendCharacterToExplore(fm.GetCharacter("Scout1"), location1);
            exploration.SendCharacterToExplore(fm.GetCharacter("Scout2"), location2);

            AssertEqual(2, exploration.ActiveExpeditions.Count, "2 active expeditions");
        }

        // -------------------------------------------------------------------------
        // Begin Phase
        // -------------------------------------------------------------------------
        [TestMethod("BeginExplorationPhase clears active expeditions")]
        private void Test_BeginPhase_ClearsExpeditions()
        {
            fm.AddCharacter("Scout");
            var location = MakeLocation("Store", ExplorationRisk.Low);
            exploration.SendCharacterToExplore(fm.GetCharacter("Scout"), location);

            exploration.BeginExplorationPhase();
            AssertEqual(0, exploration.ActiveExpeditions.Count, "Should be cleared");
        }

        // -------------------------------------------------------------------------
        // Resolve Expeditions
        // -------------------------------------------------------------------------
        [TestMethod("ResolveExpeditions marks expeditions as complete")]
        private void Test_ResolveExpeditions()
        {
            fm.AddCharacter("Scout");
            var location = MakeLocation("Store", ExplorationRisk.Low);
            exploration.SendCharacterToExplore(fm.GetCharacter("Scout"), location);

            exploration.ResolveExpeditions();

            AssertTrue(exploration.ActiveExpeditions[0].IsComplete, "Expedition should be complete");
            AssertNotNull(exploration.ActiveExpeditions[0].Result, "Result should be set");

            // Character should no longer be exploring
            AssertFalse(fm.GetCharacter("Scout").IsExploring, "Should no longer be exploring");
        }

        [TestMethod("ResolveExpeditions fires OnExplorationComplete for each expedition")]
        private void Test_ResolveExpeditions_FiresEvents()
        {
            fm.AddCharacter("Scout1");
            fm.AddCharacter("Scout2");
            var location = MakeLocation("Store", ExplorationRisk.Low);
            exploration.SendCharacterToExplore(fm.GetCharacter("Scout1"), location);
            exploration.SendCharacterToExplore(fm.GetCharacter("Scout2"), location);

            int eventCount = 0;
            System.Action<ExplorationResult> handler = r => eventCount++;
            CityExplorationManager.OnExplorationComplete += handler;

            exploration.ResolveExpeditions();
            AssertEqual(2, eventCount, "Should fire 2 events");

            CityExplorationManager.OnExplorationComplete -= handler;
        }

        // -------------------------------------------------------------------------
        // Exploration Result Data
        // -------------------------------------------------------------------------
        [TestMethod("ExplorationResult has explorer name and location name")]
        private void Test_Result_HasNames()
        {
            fm.AddCharacter("Scout");
            var location = MakeLocation("Warehouse", ExplorationRisk.Medium);
            exploration.SendCharacterToExplore(fm.GetCharacter("Scout"), location);
            exploration.ResolveExpeditions();

            var result = exploration.ActiveExpeditions[0].Result;
            AssertEqual("Scout", result.ExplorerName, "ExplorerName");
            AssertEqual("Warehouse", result.LocationName, "LocationName");
            AssertTrue(!string.IsNullOrEmpty(result.NarrativeLog), "NarrativeLog should not be empty");
        }

        [TestMethod("ExplorationResult FoundItems list is always initialized")]
        private void Test_Result_FoundItemsInitialized()
        {
            fm.AddCharacter("Scout");
            var location = MakeLocation("Store", ExplorationRisk.Low);
            exploration.SendCharacterToExplore(fm.GetCharacter("Scout"), location);
            exploration.ResolveExpeditions();

            var result = exploration.ActiveExpeditions[0].Result;
            AssertNotNull(result.FoundItems, "FoundItems should not be null");
        }

        // -------------------------------------------------------------------------
        // Complete Phase
        // -------------------------------------------------------------------------
        [TestMethod("CompleteExplorationPhase fires OnExplorationPhaseComplete")]
        private void Test_CompletePhase_FiresEvent()
        {
            bool fired = false;
            System.Action handler = () => fired = true;
            CityExplorationManager.OnExplorationPhaseComplete += handler;

            exploration.CompleteExplorationPhase();
            AssertTrue(fired, "OnExplorationPhaseComplete should fire");

            CityExplorationManager.OnExplorationPhaseComplete -= handler;
        }
    }
}
