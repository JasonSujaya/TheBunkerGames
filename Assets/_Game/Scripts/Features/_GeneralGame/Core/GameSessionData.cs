using System;
using UnityEngine;

namespace TheBunkerGames
{
    public class GameSessionData : MonoBehaviour
    {
        public GameState CurrentState;
        public int CurrentDay;
        public int FamilyCount;
        public float AverageHealth;
        public bool IsGameOver;

        [Space]
        public System.Collections.Generic.List<CharacterData> DebugFamilySnapshot = new System.Collections.Generic.List<CharacterData>();

        public void ResetData()
        {
            CurrentDay = 1;
            CurrentState = GameState.StatusReview;
            IsGameOver = false;
            FamilyCount = 4;
            AverageHealth = 100f;
            DebugFamilySnapshot.Clear();
        }

        public void UpdateSync(FamilyManager familyManager)
        {
            if (familyManager == null) return;
            
            // Sync counts
            FamilyCount = familyManager.AliveCount;
            
            // Sync Snapshot for Inspector
            DebugFamilySnapshot.Clear();
            if (familyManager.FamilyMembers != null)
            {
                DebugFamilySnapshot.AddRange(familyManager.FamilyMembers);
            }
            
            // Calc Average Health
            if (DebugFamilySnapshot.Count > 0)
            {
                float totalHealth = 0;
                foreach (var member in DebugFamilySnapshot) totalHealth += member.Health;
                AverageHealth = totalHealth / DebugFamilySnapshot.Count;
            }
        }
    }
}
