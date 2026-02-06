using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles runtime creation of places for AI-native gameplay.
    /// A.N.G.E.L. can generate dynamic exploration targets on the fly.
    /// Places created here are SESSION-BOUND and do not persist between sessions.
    /// </summary>
    public class PlaceCreator : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static PlaceCreator Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private PlaceManager placeManager;
        
        #if ODIN_INSPECTOR
        [Title("LLM Settings")]
        [InfoBox("When enabled, places are generated via LLM. When disabled, uses static fallback data.")]
        #endif
        [SerializeField] private bool useLLM = true;
        [SerializeField] private LLMPromptTemplateSO placePromptTemplate;


        // -------------------------------------------------------------------------
        // Session-Bound Runtime Places (NOT persisted)
        // -------------------------------------------------------------------------
        private List<PlaceDefinitionSO> sessionPlaces = new List<PlaceDefinitionSO>();

        public IReadOnlyList<PlaceDefinitionSO> SessionPlaces => sessionPlaces;

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

            if (placeManager == null)
            {
                placeManager = PlaceManager.Instance;
            }
        }

        private void OnDestroy()
        {
            ClearSessionPlaces();
        }

        // -------------------------------------------------------------------------
        // Public Methods - Place Creation (Session-Bound)
        // -------------------------------------------------------------------------
        public PlaceDefinitionSO CreateRuntimePlace(string id, string name, string description, int danger = 1, int loot = 50)
        {
            var newPlace = ScriptableObject.CreateInstance<PlaceDefinitionSO>();
            newPlace.Initialize(id, name, description, danger, loot);
            newPlace.name = name;
            sessionPlaces.Add(newPlace);

            Debug.Log($"[PlaceCreator] Created session place: {name} (Total: {sessionPlaces.Count})");
            return newPlace;
        }

        public void GenerateRandomPlace(System.Action<PlaceDefinitionSO> onComplete = null)
        {
            if (useLLM && LLMManager.Instance != null && placePromptTemplate != null)
            {
                Debug.Log("[PlaceCreator] <color=cyan>[LLM]</color> Generating place via AI...");
                string userPrompt = placePromptTemplate.BuildUserPrompt(Random.Range(1, 6), "post-apocalyptic bunker survival");
                
                LLMManager.Instance.QuickChat(
                    LLMManager.Provider.OpenRouter,
                    userPrompt,
                    onSuccess: (response) => {
                        if (LLMJsonParser.TryParsePlace(response, out var data))
                        {
                            var place = CreateRuntimePlace(data.placeId, data.placeName, data.description, data.dangerLevel, data.estimatedLootValue);
                            Debug.Log($"[PlaceCreator] <color=cyan>[LLM]</color> Created: {data.placeName}");
                            onComplete?.Invoke(place);
                        }
                        else
                        {
                            Debug.LogWarning("[PlaceCreator] <color=yellow>[LLM]</color> Failed to parse response, using fallback.");
                            var fallback = GenerateFallbackPlace();
                            onComplete?.Invoke(fallback);
                        }
                    },
                    onError: (err) => {
                        Debug.LogError($"[PlaceCreator] <color=red>[LLM ERROR]</color> {err}");
                        var fallback = GenerateFallbackPlace();
                        onComplete?.Invoke(fallback);
                    },
                    systemPrompt: placePromptTemplate.SystemPrompt
                );
            }
            else
            {
                Debug.Log("[PlaceCreator] <color=orange>[STATIC]</color> LLM disabled, using fallback data.");
                var place = GenerateFallbackPlace();
                onComplete?.Invoke(place);
            }
        }

        private PlaceDefinitionSO GenerateFallbackPlace()
        {
            string[] places = new[] {
                "OldPharmacy|Abandoned Pharmacy|Shelves of expired medicine|2|75",
                "SuperMarket|Looted Supermarket|Empty aisles, some canned goods remain|3|100",
                "RadioStation|Radio Transmission Hub|Old broadcasting equipment|4|50"
            };
            var data = places[Random.Range(0, places.Length)].Split('|');
            string uniqueId = $"{data[0]}_{System.DateTime.Now.Ticks % 10000}";
            return CreateRuntimePlace(uniqueId, data[1], data[2], int.Parse(data[3]), int.Parse(data[4]));
        }

        public PlaceDefinitionSO GetSessionPlace(string id)
        {
            return sessionPlaces.FirstOrDefault(p => p != null && p.PlaceId == id);
        }

        public void ClearSessionPlaces()
        {
            // Destroy ScriptableObject instances
            foreach (var place in sessionPlaces)
            {
                if (place != null)
                {
                    Destroy(place);
                }
            }
            sessionPlaces.Clear();
            Debug.Log("[PlaceCreator] Cleared all session places.");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [ShowInInspector, ReadOnly]
        private int SessionPlaceCount => sessionPlaces?.Count ?? 0;

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Generate 1 Random Place")]
        private void Debug_CreateRandomPlace()
        {
            GenerateRandomPlace();
        }

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Generate 3 Random Places")]
        private void Debug_Generate3Places()
        {
            for (int i = 0; i < 3; i++)
            {
                GenerateRandomPlace();
            }
        }

        [SerializeField] private string customPlaceId = "NewPlace";
        [SerializeField] private string customPlaceName = "Mysterious Location";
        
        [TextArea(2, 4)]
        [SerializeField] private string customDescription = "A new location in the wasteland.";
        
        [Range(1, 5)]
        [SerializeField] private int customDangerLevel = 1;
        
        [Range(0, 500)]
        [SerializeField] private int customLootValue = 50;

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Create Custom Place")]
        private void Debug_CreateCustomPlace()
        {
            CreateRuntimePlace(customPlaceId, customPlaceName, customDescription, customDangerLevel, customLootValue);
        }

        [Button("Clear All Session Places")]
        private void Debug_ClearSession()
        {
            ClearSessionPlaces();
        }

        [Button("Log Session Places")]
        private void Debug_LogSessionPlaces()
        {
            Debug.Log($"[PlaceCreator] Session places ({sessionPlaces.Count}):");
            foreach (var p in sessionPlaces)
            {
                if (p != null)
                {
                    Debug.Log($"  - {p.PlaceName} (Danger: {p.DangerLevel}, Loot: {p.EstimatedLootValue})");
                }
            }
        }
        #endif
    }
}
