using UnityEngine;
using System;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// A single challenge prompt within a player action category.
    /// Drawn from a pool and presented to the player each day.
    /// </summary>
    [Serializable]
    public class PlayerActionChallenge
    {
        #if ODIN_INSPECTOR
        [HorizontalGroup("Header", Width = 140)]
        [LabelText("Category")]
        #endif
        [SerializeField] private PlayerActionCategory category;

        #if ODIN_INSPECTOR
        [HorizontalGroup("Header")]
        [GUIColor(0.4f, 1f, 0.4f)]
        #endif
        [SerializeField] private string title = "New Challenge";

        #if ODIN_INSPECTOR
        [TextArea(3, 6)]
        #endif
        [SerializeField] private string description = "";

        /// <summary>
        /// Optional: for FamilyRequest, this holds the target character name.
        /// Use {target} in description as a placeholder that gets replaced at runtime.
        /// </summary>
        [SerializeField] private string targetCharacter = "";

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public PlayerActionCategory Category => category;
        public string Title => title;
        public string Description => description;
        public string TargetCharacter => targetCharacter;

        /// <summary>
        /// Returns the description with {target} replaced by the actual character name.
        /// </summary>
        public string GetDescription(string characterName = null)
        {
            if (string.IsNullOrEmpty(characterName))
                return description;
            return description.Replace("{target}", characterName);
        }

        public override string ToString() => $"[{category}] {title}";
    }
}
