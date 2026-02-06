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
        [Title("Settings")]
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
        public void AddPlace(PlaceDefinitionSO place)
        {
            if (place != null && !discoveredPlaces.Contains(place))
            {
                discoveredPlaces.Add(place);
                place.SetDiscovered(true);
                Debug.Log($"[PlaceManager] Added new location: {place.PlaceName}");
            }
        }

        public void AddPlace(string placeId)
        {
            var place = placeDatabase?.GetPlace(placeId);
            if (place != null)
            {
                AddPlace(place);
            }
        }

        public bool IsDiscovered(string placeId)
        {
            return discoveredPlaces.Exists(p => p != null && p.PlaceId == placeId);
        }

        public void ClearAllPlaces()
        {
            discoveredPlaces.Clear();
            Debug.Log("[PlaceManager] Cleared all places.");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [ValueDropdown("GetAllPlaceProfileList")]
        [SerializeField] private PlaceDefinitionSO debugPlaceProfile;

        [Button("Add Place From SO")]
        private void Debug_AddPlaceSO()
        {
            if (debugPlaceProfile != null)
            {
                AddPlace(debugPlaceProfile);
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

        [SerializeField] private string debugPlaceId = "OldPharmacy";

        [Button("Add By ID")]
        private void Debug_AddPlace()
        {
            if (Application.isPlaying)
            {
                AddPlace(debugPlaceId);
            }
        }

        [Button("Add Random Place")]
        private void Debug_AddRandom()
        {
            if (Application.isPlaying && placeDatabase != null && placeDatabase.AllPlaces.Count > 0)
            {
                var undiscovered = placeDatabase.AllPlaces.FindAll(p => !IsDiscovered(p.PlaceId));
                if (undiscovered.Count > 0)
                {
                    var randomPlace = undiscovered[Random.Range(0, undiscovered.Count)];
                    AddPlace(randomPlace);
                }
                else
                {
                    Debug.Log("[PlaceManager] All places have been added/discovered!");
                }
            }
        }

        [Button("Clear All Places")]
        private void Debug_ClearAllPlaces()
        {
            ClearAllPlaces();
        }

        [Button("Log All Discovered Places")]
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
