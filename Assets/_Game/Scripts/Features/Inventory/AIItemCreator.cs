using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles runtime creation of items for AI-native gameplay.
    /// A.N.G.E.L. uses this to generate dynamic scavenged items on the fly.
    /// Items created here are SESSION-BOUND and do not persist between sessions.
    /// </summary>
    public class AIItemCreator : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static AIItemCreator Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private InventoryManager inventoryManager;

        // -------------------------------------------------------------------------
        // Session-Bound Runtime Items (NOT persisted)
        // -------------------------------------------------------------------------
        private List<ItemData> sessionItems = new List<ItemData>();

        /// <summary>
        /// All items created during this session (read-only).
        /// </summary>
        public IReadOnlyList<ItemData> SessionItems => sessionItems;

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

            if (inventoryManager == null)
            {
                inventoryManager = InventoryManager.Instance;
            }
        }

        private void OnDestroy()
        {
            // Clear session items when this component is destroyed
            ClearSessionItems();
        }

        // -------------------------------------------------------------------------
        // Public Methods - Item Creation (Session-Bound)
        // -------------------------------------------------------------------------
        /// <summary>
        /// Create a new item at runtime. This item is SESSION-BOUND and will not persist.
        /// </summary>
        public ItemData CreateRuntimeItem(string itemName, string description, ItemType type, Sprite icon = null)
        {
            // Create a new ItemData ScriptableObject at runtime
            var newItem = ScriptableObject.CreateInstance<ItemData>();
            newItem.name = itemName;
            newItem.Initialize(itemName, description, type, icon);

            // Add to session list (NOT to persistent database)
            sessionItems.Add(newItem);

            Debug.Log($"[AIItemCreator] Created session item: {itemName} (Total session items: {sessionItems.Count})");
            return newItem;
        }

        /// <summary>
        /// AI-powered item creation - generates an item and adds it to inventory.
        /// </summary>
        public ItemData CreateAndAddToInventory(string itemName, string description, ItemType type, int quantity = 1)
        {
            var newItem = CreateRuntimeItem(itemName, description, type);
            
            if (newItem != null && inventoryManager != null)
            {
                inventoryManager.AddItem(newItem, quantity);
            }
            else if (inventoryManager == null)
            {
                Debug.LogError("[AIItemCreator] InventoryManager is null! Cannot add created item.");
            }

            return newItem;
        }

        /// <summary>
        /// Generate a random AI item from predefined templates.
        /// </summary>
        public ItemData GenerateRandomAIItem()
        {
            string[] randomItems = new[] { 
                "Rusted Wrench|A.N.G.E.L. found this in sector 7-B|Tools",
                "Moldy Bread|Expired 3 years ago, but calories are calories|Food",
                "Broken Clock|Time stopped at 3:47 AM|Tools",
                "Mysterious Pill Bottle|Label too faded to read|Meds",
                "Cracked Flashlight|Battery at 12%|Tools",
                "Stale Crackers|Best by 2019|Food",
                "Frayed Wire|Might be useful for repairs|Tools",
                "Dusty Photo Album|Memories from before|Tools"
            };
            
            var randomData = randomItems[Random.Range(0, randomItems.Length)].Split('|');
            var itemType = System.Enum.Parse<ItemType>(randomData[2]);
            
            return CreateAndAddToInventory(randomData[0], randomData[1], itemType, 1);
        }

        /// <summary>
        /// Look up a session item by name.
        /// </summary>
        public ItemData GetSessionItem(string itemName)
        {
            return sessionItems.FirstOrDefault(i => i != null && i.ItemName == itemName);
        }

        /// <summary>
        /// Clear all session-bound items. Called automatically on destroy.
        /// </summary>
        public void ClearSessionItems()
        {
            foreach (var item in sessionItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            sessionItems.Clear();
            Debug.Log("[AIItemCreator] Cleared all session items.");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [ShowInInspector, ReadOnly]
        private int SessionItemCount => sessionItems?.Count ?? 0;

        [Button("Generate 1 Random Item")]
        private void Debug_CreateRandomAIItem()
        {
            GenerateRandomAIItem();
        }

        [Button("Generate 5 Random Items")]
        private void Debug_Generate5Items()
        {
            for (int i = 0; i < 5; i++)
            {
                GenerateRandomAIItem();
            }
        }

        [SerializeField] private string customItemName = "New Prototype";
        [SerializeField] private ItemType customItemType = ItemType.Tools;

        [TextArea(2, 4)]
        [SerializeField] private string customDescription = "A manual entry for testing purposes.";

        [Button("Create Custom Item Instance", ButtonSizes.Medium)]
        private void Debug_CreateCustomItem()
        {
            CreateAndAddToInventory(customItemName, customDescription, customItemType, 1);
        }

        [Button("Clear All Session Items")]
        private void Debug_ClearSession()
        {
            ClearSessionItems();
        }

        [Button("Log Session Items")]
        private void Debug_LogSessionCharacters()
        {
            Debug.Log($"[AIItemCreator] Session items ({sessionItems.Count}):");
            foreach (var item in sessionItems)
            {
                if (item != null)
                {
                    Debug.Log($"  - {item.ItemName}: {item.Description}");
                }
            }
        }
        #endif
    }
}

