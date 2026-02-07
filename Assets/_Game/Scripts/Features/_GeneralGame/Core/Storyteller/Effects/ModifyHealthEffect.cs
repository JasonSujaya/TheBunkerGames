using UnityEngine;
using System;

namespace TheBunkerGames
{
    [Serializable]
    public class ModifyHealthEffect : StoryEffect
    {
        [Tooltip("Exact name (e.g. 'Father') or 'Random' or 'All'")]
        [SerializeField] private string targetName;
        [SerializeField] private float amount; // Negative to damage

        public string TargetName => targetName;
        public float Amount => amount;

        public ModifyHealthEffect(string targetName, float amount)
        {
            this.targetName = targetName;
            this.amount = amount;
        }

        public override void Execute()
        {
            if (CharacterManager.Instance == null)
            {
                Debug.LogWarning("[StoryEffect] Cannot execute ModifyHealth: CharacterManager missing.");
                return;
            }

            if (targetName.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var c in CharacterManager.Instance.AllCharacters)
                {
                    c.ModifyHealth(amount);
                }
            }
            else if (targetName.Equals("Random", StringComparison.OrdinalIgnoreCase))
            {
                var all = CharacterManager.Instance.AllCharacters;
                if (all.Count > 0)
                {
                    var target = all[UnityEngine.Random.Range(0, all.Count)];
                    target.ModifyHealth(amount);
                    Debug.Log($"[StoryEffect] Randomly modified health of {target.Name} by {amount}");
                }
            }
            else
            {
                var target = CharacterManager.Instance.GetCharacterByName(targetName);
                if (target != null)
                {
                    target.ModifyHealth(amount);
                }
                else
                {
                    Debug.LogWarning($"[StoryEffect] Character '{targetName}' not found.");
                }
            }
        }
    }
}
