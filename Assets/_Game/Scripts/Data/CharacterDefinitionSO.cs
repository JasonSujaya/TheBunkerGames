using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "CharacterDefinitionSO", menuName = "TheBunkerGames/Character Definition")]
    public class CharacterDefinitionSO : ScriptableObject
    {
        #if ODIN_INSPECTOR
        [Title("Character Info")]
        #endif
        [SerializeField] private string characterName;
        [SerializeField] private CharacterRole role;
        [SerializeField] private CharacterSubtype subtype = CharacterSubtype.Family;
        [TextArea(3, 5)]
        [SerializeField] private string description;
        [SerializeField] private Sprite portrait;
        [SerializeField] private Sprite bodyImage;

        #if ODIN_INSPECTOR
        [Title("Base Stats")]
        #endif
        [Range(0, 100)] [SerializeField] private float maxHealth = 100f;
        [Range(0, 100)] [SerializeField] private float maxHunger = 100f;
        [Range(0, 100)] [SerializeField] private float maxThirst = 100f;
        [Range(0, 100)] [SerializeField] private float maxSanity = 100f;

        #if ODIN_INSPECTOR
        [Title("Traits")]
        #endif
        // Placeholder for future traits system
        [SerializeField] private string[] startingTraits;

        public string CharacterName => characterName;
        public CharacterRole Role => role;
        public CharacterSubtype Subtype => subtype;
        public string Description => description;
        public Sprite Portrait => portrait;
        public Sprite BodyImage => bodyImage;
        public float MaxHealth => maxHealth;
        public float MaxHunger => maxHunger;
        public float MaxThirst => maxThirst;
        public float MaxSanity => maxSanity;
        public string[] StartingTraits => startingTraits;

        // Factory method to create runtime data instance
        public CharacterData CreateCharacter()
        {
            return new CharacterData(characterName, maxHunger, maxThirst, maxSanity, maxHealth, subtype);
        }
    }
}
