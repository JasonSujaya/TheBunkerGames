using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace TheBunkerGames.Editor
{
    public static class AssignPortraitsEditor
    {
        [MenuItem("TheBunkerGames/Assign Character Portraits")]
        public static void AssignPortraits()
        {
            // Portrait filename (without extension) -> Character SO name mapping
            var portraitMap = new Dictionary<string, string>
            {
                { "Chad", "Chad" },
                { "DDPrepper", "DoomsdayDave" },
                { "Donald", "DonaldTrumble" },
                { "Gordon", "ChefRamsay" },
                { "Morty", "MortySanchez" },
                { "Musk", "ElonTusk" },
                { "Rick", "RickSanchez" },
                { "Sarah", "Sarah" },
                { "Superman", "UltraMan" },
            };

            string portraitFolder = "Assets/_Game/Art/CharacterPortrait";
            string characterFolder = "Assets/_Game/ScriptableObjects/Characters";

            int assigned = 0;

            // First pass: ensure all textures are imported as Sprite
            string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { portraitFolder });
            foreach (string guid in texGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null && importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.SaveAndReimport();
                    Debug.Log($"[AssignPortraits] Set Sprite import type: {path}");
                }
            }

            // Second pass: assign sprites to character SOs
            foreach (var kvp in portraitMap)
            {
                string portraitName = kvp.Key;
                string characterSOName = kvp.Value;

                // Find the portrait sprite
                string portraitPath = $"{portraitFolder}/{portraitName}.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
                if (sprite == null)
                {
                    Debug.LogWarning($"[AssignPortraits] Portrait not found: {portraitPath}");
                    continue;
                }

                // Find the character SO
                string soPath = $"{characterFolder}/{characterSOName}.asset";
                CharacterDefinitionSO charSO = AssetDatabase.LoadAssetAtPath<CharacterDefinitionSO>(soPath);
                if (charSO == null)
                {
                    Debug.LogWarning($"[AssignPortraits] Character SO not found: {soPath}");
                    continue;
                }

                // Use SerializedObject to set the portrait field
                SerializedObject so = new SerializedObject(charSO);
                SerializedProperty portraitProp = so.FindProperty("portrait");
                if (portraitProp != null)
                {
                    portraitProp.objectReferenceValue = sprite;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(charSO);
                    assigned++;
                    Debug.Log($"[AssignPortraits] Assigned {portraitName}.png -> {characterSOName}");
                }
                else
                {
                    Debug.LogWarning($"[AssignPortraits] 'portrait' property not found on {characterSOName}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AssignPortraits] Done! Assigned {assigned}/{portraitMap.Count} portraits.");
        }
    }
}
