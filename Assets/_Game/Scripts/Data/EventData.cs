using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using System.Collections.Generic;

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "EventData", menuName = "TheBunkerGames/Event Data")]
    public class EventData : ScriptableObject
    {
        #if ODIN_INSPECTOR
        [Title("Event Details")]
        #endif
        [SerializeField] private string eventID;
        [SerializeField] private string title;
        [TextArea(5, 10)]
        [SerializeField] private string description;
        
        #if ODIN_INSPECTOR
        [Title("Choices")]
        #endif
        [SerializeField] private List<EventChoice> choices = new List<EventChoice>();

        public string EventID => eventID;
        public string Title => title;
        public string Description => description;
        public List<EventChoice> Choices => choices;
    }

    [System.Serializable]
    public class EventChoice
    {
        [SerializeField] private string choiceText;
        [TextArea(2, 5)]
        [SerializeField] private string outcomeText;
        
        // Simple outcome modifiers
        [SerializeField] private float hungerEffect;
        [SerializeField] private float thirstEffect;
        [SerializeField] private float sanityEffect;
        [SerializeField] private float healthEffect;
        
        public string ChoiceText => choiceText;
        public string OutcomeText => outcomeText;
        public float HungerEffect => hungerEffect;
        public float ThirstEffect => thirstEffect;
        public float SanityEffect => sanityEffect;
        public float HealthEffect => healthEffect;
    }
}
