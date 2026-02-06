using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "ItemData", menuName = "TheBunkerGames/Item Data")]
    public class ItemData : ScriptableObject
    {
        #if ODIN_INSPECTOR
        [Title("Item Info")]
        #endif
        [SerializeField] private string itemName;
        [TextArea(3, 5)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        
        #if ODIN_INSPECTOR
        [InfoBox("Glitch version used when A.N.G.E.L. corrupts the display")]
        #endif
        [SerializeField] private Sprite glitchIcon;
        
        [SerializeField] private ItemType itemType;

        #if ODIN_INSPECTOR
        [Title("Properties")]
        #endif
        [SerializeField] private float value; 
        [SerializeField] private bool isConsumable;
        
        // Effects when consumed (simple implementation for now)
        #if ODIN_INSPECTOR
        [ShowIf("isConsumable")]
        #endif
        [SerializeField] private float hungerRestore;
        #if ODIN_INSPECTOR
        [ShowIf("isConsumable")]
        #endif
        [SerializeField] private float thirstRestore;
        #if ODIN_INSPECTOR
        [ShowIf("isConsumable")]
        #endif
        [SerializeField] private float sanityRestore;
        #if ODIN_INSPECTOR
        [ShowIf("isConsumable")]
        #endif
        [SerializeField] private float healthRestore;

        public string ItemName => itemName;
        public string Description => description;
        public Sprite Icon => icon;
        public Sprite GlitchIcon => glitchIcon;
        public ItemType Type => itemType;
        public float Value => value;
        public bool IsConsumable => isConsumable;
        public float HungerRestore => hungerRestore;
        public float ThirstRestore => thirstRestore;
        public float SanityRestore => sanityRestore;
        public float HealthRestore => healthRestore;
    }
}
