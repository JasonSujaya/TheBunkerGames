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
        [SerializeField] private ItemManager itemManager;
        
        #if ODIN_INSPECTOR
        [Title("LLM Settings")]
        [InfoBox("When enabled, items are generated via LLM. When disabled, uses static fallback data.")]
        #endif
        [SerializeField] private bool useLLM = true;
        [SerializeField] private LLMPromptTemplateSO itemPromptTemplate;
        [SerializeField] private int defaultBatchSize = 3;


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
            if (itemManager == null)
            {
                itemManager = ItemManager.Instance;
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

            // Register with ItemManager if available
            if (itemManager != null)
            {
                itemManager.RegisterItem(newItem);
            }

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
        public void GenerateRandomAIItem(System.Action<ItemData> onComplete = null)
        {
            if (useLLM && LLMManager.Instance != null && itemPromptTemplate != null)
            {
                Debug.Log("[AIItemCreator] <color=cyan>[LLM]</color> Generating item via AI...");
                string[] itemTypes = { "Food", "Water", "Meds", "Tools", "Junk" };
                string userPrompt = itemPromptTemplate.BuildUserPrompt(itemTypes[Random.Range(0, itemTypes.Length)], "scavenged in post-apocalyptic bunker");
                
                LLMManager.Instance.QuickChat(
                    userPrompt,
                    onSuccess: (response) => {
                        if (LLMJsonParser.TryParseItem(response, out var data))
                        {
                            ItemType parsedType = ItemType.Junk;
                            // If parsing fails, Enum.TryParse overwrites with 0 (Food). 
                            // We use a temporary variable to prevent this.
                            if (System.Enum.TryParse(data.itemType, true, out ItemType result))
                            {
                                parsedType = result;
                            }
                            var item = CreateAndAddToInventory(data.itemName, data.description, parsedType, 1);
                            Debug.Log($"[AIItemCreator] <color=cyan>[LLM]</color> Created: {data.itemName}");
                            onComplete?.Invoke(item);
                        }
                        else
                        {
                            Debug.LogWarning("[AIItemCreator] <color=yellow>[LLM]</color> Failed to parse, using fallback.");
                            var fallback = GenerateFallbackItem();
                            onComplete?.Invoke(fallback);
                        }
                    },
                    onError: (err) => {
                        Debug.LogError($"[AIItemCreator] <color=red>[LLM ERROR]</color> {err}");
                        var fallback = GenerateFallbackItem();
                        onComplete?.Invoke(fallback);
                    },
                    systemPrompt: itemPromptTemplate.SystemPrompt + "\n\nSchema:\n" + itemPromptTemplate.JsonSchemaExample,
                    useJsonMode: true
                );
            }
            else
            {
                Debug.Log("[AIItemCreator] <color=orange>[STATIC]</color> LLM disabled, using fallback data.");
                var item = GenerateFallbackItem();
                onComplete?.Invoke(item);
            }
        }

        /// <summary>
        /// Generate a batch of items via AI.
        /// </summary>
        public void GenerateItemBatch(int count, System.Action<List<ItemData>> onComplete = null)
        {
            if (useLLM && LLMManager.Instance != null && itemPromptTemplate != null)
            {
                int finalCount = Mathf.Max(1, count);
                Debug.Log($"[AIItemCreator] <color=cyan>[LLM]</color> Generating {finalCount} items via AI...");
                
                string[] itemTypes = { "Food", "Water", "Meds", "Tools", "Junk" };
                string randomType = itemTypes[Random.Range(0, itemTypes.Length)];
                
                // Instruct AI to generate a list
                string userPrompt = $"Generate a list of {finalCount} unique items. At least one should be {randomType}. Context: scavenged in post-apocalyptic bunker.";

                LLMManager.Instance.QuickChat(
                    userPrompt,
                    onSuccess: (response) => {
                        if (LLMJsonParser.TryParseItemBatch(response, out var data))
                        {
                            List<ItemData> createdItems = new List<ItemData>();
                            foreach (var itemData in data.items)
                            {
                                ItemType parsedType = ItemType.Junk;
                                if (System.Enum.TryParse(itemData.itemType, true, out ItemType result))
                                    parsedType = result;

                                var item = CreateAndAddToInventory(itemData.itemName, itemData.description, parsedType, 1);
                                createdItems.Add(item);
                            }
                            Debug.Log($"[AIItemCreator] <color=cyan>[LLM]</color> Created batch of {createdItems.Count} items.");
                            onComplete?.Invoke(createdItems);
                        }
                        else
                        {
                            Debug.LogWarning("[AIItemCreator] <color=yellow>[LLM]</color> Failed to parse batch, using fallbacks.");
                            List<ItemData> fallbackItems = new List<ItemData>();
                            for(int i=0; i<finalCount; i++) fallbackItems.Add(GenerateFallbackItem());
                            onComplete?.Invoke(fallbackItems);
                        }
                    },
                    onError: (err) => {
                        Debug.LogError($"[AIItemCreator] <color=red>[LLM ERROR]</color> {err}");
                        List<ItemData> fallbackItems = new List<ItemData>();
                        for(int i=0; i<finalCount; i++) fallbackItems.Add(GenerateFallbackItem());
                        onComplete?.Invoke(fallbackItems);
                    },
                    systemPrompt: itemPromptTemplate.SystemPrompt + "\n\nCRITICAL: Respond with a JSON object containing an 'items' array of objects following this schema:\n" + itemPromptTemplate.JsonSchemaExample + "\n\nExample Output: { \"items\": [ {...}, {...} ] }",
                    useJsonMode: true
                );
            }
            else
            {
                List<ItemData> items = new List<ItemData>();
                for (int i = 0; i < count; i++) items.Add(GenerateFallbackItem());
                onComplete?.Invoke(items);
            }
        }

        private ItemData GenerateFallbackItem()
        {
            string[] randomItems = { 
                "Rusted Wrench|A.N.G.E.L. found this in sector 7-B|Tools",
                "Moldy Bread|Expired 3 years ago|Food",
                "Cracked Flashlight|Battery at 12%|Tools"
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

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Generate 1 Random Item")]
        private void Debug_CreateRandomAIItem()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test LLM generation."); return; }
            GenerateRandomAIItem();
        }

        [Button("Create Batch AI Items", ButtonSizes.Medium)]
        [GUIColor(0, 0.6f, 0.9f)]
        private void Debug_CreateBatchAIItems()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode to test LLM generation."); return; }
            GenerateItemBatch(defaultBatchSize);
        }

        [SerializeField] private string customItemName = "New Prototype";
        [SerializeField] private ItemType customItemType = ItemType.Tools;

        [TextArea(2, 4)]
        [SerializeField] private string customDescription = "A manual entry for testing purposes.";

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Create Custom Item Instance")]
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

