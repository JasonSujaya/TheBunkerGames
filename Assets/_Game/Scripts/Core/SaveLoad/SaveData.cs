using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Root save data object. Serialized to JSON via JsonUtility.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        // Game State
        public int CurrentDay;
        public string CurrentState;
        public bool IsGameOver;

        // Family
        public List<CharacterSaveData> FamilyMembers;

        // Inventory
        public List<InventorySlotSaveData> InventoryItems;

        // Quests
        public List<QuestSaveData> Quests;
    }

    [Serializable]
    public class CharacterSaveData
    {
        public string Name;
        public float Hunger;
        public float Thirst;
        public float Sanity;
        public float Health;
        public bool IsInjured;
    }

    [Serializable]
    public class InventorySlotSaveData
    {
        public string ItemId;
        public int Quantity;
    }

    [Serializable]
    public class QuestSaveData
    {
        public string Id;
        public string Description;
        public string State;
    }
}
