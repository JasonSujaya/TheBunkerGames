using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for DailyChoiceController: dilemma presentation, choice making,
    /// stat effects, voting system, edge cases.
    /// </summary>
    public class DailyChoiceTester : BaseTester
    {
        public override string TesterName => "DailyChoice";

        private DailyChoiceController dc;
        private FamilyManager fm;

        protected override void Setup()
        {
            dc = DailyChoiceController.Instance;
            AssertNotNull(dc, "DailyChoiceController.Instance");

            fm = FamilyManager.Instance;
            AssertNotNull(fm, "FamilyManager.Instance");

            fm.ClearFamily();
            fm.AddCharacter("Father", 80f, 80f, 80f, 80f);
            fm.AddCharacter("Mother", 80f, 80f, 80f, 80f);
        }

        protected override void TearDown()
        {
            fm.ClearFamily();
            dc.CompleteChoicePhase();
        }

        private DilemmaData CreateTestDilemma()
        {
            return new DilemmaData
            {
                Title = "Test Dilemma",
                Description = "A test choice.",
                Options = new List<DilemmaOptionData>
                {
                    new DilemmaOptionData
                    {
                        Label = "Option A",
                        Description = "First option",
                        OutcomeDescription = "Outcome A",
                        ExpectedOutcome = ChoiceOutcome.Positive,
                        StatEffects = new List<StatEffectData>
                        {
                            new StatEffectData { HungerChange = 10f, ThirstChange = 5f }
                        }
                    },
                    new DilemmaOptionData
                    {
                        Label = "Option B",
                        Description = "Second option",
                        OutcomeDescription = "Outcome B",
                        ExpectedOutcome = ChoiceOutcome.Negative,
                        StatEffects = new List<StatEffectData>
                        {
                            new StatEffectData { SanityChange = -20f, HealthChange = -10f }
                        }
                    }
                }
            };
        }

        // -------------------------------------------------------------------------
        // DilemmaData Class
        // -------------------------------------------------------------------------
        [TestMethod("DilemmaData has correct defaults")]
        private void Test_DilemmaData_Defaults()
        {
            var d = new DilemmaData();
            AssertEqual("", d.Title, "Default title");
            AssertNotNull(d.Options, "Options list should be initialized");
        }

        [TestMethod("DilemmaOptionData has correct defaults")]
        private void Test_DilemmaOptionData_Defaults()
        {
            var o = new DilemmaOptionData();
            AssertEqual("", o.Label, "Default label");
            AssertEqual(0, o.VoteCount, "Default vote count");
            AssertNotNull(o.StatEffects, "StatEffects should be initialized");
        }

        [TestMethod("StatEffectData has correct defaults")]
        private void Test_StatEffectData_Defaults()
        {
            var e = new StatEffectData();
            AssertEqual("", e.TargetCharacterName, "Default target");
            AssertApproxEqual(0f, e.HungerChange, 0.01f, "Default HungerChange");
            AssertApproxEqual(0f, e.ThirstChange, 0.01f, "Default ThirstChange");
            AssertApproxEqual(0f, e.SanityChange, 0.01f, "Default SanityChange");
            AssertApproxEqual(0f, e.HealthChange, 0.01f, "Default HealthChange");
        }

        // -------------------------------------------------------------------------
        // Presenting Dilemmas
        // -------------------------------------------------------------------------
        [TestMethod("BeginChoicePhase generates a mock dilemma")]
        private void Test_BeginChoicePhase()
        {
            dc.BeginChoicePhase();
            AssertNotNull(dc.CurrentDilemma, "CurrentDilemma should be set");
            AssertTrue(!string.IsNullOrEmpty(dc.CurrentDilemma.Title), "Title should not be empty");
            AssertGreaterThan(dc.CurrentDilemma.Options.Count, 0, "Options count");
        }

        [TestMethod("PresentDilemma sets current dilemma")]
        private void Test_PresentDilemma()
        {
            var dilemma = CreateTestDilemma();
            dc.PresentDilemma(dilemma);
            AssertEqual("Test Dilemma", dc.CurrentDilemma.Title, "Title");
            AssertEqual(2, dc.CurrentDilemma.Options.Count, "Options count");
        }

        [TestMethod("PresentDilemma fires OnDilemmaPresented event")]
        private void Test_PresentDilemma_FiresEvent()
        {
            DilemmaData received = null;
            System.Action<DilemmaData> handler = d => received = d;
            DailyChoiceController.OnDilemmaPresented += handler;

            dc.PresentDilemma(CreateTestDilemma());
            AssertNotNull(received, "Event should fire");
            AssertEqual("Test Dilemma", received.Title, "Event dilemma title");

            DailyChoiceController.OnDilemmaPresented -= handler;
        }

        // -------------------------------------------------------------------------
        // Making Choices
        // -------------------------------------------------------------------------
        [TestMethod("MakeChoice fires OnChoiceMade with correct option")]
        private void Test_MakeChoice_FiresEvent()
        {
            dc.PresentDilemma(CreateTestDilemma());

            DilemmaOptionData chosenOption = null;
            DilemmaOutcomeData outcome = null;
            System.Action<DilemmaOptionData, DilemmaOutcomeData> handler = (o, out_) =>
            {
                chosenOption = o;
                outcome = out_;
            };
            DailyChoiceController.OnChoiceMade += handler;

            dc.MakeChoice(0);
            AssertNotNull(chosenOption, "Chosen option");
            AssertEqual("Option A", chosenOption.Label, "Chosen label");
            AssertNotNull(outcome, "Outcome");
            AssertEqual(ChoiceOutcome.Positive, outcome.OutcomeType, "Outcome type");

            DailyChoiceController.OnChoiceMade -= handler;
        }

        [TestMethod("MakeChoice applies stat effects to all family members (no target)")]
        private void Test_MakeChoice_AppliesStats()
        {
            dc.PresentDilemma(CreateTestDilemma());

            float fatherHungerBefore = fm.GetCharacter("Father").Hunger;
            float motherHungerBefore = fm.GetCharacter("Mother").Hunger;

            dc.MakeChoice(0);  // Option A: +10 Hunger, +5 Thirst

            AssertApproxEqual(fatherHungerBefore + 10f, fm.GetCharacter("Father").Hunger, 0.1f, "Father Hunger");
            AssertApproxEqual(motherHungerBefore + 10f, fm.GetCharacter("Mother").Hunger, 0.1f, "Mother Hunger");
        }

        [TestMethod("MakeChoice applies negative stats correctly")]
        private void Test_MakeChoice_NegativeStats()
        {
            dc.PresentDilemma(CreateTestDilemma());

            float fatherSanityBefore = fm.GetCharacter("Father").Sanity;

            dc.MakeChoice(1);  // Option B: -20 Sanity, -10 Health

            AssertApproxEqual(fatherSanityBefore - 20f, fm.GetCharacter("Father").Sanity, 0.1f, "Father Sanity");
        }

        [TestMethod("MakeChoice with targeted stat effect")]
        private void Test_MakeChoice_TargetedEffect()
        {
            var dilemma = new DilemmaData
            {
                Title = "Targeted",
                Options = new List<DilemmaOptionData>
                {
                    new DilemmaOptionData
                    {
                        Label = "Target Father",
                        OutcomeDescription = "Father suffers",
                        ExpectedOutcome = ChoiceOutcome.Negative,
                        StatEffects = new List<StatEffectData>
                        {
                            new StatEffectData { TargetCharacterName = "Father", HealthChange = -30f }
                        }
                    }
                }
            };
            dc.PresentDilemma(dilemma);

            float fatherHealthBefore = fm.GetCharacter("Father").Health;
            float motherHealthBefore = fm.GetCharacter("Mother").Health;

            dc.MakeChoice(0);

            AssertApproxEqual(fatherHealthBefore - 30f, fm.GetCharacter("Father").Health, 0.1f, "Father Health reduced");
            AssertApproxEqual(motherHealthBefore, fm.GetCharacter("Mother").Health, 0.1f, "Mother Health unchanged");
        }

        [TestMethod("MakeChoice does nothing for invalid index")]
        private void Test_MakeChoice_InvalidIndex()
        {
            dc.PresentDilemma(CreateTestDilemma());
            // Should not throw
            dc.MakeChoice(-1);
            dc.MakeChoice(99);
        }

        [TestMethod("MakeChoice does nothing when no dilemma is set")]
        private void Test_MakeChoice_NoDilemma()
        {
            dc.CompleteChoicePhase();  // Clears current dilemma
            dc.MakeChoice(0);  // Should not throw
        }

        // -------------------------------------------------------------------------
        // Voting
        // -------------------------------------------------------------------------
        [TestMethod("StartVoting initializes vote timer and resets counts")]
        private void Test_StartVoting()
        {
            dc.PresentDilemma(CreateTestDilemma());
            dc.StartVoting();

            AssertTrue(dc.IsVotingActive, "Voting should be active");
            AssertGreaterThan(dc.VoteTimer, 0f, "Timer should be positive");

            foreach (var opt in dc.CurrentDilemma.Options)
            {
                AssertEqual(0, opt.VoteCount, $"VoteCount for {opt.Label}");
            }
        }

        [TestMethod("CastVote increments vote count")]
        private void Test_CastVote()
        {
            dc.PresentDilemma(CreateTestDilemma());
            dc.StartVoting();

            dc.CastVote(0);
            dc.CastVote(0);
            dc.CastVote(1);

            AssertEqual(2, dc.CurrentDilemma.Options[0].VoteCount, "Option 0 votes");
            AssertEqual(1, dc.CurrentDilemma.Options[1].VoteCount, "Option 1 votes");
        }

        [TestMethod("CastVote fires OnVoteUpdated event")]
        private void Test_CastVote_FiresEvent()
        {
            dc.PresentDilemma(CreateTestDilemma());
            dc.StartVoting();

            DilemmaOptionData votedOption = null;
            System.Action<DilemmaOptionData, float> handler = (o, p) => votedOption = o;
            DailyChoiceController.OnVoteUpdated += handler;

            dc.CastVote(0);
            AssertNotNull(votedOption, "Event should fire");
            AssertEqual("Option A", votedOption.Label, "Voted option");

            DailyChoiceController.OnVoteUpdated -= handler;
        }

        [TestMethod("CastVote does nothing when voting not active")]
        private void Test_CastVote_NotActive()
        {
            dc.PresentDilemma(CreateTestDilemma());
            // Don't start voting
            dc.CastVote(0);
            AssertEqual(0, dc.CurrentDilemma.Options[0].VoteCount, "No votes should be cast");
        }

        [TestMethod("CastVote ignores invalid index")]
        private void Test_CastVote_InvalidIndex()
        {
            dc.PresentDilemma(CreateTestDilemma());
            dc.StartVoting();
            dc.CastVote(-1);
            dc.CastVote(99);
            // No exception expected
        }

        // -------------------------------------------------------------------------
        // Complete Phase
        // -------------------------------------------------------------------------
        [TestMethod("CompleteChoicePhase clears dilemma and fires event")]
        private void Test_CompletePhase()
        {
            dc.PresentDilemma(CreateTestDilemma());

            bool fired = false;
            System.Action handler = () => fired = true;
            DailyChoiceController.OnChoicePhaseComplete += handler;

            dc.CompleteChoicePhase();
            AssertNull(dc.CurrentDilemma, "Dilemma should be null after complete");
            AssertTrue(fired, "OnChoicePhaseComplete should fire");

            DailyChoiceController.OnChoicePhaseComplete -= handler;
        }
    }
}
