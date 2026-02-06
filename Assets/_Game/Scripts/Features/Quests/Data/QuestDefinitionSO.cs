using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// ScriptableObject for static quest definitions.
    /// Use this to define quests as assets that can be referenced by the QuestDatabaseDataSO.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDefinitionSO", menuName = "TheBunkerGames/Quest Definition")]
    public class QuestDefinitionSO : ScriptableObject
    {
        #if ODIN_INSPECTOR
        [Title("Quest Info")]
        #endif
        [SerializeField] private string questId;
        [SerializeField] private string questTitle;
        [TextArea(3, 5)]
        [SerializeField] private string description;

        #if ODIN_INSPECTOR
        [Title("Quest Settings")]
        #endif
        [SerializeField] private bool isMainQuest = false;
        [SerializeField] private int priority = 0;

        #if ODIN_INSPECTOR
        [Title("Rewards")]
        #endif
        [SerializeField] private string[] rewardItemIds;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string QuestId => questId;
        public string QuestTitle => questTitle;
        public string Description => description;
        public bool IsMainQuest => isMainQuest;
        public int Priority => priority;
        public string[] RewardItemIds => rewardItemIds;

        // -------------------------------------------------------------------------
        // Factory Method
        // -------------------------------------------------------------------------
        public QuestData CreateQuestData()
        {
            return new QuestData(questId, description, QuestState.Active);
        }

        // -------------------------------------------------------------------------
        // Runtime Initialization
        // -------------------------------------------------------------------------
        public void Initialize(string id, string title, string desc, bool mainQuest = false)
        {
            questId = id;
            questTitle = title;
            description = desc;
            isMainQuest = mainQuest;
        }
    }
}
