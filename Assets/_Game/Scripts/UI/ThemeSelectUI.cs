using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// UI manager for theme/scenario selection.
    /// Has its own auto-setup that creates a Canvas root with a selection panel.
    /// Each GameThemeSO is displayed as a selectable button.
    /// </summary>
    public class ThemeSelectUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static ThemeSelectUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<GameThemeSO> OnThemeSelected;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Configuration")]
        #endif
        [SerializeField] private List<GameThemeSO> availableThemes = new List<GameThemeSO>();
        [SerializeField] private int canvasSortOrder = 100;
        [SerializeField] private bool enableDebugLogs = false;

        // -------------------------------------------------------------------------
        // Generated References (populated by AutoSetup)
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Generated References")]
        [ReadOnly]
        #endif
        [SerializeField] private GameObject canvasRoot;
        [SerializeField] private GameObject panel;

        // -------------------------------------------------------------------------
        // Runtime State
        // -------------------------------------------------------------------------
        private GameThemeSO selectedTheme;
        public GameThemeSO SelectedTheme => selectedTheme;

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

        // -------------------------------------------------------------------------
        // Auto Setup
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Auto Setup")]
        [Button("Auto Setup", ButtonSizes.Large)]
        [GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        [ContextMenu("Auto Setup")]
        public void AutoSetup()
        {
            // Clean up existing
            if (canvasRoot != null)
            {
                DestroyImmediate(canvasRoot);
                canvasRoot = null;
            }

            // Canvas root
            canvasRoot = UIBuilderUtils.CreateCanvasRoot(transform, "ThemeSelectCanvas", canvasSortOrder);
            UIBuilderUtils.EnsureEventSystem();

            // Panel
            panel = UIBuilderUtils.CreatePanel(canvasRoot.transform, "ThemeSelectPanel", new Color(0.12f, 0.12f, 0.18f, 0.95f));

            // Title
            UIBuilderUtils.CreateTitle(panel.transform, "Choose Your Theme");

            // Scroll content
            GameObject scrollContent = UIBuilderUtils.CreateScrollArea(panel.transform);
            scrollContent.name = "Content";

            // Confirm button
            UIBuilderUtils.CreateButton(panel.transform, "Confirm", "ConfirmButton");

            if (enableDebugLogs) Debug.Log("[ThemeSelectUI] Auto Setup complete.");

            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        // -------------------------------------------------------------------------
        // Show / Hide
        // -------------------------------------------------------------------------
        public void Show()
        {
            if (canvasRoot == null) return;
            canvasRoot.SetActive(true);
            selectedTheme = null;
            PopulateThemeButtons();
            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", false);

            // Wire confirm button
            Button confirmBtn = UIBuilderUtils.FindButton(panel, "ConfirmButton");
            if (confirmBtn != null)
            {
                confirmBtn.onClick.RemoveAllListeners();
                confirmBtn.onClick.AddListener(OnConfirm);
            }

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Shown with {availableThemes.Count} themes.");
        }

        public void Hide()
        {
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Population
        // -------------------------------------------------------------------------
        private void PopulateThemeButtons()
        {
            Transform content = UIBuilderUtils.FindScrollContent(panel);
            if (content == null) return;

            UIBuilderUtils.ClearChildren(content);

            foreach (var theme in availableThemes)
            {
                if (theme == null) continue;
                var capturedTheme = theme;
                UIBuilderUtils.CreateSelectionButton(content, theme.ThemeName, theme.Description, () => SelectTheme(capturedTheme));
            }
        }

        private void SelectTheme(GameThemeSO theme)
        {
            selectedTheme = theme;
            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", true);

            // Highlight selected
            Transform content = UIBuilderUtils.FindScrollContent(panel);
            if (content != null)
            {
                foreach (Transform child in content)
                {
                    Image bg = child.GetComponent<Image>();
                    if (bg != null)
                    {
                        bool isSelected = child.name == "SelectBtn_" + theme.ThemeName;
                        bg.color = isSelected
                            ? new Color(theme.ThemeColor.r * 0.5f, theme.ThemeColor.g * 0.5f, theme.ThemeColor.b * 0.5f, 1f)
                            : new Color(0.2f, 0.2f, 0.3f, 0.9f);
                    }
                }
            }

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Selected: {theme.ThemeName}");
        }

        // -------------------------------------------------------------------------
        // Confirm
        // -------------------------------------------------------------------------
        private void OnConfirm()
        {
            if (selectedTheme == null) return;

            // Apply theme event schedule
            if (selectedTheme.EventSchedule != null)
            {
                PreScriptedEventScheduleSO.SetInstance(selectedTheme.EventSchedule);
                if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Applied event schedule: {selectedTheme.EventSchedule.name}");
            }

            Hide();
            OnThemeSelected?.Invoke(selectedTheme);

            if (enableDebugLogs) Debug.Log($"[ThemeSelectUI] Theme confirmed: {selectedTheme.ThemeName}");
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [Button("Show", ButtonSizes.Medium)]
        private void Debug_Show() => Show();

        [Button("Hide", ButtonSizes.Medium)]
        private void Debug_Hide() => Hide();
        #endif
    }
}
