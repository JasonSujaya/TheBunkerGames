using UnityEngine;
using UnityEditor;

namespace TheBunkerGames.Editor
{
    /// <summary>
    /// Custom editor for FamilySelectUI.
    /// Provides an "Auto Setup" button for non-Odin users.
    /// </summary>
    [CustomEditor(typeof(FamilySelectUI))]
    public class FamilySelectUIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);

            FamilySelectUI manager = (FamilySelectUI)target;

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("Auto Setup", GUILayout.Height(35)))
            {
                Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Auto Setup FamilySelectUI");
                manager.AutoSetup();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
