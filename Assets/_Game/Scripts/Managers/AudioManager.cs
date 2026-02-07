using UnityEngine;

namespace TheBunkerGames
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private bool enableDebugLogs = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        public void PlaySound(string soundName)
        {
            if (enableDebugLogs) Debug.Log($"[AudioManager] Playing sound: {soundName}");
        }
    }
}
