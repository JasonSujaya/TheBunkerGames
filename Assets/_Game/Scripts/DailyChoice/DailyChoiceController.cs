using UnityEngine;
using System;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Controls the Daily Choice phase (Phase 4 of the Core Loop).
    /// Every day demands a sacrifice — moral dilemmas presented by A.N.G.E.L.
    /// Supports Twitch voting via Audience Mode.
    /// </summary>
    public class DailyChoiceController : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static DailyChoiceController Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<Dilemma> OnDilemmaPresented;
        public static event Action<DilemmaOption, DilemmaOutcome> OnChoiceMade;
        public static event Action OnChoicePhaseComplete;
        public static event Action<DilemmaOption, float> OnVoteUpdated;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Current Dilemma")]
        [ReadOnly]
        #endif
        [SerializeField] private Dilemma currentDilemma;

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] private bool isVotingActive;

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] private float voteTimer;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public Dilemma CurrentDilemma => currentDilemma;
        public bool IsVotingActive => isVotingActive;
        public float VoteTimer => voteTimer;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameManager.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            GameManager.OnStateChanged -= HandleStateChanged;
        }

        private void Update()
        {
            if (isVotingActive)
            {
                voteTimer -= Time.deltaTime;
                if (voteTimer <= 0f)
                {
                    EndVoting();
                }
            }
        }

        // -------------------------------------------------------------------------
        // Phase Logic
        // -------------------------------------------------------------------------
        private void HandleStateChanged(GameState newState)
        {
            if (newState == GameState.DailyChoice)
            {
                BeginChoicePhase();
            }
        }

        public void BeginChoicePhase()
        {
            Debug.Log("[DailyChoice] Choice phase started. Generating dilemma...");
            // In production, Neocortex generates a context-aware dilemma.
            // For now, use mock dilemmas.
            currentDilemma = GenerateMockDilemma();
            OnDilemmaPresented?.Invoke(currentDilemma);
        }

        /// <summary>
        /// Present a dilemma from the AI.
        /// </summary>
        public void PresentDilemma(Dilemma dilemma)
        {
            currentDilemma = dilemma;
            Debug.Log($"[DailyChoice] Dilemma: {dilemma.Title}");
            OnDilemmaPresented?.Invoke(dilemma);
        }

        /// <summary>
        /// Start the voting timer (for Twitch Audience Mode).
        /// </summary>
        public void StartVoting()
        {
            if (currentDilemma == null) return;

            var config = GameConfig.Instance;
            voteTimer = config != null ? config.VoteTimerDuration : 30f;
            isVotingActive = true;

            foreach (var option in currentDilemma.Options)
            {
                option.VoteCount = 0;
            }

            Debug.Log($"[DailyChoice] Voting started. Timer: {voteTimer:F0}s");
        }

        /// <summary>
        /// Register a vote for an option (Twitch chat command).
        /// </summary>
        public void CastVote(int optionIndex)
        {
            if (!isVotingActive || currentDilemma == null) return;
            if (optionIndex < 0 || optionIndex >= currentDilemma.Options.Count) return;

            var option = currentDilemma.Options[optionIndex];
            option.VoteCount++;

            float totalVotes = 0;
            foreach (var opt in currentDilemma.Options) totalVotes += opt.VoteCount;
            float percentage = totalVotes > 0 ? option.VoteCount / totalVotes : 0f;

            OnVoteUpdated?.Invoke(option, percentage);
        }

        /// <summary>
        /// Player makes a direct choice (solo mode) or voting ends.
        /// </summary>
        public void MakeChoice(int optionIndex)
        {
            if (currentDilemma == null) return;
            if (optionIndex < 0 || optionIndex >= currentDilemma.Options.Count) return;

            isVotingActive = false;
            var chosenOption = currentDilemma.Options[optionIndex];
            var outcome = ApplyChoice(chosenOption);

            Debug.Log($"[DailyChoice] Choice made: {chosenOption.Label} -> {outcome.OutcomeType}");
            OnChoiceMade?.Invoke(chosenOption, outcome);
        }

        public void CompleteChoicePhase()
        {
            currentDilemma = null;
            Debug.Log("[DailyChoice] Choice phase complete. Moving to Night Cycle.");
            OnChoicePhaseComplete?.Invoke();
        }

        // -------------------------------------------------------------------------
        // Internal Logic
        // -------------------------------------------------------------------------
        private void EndVoting()
        {
            isVotingActive = false;

            // Pick the option with the most votes
            int bestIndex = 0;
            int bestVotes = 0;
            for (int i = 0; i < currentDilemma.Options.Count; i++)
            {
                if (currentDilemma.Options[i].VoteCount > bestVotes)
                {
                    bestVotes = currentDilemma.Options[i].VoteCount;
                    bestIndex = i;
                }
            }

            Debug.Log($"[DailyChoice] Voting ended. Winner: {currentDilemma.Options[bestIndex].Label}");
            MakeChoice(bestIndex);
        }

        private DilemmaOutcome ApplyChoice(DilemmaOption option)
        {
            var outcome = new DilemmaOutcome
            {
                Description = option.OutcomeDescription,
                OutcomeType = option.ExpectedOutcome
            };

            // Apply stat effects to all family members
            var family = FamilyManager.Instance;
            if (family != null)
            {
                foreach (var effect in option.StatEffects)
                {
                    if (string.IsNullOrEmpty(effect.TargetCharacterName))
                    {
                        // Apply to all
                        foreach (var c in family.FamilyMembers)
                        {
                            ApplyStatEffect(c, effect);
                        }
                    }
                    else
                    {
                        var target = family.GetCharacter(effect.TargetCharacterName);
                        if (target != null) ApplyStatEffect(target, effect);
                    }
                }
            }

            return outcome;
        }

        private void ApplyStatEffect(Character character, StatEffect effect)
        {
            character.ModifyHunger(effect.HungerChange);
            character.ModifyThirst(effect.ThirstChange);
            character.ModifySanity(effect.SanityChange);
            character.ModifyHealth(effect.HealthChange);
        }

        // -------------------------------------------------------------------------
        // Mock Dilemma Generation
        // -------------------------------------------------------------------------
        private Dilemma GenerateMockDilemma()
        {
            int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

            var dilemma = new Dilemma
            {
                Title = $"Day {day}: The Air Filter",
                Description = "A.N.G.E.L. reports the air filtration system is failing. Radiation levels are rising.",
                Options = new List<DilemmaOption>
                {
                    new DilemmaOption
                    {
                        Label = "Fix the Air Filter",
                        Description = "Send someone to repair it. High radiation risk.",
                        OutcomeDescription = "The filter is repaired, but at a cost.",
                        ExpectedOutcome = ChoiceOutcome.Mixed,
                        StatEffects = new List<StatEffect>
                        {
                            new StatEffect { HealthChange = -15f, SanityChange = 5f }
                        }
                    },
                    new DilemmaOption
                    {
                        Label = "Ignore It",
                        Description = "Save energy. Let the air quality worsen.",
                        OutcomeDescription = "The bunker air grows thick and oppressive.",
                        ExpectedOutcome = ChoiceOutcome.Negative,
                        StatEffects = new List<StatEffect>
                        {
                            new StatEffect { SanityChange = -20f, HealthChange = -5f }
                        }
                    }
                }
            };

            return dilemma;
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Controls")]
        [Button("Generate Mock Dilemma", ButtonSizes.Medium)]
        [GUIColor(0.5f, 1f, 0.5f)]
        private void Debug_GenerateDilemma()
        {
            if (Application.isPlaying) BeginChoicePhase();
        }

        [Button("Choose Option 0", ButtonSizes.Medium)]
        [GUIColor(1f, 0.9f, 0.5f)]
        private void Debug_ChooseFirst()
        {
            if (Application.isPlaying) MakeChoice(0);
        }

        [Button("Choose Option 1", ButtonSizes.Medium)]
        [GUIColor(1f, 0.7f, 0.5f)]
        private void Debug_ChooseSecond()
        {
            if (Application.isPlaying) MakeChoice(1);
        }

        [Button("Start Voting", ButtonSizes.Medium)]
        [GUIColor(0.7f, 0.5f, 1f)]
        private void Debug_StartVoting()
        {
            if (Application.isPlaying) StartVoting();
        }

        [Button("Complete Phase", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_CompletePhase()
        {
            if (Application.isPlaying) CompleteChoicePhase();
        }
        #endif
    }
}
