using UnityEngine;

namespace TheBunkerGames
{
    /// <summary>
    /// Pure logic controller for City Exploration.
    /// Handles risk calculation and loot determination.
    /// State-less where possible.
    /// </summary>
    public class ExplorationRulesController
    {
        public float GetRiskFactor(ExplorationRisk risk)
        {
            switch (risk)
            {
                case ExplorationRisk.Low: return 0.3f;
                case ExplorationRisk.Medium: return 0.6f;
                case ExplorationRisk.High: return 0.85f;
                case ExplorationRisk.Deadly: return 1.0f;
                default: return 0.5f;
            }
        }

        public ExplorationResult ResolveExpedition(Expedition expedition, LootTableSO lootTable)
        {
            var result = new ExplorationResult
            {
                ExplorerName = expedition.ExplorerName,
                LocationName = expedition.Location.LocationName,
                FoundItems = new System.Collections.Generic.List<ResourceGrantData>(),
                NarrativeLog = ""
            };

            float riskFactor = GetRiskFactor(expedition.Location.Risk);

            // Determine if injured
            result.IsInjured = Random.value < riskFactor * 0.5f;
            result.HealthChange = result.IsInjured ? -Random.Range(10f, 30f) : 0f;
            result.SanityChange = -Random.Range(5f, 15f) * riskFactor;

            // Generate loot based on risk (higher risk = better rewards)
            int lootCount = Mathf.FloorToInt(Random.Range(0f, 3f) * riskFactor + 1);
            
            if (lootTable != null)
            {
                for (int i = 0; i < lootCount; i++)
                {
                    string itemId = lootTable.GetRandomLootId(expedition.Location.Risk);
                    result.FoundItems.Add(new ResourceGrantData(itemId, 1));
                }
            }
            else
            {
                Debug.LogWarning("[ExplorationRules] No Loot Table provided!");
            }

            // Simple narrative generation (could be moved to a Narrative Controller later)
            result.NarrativeLog = $"{expedition.ExplorerName} ventured into {expedition.Location.LocationName}. ";
            if (result.IsInjured)
            {
                result.NarrativeLog += "They returned battered and bruised. ";
            }
            result.NarrativeLog += $"They found {result.FoundItems.Count} item(s).";

            return result;
        }
    }
}
