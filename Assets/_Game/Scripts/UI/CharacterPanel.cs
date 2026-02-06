using UnityEngine;
using UnityEngine.UI;

namespace TheBunkerGames
{
    /// <summary>
    /// Individual character display panel showing name and stat bars.
    /// Updated every frame to reflect current Character state.
    /// </summary>
    public class CharacterPanel : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private Text nameText;

        [Header("Hunger Bar")]
        [SerializeField] private Image hungerBarFill;
        [SerializeField] private Text hungerValueText;

        [Header("Thirst Bar")]
        [SerializeField] private Image thirstBarFill;
        [SerializeField] private Text thirstValueText;

        [Header("Sanity Bar")]
        [SerializeField] private Image sanityBarFill;
        [SerializeField] private Text sanityValueText;

        [Header("Health Bar")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Text healthValueText;

        [Header("Status")]
        [SerializeField] private GameObject deadOverlay;
        [SerializeField] private Text statusText;

        private Character trackedCharacter;

        public void SetCharacter(Character character)
        {
            trackedCharacter = character;
            if (nameText != null) nameText.text = character.Name;
            UpdateDisplay();
        }

        private void LateUpdate()
        {
            if (trackedCharacter != null)
                UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (trackedCharacter == null) return;

            UpdateBar(hungerBarFill, hungerValueText, trackedCharacter.Hunger, Color.red, Color.green);
            UpdateBar(thirstBarFill, thirstValueText, trackedCharacter.Thirst, Color.red, Color.cyan);
            UpdateBar(sanityBarFill, sanityValueText, trackedCharacter.Sanity, new Color(0.8f, 0.2f, 0.8f), Color.cyan);
            UpdateBar(healthBarFill, healthValueText, trackedCharacter.Health, Color.red, Color.white);

            if (deadOverlay != null)
                deadOverlay.SetActive(!trackedCharacter.IsAlive);

            if (statusText != null)
            {
                if (!trackedCharacter.IsAlive)
                    statusText.text = "DEAD";
                else if (trackedCharacter.IsCritical)
                    statusText.text = "CRITICAL";
                else if (trackedCharacter.IsInsane)
                    statusText.text = "INSANE";
                else if (trackedCharacter.IsDehydrated)
                    statusText.text = "DEHYDRATED";
                else if (trackedCharacter.IsInjured)
                    statusText.text = "INJURED";
                else if (trackedCharacter.IsExploring)
                    statusText.text = "EXPLORING";
                else
                    statusText.text = "";
            }
        }

        private void UpdateBar(Image fill, Text valueText, float stat, Color lowColor, Color highColor)
        {
            float normalized = stat / 100f;
            if (fill != null)
            {
                fill.fillAmount = normalized;
                fill.color = Color.Lerp(lowColor, highColor, normalized);
            }
            if (valueText != null)
                valueText.text = $"{stat:F0}";
        }
    }
}
