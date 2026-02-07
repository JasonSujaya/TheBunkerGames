using UnityEngine;
using UnityEditor;

namespace TheBunkerGames.Editor
{
    /// <summary>
    /// Custom editor for InventoryDisplayUI.
    /// Provides an "Auto Setup" button for non-Odin users.
    /// </summary>
    [CustomEditor(typeof(InventoryDisplayUI))]
    public class InventoryDisplayUIEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);

            InventoryDisplayUI manager = (InventoryDisplayUI)target;

            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            if (GUILayout.Button("Auto Setup", GUILayout.Height(35)))
            {
                Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Auto Setup InventoryDisplayUI");
                manager.AutoSetup();
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
