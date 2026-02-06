using System;

namespace TheBunkerGames
{
    /// <summary>
    /// Lightweight POCO for LLM-generated item data.
    /// Used for JSON deserialization before creating actual ItemData assets.
    /// </summary>
    [Serializable]
    public class GeneratedItemData
    {
        public string itemName;
        public string description;
        public string itemType; // Will be parsed to ItemType enum
        public float value = 1f;
        public bool isConsumable = false;
        public float hungerRestore = 0f;
        public float thirstRestore = 0f;
        public float sanityRestore = 0f;
        public float healthRestore = 0f;
    }

    /// <summary>
    /// Lightweight POCO for LLM-generated character data.
    /// </summary>
    [Serializable]
    public class GeneratedCharacterData
    {
        public string name;
        public float hunger = 100f;
        public float thirst = 100f;
        public float sanity = 100f;
        public float health = 100f;
        public string subtype = "Family"; // Will be parsed to CharacterSubtype enum
        public string backstory;
    }

    /// <summary>
    /// Lightweight POCO for LLM-generated quest data.
    /// </summary>
    [Serializable]
    public class GeneratedQuestData
    {
        public string id;
        public string description;
        public string state = "ACTIVE"; // ACTIVE, COMPLETED, FAILED
    }

    /// <summary>
    /// Lightweight POCO for LLM-generated place data.
    /// </summary>
    [Serializable]
    public class GeneratedPlaceData
    {
        public string placeId;
        public string placeName;
        public string description;
        public int dangerLevel = 1;
        public int estimatedLootValue = 50;
    }
}
