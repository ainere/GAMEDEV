using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
namespace LaserCorridor.Tests
{
    public class FloorDisguiseTests
    {
        const string ScenePath="Assets/LaserCorridor/Scenes/LaserCorridor.unity";
        [Test] public void EligibleGridHas60ReachableTiles()
        {
            var s=ScriptableObject.CreateInstance<RunSettings>();var grid=TileGrid.EligibleCenters(s);Assert.That(grid.Count,Is.EqualTo(60));
            foreach(var p in grid){Assert.That(Mathf.Abs(p.x),Is.LessThanOrEqualTo(1.601f));Assert.That(p.y,Is.InRange(2f,10.801f));}
            Object.DestroyImmediate(s);
        }
        [Test] public void All85FloorSocketsKeepTheirPlateAndUseOneSharedExactBorderMesh()
        {
            EditorSceneManager.OpenScene(ScenePath);var floor=GameObject.Find("Floor");Assert.That(floor,Is.Not.Null);
            Assert.That(floor.GetComponentsInChildren<TextMesh>(true),Is.Empty);Assert.That(floor.GetComponentsInChildren<ParticleSystem>(true),Is.Empty);
            var sockets=floor.GetComponentsInChildren<FloorSocket>(true);Assert.That(sockets.Length,Is.EqualTo(85));
            var plateMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/LaserCorridor/Art/Models/Floor/Plate.asset");
            var plateMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/LaserCorridor/Materials/Steel floor plate.mat");
            var sideStrip=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/LaserCorridor/Art/Models/Floor/Bar 01.asset");
            var endStrip=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/LaserCorridor/Art/Models/Floor/Bar 02.asset");
            var seamMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/LaserCorridor/Materials/Tile seam light.mat");
            Assert.That(plateMesh,Is.Not.Null);Assert.That(plateMaterial,Is.Not.Null);Assert.That(sideStrip,Is.Not.Null);Assert.That(endStrip,Is.Not.Null);Assert.That(seamMaterial,Is.Not.Null);
            Mesh sharedBorder=null;int eligible=0;var distinctTiles=new HashSet<GameObject>();var distinctRenderers=new HashSet<Renderer>();
            foreach(var tile in sockets)
            {
                var surface=tile.plate.GetComponent<Renderer>();var shape=tile.plate.GetComponent<MeshFilter>();
                Assert.That(shape.sharedMesh,Is.SameAs(plateMesh),tile.name+" keeps the authored plate mesh");
                Assert.That(surface.sharedMaterial,Is.SameAs(plateMaterial),tile.name+" keeps the authored plate material");
                Assert.That(tile.plate.localPosition,Is.EqualTo(new Vector3(0,-.006f,0)));
                Assert.That(tile.tileLight,Is.Not.Null);Assert.That(tile.tileLight.enabled,Is.False);
                Assert.That(tile.seams,Has.Length.EqualTo(1),tile.name+" has one combined border renderer");
                var seam=tile.seams[0];Assert.That(seam,Is.Not.Null);Assert.That(seam.sharedMaterial,Is.SameAs(seamMaterial));
                Assert.That(seam.gameObject.activeSelf,Is.False);
                var borderFilter=seam.GetComponent<MeshFilter>();Assert.That(borderFilter,Is.Not.Null);
                if(sharedBorder==null)sharedBorder=borderFilter.sharedMesh;
                Assert.That(borderFilter.sharedMesh,Is.SameAs(sharedBorder),"every socket reuses the combined border mesh");
                Assert.That(distinctTiles.Add(tile.gameObject),Is.True,"each floor socket remains a separate tile");
                Assert.That(distinctRenderers.Add(seam),Is.True,"each tile retains its own independently controlled renderer");
                CollectionAssert.AreEquivalent(ExpectedBorderTriangles(sideStrip,endStrip),TriangleGeometry(borderFilter.sharedMesh,tile.transform.worldToLocalMatrix*borderFilter.transform.localToWorldMatrix),tile.name+" border preserves the original segments, gaps, and bevels");
                if(tile.eligible)eligible++;
            }
            Assert.That(eligible,Is.EqualTo(60));
        }
        [Test] public void HelpfulAndHarmfulActivationOnlyChangeLighting()
        {
            EditorSceneManager.OpenScene(ScenePath);var tile=Object.FindAnyObjectByType<FloorSocket>();var position=tile.plate.localPosition;
            tile.SetActive(PanelEffect.Healing,false,2);foreach(var seam in tile.seams)Assert.That(seam.gameObject.activeSelf,Is.True);
            var block=new MaterialPropertyBlock();tile.seams[0].GetPropertyBlock(block);Color steady=block.GetColor("_EmissionColor");tile.SetActive(PanelEffect.Healing,false,3);tile.seams[0].GetPropertyBlock(block);Assert.That(block.GetColor("_EmissionColor"),Is.EqualTo(steady));
            tile.SetActive(PanelEffect.Shock,true,2);tile.seams[0].GetPropertyBlock(block);Color flicker=block.GetColor("_EmissionColor");tile.SetActive(PanelEffect.Shock,true,3);tile.seams[0].GetPropertyBlock(block);Assert.That(block.GetColor("_EmissionColor"),Is.Not.EqualTo(flicker));
            foreach(var seam in tile.seams)Assert.That(seam.gameObject.activeSelf,Is.True);Assert.That(tile.plate.localPosition,Is.EqualTo(position));tile.SetNeutral();foreach(var seam in tile.seams)Assert.That(seam.gameObject.activeSelf,Is.False);Assert.That(tile.tileLight.enabled,Is.False);
        }
        [Test] public void DoorFramesKeepTheirCorrectMeshDimensionsAfterRenaming()
        {
            var horizontal=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/LaserCorridor/Art/Models/Exit Door/Bar 01.asset");
            var vertical=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/LaserCorridor/Art/Models/Exit Door/Bar 02.asset");
            Assert.That(horizontal,Is.Not.Null);Assert.That(vertical,Is.Not.Null);
            Assert.That(horizontal.bounds.size.x,Is.EqualTo(.88f).Within(.001f));Assert.That(horizontal.bounds.size.y,Is.EqualTo(.035f).Within(.001f));
            Assert.That(vertical.bounds.size.x,Is.EqualTo(.035f).Within(.001f));Assert.That(vertical.bounds.size.y,Is.EqualTo(.78f).Within(.001f));
        }
        [Test] public void SweptFootprintCollectsEdgesButNotDistantCorners()
        {
            var go=new GameObject("Contact fixture");var socket=go.AddComponent<FloorSocket>();
            Assert.That(socket.Touches(new Vector2(-2,0),new Vector2(2,0),.21f),Is.True,"whole plate crossing");
            Assert.That(socket.Touches(new Vector2(.55f,0),new Vector2(.55f,.1f),.21f),Is.True,"footprint edge overlap");
            Assert.That(socket.Touches(new Vector2(.56f,.56f),new Vector2(.56f,.56f),.21f),Is.False,"inflated square corner is outside the circular footprint");
            Object.DestroyImmediate(go);
        }

        static List<string> ExpectedBorderTriangles(Mesh sideStrip,Mesh endStrip)
        {
            var expected=new List<string>();
            for(int side=-1;side<=1;side+=2)for(int piece=0;piece<3;piece++)
            {
                AppendTriangleGeometry(expected,sideStrip,Matrix4x4.Translate(new Vector3(side*.391f,-.005f,(piece-1)*.257f)));
                AppendTriangleGeometry(expected,endStrip,Matrix4x4.Translate(new Vector3((piece-1)*.257f,-.005f,side*.391f)));
            }
            return expected;
        }

        static List<string> TriangleGeometry(Mesh mesh,Matrix4x4 localToExpectedSpace)
        {var result=new List<string>();AppendTriangleGeometry(result,mesh,localToExpectedSpace);return result;}

        static void AppendTriangleGeometry(List<string> result,Mesh mesh,Matrix4x4 localToExpectedSpace)
        {
            Vector3[] vertices=mesh.vertices;
            for(int submesh=0;submesh<mesh.subMeshCount;submesh++)
            {
                int[] indices=mesh.GetIndices(submesh);Assert.That(indices.Length%3,Is.Zero,"border assembly contains triangle meshes");
                for(int i=0;i<indices.Length;i+=3)
                {
                    var points=new[]{PointKey(localToExpectedSpace.MultiplyPoint3x4(vertices[indices[i]])),PointKey(localToExpectedSpace.MultiplyPoint3x4(vertices[indices[i+1]])),PointKey(localToExpectedSpace.MultiplyPoint3x4(vertices[indices[i+2]]))};
                    Array.Sort(points,StringComparer.Ordinal);result.Add(string.Join("|",points));
                }
            }
        }

        static string PointKey(Vector3 point)
        {return Mathf.RoundToInt(point.x*10000)+","+Mathf.RoundToInt(point.y*10000)+","+Mathf.RoundToInt(point.z*10000);}
    }
}
