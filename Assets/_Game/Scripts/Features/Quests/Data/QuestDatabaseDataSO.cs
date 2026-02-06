using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject holding all quest definitions in the game.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDatabaseDataSO", menuName = "TheBunkerGames/Quest Database Data")]
    public class QuestDatabaseDataSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static QuestDatabaseDataSO instance;
        public static QuestDatabaseDataSO Instance => instance;

        public static void SetInstance(QuestDatabaseDataSO database)
        {
            if (database == null)
            {
                Debug.LogError("[QuestDatabaseDataSO] Attempted to set null instance!");
                return;
            }
            instance = database;
        }

        // -------------------------------------------------------------------------
        // Quest List
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("All Quests")]
        [Searchable]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<QuestDefinitionSO> allQuests = new List<QuestDefinitionSO>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<QuestDefinitionSO> AllQuests => allQuests;

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public QuestDefinitionSO GetQuest(string id)
        {
            for (int i = 0; i < allQuests.Count; i++)
            {
                if (allQuests[i] != null && allQuests[i].QuestId == id)
                {
                    return allQuests[i];
                }
            }
            Debug.LogWarning($"[QuestDatabaseDataSO] Quest not found: {id}");
            return null;
        }

        public List<QuestDefinitionSO> GetMainQuests()
        {
            List<QuestDefinitionSO> result = new List<QuestDefinitionSO>();
            for (int i = 0; i < allQuests.Count; i++)
            {
                if (allQuests[i] != null && allQuests[i].IsMainQuest)
                {
                    result.Add(allQuests[i]);
                }
            }
            return result;
        }

        public void AddQuest(QuestDefinitionSO quest)
        {
            if (quest != null && !allQuests.Contains(quest))
            {
                allQuests.Add(quest);
            }
        }

        public int RemoveNullEntries()
        {
            int removed = allQuests.RemoveAll(q => q == null);
            if (removed > 0)
            {
                Debug.Log($"[QuestDatabaseDataSO] Removed {removed} null/missing entries.");
            }
            return removed;
        }

        // -------------------------------------------------------------------------
        // Debug
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Log All Quests", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_LogAllQuests()
        {
            Debug.Log($"[QuestDatabaseDataSO] Total quests: {allQuests.Count}");
            foreach (var quest in allQuests)
            {
                if (quest != null)
                {
                    string mainTag = quest.IsMainQuest ? "[MAIN] " : "";
                    Debug.Log($"  - {mainTag}{quest.QuestId}: {quest.QuestTitle}");
                }
            }
        }

        [Button("Find and Add All Quest Assets", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void Debug_FindAndAddAll()
        {
#if UNITY_EDITOR
            RemoveNullEntries();
            
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:QuestDefinitionSO");
            int count = 0;
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                QuestDefinitionSO quest = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestDefinitionSO>(path);
                if (quest != null && !allQuests.Contains(quest))
                {
                    allQuests.Add(quest);
                    count++;
                }
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[QuestDatabaseDataSO] Added {count} new quests to the database.");
#endif
        }

        [Button("Clean Up Missing Quests", ButtonSizes.Medium)]
        [GUIColor(1f, 0.6f, 0.3f)]
        private void Debug_CleanUpMissing()
        {
#if UNITY_EDITOR
            int removed = RemoveNullEntries();
            UnityEditor.EditorUtility.SetDirty(this);
            if (removed == 0)
            {
                Debug.Log("[QuestDatabaseDataSO] No missing quests to clean up.");
            }
#endif
        }
        #endif
    }
}
