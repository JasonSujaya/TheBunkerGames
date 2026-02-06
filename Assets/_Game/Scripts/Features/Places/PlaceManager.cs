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
        // Active Places
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Active Places")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<PlaceDefinitionSO> activePlaces = new List<PlaceDefinitionSO>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<PlaceDefinitionSO> ActivePlaces => activePlaces;
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
            if (place != null && !activePlaces.Contains(place))
            {
                activePlaces.Add(place);
                place.SetActive(true);
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

        public bool HasPlace(string placeId)
        {
            return activePlaces.Exists(p => p != null && p.PlaceId == placeId);
        }

        public void ClearAllPlaces()
        {
            activePlaces.Clear();
            Debug.Log("[PlaceManager] Cleared all places.");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [ValueDropdown("GetAllPlaceProfileList")]
        [SerializeField] private PlaceDefinitionSO debugPlaceProfile;

        [GUIColor(0.2f, 0.6f, 1.0f)]
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
                var available = placeDatabase.AllPlaces.FindAll(p => !HasPlace(p.PlaceId));
                if (available.Count > 0)
                {
                    var randomPlace = available[Random.Range(0, available.Count)];
                    AddPlace(randomPlace);
                }
                else
                {
                    Debug.Log("[PlaceManager] All places have been added.");
                }
            }
        }

        [Button("Clear All Places")]
        private void Debug_ClearAllPlaces()
        {
            ClearAllPlaces();
        }

        [Button("Log All Active Places")]
        private void Debug_LogActivePlaces()
        {
            Debug.Log($"[PlaceManager] Active places ({activePlaces.Count}):");
            foreach (var place in activePlaces)
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
