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
using UnityEngine.SceneManagement;

namespace RayOEZoning.Editor
{
    /// <summary>Authors the new level through Editor APIs; the source scene and art stay intact.</summary>
    public static class ZoningBuilder
    {
        public const string ScenePath = "Assets/RayOEZoning/Scenes/RayOEZoning.unity";
        const string SourcePath = "Assets/LaserCorridor/Scenes/LaserCorridor.unity";
        const int World = 8, PlayerLayer = 9, ClueLayer = 10, ScannerLayer = 11;
        const int Obstructions = (1 << World) | 1;
        static Material steel, dark, cyan, amber, line, ink, pale, navy, floor, black, label, cameraEnamel, serverFace;
        static TMP_FontAsset font;

        [MenuItem("Ray-OE Zoning/Create level")]
        public static void Create()
        {
            Directory.CreateDirectory("Assets/RayOEZoning/Scenes");
            Directory.CreateDirectory("Assets/RayOEZoning/Art");
            AssetDatabase.Refresh();
            SetLayers();
            DisableProjectAudio();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            steel = Existing("Brushed aluminium");
            dark = Material("Graphite service metal", new Color(.055f, .073f, .092f), false);
            navy = Material("Navy structural steel", new Color(.075f, .12f, .17f), false);
            pale = Material("Pale equipment enamel", new Color(.53f, .60f, .61f), false);
            cameraEnamel = Material("CCTV housing enamel", new Color(.68f, .76f, .76f), false);
            serverFace = Material("Server blade blue gray", new Color(.18f, .25f, .30f), false);
            black = Material("Optical glass and vents", new Color(.012f, .021f, .027f), false);
            EmissionFloor(dark, new Color(.006f, .010f, .015f));
            EmissionFloor(navy, new Color(.006f, .011f, .018f));
            EmissionFloor(pale, new Color(.011f, .017f, .021f));
            EmissionFloor(cameraEnamel, new Color(.047f, .065f, .073f));
            EmissionFloor(serverFace, new Color(.012f, .020f, .028f));
            floor = FloorMaterial();
            cyan = Material("Annex cyan", new Color(.19f, .65f, .70f), true);
            amber = Material("Scanner amber", new Color(.95f, .48f, .12f), true);
            ink = Material("Maintenance stencil", new Color(.85f, .92f, .83f), true);
            label = Material("Equipment lettering", new Color(.70f, .79f, .79f), true);
            line = Material("Scanning outline", new Color(.86f, .55f, .19f, .28f), true, true);

            var source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
            var sourceRoot = source.GetRootGameObjects().First(x => x.name == "Laser Corridor");
            var sourceEnvironment = sourceRoot.transform.Find("Environment");
            if (!sourceEnvironment) throw new InvalidOperationException("Source corridor Environment is missing.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("Ray-OE Zoning");
            var architecture = Child(root.transform, "Security annex architecture");
            Architecture(architecture);
            var exitSource = sourceEnvironment.Find("Exit Door");
            if (!exitSource) throw new InvalidOperationException("Source exit assembly is missing.");
            var exitAssembly = UnityEngine.Object.Instantiate(exitSource.gameObject, architecture, false);
            exitAssembly.name = "Archive blast door"; exitAssembly.transform.position += Vector3.forward * 14;
            StripLegacy(exitAssembly); SetLayer(exitAssembly, World);
            Transform door = exitAssembly.transform.Find("Door");
            Transform keypad = exitAssembly.transform.Find("Keypad");
            if (keypad)
            {
                keypad.position = new Vector3(1.78f, 1.25f, 26.4f);
                keypad.rotation = Quaternion.Euler(0, 90, 0);
            }
            else keypad = Child(root.transform, "Archive terminal");
            ArchiveTerminal(architecture, keypad);

            // New simple collision shell matches the retained visible architecture and its extension.
            var collision = Child(root.transform, "Solid world");
            Solid(collision, "Floor collision", new Vector3(0, -.15f, 13.5f), new Vector3(4.2f, .28f, 30), null, false);
            Solid(collision, "Left wall collision", new Vector3(-2.10f, 2.1f, 13.5f), new Vector3(.2f, 4.2f, 30), null, false);
            Solid(collision, "Right wall collision", new Vector3(2.10f, 2.1f, 13.5f), new Vector3(.2f, 4.2f, 30), null, false);
            Solid(collision, "Ceiling collision", new Vector3(0, 4.3f, 13.5f), new Vector3(4.2f, .2f, 30), null, false);
            Solid(collision, "Entrance collision", new Vector3(0, 2.1f, -.85f), new Vector3(4.2f, 4.2f, .2f), null, false);
            var cover = Child(root.transform, "Server cabinet cover");
            Cover(cover, -1, 3.3f, "01"); Cover(cover, 1, 9.3f, "02");
            Cover(cover, 1, 10.7f, "03"); Cover(cover, -1, 17.2f, "04");
            Cover(cover, -1, 18.5f, "05"); Cover(cover, 1, 22.3f, "06");

            var session = Child(root.transform, "Archive infiltration mission").gameObject;
            var game = session.AddComponent<StealthGame>();
            game.exposureSeconds = 1.35f; game.decaySeconds = 1.5f; game.escapeZ = 27.5f;
            var spawn = Child(root.transform, "Entrance spawn"); spawn.position = new Vector3(0, .06f, .7f); game.spawn = spawn;
            CreatePlayer(root.transform, game, spawn.position);
            game.keypad = keypad;

            var scanners = Child(root.transform, "Rail surveillance cameras");
            var sphere = Scanner(scanners, game, "CAM 01 / PERIMETER", DetectionShape.Sphere, new Vector3(0, 1.1f, 6.5f), 8);
            sphere.radius = 1.55f;
            var box = Scanner(scanners, game, "CAM 02 / SIGNAL", DetectionShape.Box, new Vector3(0, 1.2f, 14), 9);
            box.halfExtents = new Vector3(1.3f, 1.2f, 2);
            var capsule = Scanner(scanners, game, "CAM 03 / RECORDS", DetectionShape.Capsule, new Vector3(0, 1.1f, 21.8f), 10);
            capsule.radius = .9f; capsule.capsulePointA = new Vector3(0, 0, -2); capsule.capsulePointB = new Vector3(0, 0, 2);
            game.hazards = new[] { sphere, box, capsule };

            var markings = Child(root.transform, "Hidden maintenance stencils");
            game.clues = new[] {
                Clue(markings, 0, -1, 2.5f), Clue(markings, 1, 1, 8.5f),
                Clue(markings, 2, -1, 15.5f), Clue(markings, 3, 1, 23.2f)
            };
            var grid = Child(root.transform, "Blue exit lattice"); grid.position = new Vector3(0, 0, 27.05f);
            grid.gameObject.layer = World;
            var gridCollider = grid.gameObject.AddComponent<BoxCollider>(); gridCollider.center = new Vector3(0, 1.8f, 0); gridCollider.size = new Vector3(4, 3.6f, .10f);
            for (int y = 0; y < 6; y++) LocalLine(grid, "Archive security bar", new[] { new Vector3(-1.92f, .20f + y * .53f, 0), new Vector3(1.92f, .20f + y * .53f, 0) }, cyan, .009f);
            var danger = sourceEnvironment.Find("Fixtures/Danger stripe");
            if (danger)
            {
                var stripe = UnityEngine.Object.Instantiate(danger.gameObject, architecture, false);
                stripe.name = "Retained exit danger stripe"; stripe.transform.position = new Vector3(0, .008f, 26.8f); StripLegacy(stripe);
            }

            var lights = Lighting(root.transform);
            var presentation = session.AddComponent<StealthPresentation>();
            presentation.game = game; presentation.exitGrid = grid.gameObject; presentation.door = door;
            presentation.font = font; presentation.corridorLights = lights;
            var surfaces = exitAssembly.AddComponent<AnnexSurfaces>();
            surfaces.targets = exitAssembly.GetComponentsInChildren<Renderer>(true);
            surfaces.Apply();

            // Clear, visible controls at the entrance are ordinary world signage, never puzzle clues.
            var entry = Child(root.transform, "Entry airlock identity"); entry.position = new Vector3(0, 2.55f, -.72f);
            entry.rotation = Quaternion.Euler(0, 180, 0);
            WorldText(entry, "Annex identity", "SECURITY ANNEX", new Vector3(0, .27f, 0), 2.8f, .20f, cyan.color);
            WorldText(entry, "Annex subtitle", "RAY-OE / SERVICE ENTRY", Vector3.zero, 2.1f, .13f, new Color(.6f, .73f, .73f));
            EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Ray-OE Zoning";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("Security annex authored: three CCTV sectors, server cover, four maintenance stencils and protected archive terminal. " + ScenePath);
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

        static void Architecture(Transform root)
        {
            var shell = Child(root, "Graphite service shell");
            Primitive(shell, "Service floor underlay", PrimitiveType.Cube, new Vector3(0, -.07f, 13.5f), new Vector3(4.15f, .10f, 30), dark, false);
            Primitive(shell, "Solid ceiling", PrimitiveType.Cube, new Vector3(0, 4.21f, 13.5f), new Vector3(4.2f, .12f, 30), navy, false);
            Primitive(shell, "Entry airlock wall", PrimitiveType.Cube, new Vector3(0, 2.1f, -.88f), new Vector3(4.2f, 4.2f, .15f), navy, false);
            for (int side = -1; side <= 1; side += 2)
            {
                Primitive(shell, "Service wall backing", PrimitiveType.Cube, new Vector3(side * 2.08f, 2.1f, 13.5f), new Vector3(.16f, 4.2f, 30), navy, false);
                Primitive(shell, "Graphite skirting", PrimitiveType.Cube, new Vector3(side * 1.99f, .22f, 13.5f), new Vector3(.035f, .42f, 29), dark, false);
                Primitive(shell, "Cyan wayfinding strip", PrimitiveType.Cube, new Vector3(side * 1.965f, .115f, 13.5f), new Vector3(.012f, .013f, 28), cyan, false);
                Primitive(shell, "Upper cable trunk", PrimitiveType.Cube, new Vector3(side * 1.87f, 3.75f, 13.5f), new Vector3(.24f, .30f, 29), dark, false);
                for (int j = 0; j < 7; j++)
                {
                    float z = 1.65f + j * 4;
                    Primitive(shell, "Pale wall infill " + side + "/" + j, PrimitiveType.Cube,
                        new Vector3(side * 1.995f, 1.89f, z), new Vector3(.028f, 2.88f, 3.86f), pale, false);
                    Primitive(shell, "Panel service seam", PrimitiveType.Cube,
                        new Vector3(side * 1.973f, .65f, z), new Vector3(.012f, .023f, 3.72f), navy, false);
                    Primitive(shell, "Navy structural jamb", PrimitiveType.Cube,
                        new Vector3(side * 1.98f, 2.1f, z + 1.96f), new Vector3(.065f, 4.12f, .09f), navy, false);
                }
            }
            var deck = Child(root, "Large service floor plates");
            for (int row = 0; row < 15; row++)
                for (int column = 0; column < 4; column++)
                    Primitive(deck, "Deck plate " + row + "/" + column, PrimitiveType.Cube,
                        new Vector3(-1.5f + column, -.004f, -.5f + row * 2), new Vector3(.985f, .018f, 1.985f), floor, false);
            for (int i = 0; i < 8; i++)
            {
                float z = -.35f + i * 4;
                Primitive(shell, "Ceiling cross member", PrimitiveType.Cube, new Vector3(0, 4.07f, z), new Vector3(3.9f, .16f, .13f), dark, false);
                Primitive(shell, "Ceiling light aperture", PrimitiveType.Cube, new Vector3(0, 4.09f, z + 1.8f), new Vector3(1.6f, .035f, .12f), label, false);
            }
            SectorPortal(root, "01", "PERIMETER", "ENTRY SURVEILLANCE", 3.8f);
            SectorPortal(root, "02", "SIGNAL", "SURVEILLANCE PROCESSING", 10.15f);
            SectorPortal(root, "03", "RECORDS", "ARCHIVE APPROACH", 18.1f);
            SectorPortal(root, "A", "ARCHIVE", "MAINTENANCE ACCESS ONLY", 25.55f);
            WallSign(root, -1, 5.25f, "01", "PERIMETER", "PANORAMIC CCTV / ACTIVE");
            WallSign(root, 1, 12.6f, "02", "SIGNAL", "CAMERA NETWORK / ACTIVE");
            WallSign(root, -1, 20.1f, "03", "RECORDS", "SECURE RECORDS / ACTIVE");
            var entry = Child(root, "Entry mission placard"); entry.position = new Vector3(1.94f, 2.25f, 1.25f); entry.rotation = Quaternion.Euler(0, 90, 0);
            WorldText(entry, "Entry mission identity", "SECURITY ANNEX", new Vector3(0, .14f, -.03f), 1.18f, .13f, new Color(.12f, .20f, .25f));
            WorldText(entry, "Entry mission direction", "ARCHIVE TERMINAL  /  AHEAD", new Vector3(0, -.10f, -.03f), 1.18f, .063f, new Color(.17f, .26f, .30f));
            WorldText(entry, "Entry camera warning", "AMBER ZONES ARE UNDER CCTV", new Vector3(0, -.25f, -.03f), 1.18f, .052f, new Color(.17f, .26f, .30f));
            foreach (float z in new[] { 1.6f, 9.8f, 17.8f, 25.4f })
            {
                var arrow = Child(root, "Archive route chevron"); arrow.position = new Vector3(0, .012f, z);
                LocalLine(arrow, "Route direction", new[] { new Vector3(-.12f, 0, -.10f), new Vector3(0, 0, .04f), new Vector3(.12f, 0, -.10f) }, cyan, .016f);
            }
        }

        static void SectorPortal(Transform root, string number, string title, string subtitle, float z)
        {
            var portal = Child(root, "Sector " + number + " threshold"); portal.position = new Vector3(0, 0, z);
            Primitive(portal, "Sector lintel", PrimitiveType.Cube, new Vector3(0, 3.44f, 0), new Vector3(3.98f, .62f, .16f), navy, false);
            Primitive(portal, "Sector cyan datum", PrimitiveType.Cube, new Vector3(0, 3.12f, -.09f), new Vector3(3.82f, .018f, .02f), cyan, false);
            for (int side = -1; side <= 1; side += 2)
                Primitive(portal, "Threshold jamb", PrimitiveType.Cube, new Vector3(side * 1.99f, 1.78f, 0), new Vector3(.075f, 3.55f, .16f), navy, false);
            WorldText(portal, "Sector identity", number + "  /  " + title, new Vector3(0, 3.50f, -.105f), 2.75f, .20f, label.color);
            WorldText(portal, "Sector service caption", subtitle, new Vector3(0, 3.25f, -.105f), 2.65f, .060f, cyan.color);
        }

        static void WallSign(Transform root, int side, float z, string number, string title, string subtitle)
        {
            var sign = Child(root, "Sector " + number + " wall identity"); sign.position = new Vector3(side * 1.94f, 2.55f, z);
            sign.rotation = Quaternion.Euler(0, side < 0 ? -90 : 90, 0);
            WorldText(sign, "Sector number", "SECTOR " + number, new Vector3(0, .22f, -.02f), 1.25f, .18f, new Color(.11f, .20f, .26f));
            WorldText(sign, "Sector name", title, new Vector3(0, -.02f, -.02f), 1.25f, .12f, new Color(.11f, .20f, .26f));
            WorldText(sign, "Surveillance type", subtitle, new Vector3(0, -.22f, -.02f), 1.28f, .050f, new Color(.17f, .27f, .31f));
        }

        static void ArchiveTerminal(Transform root, Transform keypad)
        {
            var console = Child(root, "Protected archive control console"); console.position = new Vector3(1.89f, 1.43f, 26.4f); console.rotation = Quaternion.Euler(0, 90, 0);
            Primitive(console, "Console pale case", PrimitiveType.Cube, Vector3.zero, new Vector3(.96f, 1.65f, .16f), pale, false);
            Primitive(console, "Console dark inset", PrimitiveType.Cube, new Vector3(0, .03f, -.095f), new Vector3(.81f, 1.43f, .045f), dark, false);
            Primitive(console, "Terminal glass", PrimitiveType.Cube, new Vector3(0, .39f, -.124f), new Vector3(.66f, .36f, .015f), black, false);
            LocalLine(console, "Terminal cyan edge", new[] { new Vector3(-.34f, .58f, -.14f), new Vector3(.34f, .58f, -.14f) }, cyan, .012f);
            WorldText(console, "Terminal identity", "ARCHIVE", new Vector3(0, .45f, -.15f), .58f, .075f, cyan.color);
            WorldText(console, "Terminal locked state", "MAINTENANCE OVERRIDE", new Vector3(0, .31f, -.15f), .62f, .038f, label.color);
            WorldText(console, "Terminal instruction", "4 DIGITS / AUTHORIZATION", new Vector3(0, -.48f, -.15f), .67f, .044f, label.color);
            WorldText(console, "Terminal zone", "PROTECTED SERVICE TERMINAL", new Vector3(0, -.64f, -.15f), .69f, .030f, cyan.color);
            // Keep the actual legacy keypad mesh as the console's physical input device.
            keypad.position = new Vector3(1.74f, 1.25f, 26.4f); keypad.rotation = Quaternion.Euler(0, 90, 0);
        }

        static Material FloorMaterial()
        {
            const string path = "Assets/RayOEZoning/Art/Annex service deck.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                var original = Existing("Steel floor plate");
                material = original ? new Material(original) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.name = "Annex service deck"; AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", new Color(.13f, .18f, .22f)); material.SetColor("_EmissionColor", Color.black);
            material.SetFloat("_Metallic", .25f); material.SetFloat("_Smoothness", .3f); EditorUtility.SetDirty(material); return material;
        }

        static void CreatePlayer(Transform root, StealthGame game, Vector3 at)
        {
            var player = Child(root, "First person investigator").gameObject; player.tag = "Player"; player.layer = PlayerLayer; player.transform.position = at;
            var body = player.AddComponent<CharacterController>(); body.height = 1.8f; body.radius = .5f; body.center = Vector3.up * .9f;
            body.skinWidth = .025f; body.stepOffset = .15f; body.minMoveDistance = 0;
            var inputs = player.AddComponent<StarterAssetsInputs>();
            var input = player.AddComponent<PlayerInput>();
            input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions");
            input.defaultActionMap = "Player"; input.defaultControlScheme = "KeyboardMouse";
            input.notificationBehavior = PlayerNotifications.SendMessages;
            var controller = player.AddComponent<FirstPersonController>(); controller.MoveSpeed = controller.SprintSpeed = 2.2f;
            controller.JumpHeight = 0; controller.SpeedChangeRate = 12; controller.GroundedRadius = .28f;
            controller.GroundLayers = 1 << World; controller.RotationSpeed = .72f; controller.TopClamp = 82; controller.BottomClamp = -82;
            var target = Child(player.transform, "PlayerCameraRoot"); target.localPosition = Vector3.up * 1.65f;
            controller.CinemachineCameraTarget = target.gameObject;
            var cameraGo = Child(target, "Player view").gameObject; cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>(); camera.fieldOfView = 68; camera.nearClipPlane = .035f; camera.farClipPlane = 65;
            camera.allowHDR = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .04f, .05f);
            var cameraData = cameraGo.AddComponent<UniversalAdditionalCameraData>(); cameraData.renderPostProcessing = false;
            var torchRoot = Child(cameraGo.transform, "Inspection flashlight"); torchRoot.localPosition = new Vector3(.17f, -.13f, .12f);
            var torch = torchRoot.gameObject.AddComponent<Light>(); torch.type = LightType.Spot; torch.range = 8;
            torch.spotAngle = 50; torch.innerSpotAngle = 34; torch.intensity = 4.2f;
            torch.color = new Color(.88f, .94f, .80f); torch.shadows = LightShadows.Soft; torch.shadowBias = .015f; torch.shadowNormalBias = .08f;
            torch.shadowNearPlane = .04f;
            game.controller = controller; game.inputs = inputs; game.body = body; game.playerCamera = camera; game.torch = torch;
        }

        static HazardDetector Scanner(Transform root, StealthGame game, string name, DetectionShape shape, Vector3 home, float period)
        {
            var rig = Child(root, name); rig.position = home; rig.gameObject.layer = ScannerLayer;
            var detector = rig.gameObject.AddComponent<HazardDetector>(); detector.shape = shape; detector.home = home;
            detector.lateralAmplitude = 1.15f; detector.period = period; detector.phase = shape == DetectionShape.Box ? 1.8f : 0;
            detector.player = game.body.transform; detector.playerMask = 1 << PlayerLayer; detector.obstructionMask = Obstructions;
            var camera = Child(rig, "Panoramic CCTV assembly"); camera.localPosition = new Vector3(0, 2.95f - home.y, 0);
            Primitive(camera, "Pale panoramic housing", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.57f, .055f, .57f), cameraEnamel, false);
            Primitive(camera, "Graphite optical collar", PrimitiveType.Cylinder, new Vector3(0, -.062f, 0), new Vector3(.46f, .025f, .46f), dark, false);
            Primitive(camera, "Smoked optical dome", PrimitiveType.Sphere, new Vector3(0, -.105f, 0), new Vector3(.40f, .27f, .40f), black, false);
            var lensOrigin = Child(camera, "Panoramic optical origin"); lensOrigin.localPosition = new Vector3(0, -.10f, 0);
            detector.rayOrigin = lensOrigin;
            Renderer status = null;
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2 / 3;
                var diode = Primitive(camera, "Panoramic status optic", PrimitiveType.Sphere,
                    new Vector3(Mathf.Sin(a) * .19f, -.10f, Mathf.Cos(a) * .19f), Vector3.one * .047f, amber, false);
                if (status == null) status = diode.GetComponent<Renderer>();
            }
            Primitive(camera, "Camera suspension stem", PrimitiveType.Cylinder, new Vector3(0, .37f, 0), new Vector3(.065f, .32f, .065f), steel, false);
            Primitive(camera, "Rail carriage", PrimitiveType.Cube, new Vector3(0, .73f, 0), new Vector3(.38f, .13f, .30f), cameraEnamel, false);
            var identity = Child(camera, "Camera equipment label"); identity.localPosition = new Vector3(0, .73f, -.157f);
            WorldText(identity, "Camera ID", "CCTV " + (shape == DetectionShape.Sphere ? "01" : shape == DetectionShape.Box ? "02" : "03"), Vector3.zero, .29f, .055f, new Color(.09f, .17f, .23f));
            var lightRoot = Child(camera, "Amber zone illumination"); lightRoot.localPosition = new Vector3(0, -.11f, 0);
            var lamp = lightRoot.gameObject.AddComponent<Light>(); lamp.type = LightType.Point; lamp.color = new Color(1, .52f, .15f); lamp.intensity = .15f; lamp.range = 3.1f;
            var visual = rig.gameObject.AddComponent<ScannerVisual>(); visual.detector = detector; visual.game = game; visual.lineMaterial = line; visual.lamp = lamp; visual.lens = status;
            visual.statusLights = camera.GetComponentsInChildren<Renderer>().Where(r => r.name == "Panoramic status optic").ToArray();
            SetLayer(camera.gameObject, ScannerLayer);
            var rail = Child(root, "Rail " + name); rail.position = new Vector3(0, 3.74f, home.z);
            Primitive(rail, "Fixed camera rail", PrimitiveType.Cube, Vector3.zero, new Vector3(3.65f, .10f, .11f), dark, false);
            for (int side = -1; side <= 1; side += 2)
                Primitive(rail, "Ceiling rail mounting", PrimitiveType.Cube, new Vector3(side * 1.65f, .20f, 0), new Vector3(.11f, .40f, .17f), steel, false);
            return detector;
        }

        static HiddenClue Clue(Transform root, int index, int side, float z)
        {
            var definition = PuzzleCatalog.Clues[index];
            // The retained wall's raised ribs project into the room. Mount the complete plaque
            // in front of them, rather than letting ribs cut across its pictogram and sentence.
            var mount = Child(root, definition.title + " marking mount"); mount.position = new Vector3(side * 1.80f, 1.55f, z);
            mount.rotation = Quaternion.Euler(0, side < 0 ? -90 : 90, 0);
            Primitive(mount, "Pale maintenance hatch", PrimitiveType.Cube, new Vector3(0, 0, .025f), new Vector3(1.30f, 1.61f, .065f), pale, false).layer = World;
            Primitive(mount, "Recessed stencil field", PrimitiveType.Cube, new Vector3(0, -.06f, -.017f), new Vector3(1.18f, 1.18f, .023f), dark, false).layer = World;
            Primitive(mount, "Hatch mounting depth", PrimitiveType.Cube, new Vector3(0, 0, .105f), new Vector3(1.07f, 1.43f, .15f), navy, false).layer = World;
            WorldText(mount, "Maintenance hatch label", "MAINTENANCE / UV STENCIL", new Vector3(0, .655f, -.021f), 1.08f, .045f, new Color(.10f, .18f, .23f));
            for (int sideBolt = -1; sideBolt <= 1; sideBolt += 2)
                Primitive(mount, "Hatch fixing", PrimitiveType.Cube, new Vector3(sideBolt * .59f, .73f, -.014f), new Vector3(.024f, .024f, .012f), navy, false);
            // The hidden content projects away from its ordinary mounting surface toward the room.
            var content = Child(mount, definition.title + " light responsive content"); content.localPosition = new Vector3(0, 0, -.075f);
            content.gameObject.layer = ClueLayer;
            DrawGlyph(content, index, new Vector3(0, .34f, 0));
            WorldText(content, "Glyph title", definition.title.ToUpperInvariant(), new Vector3(0, .07f, 0), 1.05f, .11f, ink.color);
            WorldText(content, "Hidden digit", definition.digit, new Vector3(0, -.12f, 0), .34f, .27f, ink.color);
            string relation = definition.relation;
            if (!string.IsNullOrEmpty(relation))
            {
                string[] words = relation.ToUpperInvariant().TrimEnd('.').Split(' ');
                var lines = new List<string>(); string current = "";
                foreach (string word in words)
                {
                    if (current.Length + word.Length > 23 && current.Length > 0) { lines.Add(current); current = ""; }
                    current += (current.Length == 0 ? "" : " ") + word;
                }
                if (current.Length > 0) lines.Add(current);
                for (int i = 0; i < lines.Count; i++) WorldText(content, "Hidden statement " + i, lines[i], new Vector3(0, -.38f - i * .092f, 0), 1.16f, .075f, ink.color);
            }
            var clue = mount.gameObject.AddComponent<HiddenClue>(); clue.clueIndex = index; clue.anchor = content;
            clue.hiddenRenderers = content.GetComponentsInChildren<Renderer>(true); clue.obstructionMask = Obstructions;
            foreach (var renderer in clue.hiddenRenderers) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.enabled = false; }
            SetLayer(content.gameObject, ClueLayer);
            return clue;
        }

        static void DrawGlyph(Transform root, int index, Vector3 at)
        {
            var glyph = Child(root, "Hidden pictogram"); glyph.localPosition = at;
            if (index == 0)
            {
                var eye = new Vector3[49];
                for (int i = 0; i < eye.Length; i++) { float a = i * Mathf.PI * 2 / (eye.Length - 1); eye[i] = new Vector3(Mathf.Cos(a) * .22f, Mathf.Sin(a) * .105f, 0); }
                LocalLine(glyph, "Eye contour", eye, ink, .013f); CircleGlyph(glyph, .052f);
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
                LocalLine(glyph, "Crescent contour", moon, ink, .013f);
            }
            else if (index == 2)
            {
                LocalLine(glyph, "Gate frame", new[] { new Vector3(-.18f, -.13f, 0), new Vector3(-.18f, .14f, 0), new Vector3(.18f, .14f, 0), new Vector3(.18f, -.13f, 0) }, ink, .014f);
                for (int i = -1; i <= 1; i++) LocalLine(glyph, "Gate bar", new[] { new Vector3(i * .085f, -.13f, 0), new Vector3(i * .085f, .14f, 0) }, ink, .011f);
            }
            else
                LocalLine(glyph, "Crown contour", new[] { new Vector3(-.19f, .09f, 0), new Vector3(-.12f, -.12f, 0), new Vector3(.12f, -.12f, 0), new Vector3(.19f, .09f, 0), new Vector3(.08f, .015f, 0), new Vector3(0, .15f, 0), new Vector3(-.08f, .015f, 0), new Vector3(-.19f, .09f, 0) }, ink, .014f);
        }
        static void CircleGlyph(Transform root, float radius)
        {
            var points = new Vector3[33];
            for (int i = 0; i < points.Length; i++) { float a = i * Mathf.PI * 2 / (points.Length - 1); points[i] = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * radius; }
            LocalLine(root, "Eye pupil", points, ink, .014f);
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

        static void Cover(Transform root, int side, float z, string number)
        {
            // Same tested opaque AABB as the former slabs, now recognizable rack equipment.
            var cabinet = Child(root, "Server cabinet R" + number); cabinet.position = new Vector3(side * 1.3f, 0, z);
            Solid(cabinet, "Opaque server chassis", new Vector3(0, 1.3f, 0), new Vector3(1.4f, 2.6f, .7f), navy);
            Primitive(cabinet, "Rack top cap", PrimitiveType.Cube, new Vector3(0, 2.55f, 0), new Vector3(1.4f, .10f, .7f), pale, false);
            Primitive(cabinet, "Rack kick plate", PrimitiveType.Cube, new Vector3(0, .10f, 0), new Vector3(1.4f, .20f, .7f), dark, false);
            for (int face = -1; face <= 1; face += 2)
            {
                Primitive(cabinet, "Recessed equipment door", PrimitiveType.Cube, new Vector3(0, 1.34f, face * .354f), new Vector3(1.22f, 2.17f, .018f), black, false);
                for (int slot = 0; slot < 10; slot++)
                {
                    float y = .48f + slot * .183f;
                    Primitive(cabinet, "Server blade " + slot, PrimitiveType.Cube, new Vector3(0, y, face * .37f), new Vector3(1.08f, .143f, .018f), serverFace, false);
                    Primitive(cabinet, "Blade vent", PrimitiveType.Cube, new Vector3(-.05f, y, face * .383f), new Vector3(.70f, .032f, .012f), black, false);
                    Primitive(cabinet, "Server activity indicator", PrimitiveType.Cube, new Vector3(.455f, y, face * .386f), new Vector3(.018f, .018f, .01f), slot % 4 == 0 ? cyan : label, false);
                }
                var plate = Child(cabinet, "Rack service ID"); plate.localPosition = new Vector3(0, 2.38f, face * .382f);
                if (face > 0) plate.localRotation = Quaternion.Euler(0, 180, 0);
                WorldText(plate, "Rack equipment label", "EDGE SERVER / R" + number, Vector3.zero, .99f, .071f, label.color);
                WorldText(plate, "Rack equipment role", "SECURITY NETWORK", new Vector3(0, -.106f, 0), .99f, .038f, new Color(.38f, .54f, .58f));
            }
        }
        static Light[] Lighting(Transform root)
        {
            var lighting = Child(root, "Annex lighting");
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.028f, .048f, .065f); RenderSettings.ambientEquatorColor = new Color(.014f, .025f, .035f);
            RenderSettings.ambientGroundColor = new Color(.009f, .014f, .021f); RenderSettings.ambientIntensity = .6f;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .025f;
            RenderSettings.fogColor = new Color(.035f, .06f, .068f);
            var lamps = new List<Light>();
            for (int i = 0; i < 8; i++)
            {
                var go = Child(lighting, "Service ceiling pool " + i); go.position = new Vector3(0, 3.94f, 1.4f + i * 3.5f); go.rotation = Quaternion.Euler(90, 0, 0);
                var light = go.gameObject.AddComponent<Light>(); light.type = LightType.Spot; light.range = 7;
                light.spotAngle = 96; light.innerSpotAngle = 60; light.intensity = i == 7 ? 2.4f : 2.0f;
                light.color = new Color(.66f, .80f, .85f);
                light.shadows = i == 1 || i == 5 ? LightShadows.Soft : LightShadows.None; lamps.Add(light);
            }
            // Soft reflected service light keeps equipment faces legible below the ceiling pools.
            // This is visual fill only: camera visibility continues to use real obstruction rays.
            foreach (float z in new[] { 2.2f, 8.2f, 16.1f, 21.2f, 26.0f })
            {
                var go = Child(lighting, "Reflected service fill"); go.position = new Vector3(0, 2.25f, z);
                var fill = go.gameObject.AddComponent<Light>(); fill.type = LightType.Point; fill.range = 4.2f;
                fill.intensity = .62f; fill.color = new Color(.59f, .76f, .86f); fill.shadows = LightShadows.None;
                lamps.Add(fill);
            }
            for (int i = 0; i < 3; i++)
            {
                var go = Child(lighting, "Annex reflection " + i); go.position = new Vector3(0, 1.8f, 4 + 9 * i);
                var probe = go.gameObject.AddComponent<ReflectionProbe>(); probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake; probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                probe.size = new Vector3(4.1f, 4.2f, 11); probe.resolution = 128; probe.boxProjection = true;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor; probe.backgroundColor = RenderSettings.fogColor; probe.intensity = .45f;
            }
            return lamps.ToArray();
        }
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
        static Material Existing(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>("Assets/LaserCorridor/Materials/" + name + ".mat");
        }
        static Material Material(string name, Color color, bool unlit, bool transparent = false)
        {
            string path = "Assets/RayOEZoning/Art/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
                material.name = name; AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color); material.SetColor("_Color", color);
            if (transparent)
            {
                material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0); material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); material.SetFloat("_ZWrite", 0);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(material); return material;
        }
        static void EmissionFloor(Material material, Color color)
        {
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color);
            material.SetFloat("_Smoothness", .34f); EditorUtility.SetDirty(material);
        }
        static void StripLegacy(GameObject go)
        {
            foreach (var component in go.GetComponentsInChildren<MonoBehaviour>(true)) if (component) UnityEngine.Object.DestroyImmediate(component);
            foreach (var component in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(component);
            foreach (var component in go.GetComponentsInChildren<Light>(true)) UnityEngine.Object.DestroyImmediate(component);
            foreach (var component in go.GetComponentsInChildren<AudioSource>(true)) UnityEngine.Object.DestroyImmediate(component);
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
