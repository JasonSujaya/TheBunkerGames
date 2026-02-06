using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Data snapshot of the family's status at the start of a day.
    /// Passed to the UI and to A.N.G.E.L. for context.
    /// </summary>
    [Serializable]
    public class StatusReportData
    {
        public int Day;
        public int AliveCount;
        public int TotalCount;
        public List<CharacterStatusData> CharacterStatuses;
        public List<string> Warnings;
    }

    /// <summary>
    /// Individual character status entry within a StatusReportData.
    /// </summary>
    [Serializable]
    public class CharacterStatusData
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
