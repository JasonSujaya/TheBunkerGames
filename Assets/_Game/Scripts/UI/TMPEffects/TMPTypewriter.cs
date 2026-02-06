using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Character-by-character typewriter reveal for TMP text.
    /// Supports custom tag stripping, punctuation pauses, and per-character events.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class TMPTypewriter : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Settings
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Typewriter Settings")]
        #endif
        [SerializeField] private float charactersPerSecond = 30f;
        [SerializeField] private float punctuationDelay = 0.15f;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool stripCustomTags = true;

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------

        /// <summary>Fired after each character becomes visible. Args: character, index.</summary>
        public event Action<char, int> OnCharacterRevealed;

        /// <summary>Fired when all characters have been revealed.</summary>
        public event Action OnTypewriterComplete;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public bool IsTyping { get; private set; }
        public IReadOnlyList<TagRange> ParsedTagRanges => parsedTagRanges;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        private TMP_Text textComponent;
        private Coroutine typewriterCoroutine;
        private List<TagRange> parsedTagRanges;
        private string originalRawText;

        private static readonly HashSet<char> PunctuationChars = new()
        {
            '.', ',', '!', '?', ';', ':'
        };

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void OnDisable()
        {
            Stop();
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Start typing the current text from the beginning.
        /// </summary>
        #if ODIN_INSPECTOR
        [Button("Play")]
        #endif
        public void Play()
        {
            Play(textComponent.text);
        }

        /// <summary>
        /// Start typing with new text content.
        /// </summary>
        public void Play(string newText)
        {
            Stop();

            originalRawText = newText;

            if (stripCustomTags)
            {
                string cleaned = TMPTagParser.Parse(newText, out var ranges);
                parsedTagRanges = ranges;
                textComponent.text = cleaned;
            }
            else
            {
                parsedTagRanges = new List<TagRange>();
                textComponent.text = newText;
            }

            // Notify sibling TMPVertexEffects about parsed tags
            var vertexEffects = GetComponent<TMPVertexEffects>();
            if (vertexEffects != null)
            {
                vertexEffects.SetTagRanges(parsedTagRanges);
            }

            textComponent.ForceMeshUpdate();
            textComponent.maxVisibleCharacters = 0;

            typewriterCoroutine = StartCoroutine(TypewriterRoutine());
        }

        /// <summary>
        /// Instantly reveal all remaining characters.
        /// </summary>
        #if ODIN_INSPECTOR
        [Button("Skip")]
        #endif
        public void Skip()
        {
            if (!IsTyping) return;

            Stop();

            textComponent.ForceMeshUpdate();
            textComponent.maxVisibleCharacters = textComponent.textInfo.characterCount;
            OnTypewriterComplete?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------------
        private void Stop()
        {
            if (typewriterCoroutine != null)
            {
                StopCoroutine(typewriterCoroutine);
                typewriterCoroutine = null;
            }
            IsTyping = false;
        }

        private IEnumerator TypewriterRoutine()
        {
            IsTyping = true;

            TMP_TextInfo textInfo = textComponent.textInfo;
            int totalChars = textInfo.characterCount;

            if (totalChars == 0)
            {
                IsTyping = false;
                OnTypewriterComplete?.Invoke();
                yield break;
            }

            float baseDelay = 1f / Mathf.Max(charactersPerSecond, 0.1f);
            float elapsed = 0f;
            int revealed = 0;

            while (revealed < totalChars)
            {
                elapsed += Time.deltaTime;

                float currentDelay = baseDelay;

                // Add punctuation pause if the previous character was punctuation
                if (revealed > 0 && revealed <= totalChars)
                {
                    char prevChar = textInfo.characterInfo[revealed - 1].character;
                    if (PunctuationChars.Contains(prevChar))
                    {
                        currentDelay += punctuationDelay;
                    }
                }

                if (elapsed >= currentDelay)
                {
                    elapsed -= currentDelay;
                    revealed++;
                    textComponent.maxVisibleCharacters = revealed;

                    if (revealed > 0 && revealed <= totalChars)
                    {
                        TMP_CharacterInfo charInfo = textInfo.characterInfo[revealed - 1];
                        OnCharacterRevealed?.Invoke(charInfo.character, revealed - 1);
                    }
                }

                yield return null;
            }

            IsTyping = false;
            OnTypewriterComplete?.Invoke();
        }
    }
}
