using UnityEngine;
using UnityEditor;

namespace TheBunkerGames.Editor
{
    /// <summary>
    /// Custom editor for GameplayHudUI.
    /// Provides an "Auto Setup" button for non-Odin users.
    /// </summary>
    [CustomEditor(typeof(GameplayHudUI))]
    public class GameplayHudUIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);

            GameplayHudUI manager = (GameplayHudUI)target;

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("Auto Setup", GUILayout.Height(35)))
            {
                Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Auto Setup GameplayHudUI");
                manager.AutoSetup();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
