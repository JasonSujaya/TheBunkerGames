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
    [CreateAssetMenu(fileName = "GameConfig", menuName = "TheBunkerGames/Game Config")]
    public class GameConfig : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static GameConfig instance;
        public static GameConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameConfig>("GameConfig");
                    if (instance == null)
                    {
                        Debug.LogError("[GameConfig] No GameConfig found in Resources folder!");
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
        // Voting Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Voting / Twitch")]
        #endif
        [SerializeField] private float voteTimerDuration = 30f;
        [SerializeField] private bool streamerModeEnabled = false;

        // -------------------------------------------------------------------------
        // Stat Decay Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Stat Decay (Per Day)")]
        #endif
        [SerializeField] private float hungerDecayPerDay = 15f;
        [SerializeField] private float sanityDecayPerDay = 10f;

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
        public float SanityDecayPerDay => sanityDecayPerDay;
        public bool UseMockAgent => useMockAgent;
        public bool EnableDebugLogs => enableDebugLogs;
    }
}
