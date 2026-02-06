using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for StatusReviewController: report generation, warnings,
    /// critical conditions, event firing.
    /// </summary>
    public class StatusReviewTester : BaseTester
    {
        public override string TesterName => "StatusReview";

        private StatusReviewController sr;
        private FamilyManager fm;

        protected override void Setup()
        {
            sr = StatusReviewController.Instance;
            AssertNotNull(sr, "StatusReviewController.Instance");

            fm = FamilyManager.Instance;
            AssertNotNull(fm, "FamilyManager.Instance");

            fm.ClearFamily();
        }

        protected override void TearDown()
        {
            fm.ClearFamily();
        }

        // -------------------------------------------------------------------------
        // Report Generation
        // -------------------------------------------------------------------------
        [TestMethod("GenerateStatusReport creates a report")]
        private void Test_GenerateReport()
        {
            fm.AddCharacter("Father", 80f, 80f, 80f, 80f);
            sr.GenerateStatusReport();

            AssertNotNull(sr.LatestReport, "LatestReport should be set");
        }

        [TestMethod("GenerateStatusReport fires OnStatusReportGenerated")]
        private void Test_GenerateReport_FiresEvent()
        {
            fm.AddCharacter("Father", 80f, 80f, 80f, 80f);

            StatusReportData received = null;
            Action<StatusReportData> handler = r => received = r;
            StatusReviewController.OnStatusReportGenerated += handler;

            sr.GenerateStatusReport();
            AssertNotNull(received, "Event should fire");

            StatusReviewController.OnStatusReportGenerated -= handler;
        }

        [TestMethod("Report has correct alive and total counts")]
        private void Test_Report_Counts()
        {
            fm.AddCharacter("Alive1", 50f, 50f, 50f, 50f);
            fm.AddCharacter("Alive2", 50f, 50f, 50f, 50f);
            fm.AddCharacter("Dead", 0f, 0f, 0f, 0f);

            sr.GenerateStatusReport();

            var report = sr.LatestReport;
            AssertEqual(2, report.AliveCount, "AliveCount");
            AssertEqual(3, report.TotalCount, "TotalCount");
        }

        [TestMethod("Report has CharacterStatusData for each family member")]
        private void Test_Report_CharacterStatuses()
        {
            fm.AddCharacter("Father", 80f, 70f, 60f, 50f);
            fm.AddCharacter("Mother", 90f, 85f, 75f, 95f);

            sr.GenerateStatusReport();

            var report = sr.LatestReport;
            AssertEqual(2, report.CharacterStatuses.Count, "Should have 2 statuses");

            var fatherStatus = report.CharacterStatuses.Find(s => s.CharacterName == "Father");
            AssertNotNull(fatherStatus, "Father status");
            AssertApproxEqual(80f, fatherStatus.Hunger, 0.1f, "Father Hunger");
            AssertApproxEqual(70f, fatherStatus.Thirst, 0.1f, "Father Thirst");
            AssertApproxEqual(60f, fatherStatus.Sanity, 0.1f, "Father Sanity");
            AssertApproxEqual(50f, fatherStatus.Health, 0.1f, "Father Health");
        }

        // -------------------------------------------------------------------------
        // Warnings
        // -------------------------------------------------------------------------
        [TestMethod("No warnings for healthy characters")]
        private void Test_NoWarnings_Healthy()
        {
            fm.AddCharacter("Healthy", 80f, 80f, 80f, 80f);
            sr.GenerateStatusReport();
            AssertEqual(0, sr.LatestReport.Warnings.Count, "No warnings for healthy character");
        }

        [TestMethod("Warning generated for dead character")]
        private void Test_Warning_Dead()
        {
            fm.AddCharacter("Dead", 0f, 0f, 0f, 0f);
            sr.GenerateStatusReport();

            bool hasDeathWarning = false;
            foreach (var w in sr.LatestReport.Warnings)
            {
                if (w.Contains("Dead") && w.Contains("died"))
                    hasDeathWarning = true;
            }
            AssertTrue(hasDeathWarning, "Should have death warning");
        }

        [TestMethod("Warning generated for critical character")]
        private void Test_Warning_Critical()
        {
            fm.AddCharacter("Critical", 10f, 50f, 50f, 50f);
            sr.GenerateStatusReport();

            bool hasCriticalWarning = false;
            foreach (var w in sr.LatestReport.Warnings)
            {
                if (w.Contains("Critical") && w.Contains("critical"))
                    hasCriticalWarning = true;
            }
            AssertTrue(hasCriticalWarning, "Should have critical warning");
        }

        [TestMethod("Warning generated for insane character")]
        private void Test_Warning_Insane()
        {
            fm.AddCharacter("Crazy", 50f, 50f, 0f, 50f);
            sr.GenerateStatusReport();

            bool hasInsaneWarning = false;
            foreach (var w in sr.LatestReport.Warnings)
            {
                if (w.Contains("Crazy") && w.Contains("mind"))
                    hasInsaneWarning = true;
            }
            AssertTrue(hasInsaneWarning, "Should have insanity warning");
        }

        [TestMethod("Warning generated for dehydrated character")]
        private void Test_Warning_Dehydrated()
        {
            fm.AddCharacter("Dry", 50f, 0f, 50f, 50f);
            sr.GenerateStatusReport();

            bool hasDehydrationWarning = false;
            foreach (var w in sr.LatestReport.Warnings)
            {
                if (w.Contains("Dry") && w.Contains("dehydrated"))
                    hasDehydrationWarning = true;
            }
            AssertTrue(hasDehydrationWarning, "Should have dehydration warning");
        }

        [TestMethod("OnCriticalWarning fires for critical characters")]
        private void Test_CriticalWarning_Event()
        {
            fm.AddCharacter("Critical", 5f, 5f, 5f, 15f);

            CharacterData warningChar = null;
            string warningMsg = null;
            Action<CharacterData, string> handler = (c, m) =>
            {
                warningChar = c;
                warningMsg = m;
            };
            StatusReviewController.OnCriticalWarning += handler;

            sr.GenerateStatusReport();
            AssertNotNull(warningChar, "OnCriticalWarning should fire");

            StatusReviewController.OnCriticalWarning -= handler;
        }

        [TestMethod("Multiple warnings for character with multiple conditions")]
        private void Test_MultipleWarnings()
        {
            // Character is both insane (Sanity=0) and dehydrated (Thirst=0) and critical
            fm.AddCharacter("Doomed", 5f, 0f, 0f, 15f);
            sr.GenerateStatusReport();

            // Should have at least 3 warnings: critical, insane, dehydrated
            AssertGreaterThan(sr.LatestReport.Warnings.Count, 2, "Should have multiple warnings");
        }

        // -------------------------------------------------------------------------
        // StatusReportData / CharacterStatusData
        // -------------------------------------------------------------------------
        [TestMethod("StatusReportData can be constructed")]
        private void Test_StatusReportData_Construct()
        {
            var report = new StatusReportData
            {
                Day = 5,
                AliveCount = 3,
                TotalCount = 4,
                CharacterStatuses = new List<CharacterStatusData>(),
                Warnings = new List<string>()
            };
            AssertEqual(5, report.Day, "Day");
            AssertEqual(3, report.AliveCount, "AliveCount");
        }

        [TestMethod("CharacterStatusData stores correct values")]
        private void Test_CharacterStatusData()
        {
            var status = new CharacterStatusData
            {
                CharacterName = "Test",
                Hunger = 75f,
                Thirst = 60f,
                Sanity = 80f,
                Health = 90f,
                IsAlive = true,
                IsCritical = false
            };
            AssertEqual("Test", status.CharacterName, "Name");
            AssertApproxEqual(75f, status.Hunger, 0.01f, "Hunger");
            AssertTrue(status.IsAlive, "IsAlive");
            AssertFalse(status.IsCritical, "IsCritical");
        }

        // -------------------------------------------------------------------------
        // Complete Phase
        // -------------------------------------------------------------------------
        [TestMethod("CompleteStatusReview fires OnStatusReviewComplete")]
        private void Test_CompletePhase_FiresEvent()
        {
            bool fired = false;
            Action handler = () => fired = true;
            StatusReviewController.OnStatusReviewComplete += handler;

            sr.CompleteStatusReview();
            AssertTrue(fired, "OnStatusReviewComplete should fire");

            StatusReviewController.OnStatusReviewComplete -= handler;
        }
    }
}
