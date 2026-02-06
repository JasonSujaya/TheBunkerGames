using UnityEngine;
using System.Collections;
using TheBunkerGames;
using TheBunkerGames.Tests;
using Neocortex;

namespace TheBunkerGames.Tests
{
    public class NeocortexIntegrationTester : BaseTester
    {
        public override string TesterName => "NeocortexIntegration";

        private NeocortexIntegrator integrator;

        protected override void EnsureDependencies()
        {
            integrator = FindFirstObjectByType<NeocortexIntegrator>();
            
            if (integrator == null)
            {
                var go = new GameObject("NeocortexIntegrator_Test");
                integrator = go.AddComponent<NeocortexIntegrator>();
                // Add dependencies if possible without knowing full 3rd party assembly
                // Assuming NeocortexSmartAgent is a MonoBehaviour we can add
                go.AddComponent<NeocortexSmartAgent>(); 
            }
        }

        protected override void Setup()
        {
            integrator = FindFirstObjectByType<NeocortexIntegrator>();
            AssertNotNull(integrator, "NeocortexIntegrator instance");
        }

        [TestMethod("Integrator is initialized correctly")]
        private void Test_IntegratorSetup()
        {
            AssertNotNull(integrator, "Integrator component should be found");
        }

        [TestMethod("SendNeocortexMessage execution")]
        private void Test_SendMessage()
        {
            // Validates that the method runs without exception
            // Actual network success cannot be guaranteed in unit test without mocking
            integrator.SendNeocortexMessage("TEST_MESSAGE_UNIT_TEST");
        }
    }
}
