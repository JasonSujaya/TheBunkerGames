using UnityEngine;

namespace TheBunkerGames
{
    // -------------------------------------------------------------------------
    // Game State Machine States
    // -------------------------------------------------------------------------
    public enum GameState
    {
        StatusReview,
        CityExploration
    }

    // -------------------------------------------------------------------------
    // Item Types
    // -------------------------------------------------------------------------
    public enum ItemType
    {
        Food,
        Water,
        Meds,
        Tools,
        Junk
    }

    // -------------------------------------------------------------------------
    // Exploration Risk Levels
    // -------------------------------------------------------------------------
    public enum ExplorationRisk
    {
        Low,
        Medium,
        High,
        Deadly
    }

    // -------------------------------------------------------------------------
    // QuestData States (String constants for easy AI integration)
    // -------------------------------------------------------------------------
    public static class QuestState
    {
        public const string Active = "ACTIVE";
        public const string Completed = "COMPLETED";
        public const string Failed = "FAILED";
    }

    // -------------------------------------------------------------------------
    // Character Roles
    // -------------------------------------------------------------------------
    public enum CharacterRole
    {
        Father,
        Mother,
        Son,
        Daughter,
        Other
    }

    // -------------------------------------------------------------------------
    // Character Subtypes (Categorization)
    // -------------------------------------------------------------------------
    public enum CharacterSubtype
    {
        Family,
        Enemy,
        Survivor,
        Neutral
    }

    // -------------------------------------------------------------------------
    // Sickness Types (conditions that affect characters over time)
    // -------------------------------------------------------------------------
    public enum SicknessType
    {
        None,
        Flu,
        Infection,
        RadiationPoisoning,
        FoodPoisoning,
        Fever,
        Pneumonia,
        Dysentery,
        Plague
    }

    // -------------------------------------------------------------------------
    // Player Action Categories (daily player decisions)
    // -------------------------------------------------------------------------
    public enum PlayerActionCategory
    {
        Exploration,
        Dilemma,
        FamilyRequest
    }

    // -------------------------------------------------------------------------
    // Game Theme Types
    // -------------------------------------------------------------------------
    public enum GameThemeType
    {
        GovernmentShutdown,
        ZombieApocalypse,
        Frostpunk,
        Pandemic,
        RickMortyInvasion,
        CatUprising
    }

    // -------------------------------------------------------------------------
    // Character Actions (actions a player can perform on a character)
    // -------------------------------------------------------------------------
    public enum CharacterAction
    {
        Eat,
        Drink,
        Heal
    }

    // -------------------------------------------------------------------------
    // Pre-Scripted Event Categories (government fallout narrative beats)
    // -------------------------------------------------------------------------
    public enum ScriptedEventCategory
    {
        ResourceShortage,
        RaiderAttack,
        GovernmentBroadcast,
        Contamination,
        RescueMission,
        PoliticalUprising,
        StructuralFailure,
        DiseaseOutbreak,
        TraderArrival,
        MysteriousSignal,
        MilitaryEncounter,
        MoraleDrop,
        EvacuationNotice
    }
}
