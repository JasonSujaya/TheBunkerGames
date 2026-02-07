using UnityEngine;
using System;

namespace TheBunkerGames
{
    /// <summary>
    /// Plain C# class representing a family member in the bunker.
    /// Serializable for save/load functionality.
    /// Tracks four core stats: Hunger, Thirst, Sanity, Health.
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        // -------------------------------------------------------------------------
        // Identity
        // -------------------------------------------------------------------------
        public string Name;

        // -------------------------------------------------------------------------
        // Stats (0-100 range)
        // -------------------------------------------------------------------------
        public float Hunger = 100f;
        public float Thirst = 100f;
        public float Sanity = 100f;
        public float Health = 100f;
        public CharacterSubtype Subtype = CharacterSubtype.Family;

        // -------------------------------------------------------------------------
        // Status Conditions
        // -------------------------------------------------------------------------
        public bool IsExploring;
        public bool IsInjured;
        public bool IsDead;

        // Sickness system
        public bool IsSick;
        public SicknessType Sickness = SicknessType.None;
        public int SicknessSeverity; // 1-10, 0 = not sick

        // -------------------------------------------------------------------------
        // Constructors
        // -------------------------------------------------------------------------
        public CharacterData()
        {
            Name = "Unknown";
            Hunger = 100f;
            Thirst = 100f;
            Sanity = 100f;
            Health = 100f;
            Subtype = CharacterSubtype.Family;
            IsDead = false;
        }

        public CharacterData(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f, CharacterSubtype subtype = CharacterSubtype.Family)
        {
            Name = name;
            Hunger = Mathf.Clamp(hunger, 0f, 100f);
            Thirst = Mathf.Clamp(thirst, 0f, 100f);
            Sanity = Mathf.Clamp(sanity, 0f, 100f);
            Health = Mathf.Clamp(health, 0f, 100f);
            Subtype = subtype;
            IsDead = Health <= 0;
        }

        // -------------------------------------------------------------------------
        // Stat Modifiers
        // -------------------------------------------------------------------------
        public void ModifyHunger(float amount)
        {
            if (IsDead) return;
            Hunger = Mathf.Clamp(Hunger + amount, 0f, 100f);
        }

        public void ModifyThirst(float amount)
        {
            if (IsDead) return;
            Thirst = Mathf.Clamp(Thirst + amount, 0f, 100f);
        }

        public void ModifySanity(float amount)
        {
            if (IsDead) return;
            Sanity = Mathf.Clamp(Sanity + amount, 0f, 100f);
        }

        public void ModifyHealth(float amount)
        {
            if (IsDead) return;
            
            Health = Mathf.Clamp(Health + amount, 0f, 100f);
            
            if (Health <= 0)
            {
                IsDead = true;
                Health = 0;
                Debug.Log($"[CharacterData] {Name} has died.");
            }
        }

        // -------------------------------------------------------------------------
        // Sickness Methods
        // -------------------------------------------------------------------------
        public void Infect(SicknessType type, int severity)
        {
            if (IsDead) return;
            IsSick = true;
            Sickness = type;
            SicknessSeverity = Mathf.Clamp(severity, 1, 10);
            Debug.Log($"[CharacterData] {Name} got sick: {type} (severity {severity})");
        }

        public void CureSickness()
        {
            IsSick = false;
            Sickness = SicknessType.None;
            SicknessSeverity = 0;
            Debug.Log($"[CharacterData] {Name} has been cured.");
        }

        // -------------------------------------------------------------------------
        // Derived States
        // -------------------------------------------------------------------------
        public bool IsAlive => !IsDead;
        public bool IsInsane => Sanity <= 0f;
        public bool IsStarving => Hunger <= 0f;
        public bool IsDehydrated => Thirst <= 0f;
        public bool IsCritical => Health <= 20f || Hunger <= 10f || Thirst <= 10f;
        public bool IsAvailableForExploration => IsAlive && !IsExploring && !IsInjured && !IsSick;

        /// <summary>
        /// Get a status summary string for debug/LLM context.
        /// </summary>
        public string GetStatusSummary()
        {
            if (IsDead) return "DEAD";
            var tags = new System.Collections.Generic.List<string>();
            if (IsCritical) tags.Add("CRITICAL");
            if (IsInjured) tags.Add("INJURED");
            if (IsSick) tags.Add($"SICK:{Sickness}(severity {SicknessSeverity})");
            if (IsInsane) tags.Add("INSANE");
            if (IsStarving) tags.Add("STARVING");
            if (IsDehydrated) tags.Add("DEHYDRATED");
            return tags.Count > 0 ? string.Join(", ", tags) : "OK";
        }
    }
}
