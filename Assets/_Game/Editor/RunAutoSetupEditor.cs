using UnityEngine;
using UnityEditor;

namespace TheBunkerGames.Editor
{
    public static class RunAutoSetupEditor
    {
        [MenuItem("TheBunkerGames/Run FamilySelectUI AutoSetup")]
        public static void RunAutoSetup()
        {
            var ui = Object.FindFirstObjectByType<FamilySelectUI>(FindObjectsInactive.Include);
            if (ui == null)
            {
                Debug.LogError("[RunAutoSetup] FamilySelectUI not found in scene!");
                return;
            }

            ui.AutoSetup();
            EditorUtility.SetDirty(ui);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            Debug.Log("[RunAutoSetup] FamilySelectUI AutoSetup complete!");
        }
    }
}
