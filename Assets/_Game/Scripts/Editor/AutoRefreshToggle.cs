using UnityEditor;
using UnityEngine;

namespace TheBunkerGames.Editor
{
    public static class AutoRefreshToggle
    {
        private const string MENU_PATH = "TheBunkerGames/Toggle Auto Refresh";
        private const string PREF_KEY = "kAutoRefresh";

        [MenuItem(MENU_PATH)]
        private static void ToggleAutoRefresh()
        {
            // 1. Get current state
            bool wasEnabled = EditorPrefs.GetBool(PREF_KEY, true);
            
            // 2. Toggle to new state
            bool newState = !wasEnabled;
            EditorPrefs.SetBool(PREF_KEY, newState);
            
            // 3. Verify
            bool isNowEnabled = EditorPrefs.GetBool(PREF_KEY, true);
            
            // 4. Log result
            string status = isNowEnabled ? "<color=green>ENABLED</color>" : "<color=red>DISABLED</color>";
            string instruction = isNowEnabled 
                ? "Unity will now automatically compile changes." 
                : "You must now manually press Cmd+R (Mac) or Ctrl+R (Win) to compile changes.";
                
            Debug.Log($"[AutoRefreshToggle] Unity Auto Refresh is now {status}. ({instruction})");
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ValidateToggleAutoRefresh()
        {
            bool isEnabled = EditorPrefs.GetBool(PREF_KEY, true);
            Menu.SetChecked(MENU_PATH, isEnabled);
            return true;
        }
    }
}
