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
            bool isEnabled = EditorPrefs.GetBool(PREF_KEY, true);
            bool newState = !isEnabled;
            
            EditorPrefs.SetBool(PREF_KEY, newState);
            
            Debug.Log($"[AutoRefreshToggle] Auto Refresh is now {(newState ? "ENABLED" : "DISABLED")}.");
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
