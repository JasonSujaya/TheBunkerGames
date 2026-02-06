using UnityEngine;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles runtime creation of items for AI-native gameplay.
    /// A.N.G.E.L. uses this to generate dynamic scavenged items on the fly.
    /// </summary>
    public class AIItemCreator : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        [Required("Item Database is required for creating items")]
        #endif
        [SerializeField] private ItemDatabaseDataSO itemDatabase;

        #if ODIN_INSPECTOR
        [Required("Inventory Manager is required for adding created items")]
        #endif
        [SerializeField] private InventoryManager inventoryManager;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            // Auto-find references if not set
            if (itemDatabase == null)
            {
                itemDatabase = ItemDatabaseDataSO.Instance;
            }

            if (inventoryManager == null)
            {
                inventoryManager = InventoryManager.Instance;
            }
        }

        // -------------------------------------------------------------------------
        // Public Methods - Item Creation
        // -------------------------------------------------------------------------
        /// <summary>
        /// Create a new item at runtime (for AI-generated items).
        /// Perfect for AI-native games where A.N.G.E.L. generates items on the fly.
        /// </summary>
        public ItemData CreateRuntimeItem(string itemName, string description, ItemType type, Sprite icon = null)
        {
            if (itemDatabase == null)
            {
                Debug.LogError("[AIItemCreator] ItemDatabase is null! Cannot create item.");
                return null;
            }

            // Create a new ItemData ScriptableObject at runtime
            var newItem = ScriptableObject.CreateInstance<ItemData>();
            newItem.Initialize(itemName, description, type, icon);

            // Add to database
            itemDatabase.AddItem(newItem);

            Debug.Log($"[AIItemCreator] Created runtime item: {itemName}");
            return newItem;
        }

        /// <summary>
        /// AI-powered item creation - generates an item and adds it to inventory.
        /// For A.N.G.E.L. to dynamically create scavenged items.
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
        /// Useful for procedural scavenging results.
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

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("AI Generation", "A.N.G.E.L. procedurally generates items")]
        [HorizontalGroup("AI Generation/Buttons")]
        [Button("Generate 1 Random Item")]
        private void Debug_CreateRandomAIItem()
        {
            GenerateRandomAIItem();
        }

        [HorizontalGroup("AI Generation/Buttons")]
        [Button("Generate 5 Random Items")]
        private void Debug_Generate5Items()
        {
            for (int i = 0; i < 5; i++)
            {
                GenerateRandomAIItem();
            }
        }

        [TitleGroup("Custom Factory", "Manually define and create new items")]
        [HorizontalGroup("Custom Factory/Row1")]
        [SerializeField] private string customItemName = "New Prototype";
        
        [HorizontalGroup("Custom Factory/Row1")]
        [SerializeField] private ItemType customItemType = ItemType.Tools;

        [TextArea(2, 4)]
        [SerializeField] private string customDescription = "A manual entry for testing purposes.";

        [Button("Create Custom Item Instance", ButtonSizes.Medium)]
        private void Debug_CreateCustomItem()
        {
            CreateAndAddToInventory(customItemName, customDescription, customItemType, 1);
        }
        #endif
    }
}
