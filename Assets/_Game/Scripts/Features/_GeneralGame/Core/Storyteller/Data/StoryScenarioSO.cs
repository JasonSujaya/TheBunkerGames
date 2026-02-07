using UnityEngine;
using System.Collections.Generic;
using System;

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "NewStoryScenario", menuName = "TheBunkerGames/Story/Story Scenario")]
    public class StoryScenarioSO : ScriptableObject
    {
        public string ScenarioName = "Default Scenario";
        [TextArea] public string Description;
        public int TotalDays = 30;

        [Space]
        public List<DayEventConfig> Timeline = new List<DayEventConfig>();

        public StoryEventSO GetEventForDay(int day)
        {
            var config = Timeline.Find(x => x.DayNumber == day);
            return config.Event;
        }
    }

    [Serializable]
    public struct DayEventConfig
    {
        public int DayNumber;
        public StoryEventSO Event;
    }
}
