using UnityEngine;
using System;
using System.Collections.Generic;

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "NewStoryEvent", menuName = "TheBunkerGames/Story/Story Event")]
    public class StoryEventSO : ScriptableObject
    {
        [Header("Narrative")]
        public string Title;
        [TextArea(3, 10)] 
        public string Description;

        [Header("Consequences")]
        [SerializeReference] 
        public List<StoryEffect> ImmediateEffects = new List<StoryEffect>();

        [Header("Choices")]
        public List<StoryChoice> Choices = new List<StoryChoice>();
    }

    [Serializable]
    public struct StoryChoice
    {
        public string Text;
        [SerializeReference] 
        public List<StoryEffect> Effects;
    }
}
