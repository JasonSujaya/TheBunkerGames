using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject holding all place definitions in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "PlaceDatabaseDataSO", menuName = "TheBunkerGames/Place Database Data")]
    public class PlaceDatabaseDataSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static PlaceDatabaseDataSO instance;
        public static PlaceDatabaseDataSO Instance => instance;

        public static void SetInstance(PlaceDatabaseDataSO database)
        {
            if (database == null)
            {
                Debug.LogError("[PlaceDatabaseDataSO] Attempted to set null instance!");
                return;
            }
            instance = database;
        }

        // -------------------------------------------------------------------------
        // Place List
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("All Places")]
        [Searchable]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<PlaceDefinitionSO> allPlaces = new List<PlaceDefinitionSO>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<PlaceDefinitionSO> AllPlaces => allPlaces;

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public PlaceDefinitionSO GetPlace(string id)
        {
            for (int i = 0; i < allPlaces.Count; i++)
            {
                if (allPlaces[i] != null && allPlaces[i].PlaceId == id)
                {
                    return allPlaces[i];
                }
            }
            Debug.LogWarning($"[PlaceDatabaseDataSO] Place not found: {id}");
            return null;
        }

        public List<PlaceDefinitionSO> GetPlacesByDangerLevel(int dangerLevel)
        {
            List<PlaceDefinitionSO> result = new List<PlaceDefinitionSO>();
            for (int i = 0; i < allPlaces.Count; i++)
            {
                if (allPlaces[i] != null && allPlaces[i].DangerLevel == dangerLevel)
                {
                    result.Add(allPlaces[i]);
                }
            }
            return result;
        }

        public void AddPlace(PlaceDefinitionSO place)
        {
            if (place != null && !allPlaces.Contains(place))
            {
                allPlaces.Add(place);
            }
        }

        public int RemoveNullEntries()
        {
            int removed = allPlaces.RemoveAll(p => p == null);
            if (removed > 0)
            {
                Debug.Log($"[PlaceDatabaseDataSO] Removed {removed} null/missing entries.");
            }
            return removed;
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Log All Places", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_LogAllPlaces()
        {
            Debug.Log($"[PlaceDatabaseDataSO] Total places: {allPlaces.Count}");
            foreach (var place in allPlaces)
            {
                if (place != null)
                {
                    Debug.Log($"  - {place.PlaceName} (Danger: {place.DangerLevel}, Loot: {place.EstimatedLootValue})");
                }
            }
        }

        [Button("Find and Add All Place Assets", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void Debug_FindAndAddAll()
        {
#if UNITY_EDITOR
            RemoveNullEntries();
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:PlaceDefinitionSO");
            int count = 0;
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                PlaceDefinitionSO place = UnityEditor.AssetDatabase.LoadAssetAtPath<PlaceDefinitionSO>(path);
                if (place != null && !allPlaces.Contains(place))
                {
                    allPlaces.Add(place);
                    count++;
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[PlaceDatabaseDataSO] Added {count} new places to the database.");
#endif
        }

        [Button("Clean Up Missing Places", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void Debug_CleanUpMissing()
        {
#if UNITY_EDITOR
            int removed = RemoveNullEntries();
            UnityEditor.EditorUtility.SetDirty(this);
            if (removed == 0)
            {
                Debug.Log("[PlaceDatabaseDataSO] No missing places to clean up.");
            }
#endif
        }
        #endif
    }
}
