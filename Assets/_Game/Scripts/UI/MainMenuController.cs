using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Orchestrates the flow of the Main Menu:
    /// Home -> Theme Select -> Family Select -> Game Start
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Scene Configuration")]
        #endif
        [SerializeField] private string gameSceneName = "GameScene";

        #if ODIN_INSPECTOR
        [Title("UI References")]
        #endif
        [SerializeField] private HomeUIManager homeUI;
        [SerializeField] private ThemeSelectUI themeUI;
        [SerializeField] private FamilySelectUI familyUI;
        [SerializeField] private GameplayHudUI gameplayHUD;
        [SerializeField] private UIScreenFader fader;

        #if ODIN_INSPECTOR
        [Title("Debug")]
        #endif
        [SerializeField] private bool enableDebugLogs = true;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Start()
        {
            // Ensure subscription in case OnEnable ran before homeUI was assigned
            if (homeUI != null)
            {
                homeUI.OnStartGameRequested -= HandleStartGameRequested;
                homeUI.OnStartGameRequested += HandleStartGameRequested;
                if (enableDebugLogs) Debug.Log("[MainMenuController] (Start) Re-subscribed to HomeUI.OnStartGameRequested");
            }
            else
            {
                Debug.LogError("[MainMenuController] HomeUI is NULL in Start()! Did you run Auto Setup and save the scene?");
            }

            // Initial state: Show Home, Hide others
            ShowHome();
        }

        private void OnEnable()
        {
            if (homeUI != null)
            {
                homeUI.OnStartGameRequested -= HandleStartGameRequested; // Safety unhook
                homeUI.OnStartGameRequested += HandleStartGameRequested;
                if (enableDebugLogs) Debug.Log("[MainMenuController] Subscribed to HomeUI.OnStartGameRequested");
            }
            else
            {
                 Debug.LogWarning("[MainMenuController] HomeUI reference is MISSING in OnEnable!");
            }

            if (themeUI != null)
                ThemeSelectUI.OnThemeSelected += HandleThemeSelected;

            if (familyUI != null)
                FamilySelectUI.OnCharactersSelected += HandleCharactersSelected;
        }

        private void OnDisable()
        {
            if (homeUI != null)
                homeUI.OnStartGameRequested -= HandleStartGameRequested;

            if (ThemeSelectUI.Instance != null) // Static event, but good practice to unsubscribe
                ThemeSelectUI.OnThemeSelected -= HandleThemeSelected;

            if (FamilySelectUI.Instance != null)
                FamilySelectUI.OnCharactersSelected -= HandleCharactersSelected;
        }

        // -------------------------------------------------------------------------
        // Flow Control
        // -------------------------------------------------------------------------
        private void ShowHome()
        {
            if (enableDebugLogs) Debug.Log("[MainMenuController] Showing Home UI");
            
            if (homeUI != null) homeUI.gameObject.SetActive(true);
            if (themeUI != null) themeUI.Hide();
            if (familyUI != null) familyUI.Hide();
            if (gameplayHUD != null) gameplayHUD.Hide();
        }

        private void HandleStartGameRequested()
        {
            if (enableDebugLogs) Debug.Log("[MainMenuController] Home -> Theme Select");
            
            Transition(() => 
            {
                if (homeUI != null) homeUI.gameObject.SetActive(false);
                if (themeUI != null) themeUI.Show();
            });
        }

        private void HandleThemeSelected(GameThemeSO theme)
        {
            if (enableDebugLogs) Debug.Log($"[MainMenuController] Theme Selected: {theme.ThemeName}. Moving to Family Select.");

            Transition(() =>
            {
                if (themeUI != null) themeUI.Hide();
                if (familyUI != null) familyUI.Show();
            });
        }

        private void HandleCharactersSelected(System.Collections.Generic.List<CharacterDefinitionSO> characters)
        {
            if (enableDebugLogs) Debug.Log($"[MainMenuController] Characters Selected. Starting Game Scene: {gameSceneName}");

            Transition(() =>
            {
                if (familyUI != null) familyUI.Hide();
                StartGame();
            });
        }

        private void Transition(System.Action onMiddle)
        {
            if (fader != null)
            {
                fader.FadeOut(0.4f, () => 
                {
                    onMiddle?.Invoke();
                    fader.FadeIn(0.4f);
                });
            }
            else
            {
                // Instant fallback
                onMiddle?.Invoke();
            }
        }

        private void StartGame()
        {
            // If we are already in the target scene (or single-scene setup), skipping load prevents destroying FamilyManager
            if (SceneManager.GetActiveScene().name == gameSceneName)
            {
                if (enableDebugLogs) Debug.Log($"[MainMenuController] Already in '{gameSceneName}'. Skipping load and starting flow directly.");
                StartGameFlow();
                return;
            }

            if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                SceneManager.LoadScene(gameSceneName);
            }
            else
            {
                Debug.LogWarning($"[MainMenuController] Scene '{gameSceneName}' cannot be loaded. Using single-scene fallback.");
                StartGameFlow();
            }
        }

        private void StartGameFlow()
        {
            // Single-scene architecture: spawn family (if needed), then show HUD
            var flow = FindFirstObjectByType<GameFlowController>();
            if (flow != null)
            {
                flow.StartNewGame();
                if (enableDebugLogs) Debug.Log("[MainMenuController] GameFlowController.StartNewGame() called.");
            }

            // Show HUD and populate with the freshly spawned family
            if (gameplayHUD != null)
            {
                gameplayHUD.Show();
                if (enableDebugLogs) Debug.Log("[MainMenuController] Gameplay HUD shown.");
            }
        }
        
        // -------------------------------------------------------------------------
        // Auto Setup / Validation
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Auto Setup", ButtonSizes.Large)]
        [GUIColor(0f, 1f, 0f)]
        #endif
        [ContextMenu("Auto Setup")]
        public void AutoSetup()
        {
            // Find UI references even if they are currently disabled
            if (homeUI == null) homeUI = FindFirstObjectByType<HomeUIManager>(FindObjectsInactive.Include);
            if (themeUI == null) themeUI = FindFirstObjectByType<ThemeSelectUI>(FindObjectsInactive.Include);
            if (familyUI == null) familyUI = FindFirstObjectByType<FamilySelectUI>(FindObjectsInactive.Include);
            if (gameplayHUD == null) gameplayHUD = FindFirstObjectByType<GameplayHudUI>(FindObjectsInactive.Include);
            if (fader == null) fader = FindFirstObjectByType<UIScreenFader>(FindObjectsInactive.Include);
            
            // Setup Fader if missing
            if (fader == null)
            {
                GameObject faderObj = new GameObject("ScreenFader");
                faderObj.transform.SetParent(transform, false);
                fader = faderObj.AddComponent<UIScreenFader>();
                fader.AutoSetupFader();
                // Ensure it's last sibling to be on top (if same canvas) - but usually it needs its own canvas or highest sorting order
                // Let's check for a canvas
                Canvas c = fader.GetComponent<Canvas>();
                if (c == null)
                {
                    c = faderObj.AddComponent<Canvas>();
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    c.sortingOrder = 999; // Topmost
                    faderObj.AddComponent<CanvasScaler>();
                    faderObj.AddComponent<GraphicRaycaster>();
                }
            }
            
            // Ensure correct initial state if we are in editor mode
            if (!Application.isPlaying)
            {
                if (homeUI != null) homeUI.gameObject.SetActive(true);
                if (themeUI != null) themeUI.Hide();
                if (familyUI != null) familyUI.Hide();
                if (gameplayHUD != null) gameplayHUD.Hide();
            }

            Debug.Log($"[MainMenuController] Auto Setup Complete. Unassigned references: " +
                      $"Home={(homeUI==null?"MISSING":"OK")}, " +
                      $"Theme={(themeUI==null?"MISSING":"OK")}, " +
                      $"Family={(familyUI==null?"MISSING":"OK")}, " +
                      $"HUD={(gameplayHUD==null?"MISSING":"OK")}, " +
                      $"Fader={(fader==null?"MISSING":"OK")}");
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
}
