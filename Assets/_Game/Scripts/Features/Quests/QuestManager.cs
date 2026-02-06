using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// String-based quest manager for AI integration.
    /// AI sends commands like AddQuest("FindWater", "Locate clean water").
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static QuestManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // QuestData Data
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Active Quests")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<QuestData> quests = new List<QuestData>();

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<QuestData> Quests => quests;
        public List<QuestData> ActiveQuests => quests.FindAll(q => q.IsActive);
        public List<QuestData> CompletedQuests => quests.FindAll(q => q.IsCompleted);

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // -------------------------------------------------------------------------
        // Public Methods (AI Interface)
        // -------------------------------------------------------------------------
        /// <summary>
        /// Add a new quest. Called by AI: AddQuest("FindWater", "Locate a clean water source")
        /// </summary>
        public void AddQuest(string id, string description)
        {
            if (string.IsNullOrEmpty(id)) return;

            var existing = GetQuest(id);
            if (existing != null)
            {
                Debug.LogWarning($"[QuestManager] QuestData already exists: {id}");
                return;
            }

            var quest = new QuestData(id, description, QuestState.Active);
            quests.Add(quest);
            Debug.Log($"[QuestManager] QuestData added: {id} - {description}");
        }

        /// <summary>
        /// Update quest state. Called by AI: UpdateQuest("FindWater", "COMPLETED")
        /// </summary>
        public void UpdateQuest(string id, string newState)
        {
            var quest = GetQuest(id);
            if (quest == null)
            {
                Debug.LogWarning($"[QuestManager] QuestData not found: {id}");
                return;
            }

            quest.SetState(newState);
            Debug.Log($"[QuestManager] QuestData updated: {id} -> {newState}");
        }

        /// <summary>
        /// Get a quest by ID.
        /// </summary>
        public QuestData GetQuest(string id)
        {
            return quests.Find(q => q.Id == id);
        }

        /// <summary>
        /// Remove a quest entirely.
        /// </summary>
        public void RemoveQuest(string id)
        {
            var quest = GetQuest(id);
            if (quest != null)
            {
                quests.Remove(quest);
                Debug.Log($"[QuestManager] QuestData removed: {id}");
            }
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [HorizontalGroup("QuestData")]
        [SerializeField] private string debugQuestId = "FindWater";
        
        [HorizontalGroup("QuestData")]
        [SerializeField] private string debugDescription = "Locate a clean water source";

        [Button("Add QuestData", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_AddQuest()
        {
            if (Application.isPlaying)
            {
                AddQuest(debugQuestId, debugDescription);
            }
        }

        [HorizontalGroup("State")]
        [Button("Complete QuestData", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_CompleteQuest()
        {
            if (Application.isPlaying)
            {
                UpdateQuest(debugQuestId, QuestState.Completed);
            }
        }

        [HorizontalGroup("State")]
        [Button("Fail QuestData", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void Debug_FailQuest()
        {
            if (Application.isPlaying)
            {
                UpdateQuest(debugQuestId, QuestState.Failed);
            }
        }

        [Button("Add Sample Quests", ButtonSizes.Medium)]
        [GUIColor(0.8f, 0.8f, 0.5f)]
        private void Debug_AddSampleQuests()
        {
            if (Application.isPlaying)
            {
                AddQuest("FindWater", "Locate a clean water source");
                AddQuest("FixFilter", "Repair the air filtration system");
                AddQuest("FindMedicine", "Scavenge for medical supplies");
            }
        }

        [Button("Log All Quests", ButtonSizes.Medium)]
        private void Debug_LogQuests()
        {
            Debug.Log($"[QuestManager] Total quests: {quests.Count}, Active: {ActiveQuests.Count}");
            foreach (var quest in quests)
            {
                Debug.Log($"  - [{quest.State}] {quest.Id}: {quest.Description}");
            }
        }

        [Title("Auto Setup")]
        [Button("Auto Setup Dependencies", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void AutoSetupDependencies()
        {
            #if UNITY_EDITOR
            // Ensure Tester exists
            var testerType = System.Type.GetType("TheBunkerGames.Tests.QuestTester");
            if (testerType != null && GetComponent(testerType) == null)
            {
                gameObject.AddComponent(testerType);
                Debug.Log("[QuestManager] Added QuestTester.");
            }
            else if (testerType == null)
            {
                Debug.LogWarning("[QuestManager] Could not find QuestTester type. Ensure it is in TheBunkerGames.Tests namespace.");
            }
            #endif
        }
        #endif
    }
}
