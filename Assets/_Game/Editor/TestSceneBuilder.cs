using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace TheBunkerGames.Editor
{
    /// <summary>
    /// Editor tool that creates a SystemTest scene with all managers and controllers
    /// wired up on separate GameObjects for testing individual systems.
    /// </summary>
    public class TestSceneBuilder
    {
        [MenuItem("TheBunkerGames/Create System Test Scene")]
        private static void CreateTestScene()
        {
            // Create and save a new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // ---------------------------------------------------------------
            // [GameSystems] — Core managers that live for the whole game
            // ---------------------------------------------------------------
            var gameSystems = new GameObject("--- GAME SYSTEMS ---");

            var gameManagerObj = new GameObject("GameManager");
            gameManagerObj.transform.SetParent(gameSystems.transform);
            gameManagerObj.AddComponent<GameManager>();

            var familyManagerObj = new GameObject("FamilyManager");
            familyManagerObj.transform.SetParent(gameSystems.transform);
            familyManagerObj.AddComponent<FamilyManager>();

            var inventoryManagerObj = new GameObject("InventoryManager");
            inventoryManagerObj.transform.SetParent(gameSystems.transform);
            inventoryManagerObj.AddComponent<InventoryManager>();

            var questManagerObj = new GameObject("QuestManager");
            questManagerObj.transform.SetParent(gameSystems.transform);
            questManagerObj.AddComponent<QuestManager>();

            var saveLoadObj = new GameObject("SaveLoadManager");
            saveLoadObj.transform.SetParent(gameSystems.transform);
            saveLoadObj.AddComponent<SaveLoadManager>();

            // ---------------------------------------------------------------
            // [Phase Controllers] — One per core loop phase
            // ---------------------------------------------------------------
            var phaseControllers = new GameObject("--- PHASE CONTROLLERS ---");

            var statusReviewObj = new GameObject("StatusReviewController");
            statusReviewObj.transform.SetParent(phaseControllers.transform);
            statusReviewObj.AddComponent<StatusReviewController>();

            var angelObj = new GameObject("AngelInteractionController");
            angelObj.transform.SetParent(phaseControllers.transform);
            angelObj.AddComponent<AngelInteractionController>();

            var explorationObj = new GameObject("CityExplorationController");
            explorationObj.transform.SetParent(phaseControllers.transform);
            explorationObj.AddComponent<CityExplorationController>();

            var choiceObj = new GameObject("DailyChoiceController");
            choiceObj.transform.SetParent(phaseControllers.transform);
            choiceObj.AddComponent<DailyChoiceController>();

            var nightObj = new GameObject("NightCycleController");
            nightObj.transform.SetParent(phaseControllers.transform);
            nightObj.AddComponent<NightCycleController>();

            // ---------------------------------------------------------------
            // [Game Setup] — Spawns initial family on Start
            // ---------------------------------------------------------------
            var setupObj = new GameObject("--- GAME SETUP ---");

            var gameSetupObj = new GameObject("GameSetup");
            gameSetupObj.transform.SetParent(setupObj.transform);
            gameSetupObj.AddComponent<GameSetup>();

            var flowObj = new GameObject("GameFlowController");
            flowObj.transform.SetParent(setupObj.transform);
            flowObj.AddComponent<GameFlowController>();

            // ---------------------------------------------------------------
            // Save the scene
            // ---------------------------------------------------------------
            string scenePath = "Assets/_Game/Scenes/SystemTest.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log($"[TestSceneBuilder] SystemTest scene created at: {scenePath}");
            Debug.Log("[TestSceneBuilder] Hierarchy:");
            Debug.Log("  --- GAME SYSTEMS ---");
            Debug.Log("    GameManager, FamilyManager, InventoryManager, QuestManager, SaveLoadManager");
            Debug.Log("  --- PHASE CONTROLLERS ---");
            Debug.Log("    StatusReviewController, AngelInteractionController, CityExplorationController, DailyChoiceController, NightCycleController");
            Debug.Log("  --- GAME SETUP ---");
            Debug.Log("    GameSetup (spawns family), GameFlowController (advances phases)");
            Debug.Log("[TestSceneBuilder] Hit Play to test. Use Odin Inspector buttons on each component to test individual systems.");
        }
    }
}
