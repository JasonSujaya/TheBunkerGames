using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Controls the Night Cycle phase (Phase 5 of the Core Loop).
    /// A.N.G.E.L. processes the day's data, applies stat decay,
    /// and generates a "Dream/Nightmare" log — a hallucinated narrative
    /// of the family's mental state plus a visual illustration.
    /// </summary>
    public class NightCycleManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static NightCycleManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<NightReportData> OnNightReportGenerated;
        public static event Action<string> OnDreamLogGenerated;
        public static event Action OnNightCycleComplete;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private DreamDatabaseSO dreamDatabase;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Night Report")]
        [ReadOnly]
        #endif
        [SerializeField] private NightReportData latestReport;

        // -------------------------------------------------------------------------
        // Logic Controller
        // -------------------------------------------------------------------------
        private NightLogicController logicController;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public NightReportData LatestReport => latestReport;

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

            // Initialize Logic Controller
            logicController = new NightLogicController();
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
            if (newState == GameState.NightCycle)
            {
                ProcessNightCycle();
            }
        }

        public void ProcessNightCycle()
        {
            Debug.Log("[NightCycle] Processing night cycle...");

            latestReport = new NightReportData();
            var gameManager = GameManager.Instance;
            latestReport.Day = gameManager != null ? gameManager.CurrentDay : 0;

            // 1. Apply daily stat decay
            ApplyStatDecay();

            // 2. Check for deaths
            CheckForDeaths();

            // 3. Degrade A.N.G.E.L.'s processing
            DegradeAngel();

            // 4. Generate dream/nightmare log
            GenerateDreamLog();

            Debug.Log($"[NightCycle] Night {latestReport.Day} complete. Deaths: {latestReport.DeathsThisNight.Count}");
            OnNightReportGenerated?.Invoke(latestReport);
        }

        public void CompleteNightCycle()
        {
            // Advance to next day
            GameManager.Instance?.AdvanceDay();

            Debug.Log("[NightCycle] Night cycle complete. Advancing to next day.");
            OnNightCycleComplete?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Night Processing Steps
        // -------------------------------------------------------------------------
        private void ApplyStatDecay()
        {
            var config = GameConfigDataSO.Instance;
            var family = FamilyManager.Instance;
            if (family == null) return;

            logicController.ApplyStatDecay(family.FamilyMembers, config, latestReport.StatChanges);
        }

        private void CheckForDeaths()
        {
            var family = FamilyManager.Instance;
            if (family == null) return;

            foreach (var character in family.FamilyMembers)
            {
                if (!character.IsAlive)
                {
                    if (!latestReport.DeathsThisNight.Contains(character.Name))
                    {
                        latestReport.DeathsThisNight.Add(character.Name);
                        Debug.Log($"[NightCycle] {character.Name} has died.");
                    }
                }
            }

            // Check for total party kill
            if (family.AliveCount <= 0)
            {
                Debug.Log("[NightCycle] All family members are dead. Game Over.");
                GameManager.Instance?.EndGame(false);
            }
        }

        private void DegradeAngel()
        {
            // 5. Degrade Angel's processing power based on the day's events
            var angel = AngelInteractionManager.Instance;
            if (angel != null)
            {
                float degradation = logicController.CalculateAngelDegradation(latestReport);
                angel.DegradeProcessing(degradation);
            }
        }

        private void GenerateDreamLog()
        {
            var family = FamilyManager.Instance;
            if (family == null) return;
            
            bool isNightmare = false;
            string log = logicController.GenerateDreamLog(family.FamilyMembers, dreamDatabase, out isNightmare);
            
            latestReport.IsNightmare = isNightmare;
            latestReport.DreamLog = log;

            Debug.Log($"[NightCycle] {(isNightmare ? "NIGHTMARE" : "Dream")}: {latestReport.DreamLog}");
            OnDreamLogGenerated?.Invoke(latestReport.DreamLog);
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Process Night Cycle", ButtonSizes.Medium)]
        private void Debug_ProcessNight()
        {
            if (Application.isPlaying) ProcessNightCycle();
        }

        [Button("Complete Night (Advance Day)", ButtonSizes.Large)]
        private void Debug_CompleteNight()
        {
            if (Application.isPlaying) CompleteNightCycle();
        }

        [Title("Auto Setup")]
        [Button("Auto Setup Dependencies", ButtonSizes.Large)]
        private void AutoSetupDependencies()
        {
            #if UNITY_EDITOR
            // Ensure Tester exists
            var testerType = System.Type.GetType("TheBunkerGames.Tests.NightCycleTester");
            if (testerType != null && GetComponent(testerType) == null)
            {
                gameObject.AddComponent(testerType);
                Debug.Log("[NightCycleManager] Added NightCycleTester.");
            }
            else if (testerType == null)
            {
                Debug.LogWarning("[NightCycleManager] Could not find NightCycleTester type. Ensure it is in TheBunkerGames.Tests namespace.");
            }
            #endif
        }
        #endif
    }
}
