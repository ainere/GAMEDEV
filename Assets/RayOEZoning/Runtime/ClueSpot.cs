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

    // A surface that can carry hidden writing. The face's local XY plane is the writable area, read from local -Z.
    public sealed class ClueSpot : MonoBehaviour
    {
        public ClueMedium medium;
        // 0 entrance, 1 lab, 2 cold room, 3 server room. Each zone gets one code digit.
        public int zone;
        public bool allowOrder = true;
        public Transform face;
        public Vector2 area = new Vector2(.2f, .25f);
        // RevealInk material: the writing only exists where the flashlight cone falls.
        public Material ink;
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

        public void Show(ClueDefinition definition, int index, System.Random random)
        {
            Clear();
            var renderers = ClueWriter.Write(this, definition, random);
            if (!clue) return;
            clue.clueIndex = index;
            clue.anchor = face;
            clue.radius = area.magnitude * .5f;
            clue.hiddenRenderers = renderers;
        }

        static void Discard(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
