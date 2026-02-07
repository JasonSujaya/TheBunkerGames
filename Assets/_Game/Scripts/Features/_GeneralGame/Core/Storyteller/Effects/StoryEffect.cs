using UnityEngine;
using System;

namespace TheBunkerGames
{
    /// <summary>
    /// Base class for any effect that can happen in the story.
    /// Serialized polymorphically in StoryEventSO.
    /// </summary>
    [Serializable]
    public abstract class StoryEffect
    {
        public abstract void Execute();
    }
}
