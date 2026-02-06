using UnityEngine;
using TheBunkerGames;
using TheBunkerGames.Tests;

namespace TheBunkerGames.Tests
{
    public class AngelVoiceTester : BaseTester
    {
        public override string TesterName => "AngelVoice";

        private AudioManager audioManager;

        protected override void Setup()
        {
            audioManager = AudioManager.Instance;
            AssertNotNull(audioManager, "AudioManager.Instance");
        }

        [TestMethod("AudioManager exists")]
        private void Test_AudioManager_Exists()
        {
            AssertNotNull(audioManager, "AudioManager should be reachable");
        }

        [TestMethod("PlaySound runs without error")]
        private void Test_PlaySound()
        {
            // Just verifying it doesn't crash or throw missing reference
            audioManager.PlaySound("TEST_SOUND");
        }
    }
}
