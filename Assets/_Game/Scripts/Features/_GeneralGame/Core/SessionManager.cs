using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles the meta-game session lifecycle: Starting a new run, Giving Up, Restarting.
    /// Bridges the Main Menu and the active Gameplay Loop.
    /// </summary>
    public class SessionManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static SessionManager Instance { get; private set; }

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
            DontDestroyOnLoad(gameObject); // Persist across scenes if needed
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
#if ODIN_INSPECTOR
        [Title("Session Controls")]
        [Button("Start New Run", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.8f, 0.2f)] // Kept green as it's a primary action
#endif
        public void StartNewRun()
        {
            Debug.Log("[SessionManager] Starting new run...");
            
            // 1. Reset Game State
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }
            else
            {
                Debug.LogError("[SessionManager] GameManager missing!");
            }
            
            // 2. Future: Load Scene, Reset Audio, etc.
        }

#if ODIN_INSPECTOR
        [Button("Give Up / End Run", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.2f, 0.2f)] // Red for destructive action
#endif
        public void GiveUpRun()
        {
            if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
            {
                Debug.Log("[SessionManager] Player gave up.");
                GameManager.Instance.EndGame(false); // Survived = false
            }
        }
    }
}
