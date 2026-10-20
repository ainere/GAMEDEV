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
        static Material steel, dark, cyan, amber, line, ink;
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
            steel = Existing("Brushed aluminium"); dark = Existing("Gunmetal structure");
            cyan = Material("Annex cyan", new Color(.22f, .65f, .68f), true);
            amber = Material("Scanner amber", new Color(1, .52f, .12f), true);
            ink = Material("Marking ivory", new Color(.84f, .90f, .78f), true);
            line = Material("Scanning outline", new Color(.86f, .55f, .19f, .28f), true, true);

            var source = EditorSceneManager.OpenScene(SourcePath, OpenSceneMode.Single);
            var sourceRoot = source.GetRootGameObjects().First(x => x.name == "Laser Corridor");
            var sourceEnvironment = sourceRoot.transform.Find("Environment");
            if (!sourceEnvironment) throw new InvalidOperationException("Source corridor Environment is missing.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("Ray-OE Zoning");
            var architecture = Child(root.transform, "Retained corridor architecture");
            for (int bay = 0; bay < 2; bay++)
            {
                var module = Child(architecture, "Corridor length " + (bay + 1));
                module.position = Vector3.forward * bay * 14;
                foreach (string part in new[] { "Floor", "Walls", "Ceiling" })
                {
                    var sourcePart = sourceEnvironment.Find(part);
                    if (!sourcePart) throw new InvalidOperationException("Source architecture missing " + part);
                    var clone = UnityEngine.Object.Instantiate(sourcePart.gameObject, module, false);
                    clone.name = part; StripLegacy(clone); SetLayer(clone, World);
                }
            }
            var entranceSource = sourceEnvironment.Find("Entrance");
            if (entranceSource)
            {
                var entrance = UnityEngine.Object.Instantiate(entranceSource.gameObject, architecture, false);
                entrance.name = "Retained entrance"; StripLegacy(entrance); SetLayer(entrance, World);
            }
            var exitSource = sourceEnvironment.Find("Exit Door");
            if (!exitSource) throw new InvalidOperationException("Source exit assembly is missing.");
            var exitAssembly = UnityEngine.Object.Instantiate(exitSource.gameObject, architecture, false);
            exitAssembly.name = "Retained exit assembly"; exitAssembly.transform.position += Vector3.forward * 14;
            StripLegacy(exitAssembly); SetLayer(exitAssembly, World);
            Transform door = exitAssembly.transform.Find("Door");
            Transform keypad = exitAssembly.transform.Find("Keypad");
            if (keypad)
            {
                keypad.position = new Vector3(1.86f, 1.4f, 26.4f);
                keypad.rotation = Quaternion.Euler(0, 90, 0);
            }
            else keypad = Child(root.transform, "Exit keypad");

            // New simple collision shell matches the retained visible architecture and its extension.
            var collision = Child(root.transform, "Solid world");
            Solid(collision, "Floor collision", new Vector3(0, -.15f, 13.5f), new Vector3(4.2f, .28f, 30), null, false);
            Solid(collision, "Left wall collision", new Vector3(-2.10f, 2.1f, 13.5f), new Vector3(.2f, 4.2f, 30), null, false);
            Solid(collision, "Right wall collision", new Vector3(2.10f, 2.1f, 13.5f), new Vector3(.2f, 4.2f, 30), null, false);
            Solid(collision, "Ceiling collision", new Vector3(0, 4.3f, 13.5f), new Vector3(4.2f, .2f, 30), null, false);
            Solid(collision, "Entrance collision", new Vector3(0, 2.1f, -.85f), new Vector3(4.2f, 4.2f, .2f), null, false);
            var cover = Child(root.transform, "Opaque sight breaks");
            Cover(cover, -1, 3.3f, "01"); Cover(cover, 1, 9.3f, "02");
            Cover(cover, 1, 10.7f, "03"); Cover(cover, -1, 17.2f, "04");
            Cover(cover, -1, 18.5f, "05"); Cover(cover, 1, 22.3f, "06");

            var session = Child(root.transform, "Investigation").gameObject;
            var game = session.AddComponent<StealthGame>();
            game.exposureSeconds = 1.35f; game.decaySeconds = 1.5f; game.escapeZ = 27.5f;
            var spawn = Child(root.transform, "Entrance spawn"); spawn.position = new Vector3(0, .06f, .7f); game.spawn = spawn;
            CreatePlayer(root.transform, game, spawn.position);
            game.keypad = keypad;

            var scanners = Child(root.transform, "Patrolling detection rigs");
            var sphere = Scanner(scanners, game, "01 / Spherical observer", DetectionShape.Sphere, new Vector3(0, 1.1f, 6.5f), 8);
            sphere.radius = 1.55f;
            var box = Scanner(scanners, game, "02 / Rectangular survey", DetectionShape.Box, new Vector3(0, 1.2f, 14), 9);
            box.halfExtents = new Vector3(1.3f, 1.2f, 2);
            var capsule = Scanner(scanners, game, "03 / Longitudinal observer", DetectionShape.Capsule, new Vector3(0, 1.1f, 21.8f), 10);
            capsule.radius = .9f; capsule.capsulePointA = new Vector3(0, 0, -2); capsule.capsulePointB = new Vector3(0, 0, 2);
            game.hazards = new[] { sphere, box, capsule };

            var markings = Child(root.transform, "Light responsive markings");
            game.clues = new[] {
                Clue(markings, 0, -1, 2.5f), Clue(markings, 1, 1, 8.5f),
                Clue(markings, 2, -1, 15.5f), Clue(markings, 3, 1, 23.2f)
            };
            var grid = Child(root.transform, "Blue exit lattice"); grid.position = new Vector3(0, 0, 27.05f);
            grid.gameObject.layer = World;
            var gridCollider = grid.gameObject.AddComponent<BoxCollider>(); gridCollider.center = new Vector3(0, 1.8f, 0); gridCollider.size = new Vector3(4, 3.6f, .10f);
            for (int x = -3; x <= 3; x++) LocalLine(grid, "Exit vertical", new[] { new Vector3(x * .49f, .08f, 0), new Vector3(x * .49f, 3.48f, 0) }, cyan, .014f);
            for (int y = 0; y < 9; y++) LocalLine(grid, "Exit horizontal", new[] { new Vector3(-1.92f, .12f + y * .4f, 0), new Vector3(1.92f, .12f + y * .4f, 0) }, cyan, .012f);
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
            var surfaces = architecture.gameObject.AddComponent<AnnexSurfaces>();
            surfaces.targets = architecture.GetComponentsInChildren<Renderer>(true);
            surfaces.Apply();

            // Clear, visible controls at the entrance are ordinary world signage, never puzzle clues.
            var entry = Child(root.transform, "Entrance instructions"); entry.position = new Vector3(0, 2.55f, -.72f);
            entry.rotation = Quaternion.Euler(0, 180, 0);
            WorldText(entry, "Annex identity", "RESTRICTED ANNEX", new Vector3(0, .27f, 0), 2.8f, .20f, cyan.color);
            WorldText(entry, "Annex subtitle", "RAY-OE ZONING", Vector3.zero, 2.1f, .13f, new Color(.6f, .73f, .73f));
            WorldText(keypad, "Keypad plate", "ACCESS / 4 DIGITS", new Vector3(0, .33f, -.035f), .55f, .06f, new Color(.6f, .8f, .8f));
            EditorSceneManager.CloseScene(source, true);
            SceneManager.SetActiveScene(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.productName = "Ray-OE Zoning";
            PlayerSettings.defaultScreenWidth = 1920; PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            Debug.Log("Ray-OE Zoning authored: 28 m corridor, 3 check-shape patrols, 4 hidden clues, first-person controller and complete interface. " + ScenePath);
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
            var body = Primitive(rig, "Survey head", shape == DetectionShape.Sphere ? PrimitiveType.Sphere : PrimitiveType.Cube,
                Vector3.zero, shape == DetectionShape.Sphere ? Vector3.one * .35f : new Vector3(.34f, .20f, .31f), dark, false);
            body.layer = ScannerLayer;
            Primitive(rig, "Amber optic", PrimitiveType.Sphere, new Vector3(0, 0, -.18f), Vector3.one * .10f, amber, false).layer = ScannerLayer;
            LocalLine(rig, "Overhead suspension", new[] { new Vector3(0, .19f, 0), new Vector3(0, 2.86f, 0) }, steel, .025f);
            var lamp = rig.gameObject.AddComponent<Light>(); lamp.type = LightType.Point; lamp.color = new Color(1, .52f, .15f); lamp.intensity = .24f; lamp.range = 3.2f;
            var visual = rig.gameObject.AddComponent<ScannerVisual>(); visual.detector = detector; visual.game = game; visual.lineMaterial = line; visual.lamp = lamp;
            var rail = Child(root, "Rail " + name); rail.position = new Vector3(0, 4.02f, home.z);
            Primitive(rail, "Ceiling rail", PrimitiveType.Cube, Vector3.zero, new Vector3(3.7f, .06f, .07f), dark, false);
            return detector;
        }

        static HiddenClue Clue(Transform root, int index, int side, float z)
        {
            var definition = PuzzleCatalog.Clues[index];
            // The retained wall's raised ribs project into the room. Mount the complete plaque
            // in front of them, rather than letting ribs cut across its pictogram and sentence.
            var mount = Child(root, definition.title + " marking mount"); mount.position = new Vector3(side * 1.80f, 1.55f, z);
            mount.rotation = Quaternion.Euler(0, side < 0 ? -90 : 90, 0);
            Primitive(mount, "Recessed metal mount", PrimitiveType.Cube, new Vector3(0, 0, .018f), new Vector3(1.30f, 1.38f, .035f), dark, false).layer = World;
            // The hidden content projects away from its ordinary mounting surface toward the room.
            var content = Child(mount, definition.title + " light responsive content"); content.localPosition = new Vector3(0, 0, -.075f);
            content.gameObject.layer = ClueLayer;
            DrawGlyph(content, index, new Vector3(0, .38f, 0));
            WorldText(content, "Glyph title", definition.title.ToUpperInvariant(), new Vector3(0, .10f, 0), 1.05f, .11f, ink.color);
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
            var cover = Solid(root, "Sight break " + number, new Vector3(side * 1.3f, 1.3f, z), new Vector3(1.4f, 2.6f, .7f), dark);
            Primitive(cover.transform, "Brushed access face", PrimitiveType.Cube, new Vector3(0, .02f, -.507f), new Vector3(.88f, .90f, .025f), steel, false);
            Primitive(cover.transform, "Quiet indicator", PrimitiveType.Cube, new Vector3(-.30f, .40f, -.53f), new Vector3(.016f, .055f, .015f), cyan, false);
        }
        static Light[] Lighting(Transform root)
        {
            var lighting = Child(root, "Annex lighting");
            RenderSettings.skybox = null; RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.035f, .055f, .065f); RenderSettings.ambientEquatorColor = new Color(.016f, .026f, .030f);
            RenderSettings.ambientGroundColor = new Color(.009f, .014f, .016f); RenderSettings.ambientIntensity = .5f;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = .025f;
            RenderSettings.fogColor = new Color(.035f, .06f, .068f);
            var lamps = new List<Light>();
            for (int i = 0; i < 9; i++)
            {
                var go = Child(lighting, "Ceiling pool " + i); go.position = new Vector3(0, 3.95f, 1 + i * 3.2f); go.rotation = Quaternion.Euler(90, 0, 0);
                var light = go.gameObject.AddComponent<Light>(); light.type = LightType.Spot; light.range = 7;
                light.spotAngle = 102; light.innerSpotAngle = 58; light.intensity = 1.45f;
                light.color = i % 3 == 0 ? new Color(.49f, .72f, .77f) : new Color(.62f, .75f, .72f);
                light.shadows = i % 2 == 0 ? LightShadows.Soft : LightShadows.None; lamps.Add(light);
            }
            for (int i = 0; i < 3; i++)
            {
                var go = Child(lighting, "Annex reflection " + i); go.position = new Vector3(0, 1.8f, 4 + 9 * i);
                var probe = go.gameObject.AddComponent<ReflectionProbe>(); probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake; probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
                probe.size = new Vector3(4.1f, 4.2f, 11); probe.resolution = 128; probe.boxProjection = true;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor; probe.backgroundColor = RenderSettings.fogColor; probe.intensity = .65f;
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
