using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Tracks which player action categories are active on a given day,
    /// what challenges were drawn, and the player's inputs.
    /// </summary>
    [Serializable]
    public class DailyActionState
    {
        public int Day;

        // Which categories are active
        public bool ExplorationActive = true;
        public bool DilemmaActive;
        public bool FamilyRequestActive;

        // Drawn challenges
        public PlayerActionChallenge ExplorationChallenge;
        public PlayerActionChallenge DilemmaChallenge;
        public PlayerActionChallenge FamilyRequestChallenge;

        // Target character for family request (resolved at runtime)
        public string FamilyRequestTarget;

        // Player inputs (set from UI)
        public string ExplorationInput;
        public string DilemmaInput;
        public string FamilyRequestInput;

        // Selected items per category (item IDs the player chose to use)
        public List<string> ExplorationItems = new List<string>();
        public List<string> DilemmaItems = new List<string>();
        public List<string> FamilyRequestItems = new List<string>();

        /// <summary>
        /// Get the challenge for a specific category.
        /// </summary>
        public PlayerActionChallenge GetChallenge(PlayerActionCategory category)
        {
            switch (category)
            {
                case PlayerActionCategory.Exploration: return ExplorationChallenge;
                case PlayerActionCategory.Dilemma: return DilemmaChallenge;
                case PlayerActionCategory.FamilyRequest: return FamilyRequestChallenge;
                default: return null;
            }
        }

        /// <summary>
        /// Check if a category is active.
        /// </summary>
        public bool IsCategoryActive(PlayerActionCategory category)
        {
            switch (category)
            {
                case PlayerActionCategory.Exploration: return ExplorationActive;
                case PlayerActionCategory.Dilemma: return DilemmaActive;
                case PlayerActionCategory.FamilyRequest: return FamilyRequestActive;
                default: return false;
            }
        }

        /// <summary>
        /// Get all active categories.
        /// </summary>
        public List<PlayerActionCategory> GetActiveCategories()
        {
            var list = new List<PlayerActionCategory>();
            if (ExplorationActive) list.Add(PlayerActionCategory.Exploration);
            if (DilemmaActive) list.Add(PlayerActionCategory.Dilemma);
            if (FamilyRequestActive) list.Add(PlayerActionCategory.FamilyRequest);
            return list;
        }
    }
}
