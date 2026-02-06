using UnityEngine;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Pure logic controller for the Night Cycle.
    /// Handles stat decay calculations and Dream Log selection.
    /// </summary>
    public class NightLogicController
    {
        public void ApplyStatDecay(List<CharacterData> familyMembers, GameConfigDataSO config, List<string> outStatChanges)
        {
            foreach (var character in familyMembers)
            {
                if (!character.IsAlive) continue;

                // Base decay
                float hungerDecay = config != null ? config.HungerDecayPerDay : 10f;
                float thirstDecay = config != null ? config.ThirstDecayPerDay : 10f;
                float sanityDecay = config != null ? config.SanityDecayPerDay : 5f;

                character.ModifyHunger(-hungerDecay);
                character.ModifyThirst(-thirstDecay);
                character.ModifySanity(-sanityDecay);

                // Consequences
                if (character.IsDehydrated) character.ModifyHealth(-10f);
                if (character.Hunger <= 0f) character.ModifyHealth(-15f);
                if (character.IsInsane) character.ModifySanity(-5f);

                // Healing
                if (character.IsInjured && character.Health > 50f)
                {
                    character.IsInjured = false; // Simple rule: heal if healthy enough
                }

                outStatChanges.Add(
                    $"{character.Name}: H:{character.Hunger:F0} T:{character.Thirst:F0} " +
                    $"S:{character.Sanity:F0} HP:{character.Health:F0}"
                );
            }
        }

        public string GenerateDreamLog(List<CharacterData> familyMembers, DreamDatabaseSO dreamDB, out bool isNightmare)
        {
            float averageSanity = 0f;
            int aliveCount = 0;
            foreach (var c in familyMembers)
            {
                if (c.IsAlive)
                {
                    averageSanity += c.Sanity;
                    aliveCount++;
                }
            }
            if (aliveCount > 0) averageSanity /= aliveCount;

            isNightmare = averageSanity < 40f;

            if (dreamDB != null)
            {
                return dreamDB.GetRandomLog(isNightmare);
            }
            
            return isNightmare ? "Nightmares plague the bunker..." : "The bunker sleeps soundly...";
        }

        public float CalculateAngelDegradation(NightReportData report)
        {
            float degradation = 5f; // Base
            if (report.IsNightmare) degradation += 10f;
            if (report.DeathsThisNight.Count > 0) degradation += 15f * report.DeathsThisNight.Count;
            return degradation;
        }
    }
}
