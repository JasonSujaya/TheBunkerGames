using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Tests
{
    /// <summary>
    /// Base class for all runtime testers. Provides test discovery, execution,
    /// pass/fail tracking, and Odin Inspector buttons to run tests.
    /// Subclasses define test methods with the [TestMethod] attribute.
    /// </summary>
    public abstract class BaseTester : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // Test Result Tracking
        // -------------------------------------------------------------------------
        [Serializable]
        public class TestResult
        {
            public string TestName;
            public bool Passed;
            public string Message;
            public float DurationMs;
        }

        #if ODIN_INSPECTOR
        [Title("Test Results")]
        [ReadOnly]
        [ListDrawerSettings(ShowIndexLabels = true)]
        #endif
        [SerializeField] protected List<TestResult> results = new List<TestResult>();

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] protected int passCount;

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] protected int failCount;

        #if ODIN_INSPECTOR
        [ReadOnly]
        #endif
        [SerializeField] protected bool isRunning;
        
        #if ODIN_INSPECTOR
        [Title("Automation")]
        [InfoBox("If true, tests will run automatically when the scene starts.")]
        #endif
        [SerializeField] protected bool autoRunOnStart = false;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public List<TestResult> Results => results;
        public int PassCount => passCount;
        public int FailCount => failCount;
        public int TotalTests => passCount + failCount;
        public bool AllPassed => failCount == 0 && passCount > 0;
        public bool IsRunning => isRunning;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        protected virtual void Start()
        {
            if (autoRunOnStart)
            {
                RunAllTests();
            }
        }

        // -------------------------------------------------------------------------
        // Abstract: Subclass Identity
        // -------------------------------------------------------------------------
        public abstract string TesterName { get; }

        // -------------------------------------------------------------------------
        // Test Execution
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Controls")]
        [Button("Run All Tests", ButtonSizes.Large)]
        [GUIColor(0f, 1f, 0.4f)]
        #endif
        public void RunAllTests()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"[{TesterName}] Tests must be run in Play Mode.");
                return;
            }

            results.Clear();
            passCount = 0;
            failCount = 0;
            isRunning = true;

            Debug.Log($"<color=#00CCFF>══════════════════════════════════════</color>");
            Debug.Log($"<color=#00CCFF>[{TesterName}] Running Tests...</color>");
            Debug.Log($"<color=#00CCFF>══════════════════════════════════════</color>");

            Debug.Log($"<color=#00CCFF>══════════════════════════════════════</color>");

            EnsureDependencies();
            Setup();

            // Discover and run all methods marked with [TestMethod]
            var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<TestMethodAttribute>();
                if (attr != null)
                {
                    RunSingleTest(method, attr.Description ?? method.Name);
                }
            }

            TearDown();

            isRunning = false;

            // Summary
            string color = AllPassed ? "#00FF00" : "#FF4444";
            Debug.Log($"<color=#00CCFF>──────────────────────────────────────</color>");
            Debug.Log($"<color={color}>[{TesterName}] Results: {passCount} passed, {failCount} failed ({TotalTests} total)</color>");
            Debug.Log($"<color=#00CCFF>══════════════════════════════════════</color>");
        }

        private void RunSingleTest(MethodInfo method, string testName)
        {
            var result = new TestResult { TestName = testName };
            float startTime = Time.realtimeSinceStartup;

            try
            {
                method.Invoke(this, null);
                result.Passed = true;
                result.Message = "OK";
                passCount++;
                Debug.Log($"  <color=#00FF00>PASS</color> {testName}");
            }
            catch (TargetInvocationException tie)
            {
                var inner = tie.InnerException;
                result.Passed = false;
                result.Message = inner?.Message ?? "Unknown error";
                failCount++;
                Debug.LogError($"  <color=#FF4444>FAIL</color> {testName}: {result.Message}");
            }
            catch (Exception ex)
            {
                result.Passed = false;
                result.Message = ex.Message;
                failCount++;
                Debug.LogError($"  <color=#FF4444>FAIL</color> {testName}: {result.Message}");
            }

            result.DurationMs = (Time.realtimeSinceStartup - startTime) * 1000f;
            results.Add(result);
        }

        // -------------------------------------------------------------------------
        // Setup / TearDown Hooks (Override in subclasses)
        // -------------------------------------------------------------------------
        /// <summary>
        /// Called before Setup. Use this to ensure required objects (Managers, etc.) exist in the scene.
        /// If dependencies are missing, this method should log errors or instantiate prefabs.
        /// </summary>
        protected virtual void EnsureDependencies() { }
        
        protected virtual void Setup() { }
        protected virtual void TearDown() { }

        // -------------------------------------------------------------------------
        // Assertion Helpers
        // -------------------------------------------------------------------------
        protected void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition) throw new Exception(message);
        }

        protected void AssertEqual<T>(T expected, T actual, string context = "")
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                string msg = string.IsNullOrEmpty(context)
                    ? $"Expected {expected}, got {actual}"
                    : $"{context}: Expected {expected}, got {actual}";
                throw new Exception(msg);
            }
        }

        protected void AssertNotNull(object obj, string name = "Object")
        {
            // Unity overrides == for destroyed objects, so check both
            if (obj == null || obj.Equals(null))
                throw new Exception($"{name} is null");
        }

        protected void AssertNull(object obj, string name = "Object")
        {
            if (obj != null && !obj.Equals(null))
                throw new Exception($"{name} is not null");
        }

        protected void AssertTrue(bool condition, string message = "Expected true")
        {
            if (!condition) throw new Exception(message);
        }

        protected void AssertFalse(bool condition, string message = "Expected false")
        {
            if (condition) throw new Exception(message);
        }

        protected void AssertGreaterThan(float value, float threshold, string context = "")
        {
            if (value <= threshold)
            {
                string msg = string.IsNullOrEmpty(context)
                    ? $"Expected > {threshold}, got {value}"
                    : $"{context}: Expected > {threshold}, got {value}";
                throw new Exception(msg);
            }
        }

        protected void AssertLessThan(float value, float threshold, string context = "")
        {
            if (value >= threshold)
            {
                string msg = string.IsNullOrEmpty(context)
                    ? $"Expected < {threshold}, got {value}"
                    : $"{context}: Expected < {threshold}, got {value}";
                throw new Exception(msg);
            }
        }

        protected void AssertApproxEqual(float expected, float actual, float tolerance = 0.01f, string context = "")
        {
            if (Mathf.Abs(expected - actual) > tolerance)
            {
                string msg = string.IsNullOrEmpty(context)
                    ? $"Expected ~{expected}, got {actual} (tolerance: {tolerance})"
                    : $"{context}: Expected ~{expected}, got {actual} (tolerance: {tolerance})";
                throw new Exception(msg);
            }
        }

        protected void AssertContains(string haystack, string needle, string context = "")
        {
            if (string.IsNullOrEmpty(haystack) || !haystack.Contains(needle))
            {
                string msg = string.IsNullOrEmpty(context)
                    ? $"String does not contain '{needle}'"
                    : $"{context}: String does not contain '{needle}'";
                throw new Exception(msg);
            }
        }

        protected void AssertThrows<TException>(Action action, string context = "") where TException : Exception
        {
            try
            {
                action();
                string msg = string.IsNullOrEmpty(context)
                    ? $"Expected exception {typeof(TException).Name} but none was thrown"
                    : $"{context}: Expected exception {typeof(TException).Name} but none was thrown";
                throw new Exception(msg);
            }
            catch (TException)
            {
                // Expected
            }
        }
    }

    // -------------------------------------------------------------------------
    // TestMethod Attribute — marks methods to be auto-discovered by BaseTester
    // -------------------------------------------------------------------------
    [AttributeUsage(AttributeTargets.Method)]
    public class TestMethodAttribute : Attribute
    {
        public string Description { get; }

        public TestMethodAttribute(string description = null)
        {
            Description = description;
        }
    }
}
