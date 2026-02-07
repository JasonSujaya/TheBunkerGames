using UnityEngine;
using System.Collections.Generic;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Handles daily resource decay and survival logic for family members.
    /// Manages Hunger, Thirst, and Health consequences.
    /// </summary>
    public class SurvivalManager : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Singleton
        // -------------------------------------------------------------------------
        public static SurvivalManager Instance { get; private set; }

        // -------------------------------------------------------------------------
        // Configuration
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Decay Settings")]
        #endif
        [SerializeField] private float dailyHungerDecay = 15f;
        [SerializeField] private float dailyThirstDecay = 20f;
        
        #if ODIN_INSPECTOR
        [InfoBox("Health damage applied when Hunger or Thirst reaches 0.")]
        #endif
        [SerializeField] private float starvationHealthDamage = 10f;
        [SerializeField] private float dehydrationHealthDamage = 15f;
        [SerializeField] private bool enableDebugLogs = false;

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
            GameManager.OnDayStart += ProcessDailyDecay;
        }

        private void OnDisable()
        {
            GameManager.OnDayStart -= ProcessDailyDecay;
        }

        // -------------------------------------------------------------------------
        // Core Logic
        // -------------------------------------------------------------------------
        /// <summary>
        /// Reduces hunger and thirst for all family members.
        /// Applies damage if they are starving or dehydrated.
        /// </summary>
        [ContextMenu("Process Daily Decay")]
        public void ProcessDailyDecay()
        {
            if (FamilyManager.Instance == null)
            {
                Debug.LogWarning("[SurvivalManager] FamilyManager.Instance is null! Skipping decay.");
                return;
            }

            var family = FamilyManager.Instance.FamilyMembers;
            if (enableDebugLogs) Debug.Log($"[SurvivalManager] Processing daily decay for {family.Count} members.");

            foreach (var member in family)
            {
                if (!member.IsAlive) continue;

                // Apply Decay
                member.ModifyHunger(-dailyHungerDecay);
                member.ModifyThirst(-dailyThirstDecay);

                // Check for consequences
                if (member.Hunger <= 0)
                {
                    member.ModifyHealth(-starvationHealthDamage);
                    if (enableDebugLogs) Debug.Log($"[SurvivalManager] {member.Name} is starving! Health reduced.");
                }

                if (member.Thirst <= 0)
                {
                    member.ModifyHealth(-dehydrationHealthDamage);
                    if (enableDebugLogs) Debug.Log($"[SurvivalManager] {member.Name} is dehydrated! Health reduced.");
                }

                // Check for Death
                if (!member.IsAlive)
                {
                    Debug.LogWarning($"[SurvivalManager] {member.Name} has DIED from neglect.");
                    // TODO: Trigger Game Over or Morale loss here
                    continue;
                }

                // Log outcome if critical
                if (member.IsCritical)
                {
                    Debug.LogWarning($"[SurvivalManager] {member.Name} is in CRITICAL condition!");
                }
            }
        }

        // -------------------------------------------------------------------------
        // Debug Buttons
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Debug Tools")]
        [Button("Force Decay Now", ButtonSizes.Medium)]
        [GUIColor(1, 0.5f, 0)]
        private void Debug_ForceDecay()
        {
            if (Application.isPlaying)
            {
                ProcessDailyDecay();
            }
            else
            {
                Debug.LogWarning("Please enter Play Mode to test decay.");
            }
        }
        #endif
    }
}
