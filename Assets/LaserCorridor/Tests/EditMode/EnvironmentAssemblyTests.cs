using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LaserCorridor.Tests
{
    public class EnvironmentAssemblyTests
    {
        const string ScenePath = "Assets/LaserCorridor/Scenes/LaserCorridor.unity";
        const string Models = "Assets/LaserCorridor/Art/Models/";
        const string Materials = "Assets/LaserCorridor/Materials/";

        [Test]
        public void CeilingBaysReuseExactSurfaceAndFrameAssemblies()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var environment = GameObject.Find("Environment").transform;
            var ceiling = environment.Find("Ceiling");
            Assert.That(ceiling, Is.Not.Null);
            Assert.That(ceiling.childCount, Is.EqualTo(26));

            var crossbar = LoadMesh(Models + "Ceiling/Bar 01.asset");
            var rail = LoadMesh(Models + "Ceiling/Bar 02.asset");
            var luminous = LoadMaterial(Materials + "Ceiling luminous panel.mat");
            var cover = LoadMaterial(Materials + "Ceiling frosted luminous glass.mat");
            var aluminium = LoadMaterial(Materials + "Brushed aluminium.mat");
            Assert.That(luminous.globalIlluminationFlags, Is.EqualTo(MaterialGlobalIlluminationFlags.BakedEmissive));
            var quad = CreateUnitQuadMesh();
            Mesh sharedFrame = null, sharedSurface = null;

            for (int bayIndex = 0; bayIndex < 26; bayIndex++)
            {
                var bay = ceiling.GetChild(bayIndex);
                Assert.That(bay.name, Is.EqualTo("Ceiling panel " + (bayIndex + 1).ToString("00")));
                Assert.That(bay.childCount, Is.EqualTo(2), bay.name + " keeps only its two assembly modules");
                Assert.That(bay.GetComponentsInChildren<MeshRenderer>(true), Has.Length.EqualTo(2));

                var frame = FindAssembly(bay, "Frame");
                var surface = FindAssembly(bay, "Panel");
                var frameFilter = frame.GetComponent<MeshFilter>();
                var surfaceFilter = surface.GetComponent<MeshFilter>();
                var frameRenderer = frame.GetComponent<MeshRenderer>();
                var surfaceRenderer = surface.GetComponent<MeshRenderer>();
                Assert.That(frameFilter.sharedMesh.subMeshCount, Is.EqualTo(1));
                Assert.That(frameRenderer.sharedMaterials, Is.EqualTo(new[] { aluminium }));
                Assert.That(surfaceFilter.sharedMesh.subMeshCount, Is.EqualTo(2));
                Assert.That(surfaceRenderer.sharedMaterials, Is.EqualTo(new[] { luminous, cover }));
                Assert.That(GameObjectUtility.GetStaticEditorFlags(frame), Is.EqualTo(StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic));
                Assert.That(GameObjectUtility.GetStaticEditorFlags(surface), Is.EqualTo((StaticEditorFlags)0));

                if (sharedFrame == null) sharedFrame = frameFilter.sharedMesh;
                if (sharedSurface == null) sharedSurface = surfaceFilter.sharedMesh;
                Assert.That(frameFilter.sharedMesh, Is.SameAs(sharedFrame), "ceiling bays reuse one structural mesh");
                Assert.That(surfaceFilter.sharedMesh, Is.SameAs(sharedSurface), "ceiling bays reuse one two-material surface mesh");

                float z = 1f + bayIndex * .5f;
                var expectedFrame = ExpectedFrameTriangles(crossbar, rail, z);
                var actualFrame = TriangleGeometry(frameFilter.sharedMesh, environment.worldToLocalMatrix * frameFilter.transform.localToWorldMatrix);
                CollectionAssert.AreEquivalent(expectedFrame, actualFrame, bay.name + " frame geometry and placement");

                var expectedSurface = ExpectedSurfaceTriangles(quad, z);
                var actualSurface = TriangleGeometry(surfaceFilter.sharedMesh, environment.worldToLocalMatrix * surfaceFilter.transform.localToWorldMatrix);
                CollectionAssert.AreEquivalent(expectedSurface, actualSurface, bay.name + " luminous and frosted surface geometry and placement");
            }

            Assert.That(sharedFrame, Is.Not.Null);
            Assert.That(sharedSurface, Is.Not.Null);
        }

        [Test]
        public void ExitDoorModulesKeepTheirTrimGeometryAndKeypadPlacement()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var environment = GameObject.Find("Environment").transform;
            var exit = environment.Find("Exit Door");
            Assert.That(exit, Is.Not.Null);

            var leaf = FindAssembly(exit, "Door");
            var surround = FindAssembly(exit, "Frame");
            var keypad = FindAssembly(exit, "Keypad");
            var vaultSteel = LoadMaterial(Materials + "Vault brushed steel.mat");
            var graphite = LoadMaterial(Materials + "Gunmetal structure.mat");
            var aluminium = LoadMaterial(Materials + "Brushed aluminium.mat");
            CollectionAssert.Contains(leaf.GetComponent<MeshRenderer>().sharedMaterials, vaultSteel);
            CollectionAssert.Contains(surround.GetComponent<MeshRenderer>().sharedMaterials, graphite);
            CollectionAssert.Contains(surround.GetComponent<MeshRenderer>().sharedMaterials, aluminium);
            CollectionAssert.Contains(keypad.GetComponent<MeshRenderer>().sharedMaterials, graphite);
            CollectionAssert.Contains(keypad.GetComponent<MeshRenderer>().sharedMaterials, aluminium);

            var horizontal = LoadMesh(Models + "Exit Door/Bar 01.asset");
            var vertical = LoadMesh(Models + "Exit Door/Bar 02.asset");
            Assert.That(horizontal.bounds.size.x, Is.EqualTo(.88f).Within(.001f));
            Assert.That(horizontal.bounds.size.y, Is.EqualTo(.035f).Within(.001f));
            Assert.That(vertical.bounds.size.x, Is.EqualTo(.035f).Within(.001f));
            Assert.That(vertical.bounds.size.y, Is.EqualTo(.78f).Within(.001f));

            var expectedTrim = new List<string>();
            int horizontalPieces = 0, verticalPieces = 0;
            for (int column = 0; column < 2; column++) for (int row = 0; row < 4; row++)
            {
                Vector3 center = new Vector3((column - .5f) * .92f, .50f + row * .85f, 13.74f);
                foreach (int sign in new[] { -1, 1 })
                {
                    AddMeshGeometry(expectedTrim, vertical, Matrix4x4.Translate(center + new Vector3(sign * .425f, 0, 0)));
                    AddMeshGeometry(expectedTrim, horizontal, Matrix4x4.Translate(center + new Vector3(0, sign * .375f, 0)));
                    verticalPieces++; horizontalPieces++;
                }
            }
            Assert.That(horizontalPieces, Is.EqualTo(16));
            Assert.That(verticalPieces, Is.EqualTo(16));
            var leafFilter = leaf.GetComponent<MeshFilter>();
            CollectionAssert.IsSubsetOf(expectedTrim, TriangleGeometry(leafFilter.sharedMesh, environment.worldToLocalMatrix * leafFilter.transform.localToWorldMatrix),
                "the combined door leaf retains all 32 original bevelled trim pieces at their authored positions");

            var casing = LoadMesh(Models + "Exit Door/Keypad case.asset");
            var key = LoadMesh(Models + "Exit Door/Key.asset");
            var expectedKeypad = new List<string>();
            AddMeshGeometry(expectedKeypad, casing, Matrix4x4.Translate(new Vector3(1.58f, 1.4f, 13.77f)));
            for (int row = 0; row < 4; row++) for (int column = 0; column < 3; column++)
                AddMeshGeometry(expectedKeypad, key, Matrix4x4.Translate(new Vector3(1.5f + column * .07f, 1.29f + row * .07f, 13.69f)));
            var keypadFilter = keypad.GetComponent<MeshFilter>();
            CollectionAssert.AreEquivalent(expectedKeypad, TriangleGeometry(keypadFilter.sharedMesh, environment.worldToLocalMatrix * keypadFilter.transform.localToWorldMatrix),
                "the keypad assembly retains its casing and twelve keys at their authored positions");

            Assert.That(UnityEngine.Object.FindObjectsByType<ExitDoorStatus>(FindObjectsInactive.Include), Is.Empty,
                "the approved keypad light and its status component are removed, while the keypad remains");

            var expectedLeaf = new List<string>(expectedTrim);
            AddMeshGeometry(expectedLeaf, LoadMesh(Models + "Structure/Door.asset"), Matrix4x4.Translate(new Vector3(0,1.8f,13.84f)));
            for (int column = 0; column < 2; column++) for (int row = 0; row < 4; row++)
            {
                Vector3 center = new Vector3((column-.5f)*.92f,.50f+row*.85f,13.74f);
                AddMeshGeometry(expectedLeaf, LoadMesh(Models + "Exit Door/Inset.asset"), Matrix4x4.Translate(center+Vector3.forward*.015f));
                foreach(int sign in new[]{-1,1}) AddMeshGeometry(expectedLeaf, LoadMesh(Models + "Exit Door/Brace.asset"), Matrix4x4.TRS(center,Quaternion.Euler(0,0,sign*41),Vector3.one));
            }
            for(int hinge=0;hinge<3;hinge++) AddMeshGeometry(expectedLeaf,LoadMesh(Models + "Exit Door/Hinge.asset"),Matrix4x4.Translate(new Vector3(1.08f,.6f+hinge*1.15f,13.64f)));
            AddMeshGeometry(expectedLeaf,LoadMesh(Models + "Exit Door/Bar 04.asset"),Matrix4x4.Translate(new Vector3(1.055f,1.8f,13.64f)));
            CollectionAssert.AreEquivalent(expectedLeaf,TriangleGeometry(leafFilter.sharedMesh,environment.worldToLocalMatrix*leafFilter.transform.localToWorldMatrix),
                "the cleaned door keeps every authored panel, brace, trim, hinge and rod; only the handle mount is removed");

            var expectedSurround = new List<string>();
            AddMeshGeometry(expectedSurround,LoadMesh(Models + "Exit Door/Panel.asset"),Matrix4x4.Translate(new Vector3(0,1.83f,13.93f)));
            foreach(int side in new[]{-1,1})AddMeshGeometry(expectedSurround,LoadMesh(Models + "Structure/Frame.asset"),Matrix4x4.Translate(new Vector3(side*1.18f,1.83f,13.75f)));
            AddMeshGeometry(expectedSurround,LoadMesh(Models + "Exit Door/Bar 03.asset"),Matrix4x4.Translate(new Vector3(0,3.66f,13.75f)));
            var surroundFilter=surround.GetComponent<MeshFilter>();
            CollectionAssert.AreEquivalent(expectedSurround,TriangleGeometry(surroundFilter.sharedMesh,environment.worldToLocalMatrix*surroundFilter.transform.localToWorldMatrix),
                "the cleaned surround keeps its recess and all structural bars; only the EXIT backing is removed");
        }

        [Test]
        public void EntranceIsEnclosedWithoutAddingGameplayOrMovingTheCamera()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var environment=GameObject.Find("Environment").transform;
            var entrance=environment.Find("Entrance");Assert.That(entrance,Is.Not.Null);
            Assert.That(entrance.Find("Floor"),Is.Null,"the entrance filler must not shadow the gameplay Floor lookup");
            Assert.That(entrance.Find("Underlay"),Is.Not.Null);
            Assert.That(entrance.GetComponentsInChildren<Collider>(true),Is.Empty);
            Assert.That(entrance.GetComponentsInChildren<Light>(true),Is.Empty);
            Assert.That(entrance.GetComponentsInChildren<FloorSocket>(true),Is.Empty);
            Assert.That(entrance.Find("Wall"),Is.Not.Null);
            foreach(string side in new[]{"Left wall","Right wall"})
            {
                var wall=entrance.Find(side);Assert.That(wall,Is.Not.Null);
                Assert.That(wall.Find("Wall panel 01/Light"),Is.Not.Null);Assert.That(wall.Find("Wall panel 02/Glass"),Is.Not.Null);
                var backing=wall.Find("Panel").GetComponent<MeshRenderer>().bounds;
                Assert.That(backing.min.z,Is.LessThanOrEqualTo(-.79f));Assert.That(backing.max.z,Is.GreaterThanOrEqualTo(-.01f));
            }
            var ceiling=entrance.Find("Ceiling/Panel").GetComponent<MeshRenderer>().bounds;
            Assert.That(ceiling.min.z,Is.LessThanOrEqualTo(-.79f));Assert.That(ceiling.max.z,Is.GreaterThanOrEqualTo(.99f));
            var camera=UnityEngine.Object.FindAnyObjectByType<CorridorCamera>();
            Assert.That(camera.transform.position,Is.EqualTo(new Vector3(0,3.8f,-.45f)));
            Assert.That(environment.Find("Fixtures/Danger stripe"),Is.Not.Null);
            Assert.That(UnityEngine.Object.FindAnyObjectByType<RunController>().exitGrid,Is.Not.Null);
            string[] removed={"Door pull handle","Door handle bracket","Door lock status","EXIT stencil","Warning sign housing","Wall warning stencil","Floor lethal stencil","Camera housing","Grating","Recessed side grating"};
            foreach(var item in GameObject.Find("Laser Corridor").GetComponentsInChildren<Transform>(true))
                CollectionAssert.DoesNotContain(removed,item.name,"approved decorations are absent from the saved scene");
            foreach(string fixtureName in new[]{"Laser Emitter","Exit Projector"})
            {
                var fixture=environment.Find("Fixtures/"+fixtureName);Assert.That(fixture,Is.Not.Null);
                foreach(string bar in new[]{"Bar 01","Bar 02","Bar 03","Bar 04"})Assert.That(fixture.Find(bar).GetComponent<MeshRenderer>(),Is.Not.Null,"visible emitter mounting bars remain");
                for(int lens=1;lens<=30;lens++)Assert.That(fixture.Find("Lens "+lens.ToString("00")).GetComponent<MeshRenderer>(),Is.Not.Null,"cyan lenses remain attached to their frames");
            }
            Assert.That(AssetDatabase.LoadMainAssetAtPath("Assets/LaserCorridor/Materials/Gutter grating.mat"),Is.Null);
            Assert.That(AssetDatabase.LoadMainAssetAtPath("Assets/LaserCorridor/Art/Generated/FloorGrating.png"),Is.Null);
        }

        static GameObject FindAssembly(Transform parent, string name)
        {
            var assembly = parent.Find(name);
            Assert.That(assembly, Is.Not.Null, parent.name + " retains " + name);
            Assert.That(assembly.GetComponent<MeshFilter>(), Is.Not.Null);
            Assert.That(assembly.GetComponent<MeshRenderer>(), Is.Not.Null);
            return assembly.gameObject;
        }

        static Mesh LoadMesh(string path)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            Assert.That(mesh, Is.Not.Null, "Missing source mesh " + path);
            return mesh;
        }

        static Material LoadMaterial(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.That(material, Is.Not.Null, "Missing material " + path);
            return material;
        }

        static Mesh CreateUnitQuadMesh()
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            var mesh = quad.GetComponent<MeshFilter>().sharedMesh;
            UnityEngine.Object.DestroyImmediate(quad);
            return mesh;
        }

        static List<string> ExpectedFrameTriangles(Mesh crossbar, Mesh rail, float z)
        {
            var expected = new List<string>();
            AddMeshGeometry(expected, crossbar, Matrix4x4.Translate(new Vector3(0, 4.24f, z)));
            for (int column = 0; column < 4; column++)
            {
                float x = -1.5f + column;
                AddMeshGeometry(expected, rail, Matrix4x4.Translate(new Vector3(x - .5f, 4.24f, z + .25f)));
            }
            return expected;
        }

        static List<string> ExpectedSurfaceTriangles(Mesh quad, float z)
        {
            var expected = new List<string>();
            for (int column = 0; column < 4; column++)
            {
                float x = -1.5f + column;
                AddMeshGeometry(expected, quad, Matrix4x4.TRS(new Vector3(x, 4.285f, z + .25f), Quaternion.Euler(-90, 0, 0), new Vector3(.96f, .465f, 1)));
                AddMeshGeometry(expected, quad, Matrix4x4.TRS(new Vector3(x, 4.255f, z + .25f), Quaternion.Euler(-90, 0, 0), new Vector3(.96f, .465f, 1)));
            }
            return expected;
        }

        static List<string> TriangleGeometry(Mesh mesh, Matrix4x4 localToExpectedSpace)
        {
            var result = new List<string>();
            AddMeshGeometry(result, mesh, localToExpectedSpace);
            return result;
        }

        static void AddMeshGeometry(List<string> result, Mesh mesh, Matrix4x4 localToExpectedSpace)
        {
            var vertices = mesh.vertices;
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                var indices = mesh.GetIndices(submesh);
                Assert.That(indices.Length % 3, Is.Zero, "assembly geometry must contain triangles");
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var points = new[]
                    {
                        PointKey(localToExpectedSpace.MultiplyPoint3x4(vertices[indices[i]])),
                        PointKey(localToExpectedSpace.MultiplyPoint3x4(vertices[indices[i + 1]])),
                        PointKey(localToExpectedSpace.MultiplyPoint3x4(vertices[indices[i + 2]]))
                    };
                    Array.Sort(points, StringComparer.Ordinal);
                    result.Add(string.Join("|", points));
                }
            }
        }

        static string PointKey(Vector3 point)
        {
            return Mathf.RoundToInt(point.x * 10000) + "," + Mathf.RoundToInt(point.y * 10000) + "," + Mathf.RoundToInt(point.z * 10000);
        }
    }
}
