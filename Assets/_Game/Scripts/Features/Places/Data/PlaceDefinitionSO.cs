using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject for static place/location definitions.
    /// Places are exploration targets in the wasteland.
    /// </summary>
    [CreateAssetMenu(fileName = "PlaceDefinitionSO", menuName = "TheBunkerGames/Place Definition")]
    public class PlaceDefinitionSO : ScriptableObject
    {
        #if ODIN_INSPECTOR
        [Title("Place Info")]
        #endif
        [SerializeField] private string placeId;
        [SerializeField] private string placeName;
        [TextArea(3, 5)]
        [SerializeField] private string description;

        #if ODIN_INSPECTOR
        [Title("Exploration")]
        #endif
        [SerializeField] private int dangerLevel = 1;
        [SerializeField] private int estimatedLootValue = 50;
        [SerializeField] private bool isDiscovered = false;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string PlaceId => placeId;
        public string PlaceName => placeName;
        public string Description => description;
        public int DangerLevel => dangerLevel;
        public int EstimatedLootValue => estimatedLootValue;
        public bool IsDiscovered => isDiscovered;

        // -------------------------------------------------------------------------
        // Runtime Initialization
        // -------------------------------------------------------------------------
        public void Initialize(string id, string name, string desc, int danger = 1, int loot = 50)
        {
            placeId = id;
            placeName = name;
            description = desc;
            dangerLevel = danger;
            estimatedLootValue = loot;
            isDiscovered = false;
        }

        public void SetDiscovered(bool discovered)
        {
            isDiscovered = discovered;
        }
    }
}
