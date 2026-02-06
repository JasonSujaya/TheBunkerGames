using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Manages save/load persistence for the game.
    /// Serializes game state to JSON for retry loops and session persistence.
    /// </summary>
    public class SaveLoadManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static SaveLoadManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action OnGameSaved;
        public static event Action OnGameLoaded;

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        [SerializeField] private string saveFileName = "entropy_save.json";

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
        public bool SaveExists => File.Exists(SaveFilePath);

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
        // Save
        // -------------------------------------------------------------------------
        public void SaveGame()
        {
            var saveData = new SaveData();

            // Game state
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                saveData.CurrentDay = gameManager.CurrentDay;
                saveData.CurrentState = gameManager.CurrentState.ToString();
                saveData.IsGameOver = gameManager.IsGameOver;
            }

            // Family
            var familyManager = FamilyManager.Instance;
            if (familyManager != null)
            {
                saveData.FamilyMembers = new List<CharacterSaveData>();
                foreach (var character in familyManager.FamilyMembers)
                {
                    saveData.FamilyMembers.Add(new CharacterSaveData
                    {
                        Name = character.Name,
                        Hunger = character.Hunger,
                        Thirst = character.Thirst,
                        Sanity = character.Sanity,
                        Health = character.Health,
                        IsInjured = character.IsInjured
                    });
                }
            }

            // Inventory
            var inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null)
            {
                saveData.InventoryItems = new List<InventorySlotSaveData>();
                foreach (var slot in inventoryManager.Items)
                {
                    saveData.InventoryItems.Add(new InventorySlotSaveData
                    {
                        ItemId = slot.ItemId,
                        Quantity = slot.Quantity
                    });
                }
            }

            // A.N.G.E.L.
            var angel = AngelInteractionManager.Instance;
            if (angel != null)
            {
                saveData.AngelMood = angel.CurrentMood.ToString();
                saveData.AngelProcessingLevel = angel.ProcessingLevel;
            }

            // Quests
            var questManager = QuestManager.Instance;
            if (questManager != null)
            {
                saveData.Quests = new List<QuestSaveData>();
                foreach (var quest in questManager.Quests)
                {
                    saveData.Quests.Add(new QuestSaveData
                    {
                        Id = quest.Id,
                        Description = quest.Description,
                        State = quest.State
                    });
                }
            }

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"[SaveLoad] Game saved to: {SaveFilePath}");
            OnGameSaved?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Load
        // -------------------------------------------------------------------------
        public void LoadGame()
        {
            if (!SaveExists)
            {
                Debug.LogWarning("[SaveLoad] No save file found.");
                return;
            }

            string json = File.ReadAllText(SaveFilePath);
            var saveData = JsonUtility.FromJson<SaveData>(json);

            if (saveData == null)
            {
                Debug.LogError("[SaveLoad] Failed to parse save data.");
                return;
            }

            // Restore family
            var familyManager = FamilyManager.Instance;
            if (familyManager != null && saveData.FamilyMembers != null)
            {
                var characters = new List<CharacterData>();
                foreach (var charData in saveData.FamilyMembers)
                {
                    var character = new CharacterData(
                        charData.Name,
                        charData.Hunger,
                        charData.Thirst,
                        charData.Sanity,
                        charData.Health
                    );
                    character.IsInjured = charData.IsInjured;
                    characters.Add(character);
                }
                familyManager.LoadCharacters(characters);
            }

            // Restore inventory
            var inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null && saveData.InventoryItems != null)
            {
                inventoryManager.ClearInventory();
                foreach (var slotData in saveData.InventoryItems)
                {
                    inventoryManager.AddItem(slotData.ItemId, slotData.Quantity);
                }
            }

            // Restore quests
            var questManager = QuestManager.Instance;
            if (questManager != null && saveData.Quests != null)
            {
                // Clear and repopulate
                foreach (var questData in saveData.Quests)
                {
                    questManager.AddQuest(questData.Id, questData.Description);
                    if (questData.State != QuestState.Active)
                    {
                        questManager.UpdateQuest(questData.Id, questData.State);
                    }
                }
            }

            Debug.Log($"[SaveLoad] Game loaded. Day: {saveData.CurrentDay}");
            OnGameLoaded?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Delete
        // -------------------------------------------------------------------------
        public void DeleteSave()
        {
            if (SaveExists)
            {
                File.Delete(SaveFilePath);
                Debug.Log("[SaveLoad] Save file deleted.");
            }
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Save Game", ButtonSizes.Large)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_Save()
        {
            if (Application.isPlaying) SaveGame();
        }

        [Button("Load Game", ButtonSizes.Large)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_Load()
        {
            if (Application.isPlaying) LoadGame();
        }

        [Button("Delete Save", ButtonSizes.Medium)]
        [GUIColor(1f, 0.5f, 0.5f)]
        private void Debug_DeleteSave()
        {
            DeleteSave();
        }

        [Button("Log Save Path", ButtonSizes.Medium)]
        private void Debug_LogPath()
        {
            Debug.Log($"[SaveLoad] Save path: {SaveFilePath} | Exists: {SaveExists}");
        }
        #endif
    }
}
