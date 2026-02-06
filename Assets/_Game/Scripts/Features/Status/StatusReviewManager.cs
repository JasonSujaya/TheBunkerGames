using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Controls the Status Review phase (Phase 1 of the Core Loop).
    /// Monitors family health: Hunger, Thirst, Sanity, Physical Health.
    /// Generates warnings and alerts for critical conditions.
    /// </summary>
    /// <summary>
    /// Controls the Status Review phase (Phase 1 of the Core Loop).
    /// Monitors family health: Hunger, Thirst, Sanity, Physical Health.
    /// Generates warnings and alerts for critical conditions.
    /// </summary>
    public class StatusReviewManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static StatusReviewManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<StatusReportData> OnStatusReportGenerated;
        public static event Action<CharacterData, string> OnCriticalWarning;
        public static event Action OnStatusReviewComplete;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Current Report")]
        [ReadOnly]
        #endif
        [SerializeField] private StatusReportData latestReport;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public StatusReportData LatestReport => latestReport;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        // -------------------------------------------------------------------------
        // Phase Logic
        // -------------------------------------------------------------------------
        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.StatusReview)
            {
                GenerateStatusReport();
            }
        }

        public void GenerateStatusReport()
        {
            var familyManager = FamilyManager.Instance;
            if (familyManager == null) return;

            latestReport = new StatusReportData();
            latestReport.Day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 0;
            latestReport.CharacterStatuses = new List<CharacterStatusData>();
            latestReport.Warnings = new List<string>();

            foreach (var character in familyManager.FamilyMembers)
            {
                var status = new CharacterStatusData
                {
                    CharacterName = character.Name,
                    Hunger = character.Hunger,
                    Thirst = character.Thirst,
                    Sanity = character.Sanity,
                    Health = character.Health,
                    IsAlive = character.IsAlive,
                    IsCritical = character.IsCritical
                };
                latestReport.CharacterStatuses.Add(status);

                // Generate warnings for critical conditions
                if (!character.IsAlive)
                {
                    string warning = $"{character.Name} has died.";
                    latestReport.Warnings.Add(warning);
                    OnCriticalWarning?.Invoke(character, warning);
                }
                else if (character.IsCritical)
                {
                    string warning = $"{character.Name} is in critical condition.";
                    latestReport.Warnings.Add(warning);
                    OnCriticalWarning?.Invoke(character, warning);
                }

                if (character.IsInsane)
                {
                    latestReport.Warnings.Add($"{character.Name} has lost their mind.");
                }

                if (character.IsDehydrated)
                {
                    latestReport.Warnings.Add($"{character.Name} is severely dehydrated.");
                }
            }

            latestReport.AliveCount = familyManager.AliveCount;
            latestReport.TotalCount = familyManager.FamilyMembers.Count;

            Debug.Log($"[StatusReview] Day {latestReport.Day} | Alive: {latestReport.AliveCount}/{latestReport.TotalCount} | Warnings: {latestReport.Warnings.Count}");
            OnStatusReportGenerated?.Invoke(latestReport);
        }

        public void CompleteStatusReview()
        {
            Debug.Log("[StatusReview] Status review complete. Moving to A.N.G.E.L. Interaction.");
            OnStatusReviewComplete?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Generate Report", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_GenerateReport()
        {
            if (Application.isPlaying) GenerateStatusReport();
        }

        [Button("Complete Phase", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_CompletePhase()
        {
            if (Application.isPlaying) CompleteStatusReview();
        }

        [Title("Auto Setup")]
        [Button("Auto Setup Dependencies", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void AutoSetupDependencies()
        {
            #if UNITY_EDITOR
            // Ensure Tester exists
            var testerType = System.Type.GetType("TheBunkerGames.Tests.StatusReviewManagerTester");
            if (testerType != null && GetComponent(testerType) == null)
            {
                gameObject.AddComponent(testerType);
                Debug.Log("[StatusReviewManager] Added StatusReviewTester.");
            }
            else if (testerType == null)
            {
                Debug.LogWarning("[StatusReviewManager] Could not find StatusReviewTester type. Ensure it is in TheBunkerGames.Tests namespace.");
            }
            #endif
        }
        #endif
    }
}
