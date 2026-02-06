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

        public PlaceDefinitionSO GenerateRandomPlace()
        {
            string[] places = new[] {
                "OldPharmacy|Abandoned Pharmacy|Shelves of expired medicine and bandages|2|75",
                "SuperMarket|Looted Supermarket|Empty aisles, some canned goods remain|3|100",
                "RadioStation|Radio Transmission Hub|Old broadcasting equipment, possible survivors|4|50",
                "WaterTreatment|Water Treatment Plant|Clean water source, heavily irradiated|5|150",
                "MilitaryDepot|Military Supply Depot|Weapons cache, dangerous territory|5|200",
                "Laboratory|Research Laboratory|Unknown experiments, hazardous materials|4|120",
                "Bunker7B|Sector 7B Bunker|Abandoned shelter, strange noises reported|3|80",
                "GasStation|Highway Gas Station|Fuel reserves, overrun by bandits|2|60"
            };
            
            var data = places[Random.Range(0, places.Length)].Split('|');
            
            // Add a unique suffix to avoid duplicate IDs
            string uniqueId = $"{data[0]}_{System.DateTime.Now.Ticks % 10000}";
            
            return CreateRuntimePlace(
                uniqueId,
                data[1],
                data[2],
                int.Parse(data[3]),
                int.Parse(data[4])
            );
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

        [Button("Generate 1 Random Place")]
        private void Debug_CreateRandomPlace()
        {
            GenerateRandomPlace();
        }

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
        [SerializeField] private string customDescription = "An unexplored location in the wasteland.";
        
        [Range(1, 5)]
        [SerializeField] private int customDangerLevel = 1;
        
        [Range(0, 500)]
        [SerializeField] private int customLootValue = 50;

        [Button("Create Custom Place", ButtonSizes.Medium)]
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
