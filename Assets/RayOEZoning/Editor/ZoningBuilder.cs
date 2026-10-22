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
            CreateMaterials();
            extinguisherRed = Lit("Extinguisher red", new Color(.62f, .05f, .04f), .55f);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Ray-OE Zoning");
            var architecture = Child(root.transform, "Research laboratory");
            var equipment = Child(root.transform, "Laboratory equipment and cover");
            var lights = new List<Light>();

            Room(architecture, "Staff entry", 0, 11, -4, 4, accentTeal);
            Room(architecture, "Analytical laboratory", 11, 29, -4.5f, 6, accentTeal);
            Room(architecture, "Specimen cold storage", 29, 45, -5, 3.5f, accentIce);
            Room(architecture, "Secure research", 45, VaultStart, -4.5f, 4.5f, accentAmber);
            EndWall(architecture, "Staff entrance wall", 0, -4, 4, true);
            Bulkhead(architecture, 11, -4.5f, 6, -2.9f, -.7f, signLab);
            Bulkhead(architecture, 29, -5, 6, .6f, 2.8f, signCold);
            Bulkhead(architecture, 45, -5, 4.5f, -3.6f, -1.4f, signSecure);

            StaffEntry(equipment, lights);
            AnalyticalLab(equipment, lights);
            ColdStorage(equipment, lights);
            SecureResearch(equipment, lights);

            Transform door, keypad; ArchiveDoor(architecture, out door, out keypad);
            ArchiveVault(architecture, lights);

            var session = Child(root.transform, "Session").gameObject;
            var game = session.AddComponent<StealthGame>();
            game.exposureSeconds = 1.35f; game.decaySeconds = 1.5f; game.escapeZ = VaultStart + 1.1f;
            var spawn = Child(root.transform, "Entrance spawn"); spawn.position = new Vector3(1.0f, .06f, 1.0f); game.spawn = spawn;
            CreatePlayer(root.transform, game, spawn.position); game.keypad = keypad;

            var surveillance = Child(root.transform, "Laboratory surveillance");
            game.hazards = new[] {
                // Wide entry camera: sweeps the open floor between the supply divider and the lab door.
                Scanner(surveillance, game, DetectionShape.Sphere, new Vector3(3.62f, 2.85f, 10.62f), new Vector3(-1.0f, 0, 8.0f), 64, 8.5f, 30, 7.2f, 0f, new Vector3(.32f, .08f, .32f)),
                // Narrow long-range camera across the long islands; their spines hide a crouched player.
                Scanner(surveillance, game, DetectionShape.Box, new Vector3(5.62f, 2.9f, 17.6f), new Vector3(-3.2f, 0, 17.0f), 38, 11, 26, 8f, 1.3f, new Vector3(.36f, .1f, 0)),
                // Ceiling camera above the cold-store door looking back down the island lanes.
                Scanner(surveillance, game, DetectionShape.Capsule, new Vector3(.6f, 2.9f, 28.2f), new Vector3(-.6f, 0, 20.5f), 50, 10, 30, 8.5f, 2.8f, new Vector3(0, Ceiling - 2.9f, 0)),
                // Wide short camera on the long way round; a freezer island shades the crawlspace.
                Scanner(surveillance, game, DetectionShape.Sphere, new Vector3(3.2f, 2.8f, 33.7f), new Vector3(.9f, 0, 39.6f), 68, 8, 22, 7.8f, .8f, new Vector3(.3f, .08f, 0)),
                // Long narrow camera from the archive wall down the left server lane.
                Scanner(surveillance, game, DetectionShape.Box, new Vector3(-4.12f, 2.95f, 59.15f), new Vector3(-2.6f, 0, 49.5f), 34, 11.5f, 24, 8.8f, 2.2f, new Vector3(-.36f, .1f, .3f)),
                // Ceiling camera guarding the terminal and the archive door.
                Scanner(surveillance, game, DetectionShape.Capsule, new Vector3(2.0f, 2.9f, 53.6f), new Vector3(-2.5f, 0, 58.1f), 46, 9.5f, 25, 8f, 4.0f, new Vector3(0, Ceiling - 2.9f, 0))
            };

            var records = Child(root.transform, "Inspection records");
            game.clues = new[] {
                Clue(records, 0, new Vector3(-3.92f, 1.45f, 3.4f), -1, 0),
                Clue(records, 1, new Vector3(-4.44f, 1.42f, 16.5f), -1, 1),
                Clue(records, 2, new Vector3(-4.92f, 1.45f, 42.6f), -1, 2),
                Clue(records, 3, new Vector3(4.42f, 1.45f, 53.0f), 1, 0)
            };

            var grid = ExitInterlock(root.transform);
            var presentation = session.AddComponent<StealthPresentation>();
            presentation.game = game; presentation.exitGrid = grid.gameObject; presentation.door = door;
            presentation.font = font;
            Lighting(root.transform, lights);
            presentation.corridorLights = lights.ToArray();
            var post = new GameObject("Laboratory post processing").AddComponent<Volume>();
            post.transform.SetParent(root.transform, false);
            post.isGlobal = true; post.priority = 1; post.sharedProfile = PostProfile();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Ray-OE Zoning";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("Research laboratory authored with six CCTV searchlights, four inspection records and stance routes.");
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
        static void Room(Transform parent, string name, float start, float end, float minX, float maxX, Material zone)
        {
            var room = Child(parent, name);
            float width = maxX - minX, length = end - start, cx = (minX + maxX) * .5f, mid = (start + end) * .5f;
            Solid(room, "Floor collision", new Vector3(cx, -.1f, mid), new Vector3(width + .4f, .2f, length), floorBase, false);
            Surface(room, "Vinyl floor", new Vector3(cx, 0, mid), Quaternion.Euler(90, 0, 0), new Vector2(width, length), Tiled(floorBase, width, length, 1.2f));
            Solid(room, "Ceiling collision", new Vector3(cx, Ceiling + .1f, mid), new Vector3(width + .4f, .2f, length), ceilingBase, false);
            Surface(room, "Ceiling grid", new Vector3(cx, Ceiling, mid), Quaternion.Euler(-90, 0, 0), new Vector2(width, length), Tiled(ceilingBase, width, length, 1.2f));
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side < 0 ? minX : maxX, inward = -side;
                Solid(room, "Wall collision", new Vector3(x + side * .1f, Ceiling * .5f, mid), new Vector3(.2f, Ceiling, length), wallPaint, false);
                Surface(room, "Wall panels", new Vector3(x, Ceiling * .5f, mid), Quaternion.Euler(0, side < 0 ? -90 : 90, 0), new Vector2(length, Ceiling), Tiled(wallPaint, length, Ceiling, 1.2f));
                Box(room, "Hygienic cladding", new Vector3(x + inward * .012f, .55f, mid), new Vector3(.024f, 1.1f, length), wainscot);
                Box(room, "Cladding rail", new Vector3(x + inward * .025f, 1.11f, mid), new Vector3(.03f, .035f, length), stainless);
                Box(room, "Zone wayfinding band", new Vector3(x + inward * .004f, 1.42f, mid), new Vector3(.008f, .07f, length), zone);
                Box(room, "Coved skirting", new Vector3(x + inward * .03f, .05f, mid), new Vector3(.04f, .1f, length), skirting);
                Pipe(room, new Vector3(x + inward * .32f, Ceiling - .16f, start), new Vector3(x + inward * .32f, Ceiling - .16f, end), .05f, stainless);
                Pipe(room, new Vector3(x + inward * .46f, Ceiling - .12f, start), new Vector3(x + inward * .46f, Ceiling - .12f, end), .032f, enamelTeal);
            }
        }

        static void Surface(Transform parent, string name, Vector3 at, Quaternion rotation, Vector2 size, Material material)
        {
            var go = Primitive(parent, name, PrimitiveType.Quad, at, new Vector3(size.x, size.y, 1), material, false);
            go.transform.localRotation = rotation;
        }

        static void WallBlock(Transform parent, string name, Vector3 at, Vector3 size, bool cladding)
        {
            if (size.x <= .01f) return;
            Solid(parent, name, at, size, Tiled(wallPaint, size.x, size.y, 1.2f));
            if (!cladding) return;
            for (int face = -1; face <= 1; face += 2)
            {
                Box(parent, "Bulkhead cladding", new Vector3(at.x, .55f, at.z + face * (size.z * .5f + .012f)), new Vector3(size.x, 1.1f, .024f), wainscot);
                Box(parent, "Bulkhead rail", new Vector3(at.x, 1.11f, at.z + face * (size.z * .5f + .025f)), new Vector3(size.x, .035f, .03f), stainless);
            }
        }

        static void EndWall(Transform parent, string name, float z, float minX, float maxX, bool exitSign)
        {
            var wall = Child(parent, name);
            WallBlock(wall, "End wall", new Vector3((minX + maxX) * .5f, Ceiling * .5f, z - .15f), new Vector3(maxX - minX + .4f, Ceiling, .3f), true);
            if (!exitSign) return;
            // The staff door the player arrived through.
            var door = Placed(wall, "Staff entrance door", new Vector3(1.0f, 0, z + .01f), 0);
            Box(door, "Door leaf", new Vector3(0, 1.05f, 0), new Vector3(1.0f, 2.1f, .04f), lockerPaint);
            Box(door, "Door frame", new Vector3(0, 1.08f, -.01f), new Vector3(1.14f, 2.18f, .03f), doorFrame);
            Box(door, "Push bar", new Vector3(0, 1.02f, .045f), new Vector3(.8f, .05f, .05f), stainless);
            Box(door, "Vision panel", new Vector3(.22f, 1.55f, .023f), new Vector3(.18f, .5f, .006f), glassTint);
            WallSign(wall, new Vector3(1.0f, 2.42f, z + .03f), 0, signExit, .3f);
            Extinguisher(wall, new Vector3(-1.2f, 1.0f, z + .1f), 0);
        }

        static void Bulkhead(Transform parent, float z, float minX, float maxX, float d0, float d1, Material zoneSign)
        {
            var bulkhead = Child(parent, "Bulkhead " + z.ToString("0"));
            WallBlock(bulkhead, "Left bulkhead", new Vector3((minX + d0) * .5f, Ceiling * .5f, z), new Vector3(d0 - minX, Ceiling, .3f), true);
            WallBlock(bulkhead, "Right bulkhead", new Vector3((d1 + maxX) * .5f, Ceiling * .5f, z), new Vector3(maxX - d1, Ceiling, .3f), true);
            WallBlock(bulkhead, "Door lintel", new Vector3((d0 + d1) * .5f, (DoorHeight + Ceiling) * .5f, z), new Vector3(d1 - d0, Ceiling - DoorHeight, .3f), false);
            for (int side = -1; side <= 1; side += 2)
                Box(bulkhead, "Door jamb", new Vector3(side < 0 ? d0 - .03f : d1 + .03f, DoorHeight * .5f, z), new Vector3(.08f, DoorHeight, .36f), doorFrame);
            Box(bulkhead, "Door head", new Vector3((d0 + d1) * .5f, DoorHeight + .03f, z), new Vector3(d1 - d0 + .14f, .08f, .36f), doorFrame);
            Box(bulkhead, "Threshold plate", new Vector3((d0 + d1) * .5f, .004f, z), new Vector3(d1 - d0, .008f, .34f), stainless);
            // Glass leaves stand open in their pockets on the approach face.
            float leaf = (d1 - d0) * .5f;
            for (int side = -1; side <= 1; side += 2)
            {
                float x = side < 0 ? d0 - leaf * .5f - .06f : d1 + leaf * .5f + .06f;
                var door = Placed(bulkhead, "Open sliding leaf", new Vector3(x, 0, z - .19f), 0);
                Box(door, "Leaf glass", new Vector3(0, DoorHeight * .5f, 0), new Vector3(leaf, DoorHeight - .08f, .012f), glass).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                Box(door, "Leaf stile", new Vector3(side * leaf * .5f, DoorHeight * .5f, 0), new Vector3(.05f, DoorHeight - .06f, .03f), doorFrame);
                Box(door, "Leaf rail", new Vector3(0, .06f, 0), new Vector3(leaf, .1f, .03f), doorFrame);
                Box(door, "Leaf push plate", new Vector3(-side * leaf * .3f, 1.1f, -.02f), new Vector3(.08f, .3f, .006f), stainless);
            }
            CardReader(bulkhead, new Vector3(d1 + .32f, 1.2f, z - .17f), 180, ledGreen);
            WallSign(bulkhead, new Vector3((d0 + d1) * .5f, DoorHeight + .48f, z - .16f), 180, zoneSign, .38f);
        }

        // ------------------------------------------------------------ rooms
        static void StaffEntry(Transform parent, List<Light> lights)
        {
            var room = Child(parent, "Staff entry fittings");
            LockerBank(room, new Vector3(3.74f, 0, 3.7f), -90, 10);
            ChangingBench(room, new Vector3(2.55f, 0, 3.7f), 90, 3.0f);
            SecurityDesk(room, new Vector3(-2.55f, 0, 6.5f), 90, 2.1f);
            Shelving(room, new Vector3(1.65f, 0, 7.4f), 0, 4.7f, 2.3f);
            Dispenser(room, new Vector3(-3.95f, 1.25f, 1.6f), 90);
            WallClock(room, new Vector3(3.97f, 2.65f, 8.9f), -90);
            Extinguisher(room, new Vector3(-3.9f, 1.0f, 9.6f), 90);
            WallSign(room, new Vector3(3.97f, 2.2f, 9.9f), -90, signCctv, .28f);
            CeilingGrid(room, lights, 0, 11, -4, 4, new Color(1f, .88f, .72f), 2.6f, new[] { true, false, false });
        }

        static void AnalyticalLab(Transform parent, List<Light> lights)
        {
            var room = Child(parent, "Analytical laboratory fittings");
            FumeHood(room, new Vector3(5.55f, 0, 13.3f), -90, 1.5f);
            FumeHood(room, new Vector3(5.55f, 0, 14.95f), -90, 1.5f);
            // Long islands run with the route: their service spines hide a crouched player from the side camera.
            LabBench(room, new Vector3(-1.6f, 0, 16.75f), 90, 6.5f, 31);
            LabBench(room, new Vector3(2.4f, 0, 16.75f), 90, 6.5f, 32);
            // Tall storage island: shades the left lane from the far camera, leaving one timed gap for the side camera.
            InstrumentCabinet(room, new Vector3(-2.15f, 0, 21.42f), 180, 2.5f, 2.2f);
            InstrumentCabinet(room, new Vector3(-2.15f, 0, 21.98f), 0, 2.5f, 2.2f);
            LabBench(room, new Vector3(-2.7f, 0, 24.6f), 90, 4.0f, 34);
            LabBench(room, new Vector3(1.4f, 0, 24.6f), 90, 4.0f, 35);
            InstrumentCabinet(room, new Vector3(5.72f, 0, 20.6f), -90, 1.2f, 2.2f);
            InstrumentCabinet(room, new Vector3(5.72f, 0, 21.9f), -90, 1.2f, 2.2f);
            // Inspection workstation for the clipped record, tucked in the left lane.
            var desk = Placed(room, "Inspection workstation", new Vector3(-4.15f, 0, 16.5f), 90);
            Solid(desk, "Workstation cabinet", new Vector3(0, .45f, 0), new Vector3(1.6f, .9f, .7f), enamel);
            Box(desk, "Workstation top", new Vector3(0, .915f, 0), new Vector3(1.66f, .03f, .76f), wood);
            Box(desk, "Pinboard", new Vector3(0, 1.45f, -.33f), new Vector3(1.5f, .8f, .02f), plasticGrey);
            var lamp = Placed(desk, "Task lamp", new Vector3(.6f, .93f, -.1f), 0);
            Box(lamp, "Lamp base", new Vector3(0, .01f, 0), new Vector3(.12f, .02f, .12f), darkMetal);
            Box(lamp, "Lamp arm", new Vector3(0, .2f, .02f), new Vector3(.02f, .38f, .02f), darkMetal);
            Box(lamp, "Lamp head", new Vector3(0, .39f, .1f), new Vector3(.14f, .04f, .2f), darkMetal);
            Box(lamp, "Lamp diffuser", new Vector3(0, .368f, .1f), new Vector3(.11f, .004f, .16f), nightPanel);
            var deskGlow = Child(lamp, "Task lamp light").gameObject.AddComponent<Light>();
            deskGlow.transform.localPosition = new Vector3(0, .33f, .1f); deskGlow.transform.localRotation = Quaternion.Euler(90, 0, 0);
            deskGlow.type = LightType.Spot; deskGlow.spotAngle = 110; deskGlow.innerSpotAngle = 50; deskGlow.range = 1.6f; deskGlow.intensity = 1.6f; deskGlow.color = new Color(1, .82f, .6f); deskGlow.shadows = LightShadows.None;
            LowBypass(room, new Vector3(-3.75f, 0, 27.0f), false);
            WallSign(room, new Vector3(5.97f, 2.25f, 18.6f), -90, signCctv, .28f);
            Extinguisher(room, new Vector3(-4.42f, 1.0f, 21.0f), 90);
            WallClock(room, new Vector3(5.97f, 2.6f, 24.0f), -90);
            CeilingGrid(room, lights, 11, 29, -4.5f, 6, new Color(1f, .88f, .72f), 2.2f, new[] { false, false, true, false, false, false });
        }

        static void ColdStorage(Transform parent, List<Light> lights)
        {
            var room = Child(parent, "Cold storage fittings");
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
            WallSign(room, new Vector3(3.47f, 2.15f, 35.2f), -90, signCctv, .28f);
            WallSign(room, new Vector3(-4.97f, 2.2f, 34.6f), 90, signCold, .34f);
            Extinguisher(room, new Vector3(3.42f, 1.0f, 43.5f), -90);
            CeilingGrid(room, lights, 29, 45, -5, 3.5f, new Color(.85f, .92f, 1f), 2.0f, new[] { false, false, false, false, true });
        }

        static void SecureResearch(Transform parent, List<Light> lights)
        {
            var room = Child(parent, "Secure research fittings");
            for (int i = 0; i < 6; i++) ServerRack(room, new Vector3(-.6f, 0, 47.9f + i * .64f), 90, 60 + i);
            Box(room, "Rack cable tray", new Vector3(-.6f, 2.55f, 49.5f), new Vector3(.4f, .06f, 4.2f), steel);
            ResearchChamber(room, new Vector3(2.65f, 0, 50.2f), lights);
            // Clue niche between the chamber and the terminal.
            InstrumentCabinet(room, new Vector3(4.2f, 0, 54.6f), -90, 1.0f, 2.0f);
            WallSign(room, new Vector3(-4.48f, 2.2f, 47.0f), 90, signCctv, .28f);
            WallSign(room, new Vector3(-4.48f, 2.2f, 52.0f), 90, signSecure, .34f);
            Extinguisher(room, new Vector3(-4.42f, 1.0f, 55.0f), 90);
            CeilingGrid(room, lights, 45, VaultStart, -4.5f, 4.5f, new Color(.86f, .9f, 1f), 2f, new[] { false, false, false, false });
            // Amber security beacons either side of the archive door.
            for (int side = -1; side <= 1; side += 2)
            {
                var beacon = Placed(room, "Security beacon", new Vector3(side * 2.3f, 2.85f, VaultStart - .2f), 0);
                Cylinder(beacon, "Beacon base", Vector3.zero, .06f, .04f, darkMetal);
                Cylinder(beacon, "Beacon lens", new Vector3(0, .06f, 0), .045f, .08f, ledAmber);
                var glow = Child(beacon, "Beacon light").gameObject.AddComponent<Light>();
                glow.type = LightType.Point; glow.range = 1.8f; glow.intensity = .55f; glow.color = new Color(1, .55f, .15f); glow.shadows = LightShadows.None;
                lights.Add(glow);
            }
        }

        static void ResearchChamber(Transform parent, Vector3 at, List<Light> lights)
        {
            var chamber = Placed(parent, "Sealed research chamber", at, 0);
            const float size = 2.5f, height = 2.5f;
            Solid(chamber, "Chamber plinth", new Vector3(0, .1f, 0), new Vector3(size + .1f, .2f, size + .1f), darkMetal);
            Box(chamber, "Chamber roof", new Vector3(0, height + .1f, 0), new Vector3(size + .1f, .2f, size + .1f), enamel);
            Box(chamber, "Chamber roof light", new Vector3(0, height - .005f, 0), new Vector3(size - .5f, .01f, size - .5f), nightPanel);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Solid(chamber, "Chamber post", new Vector3(x * size * .5f, height * .5f + .1f, z * size * .5f), new Vector3(.1f, height, .1f), steel);
            GlassPane(chamber, "Chamber glass front", new Vector3(0, height * .5f + .1f, -size * .5f), new Vector3(size, height, .02f));
            GlassPane(chamber, "Chamber glass back", new Vector3(0, height * .5f + .1f, size * .5f), new Vector3(size, height, .02f));
            GlassPane(chamber, "Chamber glass left", new Vector3(-size * .5f, height * .5f + .1f, 0), new Vector3(.02f, height, size));
            Solid(chamber, "Chamber service wall", new Vector3(size * .5f, height * .5f + .1f, 0), new Vector3(.08f, height, size), enamel);
            var pedestal = Placed(chamber, "Specimen pedestal", new Vector3(0, .2f, 0), 0);
            Cylinder(pedestal, "Pedestal column", new Vector3(0, .45f, 0), .28f, .9f, enamel);
            Cylinder(pedestal, "Pedestal top", new Vector3(0, .91f, 0), .32f, .03f, stainless);
            Cylinder(pedestal, "Specimen vessel", new Vector3(0, 1.17f, 0), .14f, .48f, glass);
            Cylinder(pedestal, "Specimen culture", new Vector3(0, 1.12f, 0), .1f, .32f, sampleGlow);
            Cylinder(pedestal, "Vessel cap", new Vector3(0, 1.43f, 0), .15f, .05f, stainless);
            var glow = Child(pedestal, "Specimen glow").gameObject.AddComponent<Light>();
            glow.transform.localPosition = new Vector3(0, 1.2f, 0);
            glow.type = LightType.Point; glow.range = 2.4f; glow.intensity = 1.2f; glow.color = new Color(.3f, 1f, .85f); glow.shadows = LightShadows.None;
            lights.Add(glow);
            for (int i = 0; i < 2; i++)
            {
                var screen = Placed(chamber, "Chamber monitor", new Vector3(-.6f + i * 1.2f, 1.25f, -size * .5f - .1f), 180);
                BenchMonitor(screen, i == 0 ? screenWave : screenMolecule);
            }
        }

        static void Partition(Transform root, Vector3 at, Vector3 size)
        {
            var partition = Child(root, "Equipment screen"); partition.localPosition = at;
            Solid(partition, "Screen core", new Vector3(0, size.y * .5f, 0), size, enamel);
            Box(partition, "Screen cap", new Vector3(0, size.y - .02f, 0), new Vector3(size.x + .02f, .04f, size.z + .02f), stainless);
            for (int face = -1; face <= 1; face += 2)
                Box(partition, "Screen kick", new Vector3(0, .1f, face * (size.z * .5f + .006f)), new Vector3(size.x, .2f, .012f), skirting);
        }

        static void LowBypass(Transform root, Vector3 at, bool prone)
        {
            var bypass = Placed(root, prone ? "Optional prone cryo-line crawlspace" : "Optional crouch pipe chase", at, 0);
            float clearance = prone ? .74f : 1.30f, width = prone ? .9f : 1.5f, depth = prone ? .9f : 1.8f;
            float top = prone ? 2.0f : 2.2f;
            Solid(bypass, "Service casing", new Vector3(0, (clearance + top) * .5f, 0), new Vector3(width, top - clearance, depth), enamelTeal);
            Box(bypass, "Casing hazard band", new Vector3(0, clearance + .04f, -depth * .5f - .006f), new Vector3(width, .08f, .012f), hazardStripe);
            Box(bypass, "Casing hazard band rear", new Vector3(0, clearance + .04f, depth * .5f + .006f), new Vector3(width, .08f, .012f), hazardStripe);
            for (int side = -1; side <= 1; side += 2)
                Solid(bypass, "Casing support", new Vector3(side * (width * .5f - .05f), clearance * .5f, 0), new Vector3(.1f, clearance, depth), steel);
            for (int i = 0; i < 3; i++)
                Pipe(bypass, new Vector3(-width * .5f + .2f + i * .25f, clearance + .14f, -depth * .5f - .1f), new Vector3(-width * .5f + .2f + i * .25f, clearance + .14f, depth * .5f + .1f), .05f, i == 1 ? enamel : stainless);
        }

        // ------------------------------------------------------------ archive
        static void ArchiveDoor(Transform root, out Transform door, out Transform keypad)
        {
            var end = Child(root, "Archive threshold"); end.position = Vector3.forward * VaultStart;
            WallBlock(end, "Archive wall left", new Vector3(-3.05f, Ceiling * .5f, 0), new Vector3(3.3f, Ceiling, .3f), true);
            WallBlock(end, "Archive wall right", new Vector3(3.05f, Ceiling * .5f, 0), new Vector3(3.3f, Ceiling, .3f), true);
            WallBlock(end, "Archive lintel", new Vector3(0, (2.8f + Ceiling) * .5f, 0), new Vector3(2.8f, Ceiling - 2.8f, .3f), false);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(end, "Blast door jamb", new Vector3(side * 1.47f, 1.4f, -.02f), new Vector3(.14f, 2.8f, .42f), darkMetal);
                Box(end, "Jamb hazard edge", new Vector3(side * 1.47f, 1.4f, -.235f), new Vector3(.14f, 2.8f, .01f), hazardStripe);
            }
            Box(end, "Blast door head", new Vector3(0, 2.86f, -.02f), new Vector3(3.1f, .14f, .42f), darkMetal);
            Box(end, "Threshold hazard strip", new Vector3(0, .003f, -.55f), new Vector3(2.8f, .006f, .3f), hazardStripe);
            door = Child(end, "Sliding archive door");
            Box(door, "Door leaf", new Vector3(0, 1.4f, -.02f), new Vector3(2.8f, 2.8f, .16f), steel);
            for (int i = 0; i < 4; i++) Box(door, "Door rib", new Vector3(0, .35f + i * .7f, -.105f), new Vector3(2.7f, .06f, .02f), darkMetal);
            Box(door, "Door centre seam", new Vector3(0, 1.4f, -.11f), new Vector3(.02f, 2.8f, .012f), darkMetal);
            Box(door, "Door status strip", new Vector3(0, 2.2f, -.118f), new Vector3(1.6f, .03f, .006f), laserHousing);
            var console = Child(root, "Archive control console"); console.position = new Vector3(4.33f, 1.32f, 57.9f); console.rotation = Quaternion.Euler(0, -90, 0);
            Box(console, "Console case", new Vector3(0, 0, .03f), new Vector3(.62f, 1.0f, .14f), enamel);
            Box(console, "Console bezel", new Vector3(0, .02f, -.045f), new Vector3(.54f, .9f, .02f), darkMetal);
            Box(console, "Terminal screen", new Vector3(0, .26f, -.057f), new Vector3(.42f, .26f, .004f), screenGraph);
            for (int i = 0; i < 4; i++) Box(console, "Empty code indicator", new Vector3(-.15f + i * .1f, .07f, -.058f), new Vector3(.06f, .03f, .004f), ledAmber);
            keypad = Child(console, "Keypad"); keypad.localPosition = new Vector3(0, -.2f, -.06f);
            for (int i = 0; i < 12; i++)
            {
                var key = Child(keypad, "Physical input button"); key.localPosition = new Vector3(-.12f + (i % 3) * .12f, .13f - (i / 3) * .085f, 0);
                Box(key, "Button cap", Vector3.zero, new Vector3(.095f, .065f, .018f), stainless);
                if (i < 9 || i == 10) WorldText(key, "Numeric key", i == 10 ? "0" : (i + 1).ToString(), new Vector3(0, 0, -.011f), .045f, .042f, new Color(.08f, .09f, .1f));
                else Box(key, "Function key mark", new Vector3(0, 0, -.011f), new Vector3(.04f, .012f, .002f), i == 9 ? ledAmber : ledGreen);
            }
            CardReader(console, new Vector3(.36f, .2f, -.02f), 180, recording);
        }

        static Transform ExitInterlock(Transform root)
        {
            var hardware = Child(root, "Fixed exit emitter hardware"); hardware.position = new Vector3(0, 0, VaultStart - .42f);
            var grid = Child(hardware, "Exit interlock"); grid.gameObject.layer = World;
            var barrier = grid.gameObject.AddComponent<BoxCollider>(); barrier.center = Vector3.up * 1.4f; barrier.size = new Vector3(2.8f, 2.8f, .10f);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(hardware, "Emitter column", new Vector3(side * 1.32f, 1.25f, 0), new Vector3(.12f, 2.5f, .12f), darkMetal);
                Box(hardware, "Emitter column cap", new Vector3(side * 1.32f, 2.52f, 0), new Vector3(.16f, .04f, .16f), steel);
                Box(hardware, "Emitter column foot", new Vector3(side * 1.32f, .03f, 0), new Vector3(.22f, .06f, .22f), steel);
                Box(hardware, "Emitter optical face", new Vector3(side * 1.255f, 1.25f, 0), new Vector3(.01f, 2.3f, .06f), rubber);
                for (int y = 0; y < 6; y++)
                    Primitive(hardware, side < 0 ? "Laser emitter port" : "Laser receiver port", PrimitiveType.Sphere, new Vector3(side * 1.25f, .3f + y * .4f, 0), new Vector3(.03f, .03f, .03f), laserHousing, false);
            }
            for (int y = 0; y < 6; y++) LocalLine(grid, "Emitter to receiver beam", new[] { new Vector3(-1.25f, .3f + y * .4f, 0), new Vector3(1.25f, .3f + y * .4f, 0) }, laser, .007f);
            return grid;
        }

        static void ArchiveVault(Transform parent, List<Light> lights)
        {
            var vault = Child(parent, "Protected archive vault");
            float minX = -2.5f, maxX = 2.5f, mid = (VaultStart + VaultEnd) * .5f, length = VaultEnd - VaultStart;
            var plate = Lit("Vault floor plate", new Color(.26f, .28f, .30f), .62f, .55f);
            Solid(vault, "Vault floor", new Vector3(0, -.1f, mid), new Vector3(maxX - minX + .4f, .2f, length), plate);
            Solid(vault, "Vault ceiling", new Vector3(0, Ceiling + .1f, mid), new Vector3(maxX - minX + .4f, .2f, length), darkMetal);
            for (int side = -1; side <= 1; side += 2)
            {
                Solid(vault, "Vault wall", new Vector3(side * (maxX + .1f), Ceiling * .5f, mid), new Vector3(.2f, Ceiling, length), steel);
                // Archive drawer banks along both walls.
                var bank = Placed(vault, "Archive drawer bank", new Vector3(side * (maxX - .25f), 0, mid + .3f), side < 0 ? 90 : -90);
                Solid(bank, "Drawer carcass", new Vector3(0, 1.2f, 0), new Vector3(length - 1.4f, 2.4f, .5f), darkMetal);
                for (int row = 0; row < 6; row++)
                    for (int col = 0; col < 6; col++)
                    {
                        float x = -(length - 1.4f) * .5f + .3f + col * .6f, y = .3f + row * .37f;
                        Box(bank, "Archive drawer", new Vector3(x, y, .255f), new Vector3(.56f, .33f, .012f), stainless);
                        Box(bank, "Drawer pull", new Vector3(x, y + .08f, .268f), new Vector3(.16f, .018f, .02f), darkMetal);
                        if ((row * 7 + col * 3 + side) % 5 == 0) Box(bank, "Drawer lock state", new Vector3(x + .2f, y + .08f, .264f), new Vector3(.02f, .02f, .004f), ledGreen);
                    }
                Box(bank, "Drawer bank light", new Vector3(0, 2.46f, .2f), new Vector3(length - 1.4f, .02f, .08f), ledBlue);
            }
            Solid(vault, "Vault rear wall", new Vector3(0, Ceiling * .5f, VaultEnd + .1f), new Vector3(maxX - minX + .4f, Ceiling, .2f), steel);
            Box(vault, "Rear light frame", new Vector3(0, 1.7f, VaultEnd - .01f), new Vector3(2.2f, 2.0f, .01f), darkMetal);
            Box(vault, "Rear light panel", new Vector3(0, 1.7f, VaultEnd - .02f), new Vector3(2.0f, 1.8f, .006f), ledBlue);
            for (int i = 0; i < 2; i++)
                Box(vault, "Vault ceiling light", new Vector3(0, Ceiling - .01f, VaultStart + 1.2f + i * 2.6f), new Vector3(1.4f, .01f, .3f), nightPanel);
            var pedestal = Placed(vault, "Archive pedestal", new Vector3(0, 0, 62.7f), 0);
            Cylinder(pedestal, "Floor ring", new Vector3(0, .004f, 0), .75f, .008f, ledBlue);
            Cylinder(pedestal, "Floor disc", new Vector3(0, .006f, 0), .7f, .008f, darkMetal);
            Cylinder(pedestal, "Pedestal column", new Vector3(0, .5f, 0), .3f, 1.0f, enamel);
            Cylinder(pedestal, "Pedestal band", new Vector3(0, .82f, 0), .305f, .03f, accentTeal);
            Cylinder(pedestal, "Pedestal top", new Vector3(0, 1.02f, 0), .36f, .04f, stainless);
            Box(pedestal, "Display case", new Vector3(0, 1.28f, 0), new Vector3(.46f, .46f, .46f), glass).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Box(pedestal, "Case frame", new Vector3(0, 1.512f, 0), new Vector3(.48f, .02f, .48f), stainless);
            Box(pedestal, "Case cradle", new Vector3(0, 1.07f, 0), new Vector3(.3f, .06f, .18f), darkMetal);
            Cylinder(pedestal, "Prototype sample cassette", new Vector3(0, 1.18f, 0), .045f, .26f, sampleGlow, new Vector3(0, 0, 90));
            Cylinder(pedestal, "Cassette end cap", new Vector3(-.15f, 1.18f, 0), .052f, .04f, stainless, new Vector3(0, 0, 90));
            Cylinder(pedestal, "Cassette end cap", new Vector3(.15f, 1.18f, 0), .052f, .04f, stainless, new Vector3(0, 0, 90));
            var spot = Child(pedestal, "Pedestal spotlight").gameObject.AddComponent<Light>();
            spot.transform.localPosition = new Vector3(0, Ceiling - .1f, 0); spot.transform.localRotation = Quaternion.Euler(90, 0, 0);
            spot.type = LightType.Spot; spot.spotAngle = 46; spot.innerSpotAngle = 26; spot.range = 5; spot.intensity = 16; spot.color = new Color(1, .9f, .78f); spot.shadows = LightShadows.None;
            var glow = Child(pedestal, "Cassette glow").gameObject.AddComponent<Light>();
            glow.transform.localPosition = new Vector3(0, 1.25f, 0);
            glow.type = LightType.Point; glow.range = 2.2f; glow.intensity = 1.1f; glow.color = new Color(.3f, 1f, .85f); glow.shadows = LightShadows.None;
            lights.Add(spot); lights.Add(glow);
            for (int i = 0; i < 2; i++)
            {
                var fill = Child(vault, "Vault fill").gameObject.AddComponent<Light>();
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
                    var fixture = Placed(parent, lit ? "Night light panel" : "Switched-off LED panel", new Vector3(cx + side * width * .24f, Ceiling - .004f, z), 0);
                    Box(fixture, "Panel trim", Vector3.zero, new Vector3(.66f, .008f, 1.26f), enamel).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    Box(fixture, "Panel diffuser", new Vector3(0, -.006f, 0), new Vector3(.6f, .006f, 1.2f), lit ? nightPanel : fixtureOff).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    if (!lit) continue;
                    var go = Child(fixture, "Panel light"); go.localPosition = new Vector3(0, -.06f, 0); go.localRotation = Quaternion.Euler(90, 0, 0);
                    var lamp = go.gameObject.AddComponent<Light>();
                    lamp.type = LightType.Spot; lamp.spotAngle = 140; lamp.innerSpotAngle = 60; lamp.range = 5.5f;
                    lamp.intensity = intensity; lamp.color = color; lamp.shadows = LightShadows.None;
                    lights.Add(lamp);
                }
            }
        }

        static void Lighting(Transform root, List<Light> lights)
        {
            var lighting = Child(root, "Laboratory night lighting");
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AnnexLighting.Sky; RenderSettings.ambientEquatorColor = AnnexLighting.Ambient; RenderSettings.ambientGroundColor = AnnexLighting.Ground;
            RenderSettings.ambientLight = AnnexLighting.Ambient;
            RenderSettings.ambientIntensity = 1; RenderSettings.reflectionIntensity = .2f;
            RenderSettings.ambientProbe = AnnexLighting.Probe();
            lighting.gameObject.AddComponent<AnnexLighting>();
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .028f;
            RenderSettings.fogColor = new Color(.006f, .008f, .011f);
            var terminal = Child(lighting, "Terminal spill"); terminal.position = new Vector3(3.9f, 1.6f, 57.9f);
            var glow = terminal.gameObject.AddComponent<Light>(); glow.type = LightType.Point; glow.range = 2.4f; glow.intensity = 1.2f; glow.color = new Color(.45f, .8f, .95f); lights.Add(glow);
        }

        // ------------------------------------------------------------ player
        static void CreatePlayer(Transform root, StealthGame game, Vector3 at)
        {
            var player = Child(root, "First person investigator").gameObject; player.tag = "Player"; player.layer = PlayerLayer; player.transform.position = at;
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
            var cameraGo = Child(target, "Player view").gameObject; cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>(); camera.fieldOfView = 68; camera.nearClipPlane = .03f; camera.farClipPlane = 90;
            camera.allowHDR = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.01f, .012f, .015f);
            var cameraData = cameraGo.AddComponent<UniversalAdditionalCameraData>(); cameraData.renderPostProcessing = true;
            cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; cameraData.antialiasingQuality = AntialiasingQuality.High;
            var muzzle = HeldFlashlight(cameraGo.transform);
            var torchRoot = Child(cameraGo.transform, "Inspection beam");
            var torch = torchRoot.gameObject.AddComponent<Light>(); torch.type = LightType.Spot; torch.range = 18;
            torch.spotAngle = 56; torch.innerSpotAngle = 24; torch.intensity = 32f;
            torch.color = new Color(1f, .93f, .80f); torch.shadows = LightShadows.Soft; torch.shadowBias = .015f; torch.shadowNormalBias = .06f; torch.shadowNearPlane = .03f;
            game.controller = controller; game.inputs = inputs; game.body = body; game.playerCamera = camera; game.torch = torch; game.torchOrigin = muzzle;
            var stance = player.AddComponent<StealthStance>(); stance.game = game; stance.controller = controller; stance.body = body; stance.cameraRoot = target; stance.obstructionMask = Obstructions; game.stance = stance;
            PlayerBody(player.transform, game);
        }

        static GameObject Limb(Transform parent, string name, Vector3 a, Vector3 b, float radius, Material material)
        {
            Vector3 delta = b - a;
            float length = delta.magnitude + radius * 2;
            var go = Primitive(parent, name, PrimitiveType.Capsule, (a + b) * .5f, new Vector3(radius * 2, length * .5f, radius * 2), material, false);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.sqrMagnitude > 1e-8f ? delta.normalized : Vector3.up);
            return go;
        }

        const string MannequinPath = "Assets/ThirdParty/Quaternius/Universal Animation Library[Standard]/Unity/UAL1_Standard.fbx";

        static void PlayerBody(Transform player, StealthGame game)
        {
            var body = Child(player, "Visible investigator body");
            var visual = body.gameObject.AddComponent<PlayerBodyVisual>(); visual.game = game;
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(MannequinPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source, body);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Quaternius mannequin body";
            model.transform.localScale = Vector3.one * .95f;
            var animator = model.GetComponent<Animator>(); if (!animator) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = null; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            visual.animator = animator; visual.model = model.transform;
            var clips = AssetDatabase.LoadAllAssetsAtPath(MannequinPath).OfType<AnimationClip>().ToArray();
            AnimationClip Clip(string name) => clips.First(c => c.name == "Armature|" + name);
            visual.idle = Clip("Idle_Loop"); visual.walk = Clip("Walk_Loop"); visual.sprint = Clip("Sprint_Loop");
            visual.crouchIdle = Clip("Crouch_Idle_Loop"); visual.crouchWalk = Clip("Crouch_Fwd_Loop");
            visual.idle.SampleAnimation(model, 0);
            var bones = model.GetComponentsInChildren<Transform>(true);
            visual.hiddenBones = new[] { "spine_02" }.Select(n => bones.First(t => t.name == n)).ToArray();
            foreach (var bone in visual.hiddenBones) bone.localScale = Vector3.one * .0001f;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skin.sharedMaterials = new[] { sleeve, trousers };
                skin.updateWhenOffscreen = true; skin.shadowCastingMode = ShadowCastingMode.Off;
            }
            SetLayer(body.gameObject, PlayerLayer);
            visual.ApplyPose(PlayerStance.Standing, false, 0);
        }

        static Transform HeldFlashlight(Transform camera)
        {
            var hand = Child(camera, "Held flashlight and gloved hand"); hand.localPosition = new Vector3(.16f, -.17f, .32f);
            hand.localRotation = Quaternion.Euler(-2, -3, 0);
            var torch = Child(hand, "Flashlight body");
            Cylinder(torch, "Anodised barrel", Vector3.zero, .018f, .13f, darkMetal, new Vector3(90, 0, 0));
            for (int i = 0; i < 7; i++) Cylinder(torch, "Knurled grip ring", new Vector3(0, 0, -.05f + i * .01f), .0186f, .0035f, steel, new Vector3(90, 0, 0));
            Cylinder(torch, "Head taper", new Vector3(0, 0, .074f), .021f, .02f, darkMetal, new Vector3(90, 0, 0));
            Cylinder(torch, "Lamp head", new Vector3(0, 0, .097f), .025f, .03f, darkMetal, new Vector3(90, 0, 0));
            for (int i = 0; i < 3; i++) Cylinder(torch, "Cooling fin", new Vector3(0, 0, .088f + i * .008f), .0262f, .003f, steel, new Vector3(90, 0, 0));
            Cylinder(torch, "Bezel ring", new Vector3(0, 0, .114f), .026f, .006f, stainless, new Vector3(90, 0, 0));
            var lens = Cylinder(torch, "Lamp glass", new Vector3(0, 0, .1175f), .021f, .002f, ink, new Vector3(90, 0, 0));
            Cylinder(torch, "Tail cap", new Vector3(0, 0, -.074f), .019f, .018f, rubber, new Vector3(90, 0, 0));
            Cylinder(torch, "Tail switch", new Vector3(0, 0, -.085f), .01f, .006f, rubber, new Vector3(90, 0, 0));
            Box(torch, "Pocket clip", new Vector3(-.019f, .004f, -.03f), new Vector3(.003f, .008f, .06f), stainless);
            // Overhand grip: back of the glove over the barrel, fingers wrapped beneath, thumb along the top.
            var backOfHand = Primitive(hand, "Glove back", PrimitiveType.Sphere, new Vector3(.022f, .012f, -.022f), new Vector3(.056f, .036f, .09f), glove, false);
            backOfHand.transform.localRotation = Quaternion.Euler(0, 0, -38);
            Primitive(hand, "Knuckle guard", PrimitiveType.Cube, new Vector3(.03f, .022f, -.006f), new Vector3(.04f, .012f, .05f), rubber, false)
                .transform.localRotation = Quaternion.Euler(0, 0, -38);
            for (int finger = 0; finger < 4; finger++)
            {
                float z = .018f - finger * .019f, r = finger == 3 ? .0088f : .0098f;
                var knuckle = new Vector3(.034f, .004f, z);
                var middle = new Vector3(.024f, -.026f, z - .002f);
                var tip = new Vector3(-.004f, -.026f, z - .004f);
                Primitive(hand, "Knuckle", PrimitiveType.Sphere, knuckle, Vector3.one * r * 2.3f, glove, false);
                Limb(hand, "Gloved finger", knuckle, middle, r, glove);
                Limb(hand, "Gloved fingertip", middle, tip, r * .92f, glove);
            }
            Limb(hand, "Gloved thumb", new Vector3(.012f, .024f, -.055f), new Vector3(-.006f, .022f, -.008f), .011f, glove);
            Limb(hand, "Glove wrist", new Vector3(.032f, .002f, -.07f), new Vector3(.052f, -.03f, -.12f), .03f, glove);
            Limb(hand, "Glove cuff", new Vector3(.05f, -.026f, -.115f), new Vector3(.062f, -.045f, -.15f), .034f, rubber);
            Limb(hand, "Jacket sleeve", new Vector3(.065f, -.05f, -.155f), new Vector3(.17f, -.18f, -.4f), .046f, sleeve);
            Limb(hand, "Sleeve hem", new Vector3(.063f, -.047f, -.15f), new Vector3(.068f, -.054f, -.162f), .049f, darkMetal);
            var muzzle = Child(hand, "Actual flashlight muzzle"); muzzle.localPosition = new Vector3(0, 0, .12f);
            muzzle.rotation = camera.rotation;
            var visual = hand.gameObject.AddComponent<HeldFlashlightVisual>(); visual.lampGlass = lens.GetComponent<Renderer>(); visual.poseRoot = hand;
            foreach (var r in hand.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
            SetLayer(hand.gameObject, PlayerLayer); return muzzle;
        }

        // ------------------------------------------------------------ CCTV
        static HazardDetector Scanner(Transform root, StealthGame game, DetectionShape shape, Vector3 at, Vector3 target, float angle, float range, float sweep, float period, float phase, Vector3 mount)
        {
            var rig = Child(root, "CCTV " + shape); rig.position = at; rig.gameObject.layer = ScannerLayer;
            var detector = rig.gameObject.AddComponent<HazardDetector>(); detector.shape = shape; detector.home = at; detector.lateralAmplitude = 0;
            detector.player = game.body.transform; detector.playerMask = 1 << PlayerLayer; detector.obstructionMask = Obstructions;
            detector.useViewCone = true; detector.viewAngle = angle; detector.viewRange = range;
            var pivot = Child(rig, "Camera pan axis");
            detector.panPivot = pivot; detector.panBaseEuler = Quaternion.LookRotation(target - at).eulerAngles;
            detector.panAmplitude = sweep; detector.panPeriod = period; detector.panPhase = phase;
            var origin = Child(pivot, "Optical lens origin"); detector.rayOrigin = origin;
            bool ceiling = mount.y > .2f && Mathf.Abs(mount.x) < .01f && Mathf.Abs(mount.z) < .01f;
            // Static mount: wall plate or ceiling plate, arm and pan motor.
            if (ceiling)
            {
                Cylinder(rig, "Drop rod", new Vector3(0, mount.y * .5f + .08f, 0), .022f, mount.y - .16f, steel);
                Cylinder(rig, "Ceiling plate", new Vector3(0, mount.y - .01f, 0), .09f, .02f, cameraDark);
            }
            else
            {
                Vector3 wall = mount, elbow = new Vector3(0, .2f, 0);
                var arm = Primitive(rig, "Mount arm", PrimitiveType.Cube, (wall + elbow) * .5f, new Vector3(.045f, .045f, Vector3.Distance(wall, elbow)), cameraDark, false);
                arm.transform.localRotation = Quaternion.LookRotation(elbow - wall);
                var plate = Primitive(rig, "Wall plate", PrimitiveType.Cube, wall, new Vector3(.14f, .2f, .02f), cameraDark, false);
                Vector3 normal = new Vector3(-mount.x, 0, -mount.z).normalized;
                plate.transform.localRotation = Quaternion.LookRotation(normal);
                Cylinder(rig, "Pan motor", new Vector3(0, .17f, 0), .05f, .06f, cameraDark);
            }
            Cylinder(rig, "Pan bearing", new Vector3(0, .13f, 0), .04f, .05f, steel);
            // Moving head: everything below shares the optical pivot, so the housing follows the light.
            float body = shape == DetectionShape.Box ? .64f : shape == DetectionShape.Capsule ? .56f : .44f;
            float girth = shape == DetectionShape.Box ? .3f : shape == DetectionShape.Capsule ? .29f : .24f;
            Box(pivot, "Tilt yoke", new Vector3(0, girth * .5f + .03f, -body * .4f), new Vector3(.05f, .07f, .06f), cameraDark);
            Box(pivot, "Camera housing", new Vector3(0, 0, -body * .5f + .02f), new Vector3(girth, girth * .86f, body), cameraShell);
            Box(pivot, "Sun shield", new Vector3(0, girth * .5f + .012f, -body * .45f + .05f), new Vector3(girth + .05f, .016f, body + .08f), cameraShell);
            Box(pivot, "Rear cap", new Vector3(0, 0, -body + .02f), new Vector3(girth + .01f, girth * .9f, .03f), cameraDark);
            Box(pivot, "Housing seam", new Vector3(0, -girth * .2f, -body * .5f + .02f), new Vector3(girth + .004f, .008f, body - .04f), cameraDark);
            if (shape == DetectionShape.Capsule)
                Box(pivot, "Drive band", new Vector3(0, 0, -body * .62f), new Vector3(girth + .012f, girth * .9f, .05f), cameraDark);
            float lensRadius = shape == DetectionShape.Box ? .09f : shape == DetectionShape.Capsule ? .085f : .072f;
            Cylinder(pivot, "Lens hood", new Vector3(0, 0, .04f), lensRadius + .016f, .06f, cameraDark, new Vector3(90, 0, 0));
            var optic = Cylinder(pivot, "Optical lens glass", new Vector3(0, 0, .071f), lensRadius, .004f, lensGlass, new Vector3(90, 0, 0));
            origin.localPosition = new Vector3(0, 0, .082f);
            var status = Primitive(pivot, "Recording diode", PrimitiveType.Sphere, new Vector3(girth * .36f, girth * .3f, .02f), Vector3.one * .026f, recording, false).GetComponent<Renderer>();
            var visual = rig.gameObject.AddComponent<ScannerVisual>(); visual.detector = detector; visual.game = game; visual.lineMaterial = line; visual.shaftMaterial = shaft;
            visual.lens = optic.GetComponent<Renderer>(); visual.statusLights = new[] { status };
            var search = Child(origin, "White surveillance searchlight");
            var lamp = search.gameObject.AddComponent<Light>(); lamp.type = LightType.Spot; lamp.range = range; lamp.spotAngle = angle; lamp.innerSpotAngle = angle * ScannerVisual.InnerAngle;
            lamp.color = new Color(.86f, .93f, 1f); lamp.intensity = Mathf.Lerp(150, 95, Mathf.InverseLerp(34, 68, angle)) * range * range / 100f;
            lamp.shadows = LightShadows.Soft; lamp.shadowBias = .015f; lamp.shadowNormalBias = .04f; lamp.shadowNearPlane = .05f;
            lamp.cullingMask = ~(1 << ScannerLayer); visual.lamp = lamp;
            var lampData = new SerializedObject(search.gameObject.AddComponent<UniversalAdditionalLightData>());
            lampData.FindProperty("m_AdditionalLightsShadowResolutionTier").intValue = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh;
            lampData.ApplyModifiedPropertiesWithoutUndo();
            foreach (var r in rig.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            SetLayer(rig.gameObject, ScannerLayer); detector.SetPatrolTime(0); return detector;
        }

        // ------------------------------------------------------------ clues
        /// <summary>style 0: wall service record, 1: clipped document on a desk backboard, 2: freezer controller panel.</summary>
        static HiddenClue Clue(Transform root, int index, Vector3 at, int side, int style)
        {
            var definition = PuzzleCatalog.Clues[index];
            var mount = Child(root, definition.title + " inspection record"); mount.position = at;
            mount.rotation = Quaternion.Euler(0, side < 0 ? -90 : 90, style == 1 ? -3 : 0);
            mount.localScale = Vector3.one * .38f;
            float w = style == 1 ? 1.2f : 1.38f, h = style == 1 ? 1.72f : 1.88f;
            if (style == 2)
            {
                Primitive(mount, "Cold store controller", PrimitiveType.Cube, new Vector3(0, -.4f, .3f), new Vector3(1.9f, 3.9f, .5f), enamel, false);
                Primitive(mount, "Controller trim", PrimitiveType.Cube, new Vector3(.9f, -.4f, .28f), new Vector3(.1f, 3.9f, .54f), accentIce, false);
                Primitive(mount, "Controller status", PrimitiveType.Cube, new Vector3(-.6f, 1.25f, -.02f), new Vector3(.18f, .07f, .02f), ledBlue, false);
            }
            Primitive(mount, style == 1 ? "Clipboard backing" : "Record holder", PrimitiveType.Cube, new Vector3(0, 0, .025f), new Vector3(w, h, .055f), style == 1 ? wood : stainless, false);
            Primitive(mount, "Record sheet", PrimitiveType.Cube, new Vector3(0, -.03f, -.01f), new Vector3(w - .12f, h - .18f, .02f), style == 1 ? paper : darkMetal, false);
            if (style == 1)
            {
                Primitive(mount, "Document clip", PrimitiveType.Cube, new Vector3(0, h * .5f - .03f, -.04f), new Vector3(.34f, .12f, .05f), stainless, false);
                for (int i = 0; i < 5; i++)
                    Primitive(mount, "Printed form line", PrimitiveType.Cube, new Vector3(0, -.62f + i * .07f, -.022f), new Vector3(w - .3f, .012f, .004f), plasticGrey, false);
            }
            else
                for (int bolt = -1; bolt <= 1; bolt += 2)
                    Primitive(mount, "Record holder fixing", PrimitiveType.Cylinder, new Vector3(bolt * (w * .5f - .05f), h * .5f - .05f, -.016f), new Vector3(.03f, .01f, .03f), stainless, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            Color inkColor = style == 1 ? new Color(.05f, .16f, .2f) : ink.color;
            var content = Child(mount, definition.title + " light responsive content"); content.localPosition = new Vector3(0, 0, -.075f);
            content.gameObject.layer = ClueLayer; DrawGlyph(content, index, new Vector3(0, .55f, 0), style == 1 ? binderBlue : ink);
            WorldText(content, "Glyph title", definition.title.ToUpperInvariant(), new Vector3(0, .27f, 0), style == 1 ? .95f : 1.1f, .12f, inkColor);
            WorldText(content, "Hidden digit", definition.digit, new Vector3(0, .035f, 0), .38f, .30f, inkColor);
            if (!string.IsNullOrEmpty(definition.relation))
            {
                var lines = new List<string>(); string current = "";
                foreach (string word in definition.relation.ToUpperInvariant().TrimEnd('.').Split(' '))
                {
                    if (current.Length + word.Length > 21 && current.Length > 0) { lines.Add(current); current = ""; }
                    current += (current.Length == 0 ? "" : " ") + word;
                }
                if (current.Length > 0) lines.Add(current);
                for (int i = 0; i < lines.Count; i++) WorldText(content, "Hidden statement", lines[i], new Vector3(0, -.32f - i * .14f, 0), 1.14f, .10f, inkColor);
            }
            var clue = mount.gameObject.AddComponent<HiddenClue>(); clue.clueIndex = index; clue.anchor = content;
            clue.hiddenRenderers = content.GetComponentsInChildren<Renderer>(true); clue.obstructionMask = Obstructions;
            foreach (var renderer in clue.hiddenRenderers) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.enabled = false; }
            foreach (var renderer in mount.GetComponentsInChildren<Renderer>(true)) renderer.shadowCastingMode = ShadowCastingMode.Off;
            SetLayer(content.gameObject, ClueLayer); return clue;
        }

        static void DrawGlyph(Transform root, int index, Vector3 at, Material material)
        {
            var glyph = Child(root, "Hidden pictogram"); glyph.localPosition = at;
            if (index == 0)
            {
                var eye = new Vector3[49];
                for (int i = 0; i < eye.Length; i++) { float a = i * Mathf.PI * 2 / (eye.Length - 1); eye[i] = new Vector3(Mathf.Cos(a) * .22f, Mathf.Sin(a) * .105f, 0); }
                LocalLine(glyph, "Eye contour", eye, material, .013f);
                var pupil = new Vector3[33];
                for (int i = 0; i < pupil.Length; i++) { float a = i * Mathf.PI * 2 / (pupil.Length - 1); pupil[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * .052f; }
                LocalLine(glyph, "Eye pupil", pupil, material, .014f);
            }
            else if (index == 1)
            {
                var moon = new Vector3[65];
                for (int i = 0; i <= 32; i++)
                {
                    float a = Mathf.PI * .25f + i * Mathf.PI * 1.5f / 32;
                    moon[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * .15f;
                }
                float tip = .15f / Mathf.Sqrt(2), offset = .065f;
                float innerRadius = Mathf.Sqrt((tip - offset) * (tip - offset) + tip * tip);
                float innerAngle = Mathf.Atan2(tip, tip - offset);
                for (int i = 1; i <= 32; i++)
                {
                    float a = Mathf.Lerp(-innerAngle, -2 * Mathf.PI + innerAngle, i / 32f);
                    moon[32 + i] = new Vector3(offset + Mathf.Cos(a) * innerRadius, Mathf.Sin(a) * innerRadius, 0);
                }
                LocalLine(glyph, "Crescent contour", moon, material, .013f);
            }
            else if (index == 2)
            {
                LocalLine(glyph, "Gate frame", new[] { new Vector3(-.18f, -.13f, 0), new Vector3(-.18f, .14f, 0), new Vector3(.18f, .14f, 0), new Vector3(.18f, -.13f, 0) }, material, .014f);
                for (int i = -1; i <= 1; i++) LocalLine(glyph, "Gate bar", new[] { new Vector3(i * .085f, -.13f, 0), new Vector3(i * .085f, .14f, 0) }, material, .011f);
            }
            else
                LocalLine(glyph, "Crown contour", new[] { new Vector3(-.19f, .09f, 0), new Vector3(-.12f, -.12f, 0), new Vector3(.12f, -.12f, 0), new Vector3(.19f, .09f, 0), new Vector3(.08f, .015f, 0), new Vector3(0, .15f, 0), new Vector3(-.08f, .015f, 0), new Vector3(-.19f, .09f, 0) }, material, .014f);
        }

        static void WorldText(Transform root, string name, string value, Vector3 at, float width, float height, Color color)
        {
            var go = Child(root, name).gameObject; go.transform.localPosition = at;
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
        static GameObject Solid(Transform root, string name, Vector3 at, Vector3 size, Material material, bool visible = true)
        {
            var go = Primitive(root, name, PrimitiveType.Cube, at, size, material, true); go.layer = World;
            go.GetComponent<Renderer>().enabled = visible; return go;
        }
        static GameObject Primitive(Transform parent, string name, PrimitiveType type, Vector3 at, Vector3 size, Material material, bool solid)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
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
