using UnityEngine;

namespace TheBunkerGames
{
    /// <summary>
    /// Populates the FamilyManager with starter characters on game start.
    /// </summary>
    public class GameSetup : MonoBehaviour
    {
        [Header("Starting Family")]
        [SerializeField] private string[] characterNames = { "Marcus", "Elena", "Sam", "Lily" };
        [SerializeField] private float startingHunger = 100f;
        [SerializeField] private float startingThirst = 100f;
        [SerializeField] private float startingSanity = 100f;
        [SerializeField] private float startingHealth = 100f;

        private void Start()
        {
            if (FamilyManager.Instance == null) return;
            if (FamilyManager.Instance.FamilyMembers.Count > 0) return;

            for (int i = 0; i < characterNames.Length; i++)
            {
                float hunger = Mathf.Clamp(startingHunger - (i * 5f), 0f, 100f);
                float thirst = Mathf.Clamp(startingThirst - (i * 3f), 0f, 100f);
                float sanity = Mathf.Clamp(startingSanity - (i * 3f), 0f, 100f);
                float health = Mathf.Clamp(startingHealth, 0f, 100f);
                FamilyManager.Instance.AddCharacter(characterNames[i], hunger, thirst, sanity, health);
            }
        }
    }
}
