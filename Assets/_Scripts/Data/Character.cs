using UnityEngine;
using System;

namespace TheBunkerGames
{
    /// <summary>
    /// Plain C# class representing a family member in the bunker.
    /// Serializable for save/load functionality.
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
        public float Sanity = 100f;

        // -------------------------------------------------------------------------
        // Constructors
        // -------------------------------------------------------------------------
        public Character()
        {
            Name = "Unknown";
            Hunger = 100f;
            Sanity = 100f;
        }

        public Character(string name, float hunger = 100f, float sanity = 100f)
        {
            Name = name;
            Hunger = Mathf.Clamp(hunger, 0f, 100f);
            Sanity = Mathf.Clamp(sanity, 0f, 100f);
        }

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public void ModifyHunger(float amount)
        {
            Hunger = Mathf.Clamp(Hunger + amount, 0f, 100f);
        }

        public void ModifySanity(float amount)
        {
            Sanity = Mathf.Clamp(Sanity + amount, 0f, 100f);
        }

        public bool IsAlive => Hunger > 0f;
        public bool IsInsane => Sanity <= 0f;
    }
}
