using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Spawns CharacterPanel instances for each family member.
    /// Waits one frame for GameSetup to populate characters first.
    /// </summary>
    public class FamilyDisplayUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject characterPanelPrefab;
        [SerializeField] private Transform panelContainer;

        private List<CharacterPanel> activePanels = new List<CharacterPanel>();

        private IEnumerator Start()
        {
            yield return null;
            BuildDisplay();
        }

        private void BuildDisplay()
        {
            if (FamilyManager.Instance == null) return;

            foreach (var panel in activePanels)
            {
                if (panel != null) Destroy(panel.gameObject);
            }
            activePanels.Clear();

            foreach (var character in FamilyManager.Instance.FamilyMembers)
            {
                if (characterPanelPrefab == null) continue;
                GameObject panelObj = Instantiate(characterPanelPrefab, panelContainer);
                panelObj.SetActive(true);
                CharacterPanel panel = panelObj.GetComponent<CharacterPanel>();
                if (panel != null)
                {
                    panel.SetCharacter(character);
                    activePanels.Add(panel);
                }
            }
        }

        public void RefreshDisplay()
        {
            BuildDisplay();
        }
    }
}
