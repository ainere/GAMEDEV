using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning
{
    /// <summary>Lays a clue out on its surface in the lettering that suits it.</summary>
    public static class ClueWriter
    {
        enum Align { Left, Center }

        sealed class Page
        {
            public ClueSpot spot;
            public LetteringStyle style;
            public LetteringMesh mesh = new LetteringMesh();
            public List<Renderer> renderers = new List<Renderer>();
            public TMP_FontAsset font;
            public System.Random random;
            public float pen;
        }

        public static Renderer[] Write(ClueSpot spot, ClueDefinition clue, TMP_FontAsset font, System.Random random)
        {
            var page = new Page { spot = spot, style = spot.Style, font = font, random = random };
            float h = spot.area.y;
            page.pen = page.style == LetteringStyle.Marker ? h * .035f : page.style == LetteringStyle.Stencil ? h * .05f : h * .016f;
            if (clue.kind == ClueKind.Code) Code(page, clue);
            else Order(page, clue);
            if (!page.mesh.IsEmpty)
            {
                var go = new GameObject("Line");
                go.layer = spot.face.gameObject.layer;
                go.transform.SetParent(spot.face, false);
                go.AddComponent<MeshFilter>().sharedMesh = page.mesh.Build("Lettering " + clue.id);
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = spot.ink;
                page.renderers.Add(renderer);
            }
            foreach (var renderer in page.renderers)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.enabled = false;
            }
            return page.renderers.ToArray();
        }

        // ------------------------------------------------------------ layouts
        static void Code(Page page, ClueDefinition clue)
        {
            float w = page.spot.area.x, h = page.spot.area.y;
            string name = clue.symbol.ToUpperInvariant();
            switch (page.spot.medium)
            {
                case ClueMedium.StickyNote:
                case ClueMedium.Whiteboard:
                case ClueMedium.Lcd:
                {
                    float half = Mathf.Min(w * .19f, h * .3f);
                    var glyph = new Vector2(-w * .24f, h * .1f);
                    Symbol(page, clue.symbol, glyph, half);
                    Text(page, name, new Vector2(glyph.x, -h * .34f), Mathf.Min(h * .1f, Fit(page, name, w * .44f)), Align.Center);
                    float digit = h * (page.spot.medium == ClueMedium.Lcd ? .62f : page.spot.medium == ClueMedium.Whiteboard ? .48f : .56f);
                    if (page.spot.medium == ClueMedium.Whiteboard)
                    {
                        page.mesh.Stroke(new[] { new Vector2(-w * .02f, 0), new Vector2(w * .08f, 0) }, page.pen * .8f, true);
                        page.mesh.Stroke(new[] { new Vector2(w * .05f, h * .04f), new Vector2(w * .08f, 0), new Vector2(w * .05f, -h * .04f) }, page.pen * .8f, true);
                    }
                    Text(page, clue.digit, new Vector2(w * .27f, 0), digit, Align.Center);
                    break;
                }
                case ClueMedium.Label:
                {
                    float half = h * .3f, x = -w * .5f + h * .5f;
                    Symbol(page, clue.symbol, new Vector2(x, 0), half);
                    float cap = h * .42f;
                    Text(page, name, new Vector2(x + half + h * .25f, 0), Mathf.Min(cap, Fit(page, name, w * .55f)), Align.Left);
                    Text(page, clue.digit, new Vector2(w * .5f - h * .45f, 0), h * .52f, Align.Center);
                    break;
                }
                default:
                {
                    float half = Mathf.Min(w * .24f, h * .17f);
                    var glyph = new Vector2(0, h * .5f - h * .07f - half);
                    if (page.style == LetteringStyle.Hand) glyph.x -= w * .06f;
                    Symbol(page, clue.symbol, glyph, half);
                    float nameCap = Mathf.Min(h * .065f, Fit(page, name, w * .8f));
                    float nameY = glyph.y - half - h * .08f;
                    Text(page, name, new Vector2(glyph.x, nameY), nameCap, Align.Center);
                    float digitCap = Mathf.Min(h * .3f, (nameY - nameCap - (-h * .5f)) * .72f);
                    var digit = new Vector2(page.style == LetteringStyle.Hand ? w * .05f : 0, (nameY - nameCap * .5f - h * .5f) * .5f - h * .01f);
                    Text(page, clue.digit, digit, digitCap, Align.Center);
                    if (page.style == LetteringStyle.Hand)
                    {
                        // A quick pen flourish: the digit underlined, or circled on loose paper.
                        if (page.spot.medium == ClueMedium.Paper) Loop(page, digit, digitCap * .95f, digitCap * .75f);
                        else page.mesh.Stroke(Wobbly(page, new Vector2(digit.x - digitCap * .5f, digit.y - digitCap * .66f), new Vector2(digit.x + digitCap * .6f, digit.y - digitCap * .6f)), page.pen, true);
                    }
                    break;
                }
            }
        }

        static void Order(Page page, ClueDefinition clue)
        {
            float w = page.spot.area.x * .9f, h = page.spot.area.y * .86f;
            string prefix = page.style == LetteringStyle.Pixel ? "> " : page.style == LetteringStyle.Hand ? "- " : "";
            // Widest wrap that still reaches the best size: as few broken lines as possible.
            List<(string text, bool first)> best = null; float bestCap = 0, largest = page.spot.area.y * .12f;
            int longest = 0;
            foreach (string s in clue.statements) longest = Mathf.Max(longest, s.Length + prefix.Length);
            for (int wrap = longest; wrap >= 8; wrap--)
            {
                var lines = Wrap(clue.statements, wrap, prefix);
                // Never break one statement over more than two lines.
                if (best != null && lines.Count > clue.statements.Length * 2) continue;
                int widest = 0; foreach (var line in lines) widest = Mathf.Max(widest, line.text.Length);
                float rows = lines.Count + (clue.statements.Length - 1) * .5f;
                float cap = Mathf.Min(largest, Mathf.Min(w / (widest * Ratio(page.style)), h / (rows * 1.7f)));
                if (cap > bestCap * 1.01f) { bestCap = cap; best = lines; }
            }
            bool center = page.spot.medium == ClueMedium.Plaque || page.spot.medium == ClueMedium.Stencil;
            float lineHeight = bestCap * 1.7f;
            float total = (best.Count - 1 + (clue.statements.Length - 1) * .5f) * lineHeight, y = total * .5f;
            for (int i = 0; i < best.Count; i++)
            {
                // A half-line gap separates one statement from the next.
                if (i > 0 && best[i].first) y -= lineHeight * .5f;
                Text(page, best[i].text, new Vector2(center ? 0 : -w * .5f, y), bestCap, center ? Align.Center : Align.Left);
                y -= lineHeight;
            }
        }

        static List<(string text, bool first)> Wrap(string[] statements, int width, string prefix)
        {
            var lines = new List<(string, bool)>();
            foreach (string statement in statements)
            {
                string current = prefix;
                bool first = true;
                foreach (string word in statement.Split(' '))
                {
                    bool empty = current.Length == prefix.Length;
                    if (!empty && current.Length + 1 + word.Length > width)
                    {
                        lines.Add((current, first));
                        first = false;
                        current = new string(' ', prefix.Length);
                        empty = true;
                    }
                    current += (empty ? "" : " ") + word;
                }
                lines.Add((current, first));
            }
            return lines;
        }

        // ------------------------------------------------------------ marks
        /// <summary>Approximate line width per character, in cap heights.</summary>
        static float Ratio(LetteringStyle style) => style == LetteringStyle.Pixel ? 6f / 7f : style == LetteringStyle.Printed ? .82f : Lettering.Advance / Lettering.CapUnits;

        static float Fit(Page page, string text, float width) => width / Mathf.Max(1, text.Length * Ratio(page.style));

        static void Text(Page page, string text, Vector2 at, float cap, Align align)
        {
            if (string.IsNullOrEmpty(text)) return;
            switch (page.style)
            {
                case LetteringStyle.Pixel:
                {
                    float pitch = cap / 7;
                    float width = Lettering.PixelWidth(text, pitch);
                    float left = align == Align.Center ? at.x - width * .5f : at.x;
                    Lettering.PixelText(page.mesh, text, new Vector2(left + pitch * .5f, at.y - cap * .5f + pitch * .5f), pitch);
                    break;
                }
                case LetteringStyle.Printed:
                    Printed(page, text, at, cap, align);
                    break;
                default:
                {
                    float width = Lettering.StrokeWidth(text, cap);
                    float left = align == Align.Center ? at.x - width * .5f : at.x;
                    Lettering.StrokeText(page.mesh, text, new Vector2(left, at.y - cap * .5f), cap, page.style, page.random);
                    break;
                }
            }
        }

        static void Printed(Page page, string value, Vector2 at, float cap, Align align)
        {
            var go = new GameObject("Text (TMP)");
            go.layer = page.spot.face.gameObject.layer;
            go.transform.SetParent(page.spot.face, false);
            var text = go.AddComponent<TextMeshPro>();
            if (page.font) text.font = page.font;
            text.text = value; text.color = page.spot.textColor; text.fontSize = 36; text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(100, 10); text.richText = false;
            text.ForceMeshUpdate(true, true);
            Bounds bounds = text.textBounds;
            float scale = cap / Mathf.Max(.001f, bounds.size.y);
            float width = bounds.size.x * scale;
            var center = new Vector2(align == Align.Center ? at.x : at.x + width * .5f, at.y);
            go.transform.localScale = Vector3.one * scale;
            go.transform.localPosition = new Vector3(center.x - bounds.center.x * scale, center.y - bounds.center.y * scale, 0);
            page.renderers.Add(text.renderer);
        }

        static void Symbol(Page page, string name, Vector2 center, float half)
        {
            if (page.style == LetteringStyle.Pixel) { Lettering.SymbolPixels(page.mesh, name, center, half); return; }
            float width = page.style == LetteringStyle.Marker ? page.pen : page.style == LetteringStyle.Stencil ? half * .17f :
                page.style == LetteringStyle.Hand ? Mathf.Max(page.pen, half * .06f) : half * .07f;
            Lettering.SymbolStrokes(page.mesh, name, center, half, page.style, width, page.random);
        }

        static Vector2[] Wobbly(Page page, Vector2 a, Vector2 b)
        {
            var points = new Vector2[6];
            for (int i = 0; i < points.Length; i++)
                points[i] = Vector2.Lerp(a, b, i / 5f) + Vector2.up * (float)(page.random.NextDouble() - .5) * page.pen * .8f;
            return points;
        }

        static void Loop(Page page, Vector2 center, float rx, float ry)
        {
            var points = new Vector2[30];
            float start = (float)page.random.NextDouble() * 6;
            for (int i = 0; i < points.Length; i++)
            {
                float t = start + i / 26f * Mathf.PI * 2;
                float r = 1 + (i - 15) * .006f;
                points[i] = center + new Vector2(Mathf.Cos(t) * rx * r, Mathf.Sin(t) * ry * r);
            }
            page.mesh.Stroke(points, page.pen, true);
        }
    }
}
