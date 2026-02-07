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
        
        [Space]
        public System.Collections.Generic.List<string> InventorySnapshot = new System.Collections.Generic.List<string>();

        #if ODIN_INSPECTOR
        [Sirenix.OdinInspector.Button("Select Inventory Manager", Sirenix.OdinInspector.ButtonSizes.Medium)]
        [Sirenix.OdinInspector.GUIColor(0.2f, 0.6f, 1f)]
        private void Debug_SelectInventory()
        {
            if (GameManager.Instance != null && GameManager.Instance.Inventory != null)
            {
                #if UNITY_EDITOR
                UnityEditor.Selection.activeGameObject = GameManager.Instance.Inventory.gameObject;
                #endif
            }
        }
        #endif

        public void ResetData()
        {
            CurrentDay = 1;
            CurrentState = GameState.StatusReview;
            IsGameOver = false;
            FamilyCount = 4;
            AverageHealth = 100f;
            DebugFamilySnapshot.Clear();
            InventorySnapshot.Clear();
        }

        public void UpdateSync(FamilyManager familyManager, InventoryManager inventoryManager = null)
        {
            if (familyManager != null)
            {
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

            if (inventoryManager != null)
            {
                InventorySnapshot.Clear();
                foreach (var item in inventoryManager.Items)
                {
                    // Use ItemManager to get friendly name if possible
                    string displayName = item.ItemId;
                    if (ItemManager.Instance != null)
                    {
                        var data = ItemManager.Instance.GetItem(item.ItemId);
                        if (data != null) displayName = data.ItemName;
                    }
                    InventorySnapshot.Add($"{displayName}: {item.Quantity}");
                }
            }
        }
    }
}
