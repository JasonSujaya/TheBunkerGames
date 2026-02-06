using UnityEngine;

namespace TheBunkerGames
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void PlaySound(string soundName)
        {
            Debug.Log($"[AudioManager] Playing sound: {soundName}");
        }
    }
}
