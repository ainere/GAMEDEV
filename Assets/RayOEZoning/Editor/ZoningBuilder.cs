using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StarterAssets;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RayOEZoning.Editor
{
    /// <summary>Authors the standalone level with Editor APIs, locally generated textures and primitive models.</summary>
    public static partial class ZoningBuilder
    {
        public const string ScenePath = "Assets/RayOEZoning/Scenes/RayOEZoning.unity";
        const int World = 8, PlayerLayer = 9, ClueLayer = 10, ScannerLayer = 11, GlassLayer = 12;
        const int Obstructions = (1 << World) | 1;
        const float Ceiling = 3.4f, DoorHeight = 2.45f;
        const float VaultStart = 59.5f, VaultEnd = 64.5f;
        static TMP_FontAsset font;

        [MenuItem("Ray-OE Zoning/Create level")]
        public static void Create()
        {
            Directory.CreateDirectory("Assets/RayOEZoning/Scenes");
            AssetDatabase.Refresh(); SetLayers(); DisableProjectAudio(); ConfigurePipeline();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            dressing = new System.Random(7);
            spots.Clear();
            CreateMaterials();
            extinguisherRed = Lit("Extinguisher red", new Color(.62f, .05f, .04f), .55f);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Level");
            var lights = new List<Light>();

            var entrance = Child(root.transform, "Entrance");
            var lab = Child(root.transform, "Lab");
            var cold = Child(root.transform, "Cold Room");
            var servers = Child(root.transform, "Server Room");
            Room(entrance, 0, 11, -4, 4, accentTeal);
            Room(lab, 11, 29, -4.5f, 6, accentTeal);
            Room(cold, 29, 45, -5, 3.5f, accentIce);
            Room(servers, 45, VaultStart, -4.5f, 4.5f, accentAmber);
            EndWall(entrance, 0, -4, 4);
            Doorway(root.transform, 11, -4.5f, 6, -2.9f, -.7f);
            Doorway(root.transform, 29, -5, 6, .6f, 2.8f);
            Doorway(root.transform, 45, -5, 4.5f, -3.6f, -1.4f);

            StaffEntry(entrance, lights);
            AnalyticalLab(lab, lights);
            ColdStorage(cold, lights);
            SecureResearch(servers, lights);

            Transform door, keypad; ArchiveDoor(root.transform, out door, out keypad);
            ArchiveVault(root.transform, lights);

            var session = Child(root.transform, "Game").gameObject;
            var game = session.AddComponent<StealthGame>();
            game.exposureSeconds = 1.35f; game.decaySeconds = 1.5f; game.escapeZ = VaultStart + 1.1f;
            var spawn = Child(root.transform, "Spawn"); spawn.position = new Vector3(1.0f, .06f, 1.0f); game.spawn = spawn;
            CreatePlayer(root.transform, game, spawn.position); game.keypad = keypad;

            var surveillance = Child(root.transform, "Cameras");
            game.hazards = new[] {
                // Wide entry camera: sweeps the open floor between the supply divider and the lab door.
                Scanner(surveillance, game, DetectionShape.Sphere, new Vector3(3.62f, 2.85f, 10.62f), new Vector3(-1.0f, 0, 8.0f), 64, 8.5f, 30, 7.2f, 0f, Vector3.forward, .23f),
                // Narrow long-range camera across the long islands; their spines hide a crouched player.
                Scanner(surveillance, game, DetectionShape.Box, new Vector3(5.62f, 2.9f, 17.6f), new Vector3(-3.2f, 0, 17.0f), 38, 11, 26, 8f, 1.3f, Vector3.right, .38f),
                // Ceiling camera above the cold-store door looking back down the island lanes.
                Scanner(surveillance, game, DetectionShape.Capsule, new Vector3(.6f, 2.9f, 28.2f), new Vector3(-.6f, 0, 20.5f), 50, 10, 30, 8.5f, 2.8f, Vector3.up, Ceiling - 2.9f),
                // Wide short camera on the long way round; a freezer island shades the crawlspace.
                Scanner(surveillance, game, DetectionShape.Sphere, new Vector3(3.2f, 2.8f, 33.7f), new Vector3(.9f, 0, 39.6f), 68, 8, 22, 7.8f, .8f, Vector3.right, .3f),
                // Long narrow camera from the archive wall down the left server lane.
                Scanner(surveillance, game, DetectionShape.Box, new Vector3(-4.12f, 2.95f, 59.15f), new Vector3(-2.6f, 0, 49.5f), 34, 11.5f, 24, 8.8f, 2.2f, Vector3.forward, .2f),
                // Ceiling camera guarding the terminal and the archive door.
                Scanner(surveillance, game, DetectionShape.Capsule, new Vector3(2.0f, 2.9f, 53.6f), new Vector3(-2.5f, 0, 58.1f), 46, 9.5f, 25, 8f, 4.0f, Vector3.up, Ceiling - 2.9f)
            };

            // Every run rolls its own code and picks which of these surfaces carry it.
            var director = session.AddComponent<ClueDirector>();
            director.game = game; director.spots = spots.ToArray(); director.randomizeOnPlay = true;
            game.director = director;
            director.Apply(1);

            var grid = ExitInterlock(root.transform);
            var presentation = session.AddComponent<StealthPresentation>();
            presentation.game = game; presentation.exitGrid = grid.gameObject; presentation.door = door;
            presentation.font = font;
            Lighting(root.transform, lights);
            presentation.corridorLights = lights.ToArray();
            var post = new GameObject("Global Volume").AddComponent<Volume>();
            post.transform.SetParent(root.transform, false);
            post.isGlobal = true; post.priority = 1; post.sharedProfile = PostProfile();
            Tidy(root.transform);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Ray-OE Zoning";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("Research laboratory authored with six CCTV cameras and " + spots.Count + " clue surfaces.");
        }

        [MenuItem("Ray-OE Zoning/Build Windows")]
        public static void BuildWindows()
        {
            if (!File.Exists(ScenePath)) Create();
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = "Builds/Windows/Ray-OE Zoning.exe",
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Windows build failed: " + report.summary.result);
        }

        // ------------------------------------------------------------ architecture
        static void Room(Transform room, float start, float end, float minX, float maxX, Material zone)
        {
            var shell = Child(room, "Structure");
            float width = maxX - minX, length = end - start, cx = (minX + maxX) * .5f, mid = (start + end) * .5f;
            Solid(shell, "Collider", new Vector3(cx, -.1f, mid), new Vector3(width + .4f, .2f, length), floorBase, false);
            Surface(shell, "Floor", new Vector3(cx, 0, mid), Quaternion.Euler(90, 0, 0), new Vector2(width, length), Tiled(floorBase, width, length, 1.2f));
            Solid(shell, "Collider", new Vector3(cx, Ceiling + .1f, mid), new Vector3(width + .4f, .2f, length), ceilingBase, false);
            Surface(shell, "Ceiling", new Vector3(cx, Ceiling, mid), Quaternion.Euler(-90, 0, 0), new Vector2(width, length), Tiled(ceilingBase, width, length, 1.2f));
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side < 0 ? minX : maxX, inward = -side;
                Solid(shell, "Collider", new Vector3(x + side * .1f, Ceiling * .5f, mid), new Vector3(.2f, Ceiling, length), wallPaint, false);
                Surface(shell, "Wall", new Vector3(x, Ceiling * .5f, mid), Quaternion.Euler(0, side < 0 ? -90 : 90, 0), new Vector2(length, Ceiling), Tiled(wallPaint, length, Ceiling, 1.2f));
                Box(shell, null, new Vector3(x + inward * .012f, .55f, mid), new Vector3(.024f, 1.1f, length), wainscot);
                Box(shell, null, new Vector3(x + inward * .025f, 1.11f, mid), new Vector3(.03f, .035f, length), stainless);
                Box(shell, null, new Vector3(x + inward * .004f, 1.42f, mid), new Vector3(.008f, .07f, length), zone);
                Box(shell, null, new Vector3(x + inward * .03f, .05f, mid), new Vector3(.04f, .1f, length), skirting);
                Pipe(shell, new Vector3(x + inward * .32f, Ceiling - .16f, start), new Vector3(x + inward * .32f, Ceiling - .16f, end), .05f, stainless);
                Pipe(shell, new Vector3(x + inward * .46f, Ceiling - .12f, start), new Vector3(x + inward * .46f, Ceiling - .12f, end), .032f, enamelTeal);
            }
        }

        static void Surface(Transform parent, string name, Vector3 at, Quaternion rotation, Vector2 size, Material material)
        {
            var go = Primitive(parent, name, PrimitiveType.Quad, at, new Vector3(size.x, size.y, 1), material, false);
            go.transform.localRotation = rotation;
        }

        static void WallBlock(Transform parent, Vector3 at, Vector3 size, bool cladding)
        {
            if (size.x <= .01f) return;
            Solid(parent, "Wall", at, size, Tiled(wallPaint, size.x, size.y, 1.2f));
            if (!cladding) return;
            for (int face = -1; face <= 1; face += 2)
            {
                Box(parent, null, new Vector3(at.x, .55f, at.z + face * (size.z * .5f + .012f)), new Vector3(size.x, 1.1f, .024f), wainscot);
                Box(parent, null, new Vector3(at.x, 1.11f, at.z + face * (size.z * .5f + .025f)), new Vector3(size.x, .035f, .03f), stainless);
            }
        }

        static void EndWall(Transform room, float z, float minX, float maxX)
        {
            var wall = Child(room, "Back Wall");
            WallBlock(wall, new Vector3((minX + maxX) * .5f, Ceiling * .5f, z - .15f), new Vector3(maxX - minX + .4f, Ceiling, .3f), true);
            // The staff door the player arrived through.
            var door = Placed(wall, "Door", new Vector3(1.0f, 0, z + .01f), 0);
            Box(door, null, new Vector3(0, 1.05f, 0), new Vector3(1.0f, 2.1f, .04f), lockerPaint);
            Box(door, null, new Vector3(0, 1.08f, -.01f), new Vector3(1.14f, 2.18f, .03f), doorFrame);
            Box(door, null, new Vector3(0, 1.02f, .045f), new Vector3(.8f, .05f, .05f), stainless);
            Box(door, null, new Vector3(.22f, 1.55f, .023f), new Vector3(.18f, .5f, .006f), glassTint);
            WallSign(wall, new Vector3(1.0f, 2.42f, z), 0, signExit, .3f);
            Extinguisher(wall, new Vector3(-1.2f, 1.0f, z + .024f), 0);
        }

        static void Doorway(Transform parent, float z, float minX, float maxX, float d0, float d1)
        {
            var doorway = Child(parent, "Doorway");
            WallBlock(doorway, new Vector3((minX + d0) * .5f, Ceiling * .5f, z), new Vector3(d0 - minX, Ceiling, .3f), true);
            WallBlock(doorway, new Vector3((d1 + maxX) * .5f, Ceiling * .5f, z), new Vector3(maxX - d1, Ceiling, .3f), true);
            WallBlock(doorway, new Vector3((d0 + d1) * .5f, (DoorHeight + Ceiling) * .5f, z), new Vector3(d1 - d0, Ceiling - DoorHeight, .3f), false);
            for (int side = -1; side <= 1; side += 2)
                Box(doorway, null, new Vector3(side < 0 ? d0 - .03f : d1 + .03f, DoorHeight * .5f, z), new Vector3(.08f, DoorHeight, .36f), doorFrame);
            Box(doorway, null, new Vector3((d0 + d1) * .5f, DoorHeight + .03f, z), new Vector3(d1 - d0 + .14f, .08f, .36f), doorFrame);
            Box(doorway, null, new Vector3((d0 + d1) * .5f, .004f, z), new Vector3(d1 - d0, .008f, .34f), stainless);
            // Glass leaves stand open in their pockets on the approach face.
            float leaf = (d1 - d0) * .5f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side < 0 ? d0 - leaf * .5f - .06f : d1 + leaf * .5f + .06f;
                var door = Placed(doorway, "Sliding Door", new Vector3(x, 0, z - .19f), 0);
                Box(door, null, new Vector3(0, DoorHeight * .5f, 0), new Vector3(leaf, DoorHeight - .08f, .012f), glass).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                Box(door, null, new Vector3(side * leaf * .5f, DoorHeight * .5f, 0), new Vector3(.05f, DoorHeight - .06f, .03f), doorFrame);
                Box(door, null, new Vector3(0, .06f, 0), new Vector3(leaf - .01f, .1f, .03f), doorFrame);
                Box(door, null, new Vector3(0, DoorHeight - .05f, 0), new Vector3(leaf - .01f, .06f, .03f), doorFrame);
                Box(door, null, new Vector3(-side * leaf * .3f, 1.1f, -.02f), new Vector3(.08f, .3f, .006f), stainless);
                // Overhead track the leaf hangs from, fixed to the lintel.
                Box(door, null, new Vector3(0, DoorHeight + .02f, .02f), new Vector3(leaf + .1f, .04f, .05f), darkMetal);
            }
            CardReader(doorway, new Vector3(d1 + .32f, 1.2f, z - .15f), 180, ledGreen);
        }

        // ------------------------------------------------------------ rooms
        static void StaffEntry(Transform room, List<Light> lights)
        {
            LockerBank(room, new Vector3(3.74f, 0, 3.7f), -90, 10);
            ChangingBench(room, new Vector3(2.55f, 0, 3.7f), 90, 3.0f);
            SecurityDesk(room, new Vector3(-2.55f, 0, 6.5f), 90, 2.1f);
            // Tall enough that the entry camera cannot see over it to heads near the spawn.
            Shelving(room, new Vector3(1.65f, 0, 7.4f), 0, 4.68f, 2.65f);
            Dispenser(room, new Vector3(-4f, 1.25f, 1.6f), 90);
            WallClock(room, new Vector3(4f, 2.65f, 8.9f), -90);
            Extinguisher(room, new Vector3(-3.976f, 1.0f, 9.6f), 90);
            WallSign(room, new Vector3(4f, 2.2f, 9.9f), -90, signCctv, .28f);
            CeilingGrid(room, lights, 0, 11, -4, 4, new Color(1f, .88f, .72f), 2.6f, new[] { true, false, false });
            // Clue surfaces.
            // Wall-hung clues clear the 8 mm wayfinding stripe they cross.
            Plaque(room, 0, new Vector3(-3.992f, 1.5f, 3.4f), 90);
            StickyNote(room, 0, new Vector3(3.476f, 1.3f, 4.3f), -90, 4);
            Clipboard(room, 0, new Vector3(-.35f, 1.45f, 0), 0, 1);
        }

        static void AnalyticalLab(Transform room, List<Light> lights)
        {
            FumeHood(room, new Vector3(5.55f, 0, 13.3f), -90, 1.5f);
            FumeHood(room, new Vector3(5.55f, 0, 14.95f), -90, 1.5f);
            // Long islands run with the route: their service spines hide a crouched player from the side camera.
            // One monitor slot on the left face of the first island is kept for a clue screen.
            var island = LabBench(room, new Vector3(-1.6f, 0, 16.75f), 90, 6.5f, 31, 2.4f);
            LabBench(room, new Vector3(2.4f, 0, 16.75f), 90, 6.5f, 32);
            // Tall storage island: shades the left lane from the far camera, leaving one timed gap for the side camera.
            InstrumentCabinet(room, new Vector3(-2.15f, 0, 21.42f), 180, 2.5f, 2.2f);
            InstrumentCabinet(room, new Vector3(-2.15f, 0, 21.98f), 0, 2.5f, 2.2f);
            LabBench(room, new Vector3(-2.7f, 0, 24.6f), 90, 4.0f, 34);
            LabBench(room, new Vector3(1.4f, 0, 24.6f), 90, 4.0f, 35);
            InstrumentCabinet(room, new Vector3(5.72f, 0, 20.6f), -90, 1.2f, 2.2f);
            InstrumentCabinet(room, new Vector3(5.72f, 0, 21.9f), -90, 1.2f, 2.2f);
            // Workstation in the left lane, with a pinboard on the wall above it.
            var desk = Placed(room, "Desk", new Vector3(-4.15f, 0, 16.5f), 90);
            Solid(desk, null, new Vector3(0, .45f, 0), new Vector3(1.6f, .9f, .7f), enamel);
            Box(desk, null, new Vector3(0, .915f, 0), new Vector3(1.66f, .03f, .76f), wood);
            var pinboard = Placed(desk, "Pinboard", new Vector3(0, 1.45f, -.33f), 0);
            Box(pinboard, null, Vector3.zero, new Vector3(1.5f, .8f, .02f), plasticGrey);
            Box(pinboard, null, new Vector3(0, 0, -.004f), new Vector3(1.54f, .84f, .014f), doorFrame);
            var lamp = Placed(desk, "Lamp", new Vector3(.6f, .93f, -.1f), 0);
            Box(lamp, null, new Vector3(0, .01f, 0), new Vector3(.12f, .02f, .12f), darkMetal);
            Box(lamp, null, new Vector3(0, .2f, .02f), new Vector3(.02f, .38f, .02f), darkMetal);
            Box(lamp, null, new Vector3(0, .39f, .1f), new Vector3(.14f, .04f, .2f), darkMetal);
            Box(lamp, null, new Vector3(0, .368f, .1f), new Vector3(.11f, .004f, .16f), nightPanel);
            var deskGlow = Child(lamp, "Spot Light").gameObject.AddComponent<Light>();
            deskGlow.transform.localPosition = new Vector3(0, .33f, .1f); deskGlow.transform.localRotation = Quaternion.Euler(90, 0, 0);
            deskGlow.type = LightType.Spot; deskGlow.spotAngle = 110; deskGlow.innerSpotAngle = 50; deskGlow.range = 1.6f; deskGlow.intensity = 1.6f; deskGlow.color = new Color(1, .82f, .6f); deskGlow.shadows = LightShadows.None;
            Mug(desk, new Vector3(-.55f, .93f, .15f));
            LowBypass(room, new Vector3(-3.75f, 0, 27.0f), false);
            WallSign(room, new Vector3(6f, 2.25f, 18.6f), -90, signCctv, .28f);
            Extinguisher(room, new Vector3(-4.476f, 1.0f, 21.0f), 90);
            WallClock(room, new Vector3(6f, 2.6f, 24.0f), -90);
            CeilingGrid(room, lights, 11, 29, -4.5f, 6, new Color(1f, .88f, .72f), 2.2f, new[] { false, false, true, false, false, false });
            // Clue surfaces.
            Clipboard(room, 1, new Vector3(-4.47f, 1.45f, 16.5f), 90, 2);
            DeskPaper(room, 1, new Vector3(-3.97f, .93f, 16.85f), -90 + 7);
            BenchScreen(island, 1, 2.4f);
            Whiteboard(room, 1, new Vector3(-4.5f, 1.62f, 12.7f), 90);
        }

        static void ColdStorage(Transform room, List<Light> lights)
        {
            // Row 1 blocks the right side; the player turns left first.
            float[] row1 = { -.85f, .05f, .95f, 1.85f, 2.75f };
            for (int i = 0; i < row1.Length; i++) Freezer(room, new Vector3(row1[i], 0, 32.6f), 180, i % 2 == 1);
            // Row 2 blocks the left side apart from a low cryo-line crawlspace; the long way round is watched.
            float[] row2 = { -4.5f, -3.6f, -2.7f, -.9f, .0f };
            for (int i = 0; i < row2.Length; i++) Freezer(room, new Vector3(row2[i], 0, 38.4f), 0, i % 2 == 0);
            LowBypass(room, new Vector3(-1.8f, 0, 38.4f), true);
            // A glazed freezer island shades the crawlspace from the aisle camera.
            Freezer(room, new Vector3(-.45f, 0, 36.3f), 90, true);
            Dewar(room, new Vector3(1.2f, 0, 35.2f));
            Dewar(room, new Vector3(-3.9f, 0, 36.9f));
            Dewar(room, new Vector3(-4.3f, 0, 41.0f));
            InstrumentCabinet(room, new Vector3(-4.7f, 0, 44.0f), 90, .9f, 2.1f);
            WallSign(room, new Vector3(3.5f, 2.15f, 35.2f), -90, signCctv, .28f);
            Extinguisher(room, new Vector3(3.476f, 1.0f, 43.5f), -90);
            CeilingGrid(room, lights, 29, 45, -5, 3.5f, new Color(.85f, .92f, 1f), 2.0f, new[] { false, false, false, false, true });
            // Clue surfaces.
            ControlPanel(room, 2, new Vector3(-5f, 1.5f, 42.6f), 90);
            FreezerLabel(room, 2, new Vector3(2.8f, 1.5f, 32.11f), 180);
            Clipboard(room, 2, new Vector3(-4.992f, 1.5f, 30.4f), 90, 3);
            Stencil(room, 2, new Vector3(1.2f, 1.75f, 44.85f), 180);
        }

        static void SecureResearch(Transform room, List<Light> lights)
        {
            for (int i = 0; i < 6; i++) ServerRack(room, new Vector3(-.6f, 0, 47.9f + i * .64f), 90, 60 + i);
            CableTray(room, new Vector3(-.6f, 2.55f, 49.5f), 4.2f);
            ResearchChamber(room, new Vector3(2.65f, 0, 50.2f), lights);
            // Clue niche between the chamber and the terminal.
            InstrumentCabinet(room, new Vector3(4.2f, 0, 54.6f), -90, 1.0f, 2.0f);
            WallSign(room, new Vector3(-4.5f, 2.2f, 47.0f), 90, signCctv, .28f);
            Extinguisher(room, new Vector3(-4.476f, 1.0f, 55.0f), 90);
            CeilingGrid(room, lights, 45, VaultStart, -4.5f, 4.5f, new Color(.86f, .9f, 1f), 2f, new[] { false, false, false, false });
            // Amber security beacons either side of the archive door, on wall brackets.
            for (int side = -1; side <= 1; side += 2)
            {
                var beacon = Placed(room, "Beacon", new Vector3(side * 2.3f, 2.85f, VaultStart - .15f), 180);
                Box(beacon, null, new Vector3(0, -.03f, .06f), new Vector3(.05f, .02f, .12f), darkMetal);
                Box(beacon, null, new Vector3(0, -.03f, .005f), new Vector3(.1f, .08f, .01f), darkMetal);
                Cylinder(beacon, null, new Vector3(0, -.01f, .1f), .055f, .03f, darkMetal);
                Cylinder(beacon, null, new Vector3(0, .045f, .1f), .042f, .08f, ledAmber);
                var glow = Child(beacon, "Point Light").gameObject.AddComponent<Light>();
                glow.transform.localPosition = new Vector3(0, .05f, .1f);
                glow.type = LightType.Point; glow.range = 1.8f; glow.intensity = .55f; glow.color = new Color(1, .55f, .15f); glow.shadows = LightShadows.None;
                lights.Add(glow);
            }
            // Clue surfaces.
            Memo(room, 3, new Vector3(4.492f, 1.5f, 53.0f), -90);
            StickyNote(room, 3, new Vector3(-.6f, 1.45f, 47.59f), 180, 5);
        }

        static void ResearchChamber(Transform parent, Vector3 at, List<Light> lights)
        {
            var chamber = Placed(parent, "Chamber", at, 0);
            const float size = 2.5f, height = 2.5f;
            Solid(chamber, null, new Vector3(0, .1f, 0), new Vector3(size + .1f, .2f, size + .1f), darkMetal);
            Box(chamber, null, new Vector3(0, height + .1f, 0), new Vector3(size + .1f, .2f, size + .1f), enamel);
            Box(chamber, null, new Vector3(0, height - .005f, 0), new Vector3(size - .5f, .01f, size - .5f), nightPanel);
            const float wall = height - .1f;
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Solid(chamber, null, new Vector3(x * (size * .5f - .005f), wall * .5f + .1f, z * (size * .5f - .005f)), new Vector3(.09f, wall, .09f), steel);
            GlassPane(chamber, new Vector3(0, wall * .5f + .1f, -size * .5f), new Vector3(size, wall, .02f));
            GlassPane(chamber, new Vector3(0, wall * .5f + .1f, size * .5f), new Vector3(size, wall, .02f));
            GlassPane(chamber, new Vector3(-size * .5f, wall * .5f + .1f, 0), new Vector3(.02f, wall, size));
            Solid(chamber, null, new Vector3(size * .5f, wall * .5f + .1f, 0), new Vector3(.07f, wall, size), enamel);
            var pedestal = Placed(chamber, "Pedestal", new Vector3(0, .2f, 0), 0);
            Cylinder(pedestal, null, new Vector3(0, .45f, 0), .28f, .9f, enamel);
            Cylinder(pedestal, null, new Vector3(0, .91f, 0), .32f, .03f, stainless);
            Cylinder(pedestal, null, new Vector3(0, 1.17f, 0), .14f, .48f, glass);
            Cylinder(pedestal, null, new Vector3(0, 1.12f, 0), .1f, .32f, sampleGlow);
            Cylinder(pedestal, null, new Vector3(0, 1.43f, 0), .15f, .05f, stainless);
            var glow = Child(pedestal, "Point Light").gameObject.AddComponent<Light>();
            glow.transform.localPosition = new Vector3(0, 1.2f, 0);
            glow.type = LightType.Point; glow.range = 2.4f; glow.intensity = 1.2f; glow.color = new Color(.3f, 1f, .85f); glow.shadows = LightShadows.None;
            lights.Add(glow);
            // Two screens on a rail bolted between the front posts; the left one can carry a clue.
            float front = -size * .5f - .05f;
            Box(chamber, null, new Vector3(0, 1.36f, front - .02f), new Vector3(size - .1f, .05f, .03f), darkMetal);
            for (int side = -1; side <= 1; side += 2)
                Box(chamber, null, new Vector3(side * (size * .5f - .015f), 1.36f, front + .01f), new Vector3(.05f, .08f, .1f), darkMetal);
            WallMonitor(chamber, new Vector3(.6f, 1.36f, front - .035f), 180, screenMolecule);
            WallMonitorSpot(3, chamber, new Vector3(-.6f, 1.36f, front - .035f), 180);
        }

        static void LowBypass(Transform root, Vector3 at, bool prone)
        {
            var bypass = Placed(root, "Duct", at, 0);
            float clearance = prone ? .74f : 1.30f, width = prone ? .9f : 1.5f, depth = prone ? .9f : 1.8f;
            float top = prone ? 2.0f : 2.2f;
            Solid(bypass, null, new Vector3(0, (clearance + top) * .5f, 0), new Vector3(width, top - clearance, depth), enamelTeal);
            Box(bypass, null, new Vector3(0, clearance + .04f, -depth * .5f - .006f), new Vector3(width, .08f, .012f), hazardStripe);
            Box(bypass, null, new Vector3(0, clearance + .04f, depth * .5f + .006f), new Vector3(width, .08f, .012f), hazardStripe);
            for (int side = -1; side <= 1; side += 2)
                Solid(bypass, null, new Vector3(side * (width * .5f - .05f), clearance * .5f, 0), new Vector3(.1f, clearance, depth), steel);
            for (int i = 0; i < 3; i++)
            {
                float x = -width * .5f + .2f + i * .25f;
                var pipe = Primitive(bypass, null, PrimitiveType.Cylinder, new Vector3(x, clearance + .14f, 0), new Vector3(.1f, (depth + .2f) * .5f, .1f), i == 1 ? enamel : stainless, false);
                pipe.transform.localRotation = Quaternion.Euler(90, 0, 0);
            }
            // The casing hangs from the ceiling on two rods.
            for (int side = -1; side <= 1; side += 2)
                Cylinder(bypass, null, new Vector3(0, (top + Ceiling) * .5f, side * depth * .3f), .015f, Ceiling - top, steel);
        }

        // ------------------------------------------------------------ archive
        static void ArchiveDoor(Transform root, out Transform door, out Transform keypad)
        {
            var end = Child(root, "Vault Door"); end.position = Vector3.forward * VaultStart;
            WallBlock(end, new Vector3(-3.05f, Ceiling * .5f, 0), new Vector3(3.3f, Ceiling, .3f), true);
            WallBlock(end, new Vector3(3.05f, Ceiling * .5f, 0), new Vector3(3.3f, Ceiling, .3f), true);
            WallBlock(end, new Vector3(0, (2.8f + Ceiling) * .5f, 0), new Vector3(2.8f, Ceiling - 2.8f, .3f), false);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(end, null, new Vector3(side * 1.465f, 1.4f, -.02f), new Vector3(.15f, 2.8f, .43f), darkMetal);
                Box(end, null, new Vector3(side * 1.465f, 1.4f, -.24f), new Vector3(.15f, 2.8f, .01f), hazardStripe);
            }
            Box(end, null, new Vector3(0, 2.86f, -.02f), new Vector3(3.1f, .14f, .42f), darkMetal);
            Box(end, null, new Vector3(0, .003f, -.55f), new Vector3(2.8f, .006f, .3f), hazardStripe);
            door = Child(end, "Door");
            Box(door, null, new Vector3(0, 1.4f, -.02f), new Vector3(2.8f, 2.8f, .16f), steel);
            for (int i = 0; i < 4; i++) Box(door, null, new Vector3(0, .35f + i * .7f, -.105f), new Vector3(2.7f, .06f, .02f), darkMetal);
            Box(door, null, new Vector3(0, 1.4f, -.11f), new Vector3(.02f, 2.8f, .012f), darkMetal);
            Box(door, null, new Vector3(0, 2.2f, -.118f), new Vector3(1.6f, .03f, .006f), laserHousing);
            // Wall terminal on the right wall, facing the room.
            var console = Child(root, "Terminal"); console.position = new Vector3(4.5f, 1.32f, 57.9f); console.rotation = Quaternion.Euler(0, 90, 0);
            Box(console, null, new Vector3(0, 0, -.07f), new Vector3(.62f, 1.0f, .14f), enamel);
            Box(console, null, new Vector3(0, .02f, -.145f), new Vector3(.54f, .9f, .01f), darkMetal);
            Box(console, null, new Vector3(0, .26f, -.151f), new Vector3(.42f, .26f, .004f), screenGraph);
            for (int i = 0; i < 4; i++) Box(console, null, new Vector3(-.15f + i * .1f, .07f, -.152f), new Vector3(.06f, .03f, .004f), ledAmber);
            keypad = Child(console, "Keypad"); keypad.localPosition = new Vector3(0, -.2f, -.16f);
            for (int i = 0; i < 12; i++)
            {
                var key = Child(keypad, "Key"); key.localPosition = new Vector3(-.12f + (i % 3) * .12f, .13f - (i / 3) * .085f, 0);
                Box(key, null, Vector3.zero, new Vector3(.095f, .065f, .018f), stainless);
                if (i < 9 || i == 10) WorldText(key, i == 10 ? "0" : (i + 1).ToString(), new Vector3(0, 0, -.011f), .045f, .042f, new Color(.08f, .09f, .1f));
                else Box(key, null, new Vector3(0, 0, -.011f), new Vector3(.04f, .012f, .002f), i == 9 ? ledAmber : ledGreen);
            }
            CardReader(console, new Vector3(.36f, .2f, -.015f), 180, recording);
        }

        static Transform ExitInterlock(Transform root)
        {
            var hardware = Child(root, "Laser Gate"); hardware.position = new Vector3(0, 0, VaultStart - .42f);
            var grid = Child(hardware, "Lasers"); grid.gameObject.layer = World;
            var barrier = grid.gameObject.AddComponent<BoxCollider>(); barrier.center = Vector3.up * 1.4f; barrier.size = new Vector3(2.8f, 2.8f, .10f);
            for (int side = -1; side <= 1; side += 2)
            {
                var post = Placed(hardware, "Post", new Vector3(side * 1.32f, 0, 0), 0);
                Box(post, null, new Vector3(0, 1.25f, 0), new Vector3(.12f, 2.5f, .12f), darkMetal);
                Box(post, null, new Vector3(0, 2.52f, 0), new Vector3(.16f, .04f, .16f), steel);
                Box(post, null, new Vector3(0, .03f, 0), new Vector3(.22f, .06f, .22f), steel);
                Box(post, null, new Vector3(-side * .065f, 1.25f, 0), new Vector3(.01f, 2.3f, .06f), rubber);
                for (int y = 0; y < 6; y++)
                    Primitive(post, null, PrimitiveType.Sphere, new Vector3(-side * .07f, .3f + y * .4f, 0), new Vector3(.03f, .03f, .03f), laserHousing, false);
            }
            for (int y = 0; y < 6; y++) LocalLine(grid, "Line", new[] { new Vector3(-1.25f, .3f + y * .4f, 0), new Vector3(1.25f, .3f + y * .4f, 0) }, laser, .007f);
            return grid;
        }

        static void ArchiveVault(Transform parent, List<Light> lights)
        {
            var vault = Child(parent, "Vault");
            float minX = -2.5f, maxX = 2.5f, mid = (VaultStart + VaultEnd) * .5f, length = VaultEnd - VaultStart;
            var plate = Lit("Vault floor plate", new Color(.26f, .28f, .30f), .62f, .55f);
            Solid(vault, "Floor", new Vector3(0, -.1f, mid), new Vector3(maxX - minX + .4f, .2f, length), plate);
            Solid(vault, "Ceiling", new Vector3(0, Ceiling + .1f, mid), new Vector3(maxX - minX + .4f, .2f, length), darkMetal);
            for (int side = -1; side <= 1; side += 2)
            {
                Solid(vault, "Wall", new Vector3(side * (maxX + .1f), Ceiling * .5f, mid), new Vector3(.2f, Ceiling, length), steel);
                // Archive drawer banks along both walls.
                var bank = Placed(vault, "Drawers", new Vector3(side * (maxX - .25f), 0, mid + .3f), side < 0 ? 90 : -90);
                Solid(bank, null, new Vector3(0, 1.2f, 0), new Vector3(length - 1.4f, 2.4f, .5f), darkMetal);
                for (int row = 0; row < 6; row++)
                    for (int col = 0; col < 6; col++)
                    {
                        float x = -(length - 1.4f) * .5f + .3f + col * .6f, y = .3f + row * .37f;
                        Box(bank, null, new Vector3(x, y, .255f), new Vector3(.56f, .33f, .012f), stainless);
                        Box(bank, null, new Vector3(x, y + .08f, .268f), new Vector3(.16f, .018f, .02f), darkMetal);
                        if ((row * 7 + col * 3 + side) % 5 == 0) Box(bank, null, new Vector3(x + .2f, y + .08f, .264f), new Vector3(.02f, .02f, .004f), ledGreen);
                    }
                Box(bank, null, new Vector3(0, 2.46f, .2f), new Vector3(length - 1.4f, .02f, .08f), ledBlue);
            }
            Solid(vault, "Wall", new Vector3(0, Ceiling * .5f, VaultEnd + .1f), new Vector3(maxX - minX + .4f, Ceiling, .2f), steel);
            Box(vault, null, new Vector3(0, 1.7f, VaultEnd - .01f), new Vector3(2.2f, 2.0f, .01f), darkMetal);
            Box(vault, null, new Vector3(0, 1.7f, VaultEnd - .02f), new Vector3(2.0f, 1.8f, .006f), ledBlue);
            for (int i = 0; i < 2; i++)
                Box(vault, null, new Vector3(0, Ceiling - .01f, VaultStart + 1.2f + i * 2.6f), new Vector3(1.4f, .01f, .3f), nightPanel);
            var pedestal = Placed(vault, "Pedestal", new Vector3(0, 0, 62.7f), 0);
            Cylinder(pedestal, null, new Vector3(0, .004f, 0), .75f, .008f, ledBlue);
            Cylinder(pedestal, null, new Vector3(0, .006f, 0), .7f, .008f, darkMetal);
            Cylinder(pedestal, null, new Vector3(0, .5f, 0), .3f, 1.0f, enamel);
            Cylinder(pedestal, null, new Vector3(0, .82f, 0), .305f, .03f, accentTeal);
            Cylinder(pedestal, null, new Vector3(0, 1.02f, 0), .36f, .04f, stainless);
            Box(pedestal, null, new Vector3(0, 1.28f, 0), new Vector3(.46f, .46f, .46f), glass).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box(pedestal, null, new Vector3(0, 1.512f, 0), new Vector3(.48f, .02f, .48f), stainless);
            Box(pedestal, null, new Vector3(0, 1.07f, 0), new Vector3(.3f, .06f, .18f), darkMetal);
            var sample = Placed(pedestal, "Sample", new Vector3(0, 1.18f, 0), 0);
            Cylinder(sample, null, Vector3.zero, .045f, .26f, sampleGlow, new Vector3(0, 0, 90));
            Cylinder(sample, null, new Vector3(-.15f, 0, 0), .052f, .04f, stainless, new Vector3(0, 0, 90));
            Cylinder(sample, null, new Vector3(.15f, 0, 0), .052f, .04f, stainless, new Vector3(0, 0, 90));
            var spot = Child(pedestal, "Spot Light").gameObject.AddComponent<Light>();
            spot.transform.localPosition = new Vector3(0, Ceiling - .1f, 0); spot.transform.localRotation = Quaternion.Euler(90, 0, 0);
            spot.type = LightType.Spot; spot.spotAngle = 46; spot.innerSpotAngle = 26; spot.range = 5; spot.intensity = 16; spot.color = new Color(1, .9f, .78f); spot.shadows = LightShadows.None;
            var glow = Child(pedestal, "Point Light").gameObject.AddComponent<Light>();
            glow.transform.localPosition = new Vector3(0, 1.25f, 0);
            glow.type = LightType.Point; glow.range = 2.2f; glow.intensity = 1.1f; glow.color = new Color(.3f, 1f, .85f); glow.shadows = LightShadows.None;
            lights.Add(spot); lights.Add(glow);
            for (int i = 0; i < 2; i++)
            {
                var fill = Child(vault, "Spot Light").gameObject.AddComponent<Light>();
                fill.transform.localPosition = new Vector3(0, Ceiling - .2f, VaultStart + 1.2f + i * 2.6f); fill.transform.localRotation = Quaternion.Euler(90, 0, 0);
                fill.type = LightType.Spot; fill.spotAngle = 150; fill.innerSpotAngle = 90; fill.range = 5; fill.intensity = 2.2f; fill.color = new Color(.8f, .9f, 1f); fill.shadows = LightShadows.None;
                lights.Add(fill);
            }
        }

        // ------------------------------------------------------------ lighting
        static void CeilingGrid(Transform parent, List<Light> lights, float start, float end, float minX, float maxX, Color color, float intensity, bool[] pattern)
        {
            float cx = (minX + maxX) * .5f, width = maxX - minX;
            int index = 0;
            for (float z = start + 1.8f; z < end - .8f; z += 3f, index++)
            {
                bool lit = pattern[index % pattern.Length];
                for (int side = -1; side <= 1; side += 2)
                {
                    var fixture = Placed(parent, "Ceiling Light", new Vector3(cx + side * width * .24f, Ceiling - .004f, z), 0);
                    Box(fixture, null, Vector3.zero, new Vector3(.66f, .008f, 1.26f), enamel).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    Box(fixture, null, new Vector3(0, -.006f, 0), new Vector3(.6f, .006f, 1.2f), lit ? nightPanel : fixtureOff).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    if (!lit) continue;
                    var go = Child(fixture, "Spot Light"); go.localPosition = new Vector3(0, -.06f, 0); go.localRotation = Quaternion.Euler(90, 0, 0);
                    var lamp = go.gameObject.AddComponent<Light>();
                    lamp.type = LightType.Spot; lamp.spotAngle = 140; lamp.innerSpotAngle = 60; lamp.range = 5.5f;
                    lamp.intensity = intensity; lamp.color = color; lamp.shadows = LightShadows.None;
                    lights.Add(lamp);
                }
            }
        }

        static void Lighting(Transform root, List<Light> lights)
        {
            var lighting = Child(root, "Lighting");
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AnnexLighting.Sky; RenderSettings.ambientEquatorColor = AnnexLighting.Ambient; RenderSettings.ambientGroundColor = AnnexLighting.Ground;
            RenderSettings.ambientLight = AnnexLighting.Ambient;
            RenderSettings.ambientIntensity = 1; RenderSettings.reflectionIntensity = .2f;
            RenderSettings.ambientProbe = AnnexLighting.Probe();
            lighting.gameObject.AddComponent<AnnexLighting>();
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .028f;
            RenderSettings.fogColor = new Color(.006f, .008f, .011f);
            var terminal = Child(lighting, "Point Light"); terminal.position = new Vector3(4.0f, 1.6f, 57.9f);
            var glow = terminal.gameObject.AddComponent<Light>(); glow.type = LightType.Point; glow.range = 2.4f; glow.intensity = 1.2f; glow.color = new Color(.45f, .8f, .95f); lights.Add(glow);
        }

        // ------------------------------------------------------------ player
        static void CreatePlayer(Transform root, StealthGame game, Vector3 at)
        {
            var player = Child(root, "Player").gameObject; player.tag = "Player"; player.layer = PlayerLayer; player.transform.position = at;
            var body = player.AddComponent<CharacterController>(); body.height = 1.8f; body.radius = .28f; body.center = Vector3.up * .9f;
            body.skinWidth = .025f; body.stepOffset = .15f; body.minMoveDistance = 0;
            var inputs = player.AddComponent<StarterAssetsInputs>();
            var input = player.AddComponent<PlayerInput>();
            input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions");
            input.defaultActionMap = "Player"; input.defaultControlScheme = "KeyboardMouse"; input.notificationBehavior = PlayerNotifications.SendMessages;
            var controller = player.AddComponent<FirstPersonController>(); controller.MoveSpeed = 2.35f; controller.SprintSpeed = 4.55f;
            controller.JumpHeight = 0; controller.SpeedChangeRate = 12; controller.GroundedRadius = .28f;
            controller.GroundLayers = 1 << World; controller.RotationSpeed = .72f; controller.TopClamp = 82; controller.BottomClamp = -82;
            var target = Child(player.transform, "PlayerCameraRoot"); target.localPosition = Vector3.up * 1.65f; controller.CinemachineCameraTarget = target.gameObject;
            var cameraGo = Child(target, "Main Camera").gameObject; cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>(); camera.fieldOfView = 68; camera.nearClipPlane = .03f; camera.farClipPlane = 90;
            camera.allowHDR = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.01f, .012f, .015f);
            var cameraData = cameraGo.AddComponent<UniversalAdditionalCameraData>(); cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; cameraData.antialiasingQuality = AntialiasingQuality.High;
            var muzzle = HeldFlashlight(cameraGo.transform, out var grip);
            // Faint fill that only touches the player's own hand and torch, so they read as more than a silhouette.
            var fill = Child(cameraGo.transform, "Point Light"); fill.localPosition = new Vector3(.02f, .06f, .12f);
            var handFill = fill.gameObject.AddComponent<Light>(); handFill.type = LightType.Point; handFill.range = .9f; handFill.intensity = .35f;
            handFill.color = new Color(.75f, .82f, .9f); handFill.shadows = LightShadows.None; handFill.cullingMask = 1 << PlayerLayer;
            var torchRoot = Child(cameraGo.transform, "Spot Light");
            var torch = torchRoot.gameObject.AddComponent<Light>(); torch.type = LightType.Spot; torch.range = 18;
            torch.spotAngle = 56; torch.innerSpotAngle = 24; torch.intensity = 32f;
            torch.color = new Color(1f, .93f, .80f); torch.shadows = LightShadows.Soft; torch.shadowBias = .015f; torch.shadowNormalBias = .06f; torch.shadowNearPlane = .03f;
            game.controller = controller; game.inputs = inputs; game.body = body; game.playerCamera = camera; game.torch = torch; game.torchOrigin = muzzle;
            var stance = player.AddComponent<StealthStance>(); stance.game = game; stance.controller = controller; stance.body = body; stance.cameraRoot = target; stance.obstructionMask = Obstructions; game.stance = stance;
            PlayerBody(player.transform, game, grip, muzzle.parent);
        }

        static GameObject Limb(Transform parent, Vector3 a, Vector3 b, float radius, Material material)
        {
            Vector3 delta = b - a;
            float length = delta.magnitude + radius * 2;
            var go = Primitive(parent, null, PrimitiveType.Capsule, (a + b) * .5f, new Vector3(radius * 2, length * .5f, radius * 2), material, false);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.sqrMagnitude > 1e-8f ? delta.normalized : Vector3.up);
            return go;
        }

        // ------------------------------------------------------------ text
        static void WorldText(Transform root, string value, Vector3 at, float width, float height, Color color)
        {
            var go = Child(root, "Text (TMP)").gameObject; go.transform.localPosition = at;
            var text = go.AddComponent<TextMeshPro>(); text.font = font; text.text = value; text.color = color;
            text.fontSize = 36; text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(100, 10); text.richText = false;
            text.ForceMeshUpdate(true, true);
            Bounds bounds = text.textBounds;
            float scale = Mathf.Min(width / Mathf.Max(.001f, bounds.size.x), height / Mathf.Max(.001f, bounds.size.y));
            go.transform.localScale = Vector3.one * scale;
            text.renderer.shadowCastingMode = ShadowCastingMode.Off; text.renderer.receiveShadows = false;
        }

        // ------------------------------------------------------------ primitives
        static Transform Child(Transform parent, string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform;
        }
        /// <summary>A collider block; invisible ones are named "Collider".</summary>
        static GameObject Solid(Transform root, string name, Vector3 at, Vector3 size, Material material, bool visible = true)
        {
            var go = Primitive(root, visible ? name : "Collider", PrimitiveType.Cube, at, size, material, true); go.layer = World;
            go.GetComponent<Renderer>().enabled = visible; return go;
        }
        /// <summary>Unnamed primitives keep Unity's default name (Cube, Cylinder, ...).</summary>
        static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 at, Vector3 size, Material material, bool solid)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name ?? type.ToString(); go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = size; go.layer = World;
            if (material) go.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static LineRenderer LocalLine(Transform parent, string name, Vector3[] points, Material material, float width)
        {
            var go = Child(parent, name).gameObject; go.layer = parent.gameObject.layer;
            var renderer = go.AddComponent<LineRenderer>(); renderer.useWorldSpace = false; renderer.positionCount = points.Length; renderer.SetPositions(points);
            renderer.sharedMaterial = material; renderer.startWidth = renderer.endWidth = width;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            renderer.numCornerVertices = 3; renderer.numCapVertices = 2; return renderer;
        }
        static void SetLayer(GameObject root, int layer) { foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer; }

        /// <summary>
        /// Names siblings the way Unity names duplicates: "Server", "Server (1)", "Server (2)".
        /// Imported model hierarchies keep their own bone names.
        /// </summary>
        static void Tidy(Transform root)
        {
            var counts = new Dictionary<string, int>();
            foreach (Transform child in root)
            {
                if (child.GetComponent<Animator>()) continue;
                string name = child.name;
                counts.TryGetValue(name, out int seen);
                counts[name] = seen + 1;
                if (seen > 0) child.name = name + " (" + seen + ")";
                Tidy(child);
            }
        }

        static void SetLayers()
        {
            var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = manager.FindProperty("layers");
            layers.GetArrayElementAtIndex(World).stringValue = "ZoningWorld";
            layers.GetArrayElementAtIndex(PlayerLayer).stringValue = "ZoningPlayer";
            layers.GetArrayElementAtIndex(ClueLayer).stringValue = "ZoningClue";
            layers.GetArrayElementAtIndex(ScannerLayer).stringValue = "ZoningScanner";
            layers.GetArrayElementAtIndex(GlassLayer).stringValue = "ZoningGlass";
            manager.ApplyModifiedPropertiesWithoutUndo();
        }
        static void DisableProjectAudio()
        {
            var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset");
            if (settings.Length == 0) throw new InvalidOperationException("Project audio settings are unavailable.");
            var audio = new SerializedObject(settings[0]);
            var disabled = audio.FindProperty("m_DisableAudio");
            if (disabled == null) throw new InvalidOperationException("Project audio disable setting is unavailable.");
            disabled.boolValue = true;
            audio.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
