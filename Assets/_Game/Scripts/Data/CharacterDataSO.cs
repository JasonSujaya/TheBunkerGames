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
    [CreateAssetMenu(fileName = "CharacterDataSO_", menuName = "TheBunkerGames/Character Data")]
    public class CharacterDataSO : ScriptableObject
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
        [SerializeField] private float startingThirst = 100f;

        #if ODIN_INSPECTOR
        [PropertyRange(0, 100)]
        #endif
        [SerializeField] private float startingSanity = 100f;

        #if ODIN_INSPECTOR
        [PropertyRange(0, 100)]
        #endif
        [SerializeField] private float startingHealth = 100f;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string CharacterName => characterName;
        public Sprite Portrait => portrait;
        public float StartingHunger => startingHunger;
        public float StartingThirst => startingThirst;
        public float StartingSanity => startingSanity;
        public float StartingHealth => startingHealth;

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public Character CreateCharacter()
        {
            return new Character(characterName, startingHunger, startingThirst, startingSanity, startingHealth);
        }
    }
}
