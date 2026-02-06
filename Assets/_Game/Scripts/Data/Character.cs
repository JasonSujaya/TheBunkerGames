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
    public class Character
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

        // -------------------------------------------------------------------------
        // Exploration State
        // -------------------------------------------------------------------------
        public bool IsExploring;
        public bool IsInjured;

        // -------------------------------------------------------------------------
        // Constructors
        // -------------------------------------------------------------------------
        public Character()
        {
            Name = "Unknown";
            Hunger = 100f;
            Thirst = 100f;
            Sanity = 100f;
            Health = 100f;
        }

        public Character(string name, float hunger = 100f, float thirst = 100f, float sanity = 100f, float health = 100f)
        {
            Name = name;
            Hunger = Mathf.Clamp(hunger, 0f, 100f);
            Thirst = Mathf.Clamp(thirst, 0f, 100f);
            Sanity = Mathf.Clamp(sanity, 0f, 100f);
            Health = Mathf.Clamp(health, 0f, 100f);
        }

        // -------------------------------------------------------------------------
        // Stat Modifiers
        // -------------------------------------------------------------------------
        public void ModifyHunger(float amount)
        {
            Hunger = Mathf.Clamp(Hunger + amount, 0f, 100f);
        }

        public void ModifyThirst(float amount)
        {
            Thirst = Mathf.Clamp(Thirst + amount, 0f, 100f);
        }

        public void ModifySanity(float amount)
        {
            Sanity = Mathf.Clamp(Sanity + amount, 0f, 100f);
        }

        public void ModifyHealth(float amount)
        {
            Health = Mathf.Clamp(Health + amount, 0f, 100f);
        }

        // -------------------------------------------------------------------------
        // Derived States
        // -------------------------------------------------------------------------
        public bool IsAlive => Health > 0f && Hunger > 0f;
        public bool IsInsane => Sanity <= 0f;
        public bool IsDehydrated => Thirst <= 0f;
        public bool IsCritical => Health <= 20f || Hunger <= 10f || Thirst <= 10f;
        public bool IsAvailableForExploration => IsAlive && !IsExploring && !IsInjured;
    }
}
