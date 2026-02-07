using UnityEngine;
using System.Collections.Generic;

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "FamilyListSO", menuName = "TheBunkerGames/Family List")]
    public class FamilyListSO : ScriptableObject
    {
        public List<CharacterDefinitionSO> DefaultFamilyMembers;
        public List<ItemAmountConfig> StartingItems;
    }

    [System.Serializable]
    public struct ItemAmountConfig
    {
        public ItemData Item;
        public int Quantity;
    }
}
