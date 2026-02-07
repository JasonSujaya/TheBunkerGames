namespace TheBunkerGames
{
    /// <summary>
    /// All effect types the LLM can trigger.
    /// Use this enum for Inspector dropdowns and type-safe effect creation.
    /// </summary>
    public enum LLMEffectType
    {
        // Health Effects
        AddHP,
        ReduceHP,
        AddSanity,
        ReduceSanity,
        AddHunger,
        ReduceHunger,
        AddThirst,
        ReduceThirst,
        
        // Resource Effects
        AddFood,
        ReduceFood,
        AddWater,
        ReduceWater,
        AddSupplies,
        ReduceSupplies,
        
        // Character Effects
        InjureCharacter,
        HealCharacter,
        KillCharacter,
        
        // Special Effects
        TriggerEvent,
        UnlockArea,
        SpawnItem
    }
}
