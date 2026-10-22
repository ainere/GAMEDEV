using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning
{
    /// <summary>The diagnostic beam samples the detector's actual cone and obstruction layer mask.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class ScannerVisual : MonoBehaviour
    {
        public HazardDetector detector;
        public StealthGame game;
        public Material lineMaterial;
        public Renderer lens;
        public Renderer[] statusLights;
        public Light lamp;
        public Material shaftMaterial;
        /// <summary>Inner/outer spot ratio. Close to 1 so the lit pool ends where the detection cone ends.</summary>
        public const float InnerAngle = .93f;
        const int Around = 64, ShaftRays = 48, ShaftRings = 6;
        readonly List<LineRenderer> wires = new List<LineRenderer>();
        readonly LineRenderer[] spokes = new LineRenderer[8];
        LineRenderer rim, sightRay;
        Mesh mesh;
        MeshRenderer beam;
        Vector3[] vertices;
        MaterialPropertyBlock block;
        int wireIndex;
        Mesh shaftMesh;
        MeshRenderer shaftRenderer;
        Vector3[] shaftVertices, shaftNormals;
        Vector2[] shaftUV;
        Mesh outlineMesh;
        MeshRenderer outlineRenderer;
        Vector3[] outlineVertices, edgePoints, edgeNormals;
        bool[] edgeOnFloor;
        float alarm;

        void Start()
        {
            if (!detector) detector = GetComponent<HazardDetector>();
            if (!game) game = FindAnyObjectByType<StealthGame>();
            if (!detector || !lineMaterial) { enabled = false; return; }
            block = new MaterialPropertyBlock();
            var surface = new GameObject("Obstruction clipped vision"); surface.layer = gameObject.layer; surface.transform.SetParent(transform, false);
            mesh = new Mesh { name = "Sampled CCTV cone" }; mesh.MarkDynamic();
            surface.AddComponent<MeshFilter>().sharedMesh = mesh;
            beam = surface.AddComponent<MeshRenderer>(); beam.sharedMaterial = lineMaterial;
            beam.shadowCastingMode = ShadowCastingMode.Off; beam.receiveShadows = false;
            vertices = new Vector3[1 + Around];
            var triangles = new List<int>();
            for (int a = 0; a < Around; a++) Triangle(triangles, 0, 1 + a, 1 + (a + 1) % Around);
            mesh.vertices = vertices; mesh.triangles = triangles.ToArray();
            rim = NewLine("Clipped cone boundary", .018f, true);
            for (int i = 0; i < spokes.Length; i++) spokes[i] = NewLine("Vision boundary ray", .009f, false);
            sightRay = NewLine("Actual sampled sight ray", .022f, false);
            for (int i = 0; i < 14; i++) wires.Add(NewLine("Exact query geometry", .012f, false));
            if (shaftMaterial) BuildShaft();
        }

        void BuildShaft()
        {
            var go = new GameObject("Searchlight shaft"); go.layer = gameObject.layer; go.transform.SetParent(transform, false);
            shaftMesh = new Mesh { name = "Clipped searchlight shaft" }; shaftMesh.MarkDynamic();
            int count = ShaftRays * (ShaftRings + 1);
            shaftVertices = new Vector3[count]; shaftNormals = new Vector3[count]; shaftUV = new Vector2[count];
            var triangles = new List<int>();
            for (int a = 0; a < ShaftRays; a++)
            {
                int b = (a + 1) % ShaftRays;
                for (int k = 0; k < ShaftRings; k++)
                {
                    int a0 = a * (ShaftRings + 1) + k, b0 = b * (ShaftRings + 1) + k;
                    Triangle(triangles, a0, b0, b0 + 1); Triangle(triangles, a0, b0 + 1, a0 + 1);
                }
            }
            shaftMesh.vertices = shaftVertices; shaftMesh.normals = shaftNormals; shaftMesh.uv = shaftUV;
            shaftMesh.triangles = triangles.ToArray();
            go.AddComponent<MeshFilter>().sharedMesh = shaftMesh;
            shaftRenderer = go.AddComponent<MeshRenderer>(); shaftRenderer.sharedMaterial = shaftMaterial;
            shaftRenderer.shadowCastingMode = ShadowCastingMode.Off; shaftRenderer.receiveShadows = false;
            shaftRenderer.lightProbeUsage = LightProbeUsage.Off; shaftRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            // Always-on outline of the lit floor pool, which is exactly the area this lens covers.
            var outline = new GameObject("Floor coverage outline"); outline.layer = gameObject.layer; outline.transform.SetParent(transform, false);
            outlineMesh = new Mesh { name = "Floor coverage outline" }; outlineMesh.MarkDynamic();
            outlineVertices = new Vector3[ShaftRays * 4];
            edgePoints = new Vector3[ShaftRays]; edgeNormals = new Vector3[ShaftRays]; edgeOnFloor = new bool[ShaftRays];
            var quads = new List<int>();
            for (int i = 0; i < ShaftRays; i++) { int v = i * 4; Triangle(quads, v, v + 1, v + 2); Triangle(quads, v, v + 2, v + 3); }
            outlineMesh.vertices = outlineVertices; outlineMesh.triangles = quads.ToArray();
            outline.AddComponent<MeshFilter>().sharedMesh = outlineMesh;
            outlineRenderer = outline.AddComponent<MeshRenderer>(); outlineRenderer.sharedMaterial = lineMaterial;
            outlineRenderer.shadowCastingMode = ShadowCastingMode.Off; outlineRenderer.receiveShadows = false;
        }

        /// <summary>Rebuilds the visible light cone with every radial ray stopped by the detector's own obstruction mask.</summary>
        void UpdateShaft(Color color)
        {
            if (!shaftRenderer) return;
            bool visible = lamp && lamp.enabled;
            shaftRenderer.enabled = visible;
            if (outlineRenderer) outlineRenderer.enabled = visible;
            if (!visible) return;
            Vector3 origin = detector.SightOrigin, forward = detector.SightForward;
            Quaternion rotation = Quaternion.LookRotation(forward, detector.rayOrigin ? detector.rayOrigin.up : Vector3.up);
            float theta = detector.VisionAngle * .5f * Mathf.Deg2Rad, range = Mathf.Max(.01f, detector.VisionRange);
            for (int a = 0; a < ShaftRays; a++)
            {
                float phi = a * Mathf.PI * 2 / ShaftRays;
                Vector3 radial = rotation * new Vector3(Mathf.Cos(phi), Mathf.Sin(phi), 0);
                Vector3 direction = forward * Mathf.Cos(theta) + radial * Mathf.Sin(theta);
                Vector3 normal = transform.InverseTransformDirection(radial * Mathf.Cos(theta) - forward * Mathf.Sin(theta));
                float distance = range;
                edgeOnFloor[a] = false;
                if (Physics.Raycast(origin, direction, out RaycastHit hit, range, detector.obstructionMask, QueryTriggerInteraction.Ignore))
                {
                    distance = hit.distance;
                    edgePoints[a] = hit.point + hit.normal * .015f; edgeNormals[a] = hit.normal;
                    edgeOnFloor[a] = hit.normal.y > .6f && hit.point.y < .3f;
                }
                for (int k = 0; k <= ShaftRings; k++)
                {
                    float t = (float)k / ShaftRings;
                    int i = a * (ShaftRings + 1) + k;
                    shaftVertices[i] = transform.InverseTransformPoint(origin + direction * (distance * t));
                    shaftNormals[i] = normal;
                    shaftUV[i] = new Vector2((float)a / ShaftRays, distance * t / range);
                }
            }
            shaftMesh.vertices = shaftVertices; shaftMesh.normals = shaftNormals; shaftMesh.uv = shaftUV;
            shaftMesh.RecalculateBounds();
            block.Clear(); block.SetColor("_Color", color); shaftRenderer.SetPropertyBlock(block);
            UpdateOutline();
            Tint(outlineRenderer, Color.Lerp(new Color(.85f, .93f, 1f, .5f), new Color(1f, .25f, .2f, .7f), alarm));
        }

        static void Triangle(List<int> triangles, int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
        LineRenderer NewLine(string name, float width, bool loop)
        {
            var go = new GameObject(name); go.layer = gameObject.layer; go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true; line.loop = loop; line.startWidth = line.endWidth = width;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            line.numCornerVertices = 2; line.numCapVertices = 2; return line;
        }
        void Tint(Renderer renderer, Color color)
        {
            block.Clear(); block.SetColor("_BaseColor", color); block.SetColor("_Color", color); renderer.SetPropertyBlock(block);
        }
        void LateUpdate()
        {
            if (!detector || block == null) return;
            Color color = new Color(.83f, .92f, 1f);
            bool diagnostic = game && game.Diagnostics;
            bool cone = diagnostic && detector.useViewCone && detector.VisionAngle > 0 && detector.VisionRange > 0;
            beam.enabled = rim.enabled = cone;
            foreach (var spoke in spokes) spoke.enabled = cone;
            if (cone) UpdateCone(color);
            // The recording diode stays lit and pulses brighter while it has the player in sight.
            float pulse = detector.IsDetecting ? 7f + Mathf.Sin(Time.time * 18f) * 3f : 4.2f + Mathf.Sin(Time.time * 2.2f) * .8f;
            Color recording = new Color(1f, .035f, .03f) * pulse; recording.a = 1;
            if (statusLights != null) foreach (var status in statusLights) if (status) Tint(status, recording);
            // A seen player turns the searchlight from white towards an alarm red.
            alarm = Mathf.MoveTowards(alarm, detector.IsDetecting ? 1 : 0, Time.deltaTime * (detector.IsDetecting ? 4f : 1.5f));
            Color searchlight = Color.Lerp(new Color(.86f, .93f, 1f), new Color(1f, .42f, .36f), alarm * .75f);
            if (lamp)
            {
                lamp.color = searchlight; lamp.range = detector.VisionRange; lamp.spotAngle = detector.VisionAngle;
                lamp.innerSpotAngle = detector.VisionAngle * InnerAngle;
                lamp.transform.SetPositionAndRotation(detector.SightOrigin, Quaternion.LookRotation(detector.SightForward));
                lamp.enabled = detector.useViewCone && detector.VisionRange > 0 && detector.VisionAngle > 0;
            }
            UpdateShaft(Color.Lerp(new Color(.82f, .9f, 1f), new Color(1f, .3f, .25f), alarm * .85f));
            foreach (var wire in wires) wire.enabled = false;
            if (diagnostic) DrawQuery(color);
            sightRay.enabled = diagnostic && detector.HasSightSample;
            if (sightRay.enabled)
            {
                sightRay.positionCount = 2; sightRay.SetPosition(0, detector.LastRayOrigin); sightRay.SetPosition(1, detector.LastRayTarget);
                Tint(sightRay, detector.LastRayBlocked ? new Color(.15f, .85f, .65f, .9f) : new Color(1, .18f, .08f, .9f));
            }
        }

        void UpdateCone(Color color)
        {
            Vector3 origin = detector.SightOrigin;
            Quaternion rotation = Quaternion.LookRotation(detector.SightForward, detector.rayOrigin ? detector.rayOrigin.up : Vector3.up);
            vertices[0] = transform.InverseTransformPoint(origin);
            float theta = detector.VisionAngle * .5f * Mathf.Deg2Rad;
            for (int a = 0; a < Around; a++)
            {
                float phi = a * Mathf.PI * 2 / Around;
                Vector3 direction = rotation * new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Sin(theta) * Mathf.Sin(phi), Mathf.Cos(theta));
                vertices[1 + a] = transform.InverseTransformPoint(Clip(origin, direction));
            }
            mesh.vertices = vertices; mesh.RecalculateBounds();
            color.a = detector.IsDetecting ? .025f : .012f; Tint(beam, color);
            int edge = 1;
            rim.positionCount = Around;
            for (int a = 0; a < Around; a++) rim.SetPosition(a, transform.TransformPoint(vertices[edge + a]));
            color.a = .22f; Tint(rim, color);
            for (int i = 0; i < spokes.Length; i++)
            {
                spokes[i].positionCount = 2; spokes[i].SetPosition(0, origin);
                spokes[i].SetPosition(1, transform.TransformPoint(vertices[edge + i * Around / spokes.Length]));
                color.a = i % 2 == 0 ? .12f : .05f; Tint(spokes[i], color);
            }
        }
        void UpdateOutline()
        {
            const float halfWidth = .016f;
            for (int a = 0; a < ShaftRays; a++)
            {
                int b = (a + 1) % ShaftRays, v = a * 4;
                Vector3 p0 = edgePoints[a], p1 = edgePoints[b];
                bool segment = edgeOnFloor[a] && edgeOnFloor[b] && (p1 - p0).sqrMagnitude < 1.2f;
                if (!segment) { for (int k = 0; k < 4; k++) outlineVertices[v + k] = Vector3.zero; continue; }
                Vector3 side = Vector3.Cross((p1 - p0).normalized, Vector3.up) * halfWidth;
                outlineVertices[v] = transform.InverseTransformPoint(p0 - side);
                outlineVertices[v + 1] = transform.InverseTransformPoint(p0 + side);
                outlineVertices[v + 2] = transform.InverseTransformPoint(p1 + side);
                outlineVertices[v + 3] = transform.InverseTransformPoint(p1 - side);
            }
            outlineMesh.vertices = outlineVertices;
            outlineMesh.RecalculateBounds();
        }

        Vector3 Clip(Vector3 origin, Vector3 direction)
        {
            float distance = detector.VisionRange;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, detector.obstructionMask, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(0, hit.distance - .018f);
            return origin + direction * distance;
        }

        void DrawQuery(Color color)
        {
            wireIndex = 0; color.a = .32f;
            if (detector.shape == DetectionShape.Sphere)
            {
                Circle(detector.QueryCenter, Vector3.right, Vector3.up, detector.QueryRadius, color);
                Circle(detector.QueryCenter, Vector3.right, Vector3.forward, detector.QueryRadius, color);
                Circle(detector.QueryCenter, Vector3.forward, Vector3.up, detector.QueryRadius, color);
            }
            else if (detector.shape == DetectionShape.Box)
            {
                Vector3 e = detector.QueryHalfExtents;
                for (int axis = 0; axis < 3; axis++)
                    for (int a = -1; a <= 1; a += 2)
                        for (int b = -1; b <= 1; b += 2)
                        {
                            Vector3 first = Vector3.zero, last = Vector3.zero;
                            first[axis] = -e[axis]; last[axis] = e[axis];
                            first[(axis + 1) % 3] = last[(axis + 1) % 3] = a * e[(axis + 1) % 3];
                            first[(axis + 2) % 3] = last[(axis + 2) % 3] = b * e[(axis + 2) % 3];
                            Wire(new[] { detector.QueryCenter + detector.QueryRotation * first, detector.QueryCenter + detector.QueryRotation * last }, false, color);
                        }
            }
            else
            {
                Vector3 a = detector.QueryPointA, b = detector.QueryPointB;
                Vector3 axis = (b - a).normalized, u = Vector3.Cross(axis, Vector3.up).normalized;
                if (u.sqrMagnitude < .01f) u = Vector3.right;
                Vector3 v = Vector3.Cross(axis, u).normalized;
                Circle(a, u, v, detector.QueryRadius, color); Circle(b, u, v, detector.QueryRadius, color);
                for (int j = 0; j < 4; j++)
                {
                    Vector3 radial = (j % 2 == 0 ? u : v) * (j < 2 ? 1 : -1) * detector.QueryRadius;
                    Wire(new[] { a + radial, b + radial }, false, color);
                }
                Arc(a, -axis, u, color); Arc(a, -axis, v, color); Arc(b, axis, u, color); Arc(b, axis, v, color);
            }
        }
        void Circle(Vector3 center, Vector3 u, Vector3 v, float radius, Color color)
        {
            var points = new Vector3[64];
            for (int i = 0; i < points.Length; i++) { float a = i * Mathf.PI * 2 / points.Length; points[i] = center + (Mathf.Cos(a) * u + Mathf.Sin(a) * v) * radius; }
            Wire(points, true, color);
        }
        void Arc(Vector3 center, Vector3 outward, Vector3 radial, Color color)
        {
            var points = new Vector3[25];
            for (int i = 0; i < points.Length; i++) { float a = i * Mathf.PI / (points.Length - 1); points[i] = center + (Mathf.Cos(a) * radial + Mathf.Sin(a) * outward) * detector.QueryRadius; }
            Wire(points, false, color);
        }
        void Wire(Vector3[] points, bool loop, Color color)
        {
            var wire = wires[wireIndex++]; wire.enabled = true; wire.loop = loop; wire.positionCount = points.Length; wire.SetPositions(points); Tint(wire, color);
        }
        void OnDestroy() { if (mesh) Destroy(mesh); if (shaftMesh) Destroy(shaftMesh); if (outlineMesh) Destroy(outlineMesh); }
    }
}
