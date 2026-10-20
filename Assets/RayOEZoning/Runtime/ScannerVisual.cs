using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning
{
    /// <summary>Authored scanner light outlines, built from the detector's actual physics dimensions.</summary>
    public sealed class ScannerVisual : MonoBehaviour
    {
        public HazardDetector detector;
        public StealthGame game;
        public Material lineMaterial;
        public Renderer lens;
        public Light lamp;
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        LineRenderer ray;
        MaterialPropertyBlock block;
        void Start()
        {
            if (!detector) detector = GetComponent<HazardDetector>();
            if (!game) game = FindAnyObjectByType<StealthGame>();
            if (!lineMaterial) lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            if (detector.shape == DetectionShape.Sphere)
            {
                Circle(Vector3.zero, Vector3.right, Vector3.up, detector.radius);
                Circle(Vector3.zero, Vector3.right, Vector3.forward, detector.radius);
                Circle(Vector3.zero, Vector3.forward, Vector3.up, detector.radius);
            }
            else if (detector.shape == DetectionShape.Box)
            {
                var e = detector.halfExtents;
                for (int axis = 0; axis < 3; axis++)
                    for (int a = -1; a <= 1; a += 2)
                        for (int b = -1; b <= 1; b += 2)
                        {
                            Vector3 first = Vector3.zero, last = Vector3.zero;
                            first[axis] = -e[axis]; last[axis] = e[axis];
                            first[(axis + 1) % 3] = last[(axis + 1) % 3] = a * e[(axis + 1) % 3];
                            first[(axis + 2) % 3] = last[(axis + 2) % 3] = b * e[(axis + 2) % 3];
                            Line(new[] { first, last }, false);
                        }
            }
            else
            {
                Vector3 a = detector.capsulePointA, b = detector.capsulePointB;
                Vector3 axis = (b - a).normalized;
                Vector3 u = Vector3.Cross(axis, Vector3.up).normalized;
                if (u.sqrMagnitude < .01f) u = Vector3.right;
                Vector3 v = Vector3.Cross(axis, u).normalized;
                Circle(a, u, v, detector.radius); Circle(b, u, v, detector.radius);
                for (int j = 0; j < 4; j++)
                {
                    Vector3 radial = (j % 2 == 0 ? u : v) * (j < 2 ? 1 : -1) * detector.radius;
                    Line(new[] { a + radial, b + radial }, false);
                }
                CapsuleArc(a, -axis, u); CapsuleArc(a, -axis, v);
                CapsuleArc(b, axis, u); CapsuleArc(b, axis, v);
            }
            ray = Line(new[] { Vector3.zero, Vector3.zero }, false); ray.useWorldSpace = true;
            block = new MaterialPropertyBlock();
        }
        void CapsuleArc(Vector3 center, Vector3 outward, Vector3 radial)
        {
            var points = new Vector3[25];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI / (points.Length - 1);
                points[i] = center + (Mathf.Cos(angle) * radial + Mathf.Sin(angle) * outward) * detector.radius;
            }
            Line(points, false);
        }
        void Circle(Vector3 center, Vector3 u, Vector3 v, float radius)
        {
            var points = new Vector3[80];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / points.Length;
                points[i] = center + (Mathf.Cos(angle) * u + Mathf.Sin(angle) * v) * radius;
            }
            Line(points, true);
        }
        LineRenderer Line(Vector3[] points, bool loop)
        {
            var go = new GameObject("Scan boundary"); go.layer = gameObject.layer; go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = lineMaterial;
            line.useWorldSpace = false; line.loop = loop; line.positionCount = points.Length; line.SetPositions(points);
            line.startWidth = line.endWidth = .009f; line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false; line.numCornerVertices = 2; line.numCapVertices = 2;
            lines.Add(line); return line;
        }
        void LateUpdate()
        {
            if (!detector || block == null) return;
            bool diagnostic = game && game.Diagnostics;
            Color color = detector.IsDetecting ? new Color(1, .31f, .17f, .65f) : new Color(.81f, .51f, .17f, diagnostic ? .85f : .27f);
            block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            foreach (var line in lines)
            {
                line.SetPropertyBlock(block); line.startColor = line.endColor = Color.white;
                line.startWidth = line.endWidth = diagnostic ? .018f : .009f;
            }
            if (lamp) { lamp.color = color; lamp.intensity = detector.IsDetecting ? .52f : .24f; }
            ray.enabled = diagnostic;
            if (diagnostic)
            {
                ray.SetPosition(0, detector.LastRayOrigin); ray.SetPosition(1, detector.LastRayTarget);
                var rayBlock = new MaterialPropertyBlock();
                rayBlock.SetColor("_BaseColor", detector.LastRayBlocked ? new Color(.25f, .85f, .7f, .9f) : new Color(1, .3f, .2f, .9f));
                ray.SetPropertyBlock(rayBlock);
            }
        }
    }
}
