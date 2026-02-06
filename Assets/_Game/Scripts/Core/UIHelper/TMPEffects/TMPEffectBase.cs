using UnityEngine;
using TMPro;

namespace TheBunkerGames
{
    /// <summary>
    /// Abstract base class for per-character TMP vertex effects.
    /// Handles the ForceMeshUpdate → iterate characters → UpdateGeometry boilerplate.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public abstract class TMPEffectBase : MonoBehaviour
    {
        // -------------------------------------------------------------------------
        // References
        // -------------------------------------------------------------------------
        protected TMP_Text textComponent;

        // -------------------------------------------------------------------------
        // Unity Lifecycle
        // -------------------------------------------------------------------------
        protected virtual void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
        }

        protected virtual void LateUpdate()
        {
            if (textComponent == null) return;

            textComponent.ForceMeshUpdate();
            TMP_TextInfo textInfo = textComponent.textInfo;

            if (textInfo == null || textInfo.characterCount == 0) return;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;

                ApplyEffect(i, vertexIndex, vertices, colors, charInfo);
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
                textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }

        /// <summary>
        /// Override to apply per-character vertex and/or color modifications.
        /// Called once per visible character each frame.
        /// </summary>
        protected abstract void ApplyEffect(
            int charIndex,
            int vertexIndex,
            Vector3[] vertices,
            Color32[] colors,
            TMP_CharacterInfo charInfo
        );
    }
}
