using UnityEngine;
using UnityEditor;

namespace TheBunkerGames.Editor
{
    /// <summary>
    /// Custom editor for ThemeSelectUI.
    /// Provides an "Auto Setup" button for non-Odin users.
    /// </summary>
    [CustomEditor(typeof(ThemeSelectUI))]
    public class ThemeSelectUIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);

            ThemeSelectUI manager = (ThemeSelectUI)target;

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("Auto Setup", GUILayout.Height(35)))
            {
                Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Auto Setup ThemeSelectUI");
                manager.AutoSetup();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
