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
        
        #if ODIN_INSPECTOR
        [Title("LLM Settings")]
        [InfoBox("When enabled, quests are generated via LLM. When disabled, uses static fallback data.")]
        #endif
        [SerializeField] private bool useLLM = true;
        [SerializeField] private LLMPromptTemplateSO questPromptTemplate;


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

        public void GenerateRandomQuest(System.Action<QuestData> onComplete = null)
        {
            if (useLLM && LLMManager.Instance != null && questPromptTemplate != null)
            {
                Debug.Log("[QuestCreator] <color=cyan>[LLM]</color> Generating quest via AI...");
                string userPrompt = questPromptTemplate.BuildUserPrompt("urgent bunker survival objective");
                
                LLMManager.Instance.QuickChat(
                    userPrompt,
                    onSuccess: (response) => {
                        if (LLMJsonParser.TryParseQuest(response, out var data))
                        {
                            var quest = CreateAndAddToManager(data.id, data.description);
                            Debug.Log($"[QuestCreator] <color=cyan>[LLM]</color> Created: {data.id}");
                            onComplete?.Invoke(quest);
                        }
                        else
                        {
                            Debug.LogWarning("[QuestCreator] <color=yellow>[LLM]</color> Failed to parse, using fallback.");
                            var fallback = GenerateFallbackQuest();
                            onComplete?.Invoke(fallback);
                        }
                    },
                    onError: (err) => {
                        Debug.LogError($"[QuestCreator] <color=red>[LLM ERROR]</color> {err}");
                        var fallback = GenerateFallbackQuest();
                        onComplete?.Invoke(fallback);
                    },
                    systemPrompt: questPromptTemplate.SystemPrompt + "\n\nSchema:\n" + questPromptTemplate.JsonSchemaExample,
                    useJsonMode: true
                );
            }
            else
            {
                Debug.Log("[QuestCreator] <color=orange>[STATIC]</color> LLM disabled, using fallback data.");
                var quest = GenerateFallbackQuest();
                onComplete?.Invoke(quest);
            }
        }

        private QuestData GenerateFallbackQuest()
        {
            string[] quests = {
                "FindWater|Locate a clean water source in the eastern sector",
                "FixFilter|The air filtration system is failing - find spare parts",
                "SecureDoor|The bunker door seal is compromised - repair it"
            };
            var data = quests[Random.Range(0, quests.Length)].Split('|');
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

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Generate 1 Random Quest")]
        private void Debug_CreateRandomQuest()
        {
            GenerateRandomQuest();
        }

        [GUIColor(0.2f, 0.6f, 1.0f)]
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

        [GUIColor(0.2f, 0.6f, 1.0f)]
        [Button("Create Custom Quest")]
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
