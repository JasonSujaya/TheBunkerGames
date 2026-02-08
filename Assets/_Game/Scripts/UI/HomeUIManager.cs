using UnityEngine;
using UnityEngine.UI;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine.SceneManagement;
using TMPro;

namespace TheBunkerGames
{
    public class HomeUIManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("References")]
        #endif
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private string gameSceneName = "GameScene"; // Initial placeholder
        [SerializeField] private bool enableDebugLogs = true;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            Initialize();
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void Initialize()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(OnStartButtonClicked);
                startButton.onClick.AddListener(OnStartButtonClicked);
                if (enableDebugLogs) Debug.Log("[HomeUI] Wired Up Start Button");
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
                settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(OnQuitButtonClicked);
                quitButton.onClick.AddListener(OnQuitButtonClicked);
            }
        }

        // Event fired when "Start" is clicked
        public event System.Action OnStartGameRequested;

        public void OnStartButtonClicked()
        {
            Debug.Log("[HomeUI] Start Button Clicked (Invoking Event)"); // Always Log
            OnStartGameRequested?.Invoke();
        }

        public void OnSettingsButtonClicked()
        {
            if (enableDebugLogs) Debug.Log("[HomeUI] Settings Button Clicked");
            // TODO: Open Settings Panel
        }

        public void OnQuitButtonClicked()
        {
            if (enableDebugLogs) Debug.Log("[HomeUI] Quit Button Clicked");
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        // -------------------------------------------------------------------------
        // Auto Setup / Editor Tools
        // -------------------------------------------------------------------------
        #if UNITY_EDITOR
        
        #if ODIN_INSPECTOR
        [Button("Auto Setup UI", ButtonSizes.Large)]
        [GUIColor(0f, 1f, 0f)]
        #else
        [ContextMenu("Auto Setup UI")]
        #endif
        private void AutoSetupUI()
        {
            // 1. Find or Create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Home_Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                
                // Also need EventSystem
                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject esObj = new GameObject("EventSystem");
                    esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // 2. Create Panel
            Transform safeArea = canvas.transform.Find("SafeArea");
            if (safeArea == null)
            {
                GameObject panelObj = new GameObject("SafeArea");
                panelObj.transform.SetParent(canvas.transform, false);
                RectTransform rect = panelObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one; 
                rect.sizeDelta = Vector2.zero;
                safeArea = panelObj.transform;
            }

            // 3. Create Buttons
            if (startButton == null) startButton = CreateButton("StartButton", "Start Game", safeArea, 0);
            if (settingsButton == null) settingsButton = CreateButton("SettingsButton", "Settings", safeArea, -60);
            if (quitButton == null) quitButton = CreateButton("QuitButton", "Quit", safeArea, -120);

            Debug.Log("[HomeUIManager] Auto Setup Complete!");
        }

        private Button CreateButton(string name, string label, Transform parent, float yOffset)
        {
            // Check if exists
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<Button>();

            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            
            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            Button btn = btnObj.AddComponent<Button>();
            
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 50);
            rect.anchoredPosition = new Vector2(0, yOffset);

            // Text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            tmp.color = Color.white;

            return btn;
        }
        #endif
    }
}
