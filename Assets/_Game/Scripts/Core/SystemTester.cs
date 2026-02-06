using UnityEngine;
using System.Collections;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    public class SystemTester : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("<b>[SystemTester]</b> Initializing System Check...");
            StartCoroutine(CheckRoutine());
        }

        private IEnumerator CheckRoutine()
        {
            yield return null; // Wait one frame for Awake/Start of others

            Debug.Log("--- Checking Core Systems ---");
            CheckSystem("GameManager", FindObjectOfType<GameManager>());
            CheckSystem("AudioManager", FindObjectOfType<AudioManager>());
            
            Debug.Log("--- Checking Feature Managers ---");
            CheckSystem("FamilyManager", FamilyManager.Instance);
            CheckSystem("InventoryManager", InventoryManager.Instance);
            CheckSystem("QuestManager", FindObjectOfType<QuestManager>()); // Assuming it might not be a singleton yet
            
            Debug.Log("--- Checking Managers ---");
            CheckSystem("DailyChoiceManager", FindObjectOfType<DailyChoiceManager>());
            CheckSystem("StatusReviewManager", FindObjectOfType<StatusReviewManager>());
            CheckSystem("NightCycleManager", FindObjectOfType<NightCycleManager>());
            CheckSystem("CityExplorationManager", FindObjectOfType<CityExplorationManager>());
            
            Debug.Log("--- Checking AI ---");
            CheckSystem("NeocortexIntegrator", FindObjectOfType<NeocortexIntegrator>());
            CheckSystem("AngelInteractionManager", FindObjectOfType<AngelInteractionManager>());
        }

        private void CheckSystem(string name, MonoBehaviour instance)
        {
            if (instance != null)
                Debug.Log($"<color=green>[OK]</color> {name} is READY.");
            else
                Debug.LogError($"<color=red>[MISSING]</color> {name} NOT FOUND!");
        }

        #if ODIN_INSPECTOR
        [Button("Manual System Check")]
        public void ManualCheck()
        {
            StopAllCoroutines();
            StartCoroutine(CheckRoutine());
        }
        #endif
    }
}
