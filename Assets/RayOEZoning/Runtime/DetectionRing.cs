using UnityEngine;
using UnityEngine.UI;

namespace RayOEZoning
{
    /// <summary>
    /// Detection meter drawn as a ring around the crosshair. It fills from the bottom up both sides at once:
    /// the lower half is suspicion, the upper half is the last stretch before the cameras raise the alarm.
    /// </summary>
    public sealed class DetectionRing : MaskableGraphic
    {
        public float radius = 120, thickness = 7;
        [Range(0, 1)] public float fill;
        public Color lowColor = Color.white, highColor = Color.red, trackColor = new Color(1, 1, 1, .12f);
        /// <summary>Gap left open at the bottom and top for the markers, as a fraction of each side.</summary>
        public float gap = .05f;
        const int Steps = 48;

        public void SetFill(float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Abs(value - fill) < .0005f) return;
            fill = value; SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 center = rectTransform.rect.center;
            for (int side = -1; side <= 1; side += 2)
            {
                Arc(vh, center, side, gap, 1 - gap, trackColor);
                float end = gap + (1 - 2 * gap) * fill;
                if (fill <= 0) continue;
                // White up to the halfway line, red above it.
                Arc(vh, center, side, gap, Mathf.Min(end, .5f), lowColor);
                if (end > .5f) Arc(vh, center, side, .5f, end, highColor);
            }
        }

        /// <summary>t runs 0 at the bottom to 1 at the top along one side of the ring.</summary>
        void Arc(VertexHelper vh, Vector2 center, int side, float from, float to, Color tint)
        {
            if (to - from < .0001f) return;
            int steps = Mathf.Max(2, Mathf.CeilToInt(Steps * (to - from)));
            float inner = radius - thickness * .5f, outer = radius + thickness * .5f;
            int first = vh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                float t = Mathf.Lerp(from, to, i / (float)steps);
                float angle = (-90 + side * t * 180) * Mathf.Deg2Rad;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(center + direction * inner, tint, Vector2.zero);
                vh.AddVert(center + direction * outer, tint, Vector2.zero);
            }
            for (int i = 0; i < steps; i++)
            {
                int a = first + i * 2;
                vh.AddTriangle(a, a + 1, a + 3);
                vh.AddTriangle(a, a + 3, a + 2);
            }
        }
    }
}
