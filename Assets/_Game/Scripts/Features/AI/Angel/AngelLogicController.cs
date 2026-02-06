using UnityEngine;

namespace TheBunkerGames
{
    /// <summary>
    /// Pure logic controller for A.N.G.E.L.'s internal state and decision making.
    /// Handles processing degradation and response selection.
    /// </summary>
    public class AngelLogicController
    {
        public float CalculateProcessingLevel(float currentLevel, float degradationAmount)
        {
            return Mathf.Clamp(currentLevel - degradationAmount, 0f, 100f);
        }

        public AngelMood DetermineMood(float processingLevel, int day)
        {
            // Critical failure overrides day-based mood
            if (processingLevel <= 20f) return AngelMood.Glitching;
            if (processingLevel <= 50f) return AngelMood.Hostile;

            // Day-based personality shift
            if (day <= 5) return AngelMood.Cooperative;
            if (day <= 10) return AngelMood.Neutral;
            if (day <= 18) return AngelMood.Mocking;
            if (day <= 24) return AngelMood.Cold;
            
            return AngelMood.Hostile;
        }

        public AngelResponseData GenerateMockResponse(AngelMood mood, AngelResponsesSO responsesData)
        {
            var response = new AngelResponseData();
            
            if (responsesData != null)
            {
                response = responsesData.GetRandomResponse(mood);
            }
            else
            {
                // Fallback if data is missing
                response.Message = "[SYSTEM ERROR] Voice module offline.";
            }

            return response;
        }
    }
}
