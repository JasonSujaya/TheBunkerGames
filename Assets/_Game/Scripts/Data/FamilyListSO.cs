using UnityEngine;
using System.Collections.Generic;

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "FamilyListSO", menuName = "TheBunkerGames/Family List")]
    public class FamilyListSO : ScriptableObject
    {
        public List<CharacterDefinitionSO> DefaultFamilyMembers;
    }
}
