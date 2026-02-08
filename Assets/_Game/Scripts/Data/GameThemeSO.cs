using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames
{
    /// <summary>
    /// Light reference ScriptableObject defining a game theme/scenario.
    /// Points to existing assets (event schedules, LLM prompts) rather than
    /// duplicating data.
    /// </summary>
    [CreateAssetMenu(fileName = "GameThemeSO", menuName = "TheBunkerGames/Game Theme")]
    public class GameThemeSO : ScriptableObject
    {
        // -------------------------------------------------------------------------
        // Display Info
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Theme Info")]
        #endif
        [SerializeField] private string themeName;
        [TextArea(2, 4)]
        [SerializeField] private string description;
        [SerializeField] private Sprite themeIcon;
        [SerializeField] private Color themeColor = Color.white;
        [SerializeField] private GameThemeType themeType;

        // -------------------------------------------------------------------------
        // Asset References
        // -------------------------------------------------------------------------
        #if ODIN_INSPECTOR
        [Title("Asset References")]
        #endif
        [SerializeField] private PreScriptedEventScheduleSO eventSchedule;
        [SerializeField] private LLMPromptTemplateSO storyPrompt;

        // -------------------------------------------------------------------------
        // Public Properties
        // -------------------------------------------------------------------------
        public string ThemeName => themeName;
        public string Description => description;
        public Sprite ThemeIcon => themeIcon;
        public Color ThemeColor => themeColor;
        public GameThemeType ThemeType => themeType;
        public PreScriptedEventScheduleSO EventSchedule => eventSchedule;
        public LLMPromptTemplateSO StoryPrompt => storyPrompt;
    }
}
