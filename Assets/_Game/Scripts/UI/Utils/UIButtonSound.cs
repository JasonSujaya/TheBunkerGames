using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Adds hover and click sounds to a Button.
    /// Requires AudioManager to be present in the scene (or DDOL).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
    {
        #if ODIN_INSPECTOR
        [Title("Audio Settings")]
        #endif
        [SerializeField] private AudioClip hoverSound;
        [SerializeField] private AudioClip clickSound;
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            if (hoverSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(hoverSound, volume * 0.5f);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            if (clickSound != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX(clickSound, volume);
            }
        }
    }
}
