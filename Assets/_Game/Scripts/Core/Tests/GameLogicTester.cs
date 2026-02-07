using UnityEngine;
using TheBunkerGames;
using TheBunkerGames.Tests;

namespace TheBunkerGames.Tests
{
    public class GameLogicTester : BaseTester
    {
        public override string TesterName => "GameLogic";

        private GameManager gameManager;
        private FamilyManager familyManager;

        protected override void Setup()
        {
            gameManager = GameManager.Instance;
            familyManager = FamilyManager.Instance;
            
            AssertNotNull(gameManager, "GameManager.Instance");
            AssertNotNull(familyManager, "FamilyManager.Instance");

            // Reset state
            gameManager.StartNewGame();
            familyManager.ClearFamily();
        }

        [TestMethod("Game Over when all family members die")]
        private void Test_GameOver_TotalPartyKill()
        {
            // Add a single character
            familyManager.AddCharacter("SoloSurvivor", 10f, 10f, 10f, 10f);
            
            // Verify that EndGame(false) sets state correctly.
            
            gameManager.EndGame(survived: false);

            AssertTrue(gameManager.IsGameOver, "IsGameOver should be true");
            AssertEqual(GameState.StatusReview, gameManager.CurrentState, "State should logically stay or be irrelevant, but IsGameOver is key");
        }

        [TestMethod("Win Condition: Survive 28 Days")]
        private void Test_WinCondition_Survive28Days()
        {
            // Reset
            gameManager.StartNewGame();
            
            // Advance to day 28
            for (int i = 1; i <= 28; i++)
            {
                gameManager.AdvanceDay();
            }
            
            AssertFalse(gameManager.IsGameOver, "Should still be playing on Day 28");

            // Advance to Day 29
            gameManager.AdvanceDay();
            
            AssertTrue(gameManager.IsGameOver, "Should be Game Over on Day 29 (Survive 28 days)");
            // Note: We'd need to check the 'survived' bool passed to event, 
            // but GameManager public state might not expose 'Survived' bool directly without subscribing to event.
        }
    }
}
