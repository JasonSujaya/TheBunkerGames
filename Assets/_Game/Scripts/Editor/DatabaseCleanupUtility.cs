using UnityEngine;
using UnityEditor;

namespace TheBunkerGames
{
    /// <summary>
    /// Editor utility to auto-clean all database SOs when exiting play mode.
    /// Handles ItemDatabaseDataSO, CharacterDatabaseDataSO, QuestDatabaseDataSO, and PlaceDatabaseDataSO.
    /// </summary>
    [InitializeOnLoad]
    public static class DatabaseCleanupUtility
    {
        static DatabaseCleanupUtility()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                CleanupAllDatabases();
            }
        }

        private static void CleanupAllDatabases()
        {
            bool anyChanges = false;

            // Clean ItemDatabaseDataSO
            string[] itemGuids = AssetDatabase.FindAssets("t:ItemDatabaseDataSO");
            foreach (string guid in itemGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<ItemDatabaseDataSO>(path);
                if (db != null && db.RemoveNullEntries() > 0)
                {
                    EditorUtility.SetDirty(db);
                    anyChanges = true;
                }
            }

            // Clean CharacterDatabaseDataSO
            string[] charGuids = AssetDatabase.FindAssets("t:CharacterDatabaseDataSO");
            foreach (string guid in charGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<CharacterDatabaseDataSO>(path);
                if (db != null && db.RemoveNullEntries() > 0)
                {
                    EditorUtility.SetDirty(db);
                    anyChanges = true;
                }
            }

            // Clean QuestDatabaseDataSO
            string[] questGuids = AssetDatabase.FindAssets("t:QuestDatabaseDataSO");
            foreach (string guid in questGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<QuestDatabaseDataSO>(path);
                if (db != null && db.RemoveNullEntries() > 0)
                {
                    EditorUtility.SetDirty(db);
                    anyChanges = true;
                }
            }

            // Clean PlaceDatabaseDataSO
            string[] placeGuids = AssetDatabase.FindAssets("t:PlaceDatabaseDataSO");
            foreach (string guid in placeGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var db = AssetDatabase.LoadAssetAtPath<PlaceDatabaseDataSO>(path);
                if (db != null && db.RemoveNullEntries() > 0)
                {
                    EditorUtility.SetDirty(db);
                    anyChanges = true;
                }
            }

            if (anyChanges)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("[DatabaseCleanupUtility] Cleaned up null entries from all databases.");
            }
        }
    }
}
