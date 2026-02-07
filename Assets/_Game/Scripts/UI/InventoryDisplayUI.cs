using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// UI manager for inventory display.
    /// Has its own auto-setup that creates a Canvas root with an inventory panel.
    /// Reads from InventoryManager and displays item names + quantities.
    /// </summary>
    public class InventoryDisplayUI : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static InventoryDisplayUI Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Configuration")]
        #endif
        [SerializeField] private int canvasSortOrder = 50;
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
        // Runtime Cache
        // -------------------------------------------------------------------------
        private Transform contentContainer;

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
            canvasRoot = UIBuilderUtils.CreateCanvasRoot(transform, "InventoryCanvas", canvasSortOrder);
            UIBuilderUtils.EnsureEventSystem();

            // Panel
            panel = UIBuilderUtils.CreatePanel(canvasRoot.transform, "InventoryPanel", new Color(0.15f, 0.12f, 0.1f, 0.95f));

            // Title
            UIBuilderUtils.CreateTitle(panel.transform, "Inventory");

            // Scroll content
            GameObject scrollContent = UIBuilderUtils.CreateScrollArea(panel.transform);
            scrollContent.name = "Content";

            if (enableDebugLogs) Debug.Log("[InventoryDisplayUI] Auto Setup complete.");

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
            RefreshInventory();

            if (enableDebugLogs) Debug.Log("[InventoryDisplayUI] Shown.");
        }

        public void Hide()
        {
            if (canvasRoot != null) canvasRoot.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Refresh
        // -------------------------------------------------------------------------
        public void RefreshInventory()
        {
            if (contentContainer == null && panel != null)
                contentContainer = UIBuilderUtils.FindScrollContent(panel);

            if (contentContainer == null) return;

            UIBuilderUtils.ClearChildren(contentContainer);

            if (InventoryManager.Instance == null)
            {
                UIBuilderUtils.CreateInventoryRow(contentContainer, "No inventory manager found", "");
                return;
            }

            List<InventorySlotData> items = InventoryManager.Instance.Items;
            if (items == null || items.Count == 0)
            {
                UIBuilderUtils.CreateInventoryRow(contentContainer, "No items", "");
                return;
            }

            foreach (var slot in items)
            {
                string displayName = slot.ItemId;
                if (ItemManager.Instance != null)
                {
                    var itemData = ItemManager.Instance.GetItem(slot.ItemId);
                    if (itemData != null) displayName = itemData.ItemName;
                }
                UIBuilderUtils.CreateInventoryRow(contentContainer, displayName, $"x{slot.Quantity}");
            }

            if (enableDebugLogs) Debug.Log($"[InventoryDisplayUI] Refreshed: {items.Count} slot(s).");
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

        [Button("Refresh", ButtonSizes.Medium)]
        private void Debug_Refresh() => RefreshInventory();
        #endif
    }
}
