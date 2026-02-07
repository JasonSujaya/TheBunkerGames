using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject holding pools of challenges for each player action category.
    /// Each day, random challenges are drawn from these pools.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerActionChallengePool", menuName = "TheBunkerGames/Player Action Challenge Pool")]
    public class PlayerActionChallengePoolSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static PlayerActionChallengePoolSO instance;
        public static PlayerActionChallengePoolSO Instance => instance;

        public static void SetInstance(PlayerActionChallengePoolSO pool)
        {
            instance = pool;
        }

        // -------------------------------------------------------------------------
        // Challenge Pools
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Exploration Challenges")]
        [InfoBox("Challenges where the player must solve a problem by typing their approach.")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        #endif
        [SerializeField] private List<PlayerActionChallenge> explorationChallenges = new List<PlayerActionChallenge>();

        #if ODIN_INSPECTOR
        [Title("Dilemma Scenarios")]
        [InfoBox("Moral/practical dilemmas with no single correct answer. Player must respond (required).")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        #endif
        [SerializeField] private List<PlayerActionChallenge> dilemmaChallenges = new List<PlayerActionChallenge>();

        #if ODIN_INSPECTOR
        [Title("Family Request Scenarios")]
        [InfoBox("Family members need help. Use {target} in description for the character name placeholder.")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
        #endif
        [SerializeField] private List<PlayerActionChallenge> familyRequestChallenges = new List<PlayerActionChallenge>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<PlayerActionChallenge> ExplorationChallenges => explorationChallenges;
        public List<PlayerActionChallenge> DilemmaChallenges => dilemmaChallenges;
        public List<PlayerActionChallenge> FamilyRequestChallenges => familyRequestChallenges;

        // -------------------------------------------------------------------------
        // Random Selection
        // -------------------------------------------------------------------------

        /// <summary>
        /// Get a random exploration challenge.
        /// </summary>
        public PlayerActionChallenge GetRandomExploration()
        {
            if (explorationChallenges == null || explorationChallenges.Count == 0) return null;
            return explorationChallenges[Random.Range(0, explorationChallenges.Count)];
        }

        /// <summary>
        /// Get a random dilemma challenge.
        /// </summary>
        public PlayerActionChallenge GetRandomDilemma()
        {
            if (dilemmaChallenges == null || dilemmaChallenges.Count == 0) return null;
            return dilemmaChallenges[Random.Range(0, dilemmaChallenges.Count)];
        }

        /// <summary>
        /// Get a random family request challenge.
        /// </summary>
        public PlayerActionChallenge GetRandomFamilyRequest()
        {
            if (familyRequestChallenges == null || familyRequestChallenges.Count == 0) return null;
            return familyRequestChallenges[Random.Range(0, familyRequestChallenges.Count)];
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug")]
        [Button("Log All Challenges", ButtonSizes.Medium)]
        private void Debug_LogAll()
        {
            Debug.Log($"[ChallengePool] Exploration: {explorationChallenges.Count} | Dilemma: {dilemmaChallenges.Count} | Family: {familyRequestChallenges.Count}");
            foreach (var c in explorationChallenges) Debug.Log($"  [Exploration] {c.Title}");
            foreach (var c in dilemmaChallenges) Debug.Log($"  [Dilemma] {c.Title}");
            foreach (var c in familyRequestChallenges) Debug.Log($"  [Family] {c.Title}");
        }

        [Button("Test Random Draw", ButtonSizes.Medium)]
        [GUIColor(0, 1, 0)]
        private void Debug_TestRandom()
        {
            var e = GetRandomExploration();
            var d = GetRandomDilemma();
            var f = GetRandomFamilyRequest();
            Debug.Log($"[ChallengePool] Random draw:\n  Exploration: {e?.Title ?? "NONE"}\n  Dilemma: {d?.Title ?? "NONE"}\n  Family: {f?.Title ?? "NONE"}");
        }
        #endif
    }
}
