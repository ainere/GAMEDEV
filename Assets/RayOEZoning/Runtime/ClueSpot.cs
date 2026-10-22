using TMPro;
using UnityEngine;

namespace RayOEZoning
{
    public enum ClueMedium
    {
        Plaque,
        Clipboard,
        Paper,
        StickyNote,
        Whiteboard,
        Monitor,
        Lcd,
        Label,
        Stencil,
        Memo
    }

    /// <summary>A surface that can carry hidden writing. The face's local XY plane is the writable area, read from local -Z.</summary>
    public sealed class ClueSpot : MonoBehaviour
    {
        public ClueMedium medium;
        /// <summary>0 entrance, 1 lab, 2 cold room, 3 server room. Each zone gets one code digit.</summary>
        public int zone;
        public bool allowOrder = true;
        public Transform face;
        public Vector2 area = new Vector2(.2f, .25f);
        public Material ink;
        public Color textColor = Color.white;
        public HiddenClue clue;

        public LetteringStyle Style
        {
            get
            {
                switch (medium)
                {
                    case ClueMedium.Clipboard: case ClueMedium.Paper: return LetteringStyle.Hand;
                    case ClueMedium.StickyNote: case ClueMedium.Whiteboard: return LetteringStyle.Marker;
                    case ClueMedium.Monitor: case ClueMedium.Lcd: return LetteringStyle.Pixel;
                    case ClueMedium.Stencil: return LetteringStyle.Stencil;
                    default: return LetteringStyle.Printed;
                }
            }
        }

        public void Clear()
        {
            if (!face) return;
            for (int i = face.childCount - 1; i >= 0; i--)
            {
                var child = face.GetChild(i).gameObject;
                foreach (var filter in child.GetComponentsInChildren<MeshFilter>(true))
                    if (filter.sharedMesh) Discard(filter.sharedMesh);
                Discard(child);
            }
            if (clue) clue.hiddenRenderers = new Renderer[0];
        }

        public void Show(ClueDefinition definition, int index, TMP_FontAsset font, System.Random random)
        {
            Clear();
            var renderers = ClueWriter.Write(this, definition, font, random);
            if (!clue) return;
            clue.clueIndex = index;
            clue.anchor = face;
            clue.hiddenRenderers = renderers;
        }

        static void Discard(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
