using UnityEngine;
using System.Collections.Generic;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Master test runner. Discovers all BaseTester components in the scene
    /// and runs them in sequence. Provides a single "Run All" button.
    /// </summary>
    public class TestRunner : MonoBehaviour
    {
        #if ODIN_INSPECTOR
        [Title("Test Runner")]
        [ReadOnly]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] private List<BaseTester> testers = new List<BaseTester>();

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] private int totalPass;

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] private int totalFail;

        // -------------------------------------------------------------------------
        // Discovery
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Controls")]
        [Button("Discover Testers", ButtonSizes.Medium)]
        [GUIColor(0.5f, 0.8f, 1f)]
        #endif
        public void DiscoverTesters()
        {
            testers.Clear();
            testers.AddRange(FindObjectsByType<BaseTester>(FindObjectsSortMode.None));
            Debug.Log($"[TestRunner] Discovered {testers.Count} tester(s).");
        }

        // -------------------------------------------------------------------------
        // Run All
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Button("Run ALL Tests", ButtonSizes.Large)]
        [GUIColor(0f, 1f, 0.4f)]
        #endif
        public void RunAllTests()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[TestRunner] Tests must be run in Play Mode.");
                return;
            }

            DiscoverTesters();

            if (testers.Count == 0)
            {
                Debug.LogWarning("[TestRunner] No testers found in scene.");
                return;
            }

            totalPass = 0;
            totalFail = 0;

            Debug.Log($"<color=#FFD700>╔══════════════════════════════════════════════╗</color>");
            Debug.Log($"<color=#FFD700>║     RUNNING ALL TESTS ({testers.Count} testers)              ║</color>");
            Debug.Log($"<color=#FFD700>╚══════════════════════════════════════════════╝</color>");

            foreach (var tester in testers)
            {
                if (tester == null) continue;
                tester.RunAllTests();
                totalPass += tester.PassCount;
                totalFail += tester.FailCount;
            }

            string color = totalFail == 0 ? "#00FF00" : "#FF4444";
            Debug.Log($"<color=#FFD700>╔══════════════════════════════════════════════╗</color>");
            Debug.Log($"<color={color}>  TOTAL: {totalPass} passed, {totalFail} failed ({totalPass + totalFail} tests)</color>");
            Debug.Log($"<color=#FFD700>╚══════════════════════════════════════════════╝</color>");
        }
    }
}
