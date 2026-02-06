using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject defining an item in the game.
    /// Data-only - no behavior code here.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "TheBunkerGames/Item Data")]
    public class ItemData : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Identity")]
        #endif
        [SerializeField] private string id = "new_item";
        [SerializeField] private string displayName = "New Item";

        // -------------------------------------------------------------------------
        // Visuals
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Visuals")]
        [PreviewField(75)]
        #endif
        [SerializeField] private Sprite icon;

        #if ODIN_INSPECTOR
        [PreviewField(75)]
        [InfoBox("Glitch version used when A.N.G.E.L. corrupts the display")]
        #endif
        [SerializeField] private Sprite glitchIcon;

        // -------------------------------------------------------------------------
        // Classification
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Classification")]
        #endif
        [SerializeField] private ItemType itemType = ItemType.Junk;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Sprite GlitchIcon => glitchIcon;
        public ItemType Type => itemType;
    }
}
