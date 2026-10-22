using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RayOEZoning.Editor
{
    /// <summary>Materials, generated surface textures and the post-processing profile.</summary>
    public static partial class ZoningBuilder
    {
        const string ArtPath = "Assets/RayOEZoning/Art";
        const string TexturePath = ArtPath + "/Textures";
        const string VariantPath = ArtPath + "/Variants";

        // Architecture
        static Material wallPaint, wainscot, skirting, floorBase, ceilingBase, doorFrame, accentTeal, accentIce, accentAmber;
        // Equipment
        static Material enamel, enamelTeal, lockerPaint, epoxy, stainless, steel, darkMetal, rubber, wood, plasticGrey, paper, binderBlue, binderRed, binderYellow;
        // Optics and light
        static Material glass, glassTint, amberGlass, lensGlass, cameraShell, cameraDark, fixtureOff, nightPanel, ledGreen, ledBlue, ledAmber, recording, laser, laserHousing, sampleGlow, coldInterior;
        // Screens and signs
        static Material screenGraph, screenFeed, screenMolecule, screenWave, signExit, signCctv, hazardStripe;
        // Player
        static Material glove, sleeve, trousers;
        // Clue surfaces and their inks
        static Material whiteboard, stickyYellow, stickyGlue, labelTape, lcdBacklight, screenOff, clipboardBoard, tape, torchLens;
        static Material inkBallpoint, inkPencil, inkMarker, inkMarkerRed, inkWhiteboard, inkPaint, inkScreen, inkLcd, inkLabel, inkEngrave, inkPrint;
        // Diagnostics
        static Material line, shaft;
        static readonly Dictionary<string, Material> variants = new Dictionary<string, Material>();

        static void CreateMaterials()
        {
            Directory.CreateDirectory(TexturePath);
            Directory.CreateDirectory(VariantPath);
            variants.Clear();
            var floorTexture = FloorTexture();
            var ceilingTexture = CeilingTexture();
            var wallTexture = WallTexture();
            floorBase = Lit("Speckled lab vinyl", new Color(.80f, .83f, .84f), .42f, 0, floorTexture);
            ceilingBase = Lit("Acoustic ceiling tile", new Color(.86f, .87f, .87f), .08f, 0, ceilingTexture);
            wallPaint = Lit("Lab wall panel", new Color(.80f, .82f, .80f), .30f, 0, wallTexture);
            wainscot = Lit("Hygienic wall cladding", new Color(.20f, .40f, .40f), .55f);
            skirting = Lit("Coved skirting", new Color(.12f, .15f, .16f), .45f);
            doorFrame = Lit("Brushed door frame", new Color(.55f, .58f, .60f), .62f, .85f);
            accentTeal = Lit("Laboratory zone teal", new Color(.10f, .46f, .45f), .5f);
            accentIce = Lit("Cold storage zone blue", new Color(.22f, .44f, .66f), .5f);
            accentAmber = Lit("Secure zone amber", new Color(.86f, .52f, .10f), .5f);

            enamel = Lit("White equipment enamel", new Color(.84f, .86f, .85f), .58f);
            enamelTeal = Lit("Teal equipment enamel", new Color(.13f, .36f, .37f), .55f);
            lockerPaint = Lit("Locker powder coat", new Color(.38f, .47f, .50f), .40f, .2f);
            epoxy = Lit("Black epoxy worktop", new Color(.035f, .04f, .045f), .78f);
            stainless = Lit("Stainless steel", new Color(.70f, .72f, .74f), .70f, .95f);
            steel = Lit("Satin steel", new Color(.45f, .48f, .50f), .55f, .8f);
            darkMetal = Lit("Graphite metal", new Color(.07f, .08f, .09f), .5f, .6f);
            rubber = Lit("Black rubber", new Color(.025f, .025f, .028f), .22f);
            wood = Lit("Oiled ash bench", new Color(.56f, .40f, .25f), .45f);
            plasticGrey = Lit("Grey instrument plastic", new Color(.32f, .35f, .37f), .45f);
            paper = Lit("Document paper", new Color(.66f, .66f, .62f), .06f);
            binderBlue = Lit("Blue binder", new Color(.10f, .22f, .48f), .35f);
            binderRed = Lit("Red binder", new Color(.55f, .10f, .09f), .35f);
            binderYellow = Lit("Yellow binder", new Color(.80f, .62f, .12f), .35f);

            glass = Glass("Clear lab glass", new Color(.75f, .88f, .92f, .14f));
            glassTint = Glass("Tinted sash glass", new Color(.35f, .55f, .60f, .28f));
            amberGlass = Glass("Amber reagent glass", new Color(.55f, .28f, .06f, .62f));
            lensGlass = Lit("CCTV lens glass", new Color(.02f, .03f, .05f), .96f, .2f);
            cameraShell = Lit("CCTV housing", new Color(.66f, .68f, .69f), .55f);
            cameraDark = Lit("CCTV mount graphite", new Color(.10f, .11f, .12f), .5f, .5f);
            fixtureOff = Lit("Dimmed ceiling panel", new Color(.55f, .58f, .60f), .7f);
            nightPanel = Unlit("Night light panel", new Color(1.1f, 1.0f, .86f));
            ledGreen = Unlit("Status green", new Color(.25f, 3.2f, .9f));
            ledBlue = Unlit("Status blue", new Color(.35f, 1.4f, 3.6f));
            ledAmber = Unlit("Status amber", new Color(3.6f, 1.5f, .2f));
            recording = Unlit("Recording red", new Color(6f, .18f, .16f));
            laser = Unlit("Security laser", new Color(3.2f, .16f, .1f));
            laserHousing = Unlit("Laser aperture", new Color(2.4f, .12f, .08f));
            sampleGlow = Unlit("Specimen glow", new Color(.35f, 3.4f, 2.8f));
            coldInterior = Unlit("Freezer interior light", new Color(.9f, 1.6f, 2.4f));

            screenGraph = Screen("Monitor analysis", ScreenTexture("Monitor analysis", 0), 1.6f);
            screenFeed = Screen("Monitor camera feed", ScreenTexture("Monitor camera feed", 1), 1.3f);
            screenMolecule = Screen("Monitor molecule", ScreenTexture("Monitor molecule", 2), 1.5f);
            screenWave = Screen("Monitor waveform", ScreenTexture("Monitor waveform", 3), 1.6f);
            signExit = Screen("Sign exit", SignTexture("Sign exit", 0), 1.8f);
            signCctv = Lit("Sign surveillance", Color.white, .3f, 0, SignTexture("Sign surveillance", 4));
            hazardStripe = Lit("Hazard stripe", Color.white, .35f, 0, SignTexture("Hazard stripe", 5));

            glove = Lit("Black tactical glove", new Color(.075f, .08f, .088f), .2f);
            sleeve = Lit("Charcoal jacket", new Color(.11f, .125f, .15f), .26f);
            trousers = Lit("Dark work trousers", new Color(.07f, .08f, .095f), .15f);

            whiteboard = Lit("Whiteboard", new Color(.86f, .88f, .88f), .85f);
            stickyYellow = Lit("Sticky note", new Color(.93f, .78f, .22f), .08f);
            stickyGlue = Lit("Sticky note glue", new Color(.86f, .7f, .18f), .2f);
            labelTape = Lit("Label tape", new Color(.025f, .025f, .028f), .12f);
            lcdBacklight = Unlit("LCD backlight", new Color(.36f, .5f, .24f));
            screenOff = Lit("Screen off", new Color(.008f, .01f, .012f), .92f);
            clipboardBoard = Lit("Clipboard", new Color(.34f, .23f, .13f), .3f);
            tape = Lit("Masking tape", new Color(.78f, .72f, .56f), .2f);
            torchLens = Unlit("Flashlight lens", new Color(.03f, .03f, .03f));
            // Hidden writing uses the RevealInk shader: it exists only where the flashlight cone falls, and being
            // unlit, the hotspot cannot wash dark ink out to grey.
            inkBallpoint = Ink("Ink ballpoint", new Color(.02f, .05f, .26f));
            inkPencil = Ink("Ink pencil", new Color(.09f, .09f, .1f));
            inkMarker = Ink("Ink marker", new Color(.01f, .01f, .012f));
            inkMarkerRed = Ink("Ink marker red", new Color(.55f, .03f, .03f));
            inkWhiteboard = Lit("Ink marker blue", new Color(.05f, .12f, .45f), 0);
            inkPaint = Ink("Ink stencil paint", new Color(.95f, .76f, .16f));
            inkScreen = Ink("Ink screen", new Color(.3f, 1.5f, .6f));
            inkLcd = Ink("Ink LCD", new Color(.015f, .03f, .015f));
            inkLabel = Ink("Ink label", new Color(.9f, .9f, .86f));
            inkEngrave = Ink("Ink engraving", new Color(.86f, .88f, .85f));
            inkPrint = Ink("Ink print", new Color(.015f, .015f, .02f));
            line = Unlit("Scanning outline", new Color(.82f, .90f, .95f, .22f), true);
            line.SetFloat("_Cull", (float)CullMode.Off);
            shaft = ShaftMaterial();
        }

        // ------------------------------------------------------------ material helpers
        static Material Asset(string name, string shader)
        {
            string path = ArtPath + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            var target = Shader.Find(shader);
            if (!material) { material = new Material(target) { name = name }; AssetDatabase.CreateAsset(material, path); }
            else if (material.shader != target) material.shader = target;
            material.shaderKeywords = new string[0];
            return material;
        }

        static Material Lit(string name, Color color, float smoothness, float metallic = 0, Texture2D map = null)
        {
            var material = Asset(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", map);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetColor("_EmissionColor", Color.black);
            material.SetFloat("_Surface", 0);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// Self-lit screen or sign. Unlit with an HDR tint rather than Lit emission, whose keyword the editor's
        /// material validation can strip and leave the screen black.
        /// </summary>
        static Material Screen(string name, Texture2D image, float intensity)
        {
            var material = Asset(name, "Universal Render Pipeline/Unlit");
            material.SetTexture("_BaseMap", image);
            material.SetColor("_BaseColor", Color.white * intensity);
            material.SetFloat("_Surface", 0);
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Unlit(string name, Color color, bool transparent = false)
        {
            var material = Asset(name, "Universal Render Pipeline/Unlit");
            material.SetColor("_BaseColor", color);
            if (transparent) Transparent(material);
            else { material.SetFloat("_Surface", 0); material.renderQueue = -1; }
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Ink(string name, Color color)
        {
            var material = Asset(name, "RayOEZoning/Reveal Ink");
            material.SetColor("_BaseColor", color);
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material Glass(string name, Color color)
        {
            var material = Lit(name, color, .95f, 0);
            Transparent(material);
            material.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(material);
            return material;
        }

        static void Transparent(Material material)
        {
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One); material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        static Material ShaftMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ArtPath + "/Shaders/LightShaft.shader");
            string path = ArtPath + "/CCTV light shaft.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(shader) { name = "CCTV light shaft" }; AssetDatabase.CreateAsset(material, path); }
            material.shader = shader;
            material.SetColor("_Color", new Color(.82f, .90f, 1f));
            material.SetFloat("_Intensity", .045f);
            material.SetFloat("_Falloff", .7f);
            material.SetFloat("_EdgePower", 1.6f);
            material.SetFloat("_DepthFade", .55f);
            material.SetFloat("_NearFade", 1.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Material copy whose base map tiling matches a surface's real dimensions.</summary>
        static Material Tiled(Material source, float width, float height, float tileMetres)
        {
            var tiling = new Vector2(Mathf.Round(width / tileMetres * 4) / 4, Mathf.Round(height / tileMetres * 4) / 4);
            string key = source.name + " " + tiling.x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "x" + tiling.y.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
            if (variants.TryGetValue(key, out var cached)) return cached;
            string path = VariantPath + "/" + key + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(source) { name = key }; AssetDatabase.CreateAsset(material, path); }
            else material.CopyPropertiesFromMaterial(source);
            material.SetTextureScale("_BaseMap", tiling);
            EditorUtility.SetDirty(material);
            variants[key] = material;
            return material;
        }

        // ------------------------------------------------------------ generated textures
        sealed class Canvas
        {
            public readonly int W, H;
            readonly Color[] pixels;
            public Canvas(int w, int h, Color fill)
            {
                W = w; H = h; pixels = new Color[w * h];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = fill;
            }
            public Color Get(int x, int y) => pixels[((y % H + H) % H) * W + ((x % W + W) % W)];
            public void Set(int x, int y, Color c) => pixels[((y % H + H) % H) * W + ((x % W + W) % W)] = c;
            public void Blend(int x, int y, Color c, float a)
            {
                if (x < 0 || y < 0 || x >= W || y >= H || a <= 0) return;
                int i = y * W + x; pixels[i] = Color.Lerp(pixels[i], new Color(c.r, c.g, c.b, 1), Mathf.Clamp01(a * c.a));
            }
            delegate float Distance(Vector2 p);
            void Shape(float minX, float minY, float maxX, float maxY, Distance distance, Color color)
            {
                for (int y = Mathf.Max(0, (int)minY - 2); y <= Mathf.Min(H - 1, (int)maxY + 2); y++)
                    for (int x = Mathf.Max(0, (int)minX - 2); x <= Mathf.Min(W - 1, (int)maxX + 2); x++)
                        Blend(x, y, color, Mathf.Clamp01(.5f - distance(new Vector2(x + .5f, y + .5f))));
            }
            public void Capsule(Vector2 a, Vector2 b, float r, Color color)
            {
                Shape(Mathf.Min(a.x, b.x) - r, Mathf.Min(a.y, b.y) - r, Mathf.Max(a.x, b.x) + r, Mathf.Max(a.y, b.y) + r, p =>
                {
                    Vector2 ab = b - a; float t = ab.sqrMagnitude < 1e-5f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    return Vector2.Distance(p, a + ab * t) - r;
                }, color);
            }
            public void Circle(Vector2 c, float r, Color color) => Capsule(c, c, r, color);
            public void Ring(Vector2 c, float r, float width, Color color)
            {
                Shape(c.x - r - width, c.y - r - width, c.x + r + width, c.y + r + width, p => Mathf.Abs(Vector2.Distance(p, c) - r) - width * .5f, color);
            }
            public void Box(Vector2 min, Vector2 max, Color color, float radius = 0)
            {
                Vector2 center = (min + max) * .5f, half = (max - min) * .5f - Vector2.one * radius;
                Shape(min.x, min.y, max.x, max.y, p =>
                {
                    Vector2 d = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half;
                    return new Vector2(Mathf.Max(d.x, 0), Mathf.Max(d.y, 0)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0) - radius;
                }, color);
            }
            public void Polyline(Vector2[] points, float width, Color color)
            {
                for (int i = 1; i < points.Length; i++) Capsule(points[i - 1], points[i], width * .5f, color);
            }
            public void Noise(System.Random random, float amount)
            {
                for (int i = 0; i < pixels.Length; i++)
                {
                    float n = ((float)random.NextDouble() - .5f) * amount;
                    pixels[i] = new Color(pixels[i].r + n, pixels[i].g + n, pixels[i].b + n, 1);
                }
            }
            public Texture2D Save(string name, bool repeat)
            {
                string path = TexturePath + "/" + name + ".png";
                var texture = new Texture2D(W, H, TextureFormat.RGB24, false);
                texture.SetPixels(pixels); texture.Apply();
                byte[] bytes = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);
                if (!File.Exists(path) || !BytesEqual(File.ReadAllBytes(path), bytes)) File.WriteAllBytes(path, bytes);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Default; importer.sRGBTexture = true; importer.mipmapEnabled = true;
                importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.anisoLevel = 8; importer.filterMode = FilterMode.Trilinear; importer.maxTextureSize = 1024;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            }
            static bool BytesEqual(byte[] a, byte[] b)
            {
                if (a.Length != b.Length) return false;
                for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
                return true;
            }
        }

        /// <summary>Two 0.6 m speckled vinyl tiles per texture side.</summary>
        static Texture2D FloorTexture()
        {
            var random = new System.Random(11);
            var canvas = new Canvas(512, 512, new Color(.70f, .73f, .74f));
            for (int ty = 0; ty < 2; ty++)
                for (int tx = 0; tx < 2; tx++)
                {
                    float tone = ((float)random.NextDouble() - .5f) * .035f;
                    for (int y = ty * 256; y < ty * 256 + 256; y++)
                        for (int x = tx * 256; x < tx * 256 + 256; x++)
                        {
                            var c = canvas.Get(x, y); canvas.Set(x, y, new Color(c.r + tone, c.g + tone, c.b + tone));
                        }
                }
            canvas.Noise(random, .045f);
            for (int i = 0; i < 2600; i++)
            {
                var p = new Vector2((float)random.NextDouble() * 512, (float)random.NextDouble() * 512);
                float shade = (float)random.NextDouble();
                Color fleck = shade < .55f ? new Color(.36f, .40f, .43f) : shade < .85f ? new Color(.90f, .91f, .90f) : new Color(.30f, .48f, .50f);
                canvas.Circle(p, .6f + (float)random.NextDouble() * 1.4f, new Color(fleck.r, fleck.g, fleck.b, .75f));
            }
            for (int k = 0; k <= 2; k++)
                for (int i = 0; i < 512; i++)
                {
                    int edge = k * 256;
                    foreach (int o in new[] { edge - 1, edge, edge + 1 })
                    {
                        float a = o == edge ? .55f : .25f;
                        if (o < 0 || o > 511) continue;
                        canvas.Blend(o, i, new Color(.42f, .45f, .47f), a);
                        canvas.Blend(i, o, new Color(.42f, .45f, .47f), a);
                    }
                }
            return canvas.Save("Speckled vinyl", true);
        }

        /// <summary>Two 0.6 m mineral fibre tiles in a white exposed grid.</summary>
        static Texture2D CeilingTexture()
        {
            var random = new System.Random(23);
            var canvas = new Canvas(512, 512, new Color(.86f, .87f, .86f));
            canvas.Noise(random, .03f);
            for (int i = 0; i < 5200; i++)
            {
                var p = new Vector2((float)random.NextDouble() * 512, (float)random.NextDouble() * 512);
                canvas.Circle(p, .5f + (float)random.NextDouble() * .9f, new Color(.55f, .56f, .56f, .55f));
            }
            for (int k = 0; k <= 2; k++)
                for (int i = 0; i < 512; i++)
                    for (int o = -4; o <= 4; o++)
                    {
                        int at = k * 256 + o;
                        if (at < 0 || at > 511) continue;
                        Color grid = Mathf.Abs(o) == 4 ? new Color(.62f, .63f, .63f) : new Color(.95f, .95f, .94f);
                        canvas.Blend(at, i, grid, 1); canvas.Blend(i, at, grid, 1);
                    }
            return canvas.Save("Ceiling tile", true);
        }

        /// <summary>Hygienic wall sheet, one 1.2 m wide panel with a recessed vertical joint.</summary>
        static Texture2D WallTexture()
        {
            var random = new System.Random(37);
            var canvas = new Canvas(256, 256, new Color(.86f, .87f, .85f));
            canvas.Noise(random, .025f);
            for (int y = 0; y < 256; y++)
            {
                canvas.Blend(0, y, new Color(.45f, .47f, .47f), .8f);
                canvas.Blend(1, y, new Color(.60f, .62f, .62f), .6f);
                canvas.Blend(2, y, new Color(1, 1, 1), .35f);
                canvas.Blend(255, y, new Color(.60f, .62f, .62f), .5f);
            }
            return canvas.Save("Wall panel", true);
        }

        static Texture2D ScreenTexture(string name, int kind)
        {
            var random = new System.Random(101 + kind);
            var canvas = new Canvas(512, 320, new Color(.015f, .03f, .045f));
            Color cyan = new Color(.25f, .85f, .95f), green = new Color(.35f, .95f, .55f), amber = new Color(1f, .72f, .25f), dim = new Color(.10f, .22f, .30f);
            canvas.Box(new Vector2(0, 296), new Vector2(512, 320), new Color(.05f, .12f, .17f));
            for (int i = 0; i < 4; i++) canvas.Box(new Vector2(14 + i * 34, 302), new Vector2(40 + i * 34, 312), i == 0 ? cyan : dim, 3);
            if (kind == 0)
            {
                for (int x = 30; x < 500; x += 40) canvas.Box(new Vector2(x, 30), new Vector2(x + 1, 280), dim);
                for (int y = 30; y < 290; y += 40) canvas.Box(new Vector2(30, y), new Vector2(490, y + 1), dim);
                var a = new Vector2[60]; var b = new Vector2[60];
                for (int i = 0; i < 60; i++)
                {
                    float x = 30 + i * 7.8f;
                    a[i] = new Vector2(x, 170 + Mathf.Sin(i * .23f) * 60 + Mathf.Sin(i * .71f) * 14);
                    b[i] = new Vector2(x, 110 + Mathf.Cos(i * .17f) * 35 + (float)random.NextDouble() * 10);
                }
                canvas.Polyline(a, 3, cyan); canvas.Polyline(b, 2.5f, amber);
                for (int i = 0; i < 8; i++) canvas.Box(new Vector2(330 + i * 20, 34), new Vector2(344 + i * 20, 50 + (float)random.NextDouble() * 50), green);
            }
            else if (kind == 1)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x0 = 14 + (i % 2) * 246, y0 = 18 + (i / 2) * 140;
                    canvas.Box(new Vector2(x0, y0), new Vector2(x0 + 236, y0 + 132), new Color(.06f, .08f, .09f));
                    var vanish = new Vector2(x0 + 118 + (i - 1.5f) * 10, y0 + 72);
                    foreach (var corner in new[] { new Vector2(x0, y0), new Vector2(x0 + 236, y0), new Vector2(x0, y0 + 132), new Vector2(x0 + 236, y0 + 132) })
                        canvas.Capsule(corner, vanish, 1, new Color(.22f, .26f, .28f));
                    canvas.Box(vanish - new Vector2(34, 22), vanish + new Vector2(34, 22), new Color(.13f, .16f, .17f));
                    canvas.Circle(new Vector2(x0 + 14, y0 + 118), 5, new Color(1, .15f, .12f));
                    canvas.Box(new Vector2(x0 + 150, y0 + 112), new Vector2(x0 + 226, y0 + 122), dim, 2);
                }
            }
            else if (kind == 2)
            {
                var atoms = new List<Vector2>();
                for (int i = 0; i < 14; i++) atoms.Add(new Vector2(120 + (float)random.NextDouble() * 270, 60 + (float)random.NextDouble() * 200));
                for (int i = 1; i < atoms.Count; i++) canvas.Capsule(atoms[i], atoms[random.Next(i)], 2.5f, dim * 2.2f);
                for (int i = 0; i < atoms.Count; i++) canvas.Circle(atoms[i], 9 + i % 3 * 4, i % 4 == 0 ? amber : i % 3 == 0 ? green : cyan);
                for (int i = 0; i < 6; i++) canvas.Box(new Vector2(420, 220 - i * 26), new Vector2(495, 232 - i * 26), i == 2 ? cyan : dim, 3);
            }
            else
            {
                for (int row = 0; row < 3; row++)
                {
                    var trace = new Vector2[120];
                    float baseY = 60 + row * 85;
                    for (int i = 0; i < trace.Length; i++)
                    {
                        float x = 20 + i * 4f, phase = (i + row * 17) % 40;
                        float spike = phase == 18 ? 34 : phase == 19 ? -22 : phase == 20 ? 10 : 0;
                        trace[i] = new Vector2(x, baseY + spike + Mathf.Sin(i * .3f + row) * 3);
                    }
                    canvas.Polyline(trace, 2.5f, row == 0 ? green : row == 1 ? cyan : amber);
                }
            }
            return canvas.Save(name, false);
        }

        static Texture2D SignTexture(string name, int kind)
        {
            Color white = new Color(.97f, .97f, .95f);
            if (kind == 5)
            {
                var stripes = new Canvas(256, 256, new Color(.95f, .72f, .08f));
                for (int y = 0; y < 256; y++)
                    for (int x = 0; x < 256; x++)
                        if (((x + y) / 64) % 2 == 1) stripes.Set(x, y, new Color(.05f, .05f, .05f));
                return stripes.Save(name, true);
            }
            Color background = kind == 0 ? new Color(.05f, .55f, .25f) : new Color(.96f, .80f, .10f);
            var canvas = new Canvas(256, 256, background);
            Color ink = kind == 4 ? new Color(.06f, .06f, .06f) : white;
            if (kind == 0)
            {
                canvas.Circle(new Vector2(110, 196), 15, ink);
                canvas.Capsule(new Vector2(104, 172), new Vector2(92, 120), 10, ink);
                canvas.Capsule(new Vector2(100, 160), new Vector2(128, 140), 7, ink);
                canvas.Capsule(new Vector2(102, 162), new Vector2(76, 150), 7, ink);
                canvas.Capsule(new Vector2(92, 122), new Vector2(118, 96), 8, ink);
                canvas.Capsule(new Vector2(118, 96), new Vector2(112, 64), 8, ink);
                canvas.Capsule(new Vector2(92, 122), new Vector2(70, 92), 8, ink);
                canvas.Capsule(new Vector2(70, 92), new Vector2(46, 92), 8, ink);
                canvas.Box(new Vector2(150, 70), new Vector2(178, 200), ink);
                canvas.Box(new Vector2(156, 76), new Vector2(172, 194), background);
                canvas.Capsule(new Vector2(186, 136), new Vector2(222, 136), 6, ink);
                canvas.Polyline(new[] { new Vector2(206, 154), new Vector2(224, 136), new Vector2(206, 118) }, 12, ink);
            }
            else
            {
                canvas.Box(new Vector2(14, 14), new Vector2(242, 242), ink, 8);
                canvas.Box(new Vector2(26, 26), new Vector2(230, 230), background, 4);
                canvas.Box(new Vector2(60, 120), new Vector2(170, 170), ink, 8);
                canvas.Polyline(new[] { new Vector2(170, 132), new Vector2(206, 112), new Vector2(206, 178), new Vector2(170, 158) }, 10, ink);
                canvas.Capsule(new Vector2(100, 120), new Vector2(100, 84), 7, ink);
                canvas.Capsule(new Vector2(70, 80), new Vector2(130, 80), 7, ink);
                canvas.Circle(new Vector2(150, 145), 9, background);
            }
            return canvas.Save(name, false);
        }

        // ------------------------------------------------------------ post-processing and pipeline
        static VolumeProfile PostProfile()
        {
            string path = ArtPath + "/Laboratory post processing.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.05f); bloom.intensity.Override(.85f); bloom.scatter.Override(.62f); bloom.highQualityFiltering.Override(true);
            var adjust = profile.Add<ColorAdjustments>(true);
            adjust.postExposure.Override(.65f); adjust.contrast.Override(16); adjust.saturation.Override(-6);
            var balance = profile.Add<WhiteBalance>(true); balance.temperature.Override(-7); balance.tint.Override(2);
            var vignette = profile.Add<Vignette>(true); vignette.intensity.Override(.32f); vignette.smoothness.Override(.45f);
            var grain = profile.Add<FilmGrain>(true); grain.type.Override(FilmGrainLookup.Thin1); grain.intensity.Override(.18f); grain.response.Override(.85f);
            var lift = profile.Add<ShadowsMidtonesHighlights>(true);
            lift.shadows.Override(new Vector4(.96f, 1f, 1.06f, 0)); lift.highlights.Override(new Vector4(1.03f, 1.01f, .98f, 0));
            foreach (var component in profile.components) { component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; AssetDatabase.AddObjectToAsset(component, profile); }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static void ConfigurePipeline()
        {
            foreach (string path in new[] { "Assets/Settings/PC_RPAsset.asset" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (!asset) continue;
                var serialized = new SerializedObject(asset);
                void Set(string name, int value) { var p = serialized.FindProperty(name); if (p != null) p.intValue = value; }
                Set("m_AdditionalLightsShadowmapResolution", 4096);
                Set("m_RequireDepthTexture", 1);
                Set("m_SupportsHDR", 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
