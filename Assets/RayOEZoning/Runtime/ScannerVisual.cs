using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning
{
    /// <summary>CCTV zone footprints for play; exact physics geometry appears only in diagnostic view.</summary>
    public sealed class ScannerVisual : MonoBehaviour
    {
        public HazardDetector detector;
        public StealthGame game;
        public Material lineMaterial;
        public Renderer lens;
        public Renderer[] statusLights;
        public Light lamp;
        readonly List<LineRenderer> lines = new List<LineRenderer>();
        readonly List<LineRenderer> footprints = new List<LineRenderer>();
        bool buildingFootprint;
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
            buildingFootprint = true;
            BuildFootprint();
            buildingFootprint = false;
            ray = Line(new[] { Vector3.zero, Vector3.zero }, false); ray.useWorldSpace = true;
            block = new MaterialPropertyBlock();
        }

        void BuildFootprint()
        {
            float y = .025f - detector.home.y;
            if (detector.shape == DetectionShape.Sphere)
                Circle(new Vector3(0, y, 0), Vector3.right, Vector3.forward, detector.radius);
            else if (detector.shape == DetectionShape.Box)
            {
                var e = detector.halfExtents;
                for (int x = -1; x <= 1; x += 2)
                    for (int z = -1; z <= 1; z += 2)
                        Line(new[] {
                            new Vector3(x * (e.x - .34f), y, z * e.z),
                            new Vector3(x * e.x, y, z * e.z),
                            new Vector3(x * e.x, y, z * (e.z - .34f))
                        }, false);
            }
            else
            {
                Vector3 a = detector.capsulePointA, b = detector.capsulePointB;
                a.y = b.y = y;
                Vector3 axis = (b - a).normalized;
                Vector3 u = Vector3.Cross(Vector3.up, axis).normalized;
                var points = new Vector3[66];
                for (int i = 0; i <= 32; i++)
                {
                    float angle = i * Mathf.PI / 32;
                    points[i] = a + (Mathf.Cos(angle) * u - Mathf.Sin(angle) * axis) * detector.radius;
                    angle += Mathf.PI;
                    points[33 + i] = b + (Mathf.Cos(angle) * u - Mathf.Sin(angle) * axis) * detector.radius;
                }
                Line(points, true);
            }
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
            if (buildingFootprint) footprints.Add(line); else lines.Add(line);
            return line;
        }
        void LateUpdate()
        {
            if (!detector || block == null) return;
            bool diagnostic = game && game.Diagnostics;
            Color color = detector.IsDetecting ? new Color(1, .31f, .17f, .70f) : new Color(.88f, .54f, .16f, .65f);
            block.SetColor("_BaseColor", color); block.SetColor("_Color", color);
            foreach (var line in lines)
            {
                line.enabled = diagnostic;
                line.SetPropertyBlock(block); line.startColor = line.endColor = Color.white;
                line.startWidth = line.endWidth = .016f;
            }
            Color floorColor = detector.IsDetecting ? new Color(1, .31f, .17f, .45f) : new Color(.88f, .54f, .16f, .26f);
            block.SetColor("_BaseColor", floorColor); block.SetColor("_Color", floorColor);
            foreach (var footprint in footprints)
            {
                footprint.SetPropertyBlock(block); footprint.startWidth = footprint.endWidth = .018f;
            }
            Color opticColor = detector.IsDetecting ? new Color(1, .22f, .13f, 1) : new Color(.95f, .48f, .12f, 1);
            block.SetColor("_BaseColor", opticColor); block.SetColor("_Color", opticColor);
            if (statusLights != null) foreach (var optic in statusLights) if (optic) optic.SetPropertyBlock(block);
            if (lens) lens.SetPropertyBlock(block);
            if (lamp) { lamp.color = opticColor; lamp.intensity = detector.IsDetecting ? .32f : .15f; }
            ray.enabled = diagnostic && detector.HasPlayerOverlap;
            if (ray.enabled)
            {
                ray.SetPosition(0, detector.LastRayOrigin); ray.SetPosition(1, detector.LastRayTarget);
                var rayBlock = new MaterialPropertyBlock();
                rayBlock.SetColor("_BaseColor", detector.LastRayBlocked ? new Color(.25f, .85f, .7f, .9f) : new Color(1, .3f, .2f, .9f));
                ray.SetPropertyBlock(rayBlock);
            }
        }
    }
}
