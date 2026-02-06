using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Game configuration ScriptableObject.
    /// Central place for tunable game settings.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfigSO", menuName = "TheBunkerGames/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static GameConfigSO instance;
        public static GameConfigSO Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameConfigSO>("GameConfigSO");
                    if (instance == null)
                    {
                        Debug.LogError("[GameConfigSO] No GameConfigSO found in Resources folder!");
                    }
                }
                return instance;
            }
        }

        // -------------------------------------------------------------------------
        // Game Loop Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Game Loop")]
        #endif
        [SerializeField] private int totalDays = 28;
        [SerializeField] private float dayDurationSeconds = 300f;

        // -------------------------------------------------------------------------
        // Voting / Twitch Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Voting / Twitch")]
        #endif
        [SerializeField] private float voteTimerDuration = 30f;
        [SerializeField] private bool streamerModeEnabled = false;

        // -------------------------------------------------------------------------
        // Stat Decay Settings (Per Day)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Stat Decay (Per Day)")]
        #endif
        [SerializeField] private float hungerDecayPerDay = 15f;
        [SerializeField] private float thirstDecayPerDay = 20f;
        [SerializeField] private float sanityDecayPerDay = 10f;

        // -------------------------------------------------------------------------
        // Exploration Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Exploration")]
        #endif
        [SerializeField] private int maxExplorersPerDay = 2;
        [SerializeField] private float baseInjuryChance = 0.2f;

        // -------------------------------------------------------------------------
        // A.N.G.E.L. Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("A.N.G.E.L.")]
        #endif
        [SerializeField] private int maxAngelInteractionsPerDay = 3;
        [SerializeField] private float processingDegradePerDay = 3f;

        // -------------------------------------------------------------------------
        // Debug Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug")]
        #endif
        [SerializeField] private bool useMockAgent = false;
        [SerializeField] private bool enableDebugLogs = true;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public int TotalDays => totalDays;
        public float DayDurationSeconds => dayDurationSeconds;
        public float VoteTimerDuration => voteTimerDuration;
        public bool StreamerModeEnabled => streamerModeEnabled;
        public float HungerDecayPerDay => hungerDecayPerDay;
        public float ThirstDecayPerDay => thirstDecayPerDay;
        public float SanityDecayPerDay => sanityDecayPerDay;
        public int MaxExplorersPerDay => maxExplorersPerDay;
        public float BaseInjuryChance => baseInjuryChance;
        public int MaxAngelInteractionsPerDay => maxAngelInteractionsPerDay;
        public float ProcessingDegradePerDay => processingDegradePerDay;
        public bool UseMockAgent => useMockAgent;
        public bool EnableDebugLogs => enableDebugLogs;
    }
}
