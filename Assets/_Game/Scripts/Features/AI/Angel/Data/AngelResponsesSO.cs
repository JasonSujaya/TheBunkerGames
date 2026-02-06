using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    [CreateAssetMenu(fileName = "AngelResponses", menuName = "The Bunker/AI/Angel Responses")]
    public class AngelResponsesSO : ScriptableObject
    {
        [System.Serializable]
        public class MoodResponsePool
        {
            public AngelMood Mood;
            [TextArea(3, 10)]
            public List<string> Messages = new List<string>();
            public List<ResourceGrantData> DefaultGrants = new List<ResourceGrantData>();
        }

        #if ODIN_INSPECTOR
        [Title("Response Configuration")]
        [TableList]
        #endif
        [SerializeField] private List<MoodResponsePool> responsePools = new List<MoodResponsePool>();

        public AngelResponseData GetRandomResponse(AngelMood mood)
        {
            var data = new AngelResponseData();
            var pool = responsePools.Find(p => p.Mood == mood);

            if (pool != null && pool.Messages.Count > 0)
            {
                data.Message = pool.Messages[Random.Range(0, pool.Messages.Count)];
                
                // Copy default grants if any
                foreach (var grant in pool.DefaultGrants)
                {
                    data.GrantedItems.Add(new ResourceGrantData(grant.ItemId, grant.Quantity));
                }
            }
            else
            {
                data.Message = "...";
            }

            return data;
        }
    }
}
