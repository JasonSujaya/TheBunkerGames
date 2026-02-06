using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "DreamDatabase", menuName = "The Bunker/Night Cycle/Dream Database")]
    public class DreamDatabaseSO : ScriptableObject
    {
        #if ODIN_INSPECTOR
        [Title("Dream Logs")]
        [InfoBox("Peaceful dreams when sanity is high.")]
        #endif
        [TextArea(3, 5)]
        [SerializeField] private List<string> dreams = new List<string>();

        #if ODIN_INSPECTOR
        [Title("Nightmare Logs")]
        [InfoBox("Horror logs when sanity is low.")]
        #endif
        [TextArea(3, 5)]
        [SerializeField] private List<string> nightmares = new List<string>();

        public string GetRandomLog(bool isNightmare)
        {
            var pool = isNightmare ? nightmares : dreams;
            if (pool.Count == 0) return isNightmare ? "Darkness..." : "Silence...";
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
