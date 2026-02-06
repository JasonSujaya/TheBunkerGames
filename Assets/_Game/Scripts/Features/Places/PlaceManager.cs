using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Manages explorable places/locations in the wasteland.
    /// </summary>
    public class PlaceManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static PlaceManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Database Reference
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Database")]
        [Required("Place Database is required")]
        #endif
        [SerializeField] private PlaceDatabaseDataSO placeDatabase;

        // -------------------------------------------------------------------------
        // Discovered Places
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Discovered Places")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<PlaceDefinitionSO> discoveredPlaces = new List<PlaceDefinitionSO>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<PlaceDefinitionSO> DiscoveredPlaces => discoveredPlaces;
        public PlaceDatabaseDataSO Database => placeDatabase;

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

            // Initialize database singleton
            if (placeDatabase != null)
            {
                PlaceDatabaseDataSO.SetInstance(placeDatabase);
            }
            else
            {
                // Try to load from Resources
                placeDatabase = Resources.Load<PlaceDatabaseDataSO>("PlaceDatabaseDataSO");
                if (placeDatabase != null)
                {
                    PlaceDatabaseDataSO.SetInstance(placeDatabase);
                }
                else
                {
                    Debug.LogWarning("[PlaceManager] PlaceDatabaseDataSO not assigned and not found in Resources!");
                }
            }
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void DiscoverPlace(PlaceDefinitionSO place)
        {
            if (place != null && !discoveredPlaces.Contains(place))
            {
                discoveredPlaces.Add(place);
                place.SetDiscovered(true);
                Debug.Log($"[PlaceManager] Discovered new location: {place.PlaceName}");
            }
        }

        public void DiscoverPlace(string placeId)
        {
            var place = placeDatabase?.GetPlace(placeId);
            if (place != null)
            {
                DiscoverPlace(place);
            }
        }

        public bool IsDiscovered(string placeId)
        {
            return discoveredPlaces.Exists(p => p != null && p.PlaceId == placeId);
        }

        public void ClearDiscoveredPlaces()
        {
            discoveredPlaces.Clear();
            Debug.Log("[PlaceManager] Cleared all discovered places.");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [HorizontalGroup("Discover")]
        [ValueDropdown("GetAllPlaceProfileList")]
        [SerializeField] private PlaceDefinitionSO debugPlaceProfile;

        [HorizontalGroup("Discover")]
        [Button("Discover Place", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_DiscoverPlaceSO()
        {
            if (debugPlaceProfile != null)
            {
                DiscoverPlace(debugPlaceProfile);
            }
            else
            {
                Debug.LogWarning("[PlaceManager] No Place Profile selected.");
            }
        }

        private IEnumerable<ValueDropdownItem<PlaceDefinitionSO>> GetAllPlaceProfileList()
        {
            var list = new ValueDropdownList<PlaceDefinitionSO>();

            // 1. Persistent
            if (placeDatabase != null && placeDatabase.AllPlaces != null)
            {
                foreach (var p in placeDatabase.AllPlaces)
                {
                    if (p != null)
                        list.Add($"[P] {p.PlaceName}", p);
                }
            }

            // 2. Session
            if (PlaceCreator.Instance != null && PlaceCreator.Instance.SessionPlaces != null)
            {
                foreach (var p in PlaceCreator.Instance.SessionPlaces)
                {
                    if (p != null)
                        list.Add($"[S] {p.PlaceName}", p);
                }
            }

            return list;
        }

        [HorizontalGroup("Manual")]
        [SerializeField] private string debugPlaceId = "OldPharmacy";

        [HorizontalGroup("Manual")]
        [Button("Discover By ID", ButtonSizes.Medium)]
        private void Debug_DiscoverPlace()
        {
            if (Application.isPlaying)
            {
                DiscoverPlace(debugPlaceId);
            }
        }

        [Button("Discover Random Place", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_DiscoverRandom()
        {
            if (Application.isPlaying && placeDatabase != null && placeDatabase.AllPlaces.Count > 0)
            {
                var undiscovered = placeDatabase.AllPlaces.FindAll(p => !IsDiscovered(p.PlaceId));
                if (undiscovered.Count > 0)
                {
                    var randomPlace = undiscovered[Random.Range(0, undiscovered.Count)];
                    DiscoverPlace(randomPlace);
                }
                else
                {
                    Debug.Log("[PlaceManager] All places have been discovered!");
                }
            }
        }

        [Button("Clear Discovered Places", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void Debug_ClearDiscoveredPlaces()
        {
            ClearDiscoveredPlaces();
        }

        [Button("Log All Discovered Places", ButtonSizes.Medium)]
        private void Debug_LogDiscoveredPlaces()
        {
            Debug.Log($"[PlaceManager] Discovered places ({discoveredPlaces.Count}):");
            foreach (var place in discoveredPlaces)
            {
                if (place != null)
                {
                    Debug.Log($"  - {place.PlaceName}: {place.Description}");
                }
            }
        }
        #endif
    }
}
