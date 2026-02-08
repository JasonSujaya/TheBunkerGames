using UnityEngine;
using UnityEditor;

namespace TheBunkerGames.Editor
{
    public static class SetFamilyUISpritesEditor
    {
        [MenuItem("TheBunkerGames/Set FamilySelectUI Sprites From ThemeSelectUI")]
        public static void SetSprites()
        {
            // Find both UIs in scene
            var familyUI = Object.FindFirstObjectByType<FamilySelectUI>(FindObjectsInactive.Include);
            var themeUI = Object.FindFirstObjectByType<ThemeSelectUI>(FindObjectsInactive.Include);

            if (familyUI == null)
            {
                Debug.LogError("[SetFamilyUISprites] FamilySelectUI not found!");
                return;
            }
            if (themeUI == null)
            {
                Debug.LogError("[SetFamilyUISprites] ThemeSelectUI not found!");
                return;
            }

            // Use SerializedObject to copy sprite references
            SerializedObject familySO = new SerializedObject(familyUI);
            SerializedObject themeSO = new SerializedObject(themeUI);

            // Copy titleBannerSprite from ThemeSelectUI
            var themeBanner = themeSO.FindProperty("titleBannerSprite");
            var familyBanner = familySO.FindProperty("titleBannerSprite");
            if (themeBanner != null && familyBanner != null)
            {
                familyBanner.objectReferenceValue = themeBanner.objectReferenceValue;
                Debug.Log($"[SetFamilyUISprites] Set titleBannerSprite: {themeBanner.objectReferenceValue?.name ?? "null"}");
            }

            // Copy cardFrameSprite -> portraitFrameSprite
            var themeCardFrame = themeSO.FindProperty("cardFrameSprite");
            var familyPortraitFrame = familySO.FindProperty("portraitFrameSprite");
            if (themeCardFrame != null && familyPortraitFrame != null)
            {
                familyPortraitFrame.objectReferenceValue = themeCardFrame.objectReferenceValue;
                Debug.Log($"[SetFamilyUISprites] Set portraitFrameSprite: {themeCardFrame.objectReferenceValue?.name ?? "null"}");
            }

            familySO.ApplyModifiedProperties();
            EditorUtility.SetDirty(familyUI);

            // Now re-run AutoSetup to rebuild with new sprites
            familyUI.AutoSetup();
            EditorUtility.SetDirty(familyUI);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(familyUI.gameObject.scene);

            Debug.Log("[SetFamilyUISprites] Done! Sprites assigned and AutoSetup re-run.");
        }
    }
}
