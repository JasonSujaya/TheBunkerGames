using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// A moral dilemma presented to the player by A.N.G.E.L.
    /// Can be AI-generated via Neocortex or hand-authored.
    /// </summary>
    [Serializable]
    public class DilemmaData
    {
        public string Title = "";
        public string Description = "";
        public List<DilemmaOptionData> Options = new List<DilemmaOptionData>();
    }

    /// <summary>
    /// One option within a dilemma. Includes stat consequences and vote tracking.
    /// </summary>
    [Serializable]
    public class DilemmaOptionData
    {
        public string Label = "";
        public string Description = "";
        public string OutcomeDescription = "";
        public ChoiceOutcome ExpectedOutcome;
        public List<StatEffectData> StatEffects = new List<StatEffectData>();

        // Voting (Twitch Audience Mode)
        public int VoteCount;
    }

    /// <summary>
    /// Stat changes applied to a character (or all characters) as a result of a choice.
    /// Leave TargetCharacterName empty to apply to all family members.
    /// </summary>
    [Serializable]
    public class StatEffectData
    {
        public string TargetCharacterName = "";
        public float HungerChange;
        public float ThirstChange;
        public float SanityChange;
        public float HealthChange;
    }

    /// <summary>
    /// The result of a dilemma choice after it is resolved.
    /// </summary>
    [Serializable]
    public class DilemmaOutcomeData
    {
        public string Description = "";
        public ChoiceOutcome OutcomeType;
    }
}
