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
        // Database Reference
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        [Required("Quest Database is required")]
        #endif
        [SerializeField] private QuestDatabaseDataSO questDatabase;

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
        public QuestDatabaseDataSO Database => questDatabase;

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

            // Initialize database singleton
            if (questDatabase != null)
            {
                QuestDatabaseDataSO.SetInstance(questDatabase);
            }
            else
            {
                // Try to load from Resources
                questDatabase = Resources.Load<QuestDatabaseDataSO>("QuestDatabaseDataSO");
                if (questDatabase != null)
                {
                    QuestDatabaseDataSO.SetInstance(questDatabase);
                }
                else
                {
                    Debug.LogWarning("[QuestManager] QuestDatabaseDataSO not assigned and not found in Resources!");
                }
            }
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
        [TitleGroup("Debug Controls")]
        [ValueDropdown("GetAllQuestProfileList")]
        [SerializeField] private QuestDefinitionSO debugQuestProfile;

        [Button("Add Quest From SO")]
        private void Debug_AddQuestFromSO()
        {
            if (debugQuestProfile != null)
            {
                AddQuest(debugQuestProfile.QuestId, debugQuestProfile.Description);
            }
            else
            {
                Debug.LogWarning("[QuestManager] No Quest Profile selected.");
            }
        }

        private IEnumerable<ValueDropdownItem<QuestDefinitionSO>> GetAllQuestProfileList()
        {
            var list = new ValueDropdownList<QuestDefinitionSO>();

            // 1. Persistent
            if (questDatabase != null && questDatabase.AllQuests != null)
            {
                foreach (var q in questDatabase.AllQuests)
                {
                    if (q != null)
                        list.Add($"[P] {q.QuestId}", q);
                }
            }

            // 2. Session
            if (QuestCreator.Instance != null && QuestCreator.Instance.SessionQuests != null)
            {
                foreach (var q in QuestCreator.Instance.SessionQuests)
                {
                    if (q != null)
                        list.Add($"[S] {q.Id}", null); // QuestData cannot be cast to QuestDefinitionSO
                }
            }

            return list;
        }

        [SerializeField] private string debugQuestId = "FindWater";
        [SerializeField] private string debugDescription = "Locate a clean water source";

        [Button("Add Manual Quest")]
        private void Debug_AddQuest()
        {
            if (Application.isPlaying)
            {
                AddQuest(debugQuestId, debugDescription);
            }
        }

        [Button("Complete Quest")]
        private void Debug_CompleteQuest()
        {
            if (Application.isPlaying)
            {
                UpdateQuest(debugQuestId, QuestState.Completed);
            }
        }

        [Button("Fail Quest")]
        private void Debug_FailQuest()
        {
            if (Application.isPlaying)
            {
                UpdateQuest(debugQuestId, QuestState.Failed);
            }
        }

        [Button("Add Sample Quests")]
        private void Debug_AddSampleQuests()
        {
            if (Application.isPlaying)
            {
                AddQuest("FindWater", "Locate a clean water source");
                AddQuest("FixFilter", "Repair the air filtration system");
                AddQuest("FindMedicine", "Scavenge for medical supplies");
            }
        }

        [Button("Log All Quests")]
        private void Debug_LogQuests()
        {
            Debug.Log($"[QuestManager] Total quests: {quests.Count}, Active: {ActiveQuests.Count}");
            foreach (var quest in quests)
            {
                Debug.Log($"  - [{quest.State}] {quest.Id}: {quest.Description}");
            }
        }
        #endif
    }
}
