using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    /// <summary>
    /// Data generated during the Night Cycle phase.
    /// Contains the dream/nightmare log, stat change summary,
    /// and any deaths that occurred.
    /// </summary>
    [Serializable]
    public class NightReportData
    {
        public int Day;
        public bool IsNightmare;
        public string DreamLog = "";
        public List<string> StatChanges = new List<string>();
        public List<string> DeathsThisNight = new List<string>();
    }
}
