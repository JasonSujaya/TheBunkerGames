using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject defining a character template.
    /// Used to create instances of Character at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterData_", menuName = "TheBunkerGames/Character Data")]
    public class CharacterData : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Identity")]
        #endif
        [SerializeField] private string characterName = "New Character";
        [SerializeField] private Sprite portrait;

        // -------------------------------------------------------------------------
        // Starting Stats
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Starting Stats")]
        [PropertyRange(0, 100)]
        #endif
        [SerializeField] private float startingHunger = 100f;

        #if ODIN_INSPECTOR
        [PropertyRange(0, 100)]
        #endif
        [SerializeField] private float startingSanity = 100f;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string CharacterName => characterName;
        public Sprite Portrait => portrait;
        public float StartingHunger => startingHunger;
        public float StartingSanity => startingSanity;

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        /// <summary>
        /// Creates a new Character instance from this template.
        /// </summary>
        public Character CreateCharacter()
        {
            return new Character(characterName, startingHunger, startingSanity);
        }
    }
}
