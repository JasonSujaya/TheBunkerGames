namespace TheBunkerGames
{
    /// <summary>
    /// Captures the result of an executed effect for UI display.
    /// Created by LLMEffectExecutor after applying an effect.
    /// </summary>
    public class EffectDisplayData
    {
        public string EffectType;
        public string Target;
        public float ValueChange;
        public bool IsPositive;
        public string DisplayLabel;
    }
}
