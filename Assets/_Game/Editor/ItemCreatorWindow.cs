using UnityEngine;
using UnityEditor;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace TheBunkerGames.Editor
{
    /// <summary>
    /// Fast item creation window.
    /// Creates ItemDataSO assets quickly and adds them to the database.
    /// </summary>
    public class ItemCreatorWindow : EditorWindow
    {
        // -------------------------------------------------------------------------
        // Fields
        // -------------------------------------------------------------------------
        private string itemName = "NewItem";
        private ItemType itemType = ItemType.Junk;
        private ItemDatabaseDataSO targetDatabase;

        // -------------------------------------------------------------------------
        // Menu Item
        // -------------------------------------------------------------------------
        [MenuItem("TheBunkerGames/Item Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<ItemCreatorWindow>("Item Creator");
            window.minSize = new Vector2(300, 200);
        }

        // -------------------------------------------------------------------------
        // GUI
        // -------------------------------------------------------------------------
        private void OnEnable()
        {
            // Try to find existing database
            string[] guids = AssetDatabase.FindAssets("t:ItemDatabaseDataSO");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                targetDatabase = AssetDatabase.LoadAssetAtPath<ItemDatabaseDataSO>(path);
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Create New Item", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Item Name
            itemName = EditorGUILayout.TextField("Item Name", itemName);

            // Item Type
            itemType = (ItemType)EditorGUILayout.EnumPopup("Item Type", itemType);

            GUILayout.Space(10);

            // Target Database
            targetDatabase = (ItemDatabaseDataSO)EditorGUILayout.ObjectField(
                "Target Database", 
                targetDatabase, 
                typeof(ItemDatabaseDataSO), 
                false
            );

            GUILayout.Space(20);

            // Create Button
            GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button("Create & Add to DB", GUILayout.Height(40)))
            {
                CreateItem();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(10);

            // Help Box
            EditorGUILayout.HelpBox(
                "Creates 'Item_[Name].asset' in Assets/_Data/Items/ and auto-adds to the database.", 
                MessageType.Info
            );
        }

        // -------------------------------------------------------------------------
        // Create Item
        // -------------------------------------------------------------------------
        private void CreateItem()
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                EditorUtility.DisplayDialog("Error", "Please enter an item name.", "OK");
                return;
            }

            // Ensure folder exists
            string folderPath = "Assets/_Data/Items";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets/_Data", "Items");
            }

            // Create the asset
            string sanitizedName = itemName.Replace(" ", "");
            string assetPath = $"{folderPath}/Item_{sanitizedName}.asset";

            // Check if exists
            if (AssetDatabase.LoadAssetAtPath<ItemDataSO>(assetPath) != null)
            {
                EditorUtility.DisplayDialog("Error", $"Item already exists at:\n{assetPath}", "OK");
                return;
            }

            // Create new ItemDataSO
            ItemDataSO newItem = CreateInstance<ItemDataSO>();
            
            // Use SerializedObject to set private fields
            AssetDatabase.CreateAsset(newItem, assetPath);
            
            SerializedObject so = new SerializedObject(newItem);
            so.FindProperty("id").stringValue = sanitizedName.ToLower();
            so.FindProperty("displayName").stringValue = itemName;
            so.FindProperty("itemType").enumValueIndex = (int)itemType;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();

            // Add to database
            if (targetDatabase != null)
            {
                SerializedObject dbSo = new SerializedObject(targetDatabase);
                SerializedProperty allItems = dbSo.FindProperty("allItems");
                allItems.arraySize++;
                allItems.GetArrayElementAtIndex(allItems.arraySize - 1).objectReferenceValue = newItem;
                dbSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(targetDatabase);
                AssetDatabase.SaveAssets();
            }

            // Ping the new asset
            EditorGUIUtility.PingObject(newItem);
            Selection.activeObject = newItem;

            Debug.Log($"[ItemCreator] Created: {assetPath}");

            // Clear for next input
            itemName = "NewItem";
        }
    }
}
