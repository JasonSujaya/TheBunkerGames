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
    /// <summary>
    /// Controls the Daily Choice phase (Phase 4 of the Core Loop).
    /// Every day demands a sacrifice — moral dilemmas presented by A.N.G.E.L.
    /// Supports Twitch voting via Audience Mode.
    /// </summary>
    public class DailyChoiceManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static DailyChoiceManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Events
        // -------------------------------------------------------------------------
        public static event Action<DilemmaData> OnDilemmaPresented;
        public static event Action<DilemmaOptionData, DilemmaOutcomeData> OnChoiceMade;
        public static event Action OnChoicePhaseComplete;

        // -------------------------------------------------------------------------
        // State
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Current DilemmaData")]
        [ReadOnly]
        #endif
        [SerializeField] private DilemmaData currentDilemma;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public DilemmaData CurrentDilemma => currentDilemma;

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
        public void PresentDilemma(DilemmaData dilemma)
        {
            currentDilemma = dilemma;
            Debug.Log($"[DailyChoice] DilemmaData: {dilemma.Title}");
            OnDilemmaPresented?.Invoke(dilemma);
        }

        /// <summary>
        /// Player makes a direct choice.
        /// </summary>
        public void MakeChoice(int optionIndex)
        {
            if (currentDilemma == null) return;
            if (optionIndex < 0 || optionIndex >= currentDilemma.Options.Count) return;

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


        private DilemmaOutcomeData ApplyChoice(DilemmaOptionData option)
        {
            var outcome = new DilemmaOutcomeData
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

        private void ApplyStatEffect(CharacterData character, StatEffectData effect)
        {
            character.ModifyHunger(effect.HungerChange);
            character.ModifyThirst(effect.ThirstChange);
            character.ModifySanity(effect.SanityChange);
            character.ModifyHealth(effect.HealthChange);
        }

        // -------------------------------------------------------------------------
        // Mock DilemmaData Generation
        // -------------------------------------------------------------------------
        private DilemmaData GenerateMockDilemma()
        {
            int day = GameManager.Instance != null ? GameManager.Instance.CurrentDay : 1;

            var dilemma = new DilemmaData
            {
                Title = $"Day {day}: The Air Filter",
                Description = "A.N.G.E.L. reports the air filtration system is failing. Radiation levels are rising.",
                Options = new List<DilemmaOptionData>
                {
                    new DilemmaOptionData
                    {
                        Label = "Fix the Air Filter",
                        Description = "Send someone to repair it. High radiation risk.",
                        OutcomeDescription = "The filter is repaired, but at a cost.",
                        ExpectedOutcome = ChoiceOutcome.Mixed,
                        StatEffects = new List<StatEffectData>
                        {
                            new StatEffectData { HealthChange = -15f, SanityChange = 5f }
                        }
                    },
                    new DilemmaOptionData
                    {
                        Label = "Ignore It",
                        Description = "Save energy. Let the air quality worsen.",
                        OutcomeDescription = "The bunker air grows thick and oppressive.",
                        ExpectedOutcome = ChoiceOutcome.Negative,
                        StatEffects = new List<StatEffectData>
                        {
                            new StatEffectData { SanityChange = -20f, HealthChange = -5f }
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
        [Button("Generate Mock DilemmaData", ButtonSizes.Medium)]
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

        [Button("Complete Phase", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        private void Debug_CompletePhase()
        {
            if (Application.isPlaying) CompleteChoicePhase();
        }

        [Title("Auto Setup")]
        [Button("Auto Setup Dependencies", ButtonSizes.Large)]
        [GUIColor(0.4f, 1f, 0.4f)]
        private void AutoSetupDependencies()
        {
            #if UNITY_EDITOR
            // Ensure Tester exists
            var testerType = System.Type.GetType("TheBunkerGames.Tests.DailyChoiceTester");
            if (testerType != null && GetComponent(testerType) == null)
            {
                gameObject.AddComponent(testerType);
                Debug.Log("[DailyChoiceManager] Added DailyChoiceTester.");
            }
            else if (testerType == null)
            {
                Debug.LogWarning("[DailyChoiceManager] Could not find DailyChoiceTester type. Ensure it is in TheBunkerGames.Tests namespace.");
            }
            #endif
        }
        #endif
    }
}
