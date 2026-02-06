using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Data snapshot of the family's status at the start of a day.
    /// Passed to the UI and to A.N.G.E.L. for context.
    /// </summary>
    [Serializable]
    public class StatusReport
    {
        public int Day;
        public int AliveCount;
        public int TotalCount;
        public List<CharacterStatus> CharacterStatuses;
        public List<string> Warnings;
    }

    /// <summary>
    /// Individual character status entry within a StatusReport.
    /// </summary>
    [Serializable]
    public class CharacterStatus
    {
        public string CharacterName;
        public float Hunger;
        public float Thirst;
        public float Sanity;
        public float Health;
        public bool IsAlive;
        public bool IsCritical;
    }
}
