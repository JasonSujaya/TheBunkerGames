using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Represents A.N.G.E.L.'s response to a player resource request.
    /// Populated by the AI or by mock fallback logic.
    /// </summary>
    [Serializable]
    public class AngelResponseData
    {
        public string Message = "";
        public AngelMood Mood;
        public List<ResourceGrantData> GrantedItems = new List<ResourceGrantData>();
        public string EmotionalTag = "Neutral";
    }

    /// <summary>
    /// A single resource grant from A.N.G.E.L.
    /// </summary>
    [Serializable]
    public class ResourceGrantData
    {
        public string ItemId;
        public int Quantity;

        public ResourceGrantData() { }

        public ResourceGrantData(string itemId, int quantity)
        {
            ItemId = itemId;
            Quantity = quantity;
        }
    }
}
