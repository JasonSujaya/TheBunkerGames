using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace TheBunkerGames.Editor
{
    #if ODIN_INSPECTOR
    public class ProjectSetupTool : OdinEditorWindow
    #else
    public class ProjectSetupTool : EditorWindow
    #endif
    {
        [MenuItem("TheBunkerGames/Project Setup Tool")]
        private static void OpenWindow()
        {
            GetWindow<ProjectSetupTool>().Show();
        }

        #if ODIN_INSPECTOR
        [Title("Project Setup")]
        [Button(ButtonSizes.Large), GUIColor(0.2f, 0.8f, 0.2f)]
        #endif
        private void CreateFolderStructure()
        {
            string root = "Assets/_Game";
            string[] folders = new string[]
            {
                "Scripts/Managers", "Scripts/Controllers", "Scripts/Actions", "Scripts/UI", "Scripts/Data", "Scripts/Utils",
                "Prefabs", "Scenes", "Art/Materials", "Art/Models", "Art/Textures", "Audio/Music", "Audio/SFX"
            };

            if (!AssetDatabase.IsValidFolder(root)) AssetDatabase.CreateFolder("Assets", "_Game");

            foreach (string folder in folders)
            {
                string fullPath = Path.Combine(root, folder);
                if (!Directory.Exists(fullPath)) Directory.CreateDirectory(fullPath);
            }
            AssetDatabase.Refresh();
            Debug.Log("Folders Generated!");
        }

        #if ODIN_INSPECTOR
        [Title("Code & Assets")]
        [Button(ButtonSizes.Medium), GUIColor(0.2f, 0.6f, 1f)]
        #endif
        private void CreateTemplateScripts()
        {
            CreateScript("Assets/_Game/Scripts/Managers/GameManager.cs", GetGameManagerTemplate());
            CreateScript("Assets/_Game/Scripts/Data/GameSettings.cs", GetGameSettingsTemplate());
            CreateScript("Assets/_Game/Scripts/Controllers/PlayerController.cs", GetPlayerControllerTemplate());
            AssetDatabase.Refresh();
            Debug.Log("Template Scripts Generated! Wait for compilation.");
        }

        #if ODIN_INSPECTOR
        [Button(ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.2f)]
        #endif
        private void CreateExampleAssets()
        {
            CreatePlayerPrefab();
            CreateGameSettingsInternal();
        }

        private void CreateScript(string path, string content)
        {
            if (File.Exists(path)) { Debug.LogWarning($"File exists: {path}"); return; }
            File.WriteAllText(path, content);
            Debug.Log($"Created: {path}");
        }

        private void CreatePlayerPrefab()
        {
            string path = "Assets/_Game/Prefabs/Player.prefab";
            if (File.Exists(path)) return;

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            // Check for PlayerController AFTER compilation
            var type = System.Type.GetType("TheBunkerGames.Controllers.PlayerController, Assembly-CSharp");
            if (type != null) go.AddComponent(type);
            
            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
            Debug.Log("Created Player Prefab");
        }

        private void CreateGameSettingsInternal()
        {
            string path = "Assets/_Game/Scripts/Data/GlobalSettings.asset";
            if (File.Exists(path)) return;

            var type = System.Type.GetType("TheBunkerGames.Data.GameSettings, Assembly-CSharp");
            if (type != null)
            {
                var so = ScriptableObject.CreateInstance(type);
                AssetDatabase.CreateAsset(so, path);
                Debug.Log("Created GameSettings SO");
            }
            else
            {
                Debug.LogError("GameSettings type not found. Did you run 'Create Template Scripts' and wait for compile?");
            }
        }

        // ----------------------------------------------------------------------------------
        // Templates
        // ----------------------------------------------------------------------------------

        private string GetGameManagerTemplate()
        {
            return @"using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        #if ODIN_INSPECTOR
        [Button]
        #endif
        public void StartGame()
        {
            Debug.Log(""Game Started"");
        }
    }
}";
        }

        private string GetGameSettingsTemplate()
        {
            return @"using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Data
{
    [CreateAssetMenu(fileName = ""GameSettings"", menuName = ""TheBunkerGames/GameSettings"")]
    public class GameSettings : ScriptableObject
    {
        #if ODIN_INSPECTOR
        [Title(""General Settings"")]
        #endif
        public float defaultSpeed = 5f;
        public int maxHealth = 100;
    }
}";
        }

        private string GetPlayerControllerTemplate()
        {
            return @"using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace TheBunkerGames.Controllers
{
    public class PlayerController : MonoBehaviour
    {
        #if ODIN_INSPECTOR
        [Title(""Stats"")]
        #endif
        [SerializeField] private float speed = 5f;

        private void Update()
        {
            // Simple movement logic would go here
        }
    }
}";
        }

        #if !ODIN_INSPECTOR
        private void OnGUI()
        {
            if (GUILayout.Button("Create Folder Structure")) CreateFolderStructure();
            GUILayout.Space(10);
            if (GUILayout.Button("Create Template Scripts")) CreateTemplateScripts();
            GUILayout.Space(10);
            if (GUILayout.Button("Create Example Assets")) CreateExampleAssets();
        }
        #endif
    }
}
