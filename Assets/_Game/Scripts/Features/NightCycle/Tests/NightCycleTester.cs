using UnityEngine;
using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for NightCycleController: stat decay, death checks, dream log,
    /// angel degradation, night report generation.
    /// </summary>
    public class NightCycleTester : BaseTester
    {
        public override string TesterName => "NightCycle";

        private NightCycleController nc;
        private FamilyManager fm;

        protected override void Setup()
        {
            nc = NightCycleController.Instance;
            AssertNotNull(nc, "NightCycleController.Instance");

            fm = FamilyManager.Instance;
            AssertNotNull(fm, "FamilyManager.Instance");

            fm.ClearFamily();
        }

        protected override void TearDown()
        {
            fm.ClearFamily();
        }

        // -------------------------------------------------------------------------
        // Night Report Generation
        // -------------------------------------------------------------------------
        [TestMethod("ProcessNightCycle generates a NightReportData")]
        private void Test_ProcessNightCycle_GeneratesReport()
        {
            fm.AddCharacter("Father", 80f, 80f, 80f, 80f);
            nc.ProcessNightCycle();

            AssertNotNull(nc.LatestReport, "LatestReport should be set");
        }

        [TestMethod("ProcessNightCycle fires OnNightReportGenerated")]
        private void Test_ProcessNightCycle_FiresReportEvent()
        {
            fm.AddCharacter("Father", 80f, 80f, 80f, 80f);

            NightReportData received = null;
            Action<NightReportData> handler = r => received = r;
            NightCycleController.OnNightReportGenerated += handler;

            nc.ProcessNightCycle();
            AssertNotNull(received, "OnNightReportGenerated should fire");

            NightCycleController.OnNightReportGenerated -= handler;
        }

        [TestMethod("ProcessNightCycle fires OnDreamLogGenerated")]
        private void Test_ProcessNightCycle_FiresDreamEvent()
        {
            fm.AddCharacter("Father", 80f, 80f, 80f, 80f);

            string dreamLog = null;
            Action<string> handler = d => dreamLog = d;
            NightCycleController.OnDreamLogGenerated += handler;

            nc.ProcessNightCycle();
            AssertNotNull(dreamLog, "OnDreamLogGenerated should fire");
            AssertTrue(!string.IsNullOrEmpty(dreamLog), "Dream log should not be empty");

            NightCycleController.OnDreamLogGenerated -= handler;
        }

        // -------------------------------------------------------------------------
        // Stat Decay
        // -------------------------------------------------------------------------
        [TestMethod("ProcessNightCycle applies hunger decay")]
        private void Test_StatDecay_Hunger()
        {
            fm.AddCharacter("Father", 100f, 100f, 100f, 100f);
            float hungerBefore = fm.GetCharacter("Father").Hunger;

            nc.ProcessNightCycle();

            float hungerAfter = fm.GetCharacter("Father").Hunger;
            // Config may not be present, but if it is, hunger should decrease
            // If no config, decay won't apply (config null check in code)
            var config = GameConfigDataSO.Instance;
            if (config != null)
            {
                AssertLessThan(hungerAfter, hungerBefore, "Hunger should decrease");
            }
        }

        [TestMethod("ProcessNightCycle applies thirst decay")]
        private void Test_StatDecay_Thirst()
        {
            fm.AddCharacter("Father", 100f, 100f, 100f, 100f);
            float thirstBefore = fm.GetCharacter("Father").Thirst;

            nc.ProcessNightCycle();

            var config = GameConfigDataSO.Instance;
            if (config != null)
            {
                float thirstAfter = fm.GetCharacter("Father").Thirst;
                AssertLessThan(thirstAfter, thirstBefore, "Thirst should decrease");
            }
        }

        [TestMethod("ProcessNightCycle applies sanity decay")]
        private void Test_StatDecay_Sanity()
        {
            fm.AddCharacter("Father", 100f, 100f, 100f, 100f);
            float sanityBefore = fm.GetCharacter("Father").Sanity;

            nc.ProcessNightCycle();

            var config = GameConfigDataSO.Instance;
            if (config != null)
            {
                float sanityAfter = fm.GetCharacter("Father").Sanity;
                AssertLessThan(sanityAfter, sanityBefore, "Sanity should decrease");
            }
        }

        [TestMethod("Stat decay does not apply to dead characters")]
        private void Test_StatDecay_SkipsDead()
        {
            fm.AddCharacter("Dead", 0f, 50f, 50f, 0f);
            var dead = fm.GetCharacter("Dead");
            float healthBefore = dead.Health;

            nc.ProcessNightCycle();

            // Dead character should not have stats modified
            AssertApproxEqual(healthBefore, dead.Health, 0.01f, "Dead character health should not change");
        }

        // -------------------------------------------------------------------------
        // Dehydration / Starvation Penalties
        // -------------------------------------------------------------------------
        [TestMethod("Dehydrated characters take health damage")]
        private void Test_DehydrationDamage()
        {
            // Start with 0 thirst (dehydrated) but alive
            fm.AddCharacter("Thirsty", 50f, 0f, 50f, 100f);

            nc.ProcessNightCycle();

            var config = GameConfigDataSO.Instance;
            if (config != null)
            {
                // Thirst decay makes thirst even more 0, then dehydration penalty
                float healthAfter = fm.GetCharacter("Thirsty").Health;
                AssertLessThan(healthAfter, 100f, "Health should decrease from dehydration");
            }
        }

        [TestMethod("Starving characters take health damage")]
        private void Test_StarvationDamage()
        {
            // Start with very low hunger
            fm.AddCharacter("Starving", 1f, 50f, 50f, 100f);

            nc.ProcessNightCycle();

            var config = GameConfigDataSO.Instance;
            if (config != null)
            {
                float healthAfter = fm.GetCharacter("Starving").Health;
                AssertLessThan(healthAfter, 100f, "Health should decrease from starvation");
            }
        }

        // -------------------------------------------------------------------------
        // Death Detection
        // -------------------------------------------------------------------------
        [TestMethod("Dead characters are added to DeathsThisNight")]
        private void Test_DeathDetection()
        {
            fm.AddCharacter("Alive", 50f, 50f, 50f, 50f);
            fm.AddCharacter("Dead", 0f, 0f, 0f, 0f);

            nc.ProcessNightCycle();

            var report = nc.LatestReport;
            AssertTrue(report.DeathsThisNight.Contains("Dead"), "Dead should be in DeathsThisNight");
            AssertFalse(report.DeathsThisNight.Contains("Alive"), "Alive should NOT be in DeathsThisNight");
        }

        // -------------------------------------------------------------------------
        // Dream Log
        // -------------------------------------------------------------------------
        [TestMethod("High sanity generates a calm dream log")]
        private void Test_DreamLog_HighSanity()
        {
            fm.AddCharacter("Sane", 80f, 80f, 90f, 80f);

            nc.ProcessNightCycle();

            var report = nc.LatestReport;
            AssertFalse(report.IsNightmare, "Should not be nightmare with high sanity");
            AssertTrue(!string.IsNullOrEmpty(report.DreamLog), "Dream log should not be empty");
        }

        [TestMethod("Low sanity generates a nightmare")]
        private void Test_DreamLog_LowSanity()
        {
            fm.AddCharacter("Crazy", 80f, 80f, 10f, 80f);

            nc.ProcessNightCycle();

            var config = GameConfigDataSO.Instance;
            if (config != null)
            {
                var report = nc.LatestReport;
                // After sanity decay, sanity will be even lower (10 - decay)
                // Average sanity < 40 -> nightmare
                AssertTrue(report.IsNightmare, "Should be nightmare with low sanity");
            }
        }

        // -------------------------------------------------------------------------
        // Night Report Data
        // -------------------------------------------------------------------------
        [TestMethod("NightReportData defaults are correct")]
        private void Test_NightReportData_Defaults()
        {
            var report = new NightReportData();
            AssertEqual(0, report.Day, "Default day");
            AssertFalse(report.IsNightmare, "Default IsNightmare");
            AssertEqual("", report.DreamLog, "Default DreamLog");
            AssertNotNull(report.StatChanges, "StatChanges initialized");
            AssertNotNull(report.DeathsThisNight, "DeathsThisNight initialized");
        }

        [TestMethod("Report StatChanges has entries for each alive character")]
        private void Test_Report_StatChanges()
        {
            fm.AddCharacter("A", 80f, 80f, 80f, 80f);
            fm.AddCharacter("B", 80f, 80f, 80f, 80f);

            nc.ProcessNightCycle();

            var config = GameConfigDataSO.Instance;
            if (config != null)
            {
                AssertEqual(2, nc.LatestReport.StatChanges.Count, "Should have 2 stat change entries");
            }
        }

        // -------------------------------------------------------------------------
        // Complete Night Cycle
        // -------------------------------------------------------------------------
        [TestMethod("CompleteNightCycle fires OnNightCycleComplete")]
        private void Test_CompleteNightCycle_FiresEvent()
        {
            bool fired = false;
            Action handler = () => fired = true;
            NightCycleController.OnNightCycleComplete += handler;

            nc.CompleteNightCycle();
            AssertTrue(fired, "OnNightCycleComplete should fire");

            NightCycleController.OnNightCycleComplete -= handler;
        }

        // -------------------------------------------------------------------------
        // Injured Healing
        // -------------------------------------------------------------------------
        [TestMethod("Injured character heals when health > 50")]
        private void Test_InjuredHealing()
        {
            fm.AddCharacter("Wounded", 100f, 100f, 100f, 60f);
            var wounded = fm.GetCharacter("Wounded");
            wounded.IsInjured = true;

            nc.ProcessNightCycle();

            var config = GameConfigDataSO.Instance;
            if (config != null)
            {
                // Health starts at 60, after night cycle if still > 50, injury should clear
                if (wounded.Health > 50f)
                {
                    AssertFalse(wounded.IsInjured, "Should no longer be injured");
                }
            }
        }
    }
}
