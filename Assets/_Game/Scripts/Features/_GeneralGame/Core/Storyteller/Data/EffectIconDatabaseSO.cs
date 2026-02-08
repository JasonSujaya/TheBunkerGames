using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Database mapping effect categories to display icons and colors.
    /// Assign sprites for each category (HP, Sanity, Hunger, etc.) in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "EffectIconDatabaseSO", menuName = "TheBunkerGames/Effect Icon Database")]
    public class EffectIconDatabaseSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Singleton Access
        // -------------------------------------------------------------------------
        private static EffectIconDatabaseSO instance;
        public static EffectIconDatabaseSO Instance
        {
            get
            {
                if (instance == null)
                    instance = Resources.Load<EffectIconDatabaseSO>("EffectIconDatabaseSO");
                return instance;
            }
        }

        public static void SetInstance(EffectIconDatabaseSO database)
        {
            instance = database;
        }

        // -------------------------------------------------------------------------
        // Data
        // -------------------------------------------------------------------------
        [Serializable]
        public class EffectIconEntry
        {
            #if ODIN_INSPECTOR
            [HorizontalGroup("Row"), LabelWidth(80)]
            #endif
            public string category;

            #if ODIN_INSPECTOR
            [HorizontalGroup("Row"), LabelWidth(40), PreviewField(50, ObjectFieldAlignment.Left)]
            #endif
            public Sprite icon;

            #if ODIN_INSPECTOR
            [HorizontalGroup("Colors"), LabelWidth(60)]
            #endif
            public Color positiveColor = new Color(0.2f, 0.8f, 0.2f, 1f);

            #if ODIN_INSPECTOR
            [HorizontalGroup("Colors"), LabelWidth(60)]
            #endif
            public Color negativeColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        }

        #if ODIN_INSPECTOR
        [Title("Effect Icons")]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<EffectIconEntry> entries = new List<EffectIconEntry>();

        #if ODIN_INSPECTOR
        [Title("Fallback")]
        [PreviewField(50)]
        #endif
        [SerializeField] private Sprite fallbackIcon;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public Sprite FallbackIcon => fallbackIcon;

        // -------------------------------------------------------------------------
        // Public Methods
        // -------------------------------------------------------------------------
        public EffectIconEntry GetEntry(string category)
        {
            if (string.IsNullOrEmpty(category)) return null;

            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && string.Equals(entries[i].category, category, StringComparison.OrdinalIgnoreCase))
                    return entries[i];
            }
            return null;
        }

        public Sprite GetIcon(string category)
        {
            var entry = GetEntry(category);
            return entry != null ? entry.icon : fallbackIcon;
        }

        public Color GetColor(string category, bool isPositive)
        {
            var entry = GetEntry(category);
            if (entry != null)
                return isPositive ? entry.positiveColor : entry.negativeColor;
            return isPositive ? new Color(0.2f, 0.8f, 0.2f, 1f) : new Color(0.9f, 0.2f, 0.2f, 1f);
        }

        // -------------------------------------------------------------------------
        // Static Helpers
        // -------------------------------------------------------------------------
        /// <summary>
        /// Maps an effect type string (e.g., "AddHP", "ReduceSanity", "KillCharacter")
        /// to a display category (e.g., "HP", "Sanity", "Death").
        /// </summary>
        public static string EffectTypeToCategory(string effectType)
        {
            if (string.IsNullOrEmpty(effectType)) return "Unknown";

            switch (effectType)
            {
                case "AddHP":
                case "ReduceHP":
                case "HealCharacter":
                    return "HP";

                case "AddSanity":
                case "ReduceSanity":
                    return "Sanity";

                case "AddHunger":
                case "ReduceHunger":
                    return "Hunger";

                case "AddThirst":
                case "ReduceThirst":
                    return "Thirst";

                case "AddFood":
                case "ReduceFood":
                    return "Food";

                case "AddWater":
                case "ReduceWater":
                    return "Water";

                case "AddSupplies":
                case "ReduceSupplies":
                    return "Supplies";

                case "InjureCharacter":
                    return "Injury";

                case "KillCharacter":
                    return "Death";

                case "InfectCharacter":
                    return "Sickness";

                case "CureCharacter":
                    return "Cure";

                default:
                    return "Unknown";
            }
        }

        // -------------------------------------------------------------------------
        // Debug / Editor
        // -------------------------------------------------------------------------
#if ODIN_INSPECTOR
        [Title("Setup")]
        [Button("Create Default Entries", ButtonSizes.Large)]
        [GUIColor(0f, 1f, 0f)]
        private void CreateDefaultEntries()
        {
            entries.Clear();

            var defaults = new (string category, Color positive, Color negative)[]
            {
                ("HP",       new Color(0.2f, 0.8f, 0.2f), new Color(0.9f, 0.2f, 0.2f)),
                ("Sanity",   new Color(0.3f, 0.7f, 0.9f), new Color(0.6f, 0.2f, 0.8f)),
                ("Hunger",   new Color(0.2f, 0.8f, 0.2f), new Color(0.9f, 0.6f, 0.1f)),
                ("Thirst",   new Color(0.3f, 0.7f, 0.9f), new Color(0.9f, 0.6f, 0.1f)),
                ("Food",     new Color(0.2f, 0.8f, 0.2f), new Color(0.9f, 0.2f, 0.2f)),
                ("Water",    new Color(0.3f, 0.7f, 0.9f), new Color(0.9f, 0.2f, 0.2f)),
                ("Supplies", new Color(0.2f, 0.8f, 0.2f), new Color(0.9f, 0.2f, 0.2f)),
                ("Injury",   new Color(0.2f, 0.8f, 0.2f), new Color(0.9f, 0.2f, 0.2f)),
                ("Sickness", new Color(0.2f, 0.8f, 0.2f), new Color(0.8f, 0.8f, 0.1f)),
                ("Death",    new Color(0.5f, 0.5f, 0.5f), new Color(0.9f, 0.1f, 0.1f)),
                ("Cure",     new Color(0.2f, 0.9f, 0.4f), new Color(0.5f, 0.5f, 0.5f)),
            };

            foreach (var d in defaults)
            {
                entries.Add(new EffectIconEntry
                {
                    category = d.category,
                    icon = null, // Assign sprites in Inspector
                    positiveColor = d.positive,
                    negativeColor = d.negative,
                });
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[EffectIconDatabaseSO] Created {entries.Count} default entries. Assign icon sprites in the Inspector.");
        }
#endif
    }
}
