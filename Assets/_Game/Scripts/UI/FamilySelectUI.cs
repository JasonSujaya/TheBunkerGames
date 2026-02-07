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
    /// UI manager for family profile selection.
    /// Has its own auto-setup that creates a Canvas root with a selection panel.
    /// Each FamilyListSO is displayed as a selectable button showing name + member count.
    /// </summary>
    public class FamilySelectUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static FamilySelectUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<FamilyListSO> OnFamilySelected;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Configuration")]
        #endif
        [SerializeField] private List<FamilyListSO> availableFamilies = new List<FamilyListSO>();
        [SerializeField] private int canvasSortOrder = 101;
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
        private FamilyListSO selectedFamily;
        public FamilyListSO SelectedFamily => selectedFamily;

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
            canvasRoot = UIBuilderUtils.CreateCanvasRoot(transform, "FamilySelectCanvas", canvasSortOrder);
            UIBuilderUtils.EnsureEventSystem();

            // Panel
            panel = UIBuilderUtils.CreatePanel(canvasRoot.transform, "FamilySelectPanel", new Color(0.12f, 0.15f, 0.12f, 0.95f));

            // Title
            UIBuilderUtils.CreateTitle(panel.transform, "Choose Your Family");

            // Scroll content
            GameObject scrollContent = UIBuilderUtils.CreateScrollArea(panel.transform);
            scrollContent.name = "Content";

            // Confirm button
            UIBuilderUtils.CreateButton(panel.transform, "Confirm", "ConfirmButton");

            if (enableDebugLogs) Debug.Log("[FamilySelectUI] Auto Setup complete.");

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
            selectedFamily = null;
            PopulateFamilyButtons();
            UIBuilderUtils.SetButtonInteractable(panel, "ConfirmButton", false);

            // Wire confirm button
            Button confirmBtn = UIBuilderUtils.FindButton(panel, "ConfirmButton");
            if (confirmBtn != null)
            {
                confirmBtn.onClick.RemoveAllListeners();
                confirmBtn.onClick.AddListener(OnConfirm);
            }

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Shown with {availableFamilies.Count} profiles.");
        }

        public void Hide()
        {
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Population
        // -------------------------------------------------------------------------
        private void PopulateFamilyButtons()
        {
            Transform content = UIBuilderUtils.FindScrollContent(panel);
            if (content == null) return;

            UIBuilderUtils.ClearChildren(content);

            foreach (var family in availableFamilies)
            {
                if (family == null) continue;
                var capturedFamily = family;
                int memberCount = family.DefaultFamilyMembers != null ? family.DefaultFamilyMembers.Count : 0;
                string sublabel = $"{memberCount} member(s)";
                UIBuilderUtils.CreateSelectionButton(content, family.name, sublabel, () => SelectFamily(capturedFamily));
            }
        }

        private void SelectFamily(FamilyListSO family)
        {
            selectedFamily = family;
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
                        bool isSelected = child.name == "SelectBtn_" + family.name;
                        bg.color = isSelected
                            ? new Color(0.2f, 0.45f, 0.2f, 1f)
                            : new Color(0.2f, 0.2f, 0.3f, 0.9f);
                    }
                }
            }

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Selected: {family.name}");
        }

        // -------------------------------------------------------------------------
        // Confirm
        // -------------------------------------------------------------------------
        private void OnConfirm()
        {
            if (selectedFamily == null) return;

            // Wire selected family profile to FamilyManager
            ApplyFamilyProfile(selectedFamily);

            Hide();
            OnFamilySelected?.Invoke(selectedFamily);

            if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Family confirmed: {selectedFamily.name}");
        }

        private void ApplyFamilyProfile(FamilyListSO profile)
        {
            if (FamilyManager.Instance == null) return;

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var so = new UnityEditor.SerializedObject(FamilyManager.Instance);
                var prop = so.FindProperty("defaultFamilyProfile");
                if (prop != null)
                {
                    prop.objectReferenceValue = profile;
                    so.ApplyModifiedProperties();
                }
                return;
            }
            #endif

            // Runtime: use reflection to set the private serialized field
            var field = typeof(FamilyManager).GetField("defaultFamilyProfile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(FamilyManager.Instance, profile);
                if (enableDebugLogs) Debug.Log($"[FamilySelectUI] Applied profile to FamilyManager: {profile.name}");
            }
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
