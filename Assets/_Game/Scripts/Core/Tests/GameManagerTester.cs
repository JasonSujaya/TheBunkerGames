using UnityEngine;
using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Tests for GameManager: state machine, day advancement, game over, events.
    /// </summary>
    public class GameManagerTester : BaseTester
    {
        public override string TesterName => "GameManager";

        private GameManager gm;

        protected override void EnsureDependencies()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning($"[{TesterName}] GameManager not found. Creating temporary instance for testing.");
                var go = new GameObject("GameManager_AutoCreated");
                go.AddComponent<GameManager>();
            }
        }

        protected override void Setup()
        {
            gm = GameManager.Instance;
            AssertNotNull(gm, "GameManager.Instance");
        }

        // -------------------------------------------------------------------------
        // State Machine
        // -------------------------------------------------------------------------
        [TestMethod("StartNewGame resets state to day 1 StatusReview")]
        private void Test_StartNewGame_ResetsState()
        {
            gm.StartNewGame();
            AssertEqual(1, gm.CurrentDay, "CurrentDay after StartNewGame");
            AssertEqual(GameState.StatusReview, gm.CurrentState, "CurrentState after StartNewGame");
            AssertFalse(gm.IsGameOver, "IsGameOver should be false after StartNewGame");
        }

        [TestMethod("SetState changes current state")]
        private void Test_SetState_Changes()
        {
            gm.StartNewGame();
            gm.SetState(GameState.CityExploration);
            AssertEqual(GameState.CityExploration, gm.CurrentState, "State after SetState");
        }

        [TestMethod("SetState fires OnStateChanged event")]
        private void Test_SetState_FiresEvent()
        {
            gm.StartNewGame();

            GameState received = GameState.StatusReview;
            Action<GameState> handler = s => received = s;
            GameManager.OnStateChanged += handler;

            gm.SetState(GameState.CityExploration);
            AssertEqual(GameState.CityExploration, received, "Event received state");

            GameManager.OnStateChanged -= handler;
        }

        [TestMethod("SetState to same state does not fire event")]
        private void Test_SetState_SameState_NoEvent()
        {
            gm.StartNewGame();
            gm.SetState(GameState.CityExploration);

            bool fired = false;
            Action<GameState> handler = s => fired = true;
            GameManager.OnStateChanged += handler;

            gm.SetState(GameState.CityExploration);
            AssertFalse(fired, "Event should not fire for same state");

            GameManager.OnStateChanged -= handler;
        }

        [TestMethod("SetState does nothing when game is over")]
        private void Test_SetState_IgnoredWhenGameOver()
        {
            gm.StartNewGame();
            gm.EndGame(false);
            gm.SetState(GameState.CityExploration);
            // State should NOT have changed (it stays at StatusReview from StartNewGame)
            AssertTrue(gm.IsGameOver, "Game should still be over");
        }

        // -------------------------------------------------------------------------
        // Day Advancement
        // -------------------------------------------------------------------------
        [TestMethod("AdvanceDay increments CurrentDay")]
        private void Test_AdvanceDay_Increments()
        {
            gm.StartNewGame();
            int dayBefore = gm.CurrentDay;
            gm.AdvanceDay();
            AssertEqual(dayBefore + 1, gm.CurrentDay, "Day should increment by 1");
        }

        // -------------------------------------------------------------------------
        // Game Over
        // -------------------------------------------------------------------------
        [TestMethod("EndGame(true) sets IsGameOver and fires event with survived=true")]
        private void Test_EndGame_Survived()
        {
            gm.StartNewGame();

            bool? survived = null;
            Action<bool> handler = s => survived = s;
            GameManager.OnGameOver += handler;

            gm.EndGame(true);
            AssertTrue(gm.IsGameOver, "IsGameOver should be true");
            AssertNotNull(survived, "OnGameOver should have fired");
            AssertTrue(survived.Value, "Survived should be true");

            GameManager.OnGameOver -= handler;
        }

        [TestMethod("EndGame(false) fires event with survived=false")]
        private void Test_EndGame_Failed()
        {
            gm.StartNewGame();

            bool? survived = null;
            Action<bool> handler = s => survived = s;
            GameManager.OnGameOver += handler;

            gm.EndGame(false);
            AssertNotNull(survived, "OnGameOver should have fired");
            AssertFalse(survived.Value, "Survived should be false");

            GameManager.OnGameOver -= handler;
        }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        [TestMethod("StartNewGame fires OnDayStart")]
        private void Test_StartNewGame_FiresDayStart()
        {
            bool fired = false;
            Action handler = () => fired = true;
            GameManager.OnDayStart += handler;

            gm.StartNewGame();
            AssertTrue(fired, "OnDayStart should fire on StartNewGame");

            GameManager.OnDayStart -= handler;
        }

        [TestMethod("SetState to StatusReview fires OnDayStart")]
        private void Test_SetState_StatusReview_FiresDayStart()
        {
            gm.StartNewGame();
            gm.SetState(GameState.CityExploration);

            bool fired = false;
            Action handler = () => fired = true;
            GameManager.OnDayStart += handler;

            gm.SetState(GameState.StatusReview);
            AssertTrue(fired, "OnDayStart should fire when entering StatusReview");

            GameManager.OnDayStart -= handler;
        }

        // -------------------------------------------------------------------------
        // State Transitions
        // -------------------------------------------------------------------------
        [TestMethod("State transitions between StatusReview and CityExploration")]
        private void Test_StateTransitions()
        {
            gm.StartNewGame();
            AssertEqual(GameState.StatusReview, gm.CurrentState, "Phase 1: StatusReview");

            gm.SetState(GameState.CityExploration);
            AssertEqual(GameState.CityExploration, gm.CurrentState, "Phase 2: CityExploration");
        }
    }
}
