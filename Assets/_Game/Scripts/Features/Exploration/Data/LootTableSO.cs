using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "LootTable", menuName = "The Bunker/Exploration/Loot Table")]
    public class LootTableSO : ScriptableObject
    {
        [System.Serializable]
        public class LootPool
        {
            public ExplorationRisk RiskLevel;
            public List<string> ItemIds = new List<string>();
        }

        #if ODIN_INSPECTOR
        [Title("Loot Configuration")]
        [TableList]
        #endif
        [SerializeField] private List<LootPool> lootPools = new List<LootPool>();

        public string GetRandomLootId(ExplorationRisk risk)
        {
            var pool = lootPools.Find(p => p.RiskLevel == risk);
            if (pool != null && pool.ItemIds.Count > 0)
            {
                return pool.ItemIds[Random.Range(0, pool.ItemIds.Count)];
            }
            
            // Fallback if strict match fails (or simplify to use a default pool)
            Debug.LogWarning($"[LootTable] No loot pool defined for {risk}, returning default Junk.");
            return "junk";
        }
    }
}
