using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning
{
    public enum LetteringStyle
    {
        /// <summary>Typeset text (TextMeshPro).</summary>
        Printed,
        /// <summary>Ballpoint or pencil handwriting.</summary>
        Hand,
        /// <summary>Thick felt-tip handwriting.</summary>
        Marker,
        /// <summary>Spray-painted stencil with bridged strokes.</summary>
        Stencil,
        /// <summary>Dot-matrix screen text.</summary>
        Pixel
    }

    /// <summary>Flat lettering mesh in a local XY plane, readable from local -Z.</summary>
    public sealed class LetteringMesh
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<int> triangles = new List<int>();

        public bool IsEmpty => vertices.Count == 0;

        void Triangle(int a, int b, int c)
        {
            Vector3 pa = vertices[a], pb = vertices[b], pc = vertices[c];
            float z = (pb.x - pa.x) * (pc.y - pa.y) - (pb.y - pa.y) * (pc.x - pa.x);
            // Clockwise as seen from -Z is the visible side.
            if (z > 0) { triangles.Add(a); triangles.Add(c); triangles.Add(b); }
            else { triangles.Add(a); triangles.Add(b); triangles.Add(c); }
        }

        public void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int i = vertices.Count;
            vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
            Triangle(i, i + 1, i + 2); Triangle(i, i + 2, i + 3);
        }

        public void Rect(Vector2 center, Vector2 size)
        {
            Vector2 h = size * .5f;
            Quad(center + new Vector2(-h.x, -h.y), center + new Vector2(-h.x, h.y), center + new Vector2(h.x, h.y), center + new Vector2(h.x, -h.y));
        }

        public void Disc(Vector2 center, float radius, int sides = 8)
        {
            int first = vertices.Count;
            vertices.Add(center);
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                vertices.Add(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
            for (int i = 0; i < sides; i++) Triangle(first, first + 1 + i, first + 1 + (i + 1) % sides);
        }

        /// <summary>A pen stroke. Round strokes get round joints and caps; square strokes get flat extended caps.</summary>
        public void Stroke(IList<Vector2> points, float width, bool round)
        {
            if (points.Count < 2) return;
            float half = width * .5f;
            for (int i = 1; i < points.Count; i++)
            {
                Vector2 a = points[i - 1], b = points[i], delta = b - a;
                if (delta.sqrMagnitude < 1e-10f) continue;
                Vector2 d = delta.normalized, n = new Vector2(-d.y, d.x) * half;
                if (!round)
                {
                    if (i == 1) a -= d * half;
                    if (i == points.Count - 1) b += d * half;
                }
                Quad(a - n, a + n, b + n, b - n);
            }
            for (int i = round ? 0 : 1; i < (round ? points.Count : points.Count - 1); i++)
                Disc(points[i], half, round ? 8 : 6);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            var normals = new Vector3[vertices.Count];
            for (int i = 0; i < normals.Length; i++) normals[i] = Vector3.back;
            mesh.normals = normals;
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>Stroke font, dot-matrix font and symbol outlines drawn without any font or texture dependency.</summary>
    public static class Lettering
    {
        // Glyphs on a 4 x 6 grid (baseline 0, cap height 6). Strokes separated by '|'.
        static readonly Dictionary<char, string> StrokeGlyphs = new Dictionary<char, string>
        {
            ['A'] = "0 0 2 6 4 0|0.8 2.4 3.2 2.4",
            ['B'] = "0 0 0 6 2.8 6 3.6 5.5 3.8 4.6 3.4 3.5 2.6 3.1 0 3.1|2.6 3.1 3.5 2.7 3.9 1.7 3.6 0.6 2.9 0 0 0",
            ['C'] = "3.9 4.9 3.2 5.8 2 6 0.9 5.6 0.2 4.4 0 3 0.2 1.6 0.9 0.4 2 0 3.2 0.2 3.9 1.1",
            ['D'] = "0 0 0 6 1.8 6 3.2 5.4 3.9 4 4 3 3.9 2 3.2 0.6 1.8 0 0 0",
            ['E'] = "4 6 0 6 0 0 4 0|0 3.1 3 3.1",
            ['F'] = "4 6 0 6 0 0|0 3.1 3 3.1",
            ['G'] = "3.9 4.9 3.2 5.8 2 6 0.9 5.6 0.2 4.4 0 3 0.2 1.6 0.9 0.4 2 0 3.2 0.2 4 1 4 2.6 2.2 2.6",
            ['H'] = "0 0 0 6|4 0 4 6|0 3.1 4 3.1",
            ['I'] = "2 0 2 6|0.9 6 3.1 6|0.9 0 3.1 0",
            ['J'] = "4 6 4 1.6 3.6 0.5 2.6 0 1.4 0 0.4 0.5 0 1.6",
            ['K'] = "0 0 0 6|4 6 0 2.2|1.3 3.4 4 0",
            ['L'] = "0 6 0 0 4 0",
            ['M'] = "0 0 0 6 2 2.4 4 6 4 0",
            ['N'] = "0 0 0 6 4 0 4 6",
            ['O'] = "2 6 3.2 5.6 3.9 4.4 4 3 3.9 1.6 3.2 0.4 2 0 0.8 0.4 0.1 1.6 0 3 0.1 4.4 0.8 5.6 2 6",
            ['P'] = "0 0 0 6 2.8 6 3.6 5.5 3.9 4.6 3.6 3.6 2.8 3 0 3",
            ['Q'] = "2 6 3.2 5.6 3.9 4.4 4 3 3.9 1.6 3.2 0.4 2 0 0.8 0.4 0.1 1.6 0 3 0.1 4.4 0.8 5.6 2 6|2.4 1.4 4 -0.2",
            ['R'] = "0 0 0 6 2.8 6 3.6 5.5 3.9 4.6 3.6 3.6 2.8 3 0 3|2 3 4 0",
            ['S'] = "3.8 5 3.2 5.8 2 6 0.8 5.8 0.2 5 0.3 4 1 3.4 3 2.7 3.8 2 3.9 1 3.2 0.2 2 0 0.8 0.2 0.1 1",
            ['T'] = "0 6 4 6|2 6 2 0",
            ['U'] = "0 6 0 1.6 0.4 0.5 1.4 0 2.6 0 3.6 0.5 4 1.6 4 6",
            ['V'] = "0 6 2 0 4 6",
            ['W'] = "0 6 1 0 2 4 3 0 4 6",
            ['X'] = "0 6 4 0|0 0 4 6",
            ['Y'] = "0 6 2 3 4 6|2 3 2 0",
            ['Z'] = "0 6 4 6 0 0 4 0",
            ['0'] = "2 6 3.1 5.6 3.7 4.4 3.9 3 3.7 1.6 3.1 0.4 2 0 0.9 0.4 0.3 1.6 0.1 3 0.3 4.4 0.9 5.6 2 6",
            ['1'] = "0.9 4.8 2.4 6 2.4 0",
            ['2'] = "0.2 4.8 0.8 5.7 2 6 3.2 5.7 3.8 4.7 3.6 3.6 0 0 4 0",
            ['3'] = "0.2 5.2 1 5.9 2.2 6 3.3 5.6 3.7 4.6 3.2 3.6 1.8 3.2 3.2 2.9 3.9 2 3.8 1 3.2 0.3 2 0 0.8 0.2 0 1",
            ['4'] = "3 0 3 6 0 1.8 4 1.8",
            ['5'] = "3.8 6 0.6 6 0.3 3.3 1.4 3.8 2.4 3.9 3.4 3.4 3.9 2.3 3.8 1.1 3.1 0.3 2 0 0.9 0.2 0.1 1",
            ['6'] = "3.6 5.6 2.6 6 1.6 5.9 0.7 5.2 0.2 4 0 2.6 0.1 1.4 0.7 0.4 1.8 0 2.9 0.2 3.7 1 3.9 2 3.6 3 2.8 3.6 1.8 3.7 0.8 3.2 0.1 2.2",
            ['7'] = "0 6 4 6 1.4 0",
            ['8'] = "2 3.3 1 3.7 0.5 4.6 0.8 5.6 2 6 3.2 5.6 3.5 4.6 3 3.7 2 3.3 0.9 2.8 0.2 1.8 0.4 0.6 1.4 0 2.6 0 3.6 0.6 3.8 1.8 3.1 2.8 2 3.3",
            ['9'] = "3.9 4.4 3.6 3.4 2.8 2.9 1.8 2.9 0.8 3.4 0.2 4.4 0.5 5.5 1.4 6 2.6 6 3.5 5.5 3.9 4.4 3.9 2.6 3.5 1.1 2.6 0.2 1.6 0 0.6 0.4",
            ['='] = "0.4 3.9 3.6 3.9|0.4 2.1 3.6 2.1",
            ['-'] = "0.6 3 3.4 3",
            ['>'] = "0.6 5 3.4 3 0.6 1",
            [':'] = "2 4.2 2 4.3|2 1.2 2 1.3",
            ['.'] = "2 0.1 2 0.2",
            [','] = "2 0.4 1.6 -0.8"
        };

        // Classic 5 x 7 dot-matrix glyphs, top row first.
        static readonly Dictionary<char, string> PixelGlyphs = new Dictionary<char, string>
        {
            ['A'] = "01110100011000111111100011000110001", ['B'] = "11110100011000111110100011000111110",
            ['C'] = "01110100011000010000100001000101110", ['D'] = "11100100101000110001100011001011100",
            ['E'] = "11111100001000011110100001000011111", ['F'] = "11111100001000011110100001000010000",
            ['G'] = "01110100011000010111100011000101111", ['H'] = "10001100011000111111100011000110001",
            ['I'] = "01110001000010000100001000010001110", ['J'] = "00111000100001000010000101001001100",
            ['K'] = "10001100101010011000101001001010001", ['L'] = "10000100001000010000100001000011111",
            ['M'] = "10001110111010110101100011000110001", ['N'] = "10001100011100110101100111000110001",
            ['O'] = "01110100011000110001100011000101110", ['P'] = "11110100011000111110100001000010000",
            ['Q'] = "01110100011000110001101011001001101", ['R'] = "11110100011000111110101001001010001",
            ['S'] = "01111100001000001110000010000111110", ['T'] = "11111001000010000100001000010000100",
            ['U'] = "10001100011000110001100011000101110", ['V'] = "10001100011000110001100010101000100",
            ['W'] = "10001100011000110101101011010101010", ['X'] = "10001100010101000100010101000110001",
            ['Y'] = "10001100011000101010001000010000100", ['Z'] = "11111000010001000100010001000011111",
            ['0'] = "01110100011001110101110011000101110", ['1'] = "00100011000010000100001000010001110",
            ['2'] = "01110100010000100010001000100011111", ['3'] = "11111000100010000010000011000101110",
            ['4'] = "00010001100101010010111110001000010", ['5'] = "11111100001111000001000011000101110",
            ['6'] = "00110010001000011110100011000101110", ['7'] = "11111000010001000100010000100001000",
            ['8'] = "01110100011000101110100011000101110", ['9'] = "01110100011000101111000010001001100",
            ['='] = "00000000001111100000111110000000000", ['-'] = "00000000000000011111000000000000000",
            ['>'] = "10000010000010000010001000100010000", [':'] = "00000011000110000000011000110000000",
            ['.'] = "00000000000000000000000000110001100", ['_'] = "00000000000000000000000000000011111"
        };

        static readonly Dictionary<char, List<Vector2[]>> parsed = new Dictionary<char, List<Vector2[]>>();
        static readonly Dictionary<string, List<Vector2[]>> symbols = new Dictionary<string, List<Vector2[]>>();

        public const float CapUnits = 6, Advance = 5.6f, SpaceAdvance = 3.6f;

        /// <summary>Width of a line of stroke text with the given cap height.</summary>
        public static float StrokeWidth(string text, float cap)
        {
            float unit = cap / CapUnits, width = 0;
            foreach (char c in text) width += (c == ' ' ? SpaceAdvance : Advance) * unit;
            return Mathf.Max(0, width - 1.6f * unit);
        }

        /// <summary>Width of a line of dot-matrix text whose dots are the given pitch.</summary>
        public static float PixelWidth(string text, float pitch) => Mathf.Max(0, text.Length * 6 - 1) * pitch;

        static List<Vector2[]> Glyph(char c)
        {
            if (parsed.TryGetValue(c, out var strokes)) return strokes;
            strokes = new List<Vector2[]>();
            if (StrokeGlyphs.TryGetValue(c, out string source))
                foreach (string part in source.Split('|'))
                {
                    var numbers = part.Split(' ');
                    var points = new Vector2[numbers.Length / 2];
                    for (int i = 0; i < points.Length; i++)
                        points[i] = new Vector2(float.Parse(numbers[i * 2], System.Globalization.CultureInfo.InvariantCulture),
                            float.Parse(numbers[i * 2 + 1], System.Globalization.CultureInfo.InvariantCulture));
                    strokes.Add(points);
                }
            parsed[c] = strokes;
            return strokes;
        }

        /// <summary>Writes stroke text starting at the baseline-left point.</summary>
        public static void StrokeText(LetteringMesh mesh, string text, Vector2 baseline, float cap, LetteringStyle style, System.Random random)
        {
            float unit = cap / CapUnits;
            float slant = style == LetteringStyle.Hand ? .2f : style == LetteringStyle.Marker ? .12f : 0;
            float width = (style == LetteringStyle.Hand ? .68f : style == LetteringStyle.Marker ? 1.05f : 1.35f) * unit;
            float wobble = style == LetteringStyle.Stencil ? 0 : style == LetteringStyle.Marker ? .14f : .1f;
            float x = baseline.x;
            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);
                if (c == ' ') { x += SpaceAdvance * unit; continue; }
                float scale = 1 + Jitter(random, style == LetteringStyle.Stencil ? 0 : .06f);
                float lift = Jitter(random, wobble * 1.2f);
                float turn = Jitter(random, style == LetteringStyle.Stencil ? 0 : .05f);
                foreach (var stroke in Glyph(c))
                {
                    var points = new Vector2[stroke.Length];
                    for (int i = 0; i < stroke.Length; i++)
                    {
                        Vector2 p = stroke[i] - new Vector2(2, 3);
                        p = new Vector2(p.x * Mathf.Cos(turn) - p.y * Mathf.Sin(turn), p.x * Mathf.Sin(turn) + p.y * Mathf.Cos(turn)) * scale + new Vector2(2, 3);
                        p += new Vector2(Jitter(random, wobble), Jitter(random, wobble) + lift);
                        p.x += p.y * slant;
                        points[i] = new Vector2(x, baseline.y) + p * unit;
                    }
                    if (style == LetteringStyle.Stencil) StencilStroke(mesh, points, width, .5f * unit);
                    else mesh.Stroke(points, width, true);
                }
                x += Advance * unit;
            }
        }

        /// <summary>Breaks closed loops and trims stroke ends so the shape could be cut from a stencil sheet.</summary>
        static void StencilStroke(LetteringMesh mesh, Vector2[] points, float width, float gap)
        {
            bool closed = points.Length > 3 && (points[0] - points[points.Length - 1]).sqrMagnitude < 1e-4f;
            if (closed)
            {
                int half = points.Length / 2;
                StencilStroke(mesh, Slice(points, 0, half + 1), width, gap);
                StencilStroke(mesh, Slice(points, half, points.Length - half), width, gap);
                return;
            }
            float length = 0;
            for (int i = 1; i < points.Length; i++) length += Vector2.Distance(points[i - 1], points[i]);
            if (length > gap * 4) points = Trim(Trim(points, gap), gap, true);
            mesh.Stroke(points, width, false);
        }

        static Vector2[] Slice(Vector2[] points, int start, int count)
        {
            var result = new Vector2[count];
            System.Array.Copy(points, start, result, 0, count);
            return result;
        }

        static Vector2[] Trim(Vector2[] points, float amount, bool fromEnd = false)
        {
            var list = new List<Vector2>(points);
            if (fromEnd) list.Reverse();
            while (list.Count > 1 && amount > 0)
            {
                float segment = Vector2.Distance(list[0], list[1]);
                if (segment > amount) { list[0] = Vector2.MoveTowards(list[0], list[1], amount); break; }
                amount -= segment; list.RemoveAt(0);
            }
            if (fromEnd) list.Reverse();
            return list.ToArray();
        }

        /// <summary>Writes dot-matrix text; baseline-left is the bottom-left dot centre.</summary>
        public static void PixelText(LetteringMesh mesh, string text, Vector2 baseline, float pitch, float dot = .8f)
        {
            float x = baseline.x;
            foreach (char raw in text)
            {
                char c = char.ToUpperInvariant(raw);
                if (PixelGlyphs.TryGetValue(c, out string bits))
                    for (int row = 0; row < 7; row++)
                        for (int col = 0; col < 5; col++)
                            if (bits[row * 5 + col] == '1')
                                mesh.Rect(new Vector2(x + col * pitch, baseline.y + (6 - row) * pitch), Vector2.one * pitch * dot);
                x += 6 * pitch;
            }
        }

        static float Jitter(System.Random random, float amount) => amount <= 0 ? 0 : ((float)random.NextDouble() * 2 - 1) * amount;

        // ------------------------------------------------------------ symbols (unit square -1..1)
        public static List<Vector2[]> Symbol(string name)
        {
            if (symbols.TryGetValue(name, out var strokes)) return strokes;
            strokes = new List<Vector2[]>();
            switch (name)
            {
                case "Eye":
                    strokes.Add(Arc(Vector2.zero, new Vector2(1, .52f), 0, 360, 40));
                    strokes.Add(Arc(Vector2.zero, Vector2.one * .27f, 0, 360, 20)); break;
                case "Moon":
                {
                    var moon = new List<Vector2>();
                    for (int i = 0; i <= 32; i++) { float a = Mathf.PI * .25f + i * Mathf.PI * 1.5f / 32; moon.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * .95f); }
                    float tip = .95f / Mathf.Sqrt(2), offset = .42f;
                    float inner = Mathf.Sqrt((tip - offset) * (tip - offset) + tip * tip), angle = Mathf.Atan2(tip, tip - offset);
                    for (int i = 1; i <= 32; i++) { float a = Mathf.Lerp(-angle, -2 * Mathf.PI + angle, i / 32f); moon.Add(new Vector2(offset + Mathf.Cos(a) * inner, Mathf.Sin(a) * inner)); }
                    strokes.Add(moon.ToArray()); break;
                }
                case "Gate":
                    strokes.Add(P(-.9f, -.9f, -.9f, .45f, -.6f, .85f, 0, 1, .6f, .85f, .9f, .45f, .9f, -.9f));
                    strokes.Add(P(-.9f, -.9f, .9f, -.9f));
                    for (int i = -1; i <= 1; i++) strokes.Add(P(i * .45f, -.9f, i * .45f, i == 0 ? .95f : .8f)); break;
                case "Crown":
                    strokes.Add(P(-1, .6f, -.7f, -.75f, .7f, -.75f, 1, .6f, .45f, .05f, 0, .95f, -.45f, .05f, -1, .6f));
                    strokes.Add(P(-.7f, -.45f, .7f, -.45f)); break;
                case "Star":
                {
                    var star = new Vector2[11];
                    for (int i = 0; i <= 10; i++) { float a = Mathf.PI * .5f + i * Mathf.PI / 5; float r = i % 2 == 0 ? 1 : .42f; star[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }
                    strokes.Add(star); break;
                }
                case "Sun":
                    strokes.Add(Arc(Vector2.zero, Vector2.one * .45f, 0, 360, 28));
                    for (int i = 0; i < 8; i++) { float a = i * Mathf.PI / 4; var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)); strokes.Add(new[] { d * .66f, d * 1f }); } break;
                case "Key":
                    strokes.Add(Arc(new Vector2(-.58f, 0), Vector2.one * .38f, 0, 360, 24));
                    strokes.Add(P(-.2f, 0, 1, 0));
                    strokes.Add(P(.62f, 0, .62f, -.38f)); strokes.Add(P(.88f, 0, .88f, -.3f)); break;
                case "Bell":
                    strokes.Add(P(-.85f, -.55f, -.68f, -.25f, -.62f, .3f, -.45f, .7f, -.2f, .9f, 0, .95f, .2f, .9f, .45f, .7f, .62f, .3f, .68f, -.25f, .85f, -.55f));
                    strokes.Add(P(-1, -.55f, 1, -.55f));
                    strokes.Add(Arc(new Vector2(0, -.78f), Vector2.one * .16f, 0, 360, 14)); break;
                case "Fish":
                    strokes.Add(P(.45f, 0, .2f, .35f, -.2f, .5f, -.6f, .4f, -.95f, 0, -.6f, -.4f, -.2f, -.5f, .2f, -.35f, .45f, 0, 1, .48f, 1, -.48f, .45f, 0));
                    strokes.Add(Arc(new Vector2(-.55f, .1f), Vector2.one * .07f, 0, 360, 8)); break;
                case "Leaf":
                    strokes.Add(P(-.85f, -.85f, -.75f, .05f, -.4f, .6f, .1f, .85f, .85f, .85f, .85f, .1f, .6f, -.4f, .05f, -.75f, -.85f, -.85f));
                    strokes.Add(P(-1, -1, .55f, .55f));
                    strokes.Add(P(-.2f, -.2f, -.2f, .35f)); strokes.Add(P(-.2f, -.2f, .35f, -.2f)); break;
                case "Bolt":
                    strokes.Add(P(.25f, 1, -.5f, -.05f, 0, -.05f, -.3f, -1, .55f, .18f, .05f, .18f, .25f, 1)); break;
                case "Anchor":
                    strokes.Add(Arc(new Vector2(0, .78f), Vector2.one * .2f, 0, 360, 16));
                    strokes.Add(P(0, .58f, 0, -.9f));
                    strokes.Add(P(-.45f, .32f, .45f, .32f));
                    strokes.Add(Arc(new Vector2(0, -.15f), Vector2.one * .75f, 180, 360, 20));
                    strokes.Add(P(-.95f, .05f, -.75f, -.15f, -.5f, .02f)); strokes.Add(P(.95f, .05f, .75f, -.15f, .5f, .02f)); break;
                case "Drop":
                {
                    var drop = new List<Vector2> { new Vector2(0, 1) };
                    foreach (var p in Arc(new Vector2(0, -.35f), Vector2.one * .6f, 160, 380, 24)) drop.Add(p);
                    drop.Add(new Vector2(0, 1));
                    strokes.Add(drop.ToArray()); break;
                }
                case "Spiral":
                {
                    var spiral = new Vector2[60];
                    for (int i = 0; i < spiral.Length; i++) { float t = i / 59f, a = t * Mathf.PI * 5; spiral[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (.06f + .9f * t); }
                    strokes.Add(spiral); break;
                }
                case "Flame":
                    strokes.Add(P(0, 1, -.35f, .45f, -.62f, -.1f, -.55f, -.55f, -.3f, -.85f, 0, -.95f, .3f, -.85f, .55f, -.55f, .62f, -.1f, .42f, .3f, .28f, .02f, .15f, .4f, 0, 1));
                    strokes.Add(P(0, -.72f, -.24f, -.45f, -.14f, -.08f, 0, .15f, .14f, -.1f, .24f, -.45f, 0, -.72f)); break;
                default: // Tree
                    strokes.Add(P(0, 1, -.5f, .25f, -.25f, .25f, -.75f, -.4f, .75f, -.4f, .25f, .25f, .5f, .25f, 0, 1));
                    strokes.Add(P(-.14f, -.4f, -.14f, -.95f, .14f, -.95f, .14f, -.4f)); break;
            }
            symbols[name] = strokes;
            return strokes;
        }

        static Vector2[] P(params float[] xy)
        {
            var points = new Vector2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector2(xy[i * 2], xy[i * 2 + 1]);
            return points;
        }

        static Vector2[] Arc(Vector2 center, Vector2 radius, float from, float to, int steps)
        {
            var points = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(from, to, i / (float)steps) * Mathf.Deg2Rad;
                points[i] = center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y);
            }
            return points;
        }

        /// <summary>Draws a symbol centred on a point with the given half size.</summary>
        public static void SymbolStrokes(LetteringMesh mesh, string name, Vector2 center, float half, LetteringStyle style, float width, System.Random random)
        {
            float wobble = style == LetteringStyle.Hand || style == LetteringStyle.Marker ? .045f : 0;
            float turn = wobble > 0 ? Jitter(random, .06f) : 0;
            foreach (var stroke in Symbol(name))
            {
                var points = new Vector2[stroke.Length];
                for (int i = 0; i < stroke.Length; i++)
                {
                    Vector2 p = stroke[i];
                    p = new Vector2(p.x * Mathf.Cos(turn) - p.y * Mathf.Sin(turn), p.x * Mathf.Sin(turn) + p.y * Mathf.Cos(turn));
                    p += new Vector2(Jitter(random, wobble), Jitter(random, wobble));
                    points[i] = center + p * half;
                }
                if (style == LetteringStyle.Stencil) StencilStroke(mesh, points, width, width * .45f);
                else mesh.Stroke(points, width, true);
            }
        }

        /// <summary>Rasterises a symbol onto a dot grid, as a screen would show it.</summary>
        public static void SymbolPixels(LetteringMesh mesh, string name, Vector2 center, float half, int grid = 13)
        {
            var strokes = Symbol(name);
            float pitch = half * 2 / grid;
            for (int gy = 0; gy < grid; gy++)
                for (int gx = 0; gx < grid; gx++)
                {
                    var p = new Vector2((gx + .5f) / grid * 2 - 1, (gy + .5f) / grid * 2 - 1) * 1.08f;
                    float best = float.MaxValue;
                    foreach (var stroke in strokes)
                        for (int i = 1; i < stroke.Length; i++) best = Mathf.Min(best, SegmentDistance(p, stroke[i - 1], stroke[i]));
                    if (best < .1f) mesh.Rect(center + new Vector2((gx + .5f) * pitch - half, (gy + .5f) * pitch - half), Vector2.one * pitch * .82f);
                }
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = ab.sqrMagnitude < 1e-8f ? 0 : Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
