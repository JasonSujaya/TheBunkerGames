using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles runtime creation of quests for AI-native gameplay.
    /// A.N.G.E.L. can generate dynamic objectives on the fly.
    /// Quests created here are SESSION-BOUND and do not persist between sessions.
    /// </summary>
    public class QuestCreator : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static QuestCreator Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Settings")]
        #endif
        [SerializeField] private QuestManager questManager;

        // -------------------------------------------------------------------------
        // Session-Bound Runtime Quests (NOT persisted)
        // -------------------------------------------------------------------------
        private List<QuestData> sessionQuests = new List<QuestData>();

        public IReadOnlyList<QuestData> SessionQuests => sessionQuests;

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

            if (questManager == null)
            {
                questManager = QuestManager.Instance;
            }
        }

        private void OnDestroy()
        {
            ClearSessionQuests();
        }

        // -------------------------------------------------------------------------
        // Public Methods - Quest Creation (Session-Bound)
        // -------------------------------------------------------------------------
        public QuestData CreateRuntimeQuest(string id, string description)
        {
            var newQuest = new QuestData(id, description, QuestState.Active);
            sessionQuests.Add(newQuest);

            Debug.Log($"[QuestCreator] Created session quest: {id} (Total: {sessionQuests.Count})");
            return newQuest;
        }

        public QuestData CreateAndAddToManager(string id, string description)
        {
            var newQuest = CreateRuntimeQuest(id, description);
            
            if (questManager != null)
            {
                questManager.Quests.Add(newQuest);
                Debug.Log($"[QuestCreator] Added quest {id} to manager.");
            }
            else
            {
                Debug.LogError("[QuestCreator] QuestManager is null! Cannot add quest.");
            }

            return newQuest;
        }

        public QuestData GenerateRandomQuest()
        {
            string[] quests = new[] {
                "FindWater|Locate a clean water source in the eastern sector",
                "FixFilter|The air filtration system is failing - find spare parts",
                "FindMedicine|Scavenge for medical supplies in the old pharmacy",
                "SecureDoor|The bunker door seal is compromised - repair it",
                "FindFood|Food supplies are running low - scavenge the warehouse",
                "RestorePower|Generator is failing - find fuel cells",
                "ContactSurvivors|Radio signal detected - investigate",
                "InvestigateNoise|Strange sounds from sector 7B - A.N.G.E.L. demands answers"
            };
            
            var data = quests[Random.Range(0, quests.Length)].Split('|');
            
            // Add a unique suffix to avoid duplicate IDs
            string uniqueId = $"{data[0]}_{System.DateTime.Now.Ticks % 10000}";
            
            return CreateAndAddToManager(uniqueId, data[1]);
        }

        public QuestData GetSessionQuest(string id)
        {
            return sessionQuests.FirstOrDefault(q => q != null && q.Id == id);
        }

        public void ClearSessionQuests()
        {
            sessionQuests.Clear();
            Debug.Log("[QuestCreator] Cleared all session quests.");
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [TitleGroup("Debug Controls")]
        [ShowInInspector, ReadOnly]
        private int SessionQuestCount => sessionQuests?.Count ?? 0;

        [Button("Generate 1 Random Quest")]
        private void Debug_CreateRandomQuest()
        {
            GenerateRandomQuest();
        }

        [Button("Generate 3 Random Quests")]
        private void Debug_Generate3Quests()
        {
            for (int i = 0; i < 3; i++)
            {
                GenerateRandomQuest();
            }
        }

        [SerializeField] private string customQuestId = "NewQuest";
        
        [TextArea(2, 4)]
        [SerializeField] private string customDescription = "A.N.G.E.L. has a new objective for you.";

        [Button("Create Custom Quest", ButtonSizes.Medium)]
        private void Debug_CreateCustomQuest()
        {
            CreateAndAddToManager(customQuestId, customDescription);
        }

        [Button("Clear All Session Quests")]
        private void Debug_ClearSession()
        {
            ClearSessionQuests();
        }

        [Button("Log Session Quests")]
        private void Debug_LogSessionQuests()
        {
            Debug.Log($"[QuestCreator] Session quests ({sessionQuests.Count}):");
            foreach (var q in sessionQuests)
            {
                if (q != null)
                {
                    Debug.Log($"  - [{q.State}] {q.Id}: {q.Description}");
                }
            }
        }
        #endif
    }
}
