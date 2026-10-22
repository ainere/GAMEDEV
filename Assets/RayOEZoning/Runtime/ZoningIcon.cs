using UnityEngine;
using UnityEngine.UI;

namespace RayOEZoning
{
    public enum ZoningSymbol { Flashlight, Notebook, Eye, Keypad, Close, Check, Play, Restart, Backspace, Camera, Standing, Crouched, Prone, Sprinting, Chevron, Move }

    /// <summary>Small vector symbols drawn as UI geometry, with no font or external art dependency.</summary>
    public sealed class ZoningIcon : MaskableGraphic
    {
        public ZoningSymbol symbol;
        VertexHelper helper;
        float unit;
        Vector2 origin;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); helper = vh; unit = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height); origin = rectTransform.rect.center;
            switch (symbol)
            {
                case ZoningSymbol.Flashlight:
                    Path(new[] { new Vector2(-.30f, -.17f), new Vector2(.10f, -.17f), new Vector2(.10f, .17f), new Vector2(-.30f, .17f), new Vector2(-.30f, -.17f) });
                    Path(new[] { new Vector2(.10f, -.17f), new Vector2(.25f, -.26f), new Vector2(.25f, .26f), new Vector2(.10f, .17f) });
                    Stroke(new Vector2(.35f, -.24f), new Vector2(.47f, -.33f)); Stroke(new Vector2(.36f, 0), new Vector2(.49f, 0)); Stroke(new Vector2(.35f, .24f), new Vector2(.47f, .33f)); break;
                case ZoningSymbol.Notebook:
                    Rect(-.32f, -.40f, .29f, .40f); Stroke(new Vector2(-.19f, -.40f), new Vector2(-.19f, .40f));
                    for (int i = 0; i < 3; i++) Stroke(new Vector2(-.43f, -.24f + i * .24f), new Vector2(-.23f, -.24f + i * .24f)); break;
                case ZoningSymbol.Eye:
                    var eye = new Vector2[33];
                    for (int i = 0; i < eye.Length; i++) { float a = i * Mathf.PI * 2 / (eye.Length - 1); eye[i] = new Vector2(Mathf.Cos(a) * .46f, Mathf.Sin(a) * .24f); }
                    Path(eye); Circle(Vector2.zero, .115f, 0, Mathf.PI * 2); break;
                case ZoningSymbol.Keypad:
                    Rect(-.34f, -.43f, .34f, .43f); Stroke(new Vector2(-.21f, .25f), new Vector2(.21f, .25f));
                    for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++) Circle(new Vector2(-.18f + x * .18f, .07f - y * .17f), .025f, 0, Mathf.PI * 2); break;
                case ZoningSymbol.Close:
                    Stroke(new Vector2(-.30f, -.30f), new Vector2(.30f, .30f)); Stroke(new Vector2(-.30f, .30f), new Vector2(.30f, -.30f)); break;
                case ZoningSymbol.Check:
                    Path(new[] { new Vector2(-.37f, -.02f), new Vector2(-.10f, -.29f), new Vector2(.40f, .32f) }); break;
                case ZoningSymbol.Play:
                    Path(new[] { new Vector2(-.22f, -.37f), new Vector2(.35f, 0), new Vector2(-.22f, .37f), new Vector2(-.22f, -.37f) }); break;
                case ZoningSymbol.Restart:
                    Circle(Vector2.zero, .34f, Mathf.PI * .15f, Mathf.PI * 1.88f);
                    Path(new[] { new Vector2(.20f, .08f), new Vector2(.34f, -.13f), new Vector2(.46f, .08f) }); break;
                case ZoningSymbol.Backspace:
                    Path(new[] { new Vector2(.40f, .29f), new Vector2(-.10f, .29f), new Vector2(-.43f, 0), new Vector2(-.10f, -.29f), new Vector2(.40f, -.29f), new Vector2(.40f, .29f) });
                    Stroke(new Vector2(.03f, -.13f), new Vector2(.26f, .13f)); Stroke(new Vector2(.03f, .13f), new Vector2(.26f, -.13f)); break;
                case ZoningSymbol.Camera:
                    Rect(-.40f, -.23f, .13f, .23f); Path(new[] { new Vector2(.13f, -.13f), new Vector2(.42f, -.31f), new Vector2(.42f, .31f), new Vector2(.13f, .13f) });
                    Stroke(new Vector2(-.23f, -.24f), new Vector2(-.23f, -.40f)); Stroke(new Vector2(-.39f, -.40f), new Vector2(-.08f, -.40f)); break;
                case ZoningSymbol.Standing:
                    Circle(new Vector2(0, .35f), .085f, 0, Mathf.PI * 2);
                    Path(new[] { new Vector2(0, .22f), new Vector2(0, -.12f), new Vector2(-.18f, -.43f) });
                    Path(new[] { new Vector2(0, -.12f), new Vector2(.18f, -.43f) });
                    Path(new[] { new Vector2(-.23f, -.04f), new Vector2(0, .14f), new Vector2(.23f, -.04f) }); break;
                case ZoningSymbol.Crouched:
                    Circle(new Vector2(.12f, .22f), .085f, 0, Mathf.PI * 2);
                    Path(new[] { new Vector2(.04f, .10f), new Vector2(-.12f, -.12f), new Vector2(.17f, -.24f), new Vector2(.06f, -.42f), new Vector2(.28f, -.42f) });
                    Path(new[] { new Vector2(-.01f, .03f), new Vector2(.18f, -.09f), new Vector2(.33f, -.09f) }); break;
                case ZoningSymbol.Prone:
                    Circle(new Vector2(.35f, -.02f), .085f, 0, Mathf.PI * 2);
                    Path(new[] { new Vector2(.21f, -.12f), new Vector2(-.08f, -.17f), new Vector2(-.42f, -.25f) });
                    Path(new[] { new Vector2(.12f, -.15f), new Vector2(.28f, -.30f), new Vector2(.45f, -.30f) });
                    Stroke(new Vector2(-.42f, -.35f), new Vector2(.45f, -.35f)); break;
                case ZoningSymbol.Sprinting:
                    Circle(new Vector2(.20f, .32f), .085f, 0, Mathf.PI * 2);
                    Path(new[] { new Vector2(.12f, .20f), new Vector2(-.08f, -.06f), new Vector2(-.29f, -.38f) });
                    Path(new[] { new Vector2(-.08f, -.06f), new Vector2(.19f, -.18f), new Vector2(.08f, -.42f) });
                    Path(new[] { new Vector2(-.18f, .22f), new Vector2(.02f, .12f), new Vector2(.24f, .02f), new Vector2(.39f, .12f) });
                    Stroke(new Vector2(-.47f, .06f), new Vector2(-.26f, .06f));
                    Stroke(new Vector2(-.47f, -.08f), new Vector2(-.31f, -.08f)); break;
                case ZoningSymbol.Chevron:
                    Fill(new Vector2(-.42f, -.18f), new Vector2(0, .26f), new Vector2(0, .08f));
                    Fill(new Vector2(-.42f, -.18f), new Vector2(0, .08f), new Vector2(-.30f, -.30f));
                    Fill(new Vector2(.42f, -.18f), new Vector2(0, .08f), new Vector2(0, .26f));
                    Fill(new Vector2(.42f, -.18f), new Vector2(.30f, -.30f), new Vector2(0, .08f)); break;
                case ZoningSymbol.Move:
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI * .5f; Vector2 d = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), n = new Vector2(-d.y, d.x);
                        Path(new[] { d * .2f + n * .1f, d * .38f, d * .2f - n * .1f });
                    }
                    Circle(Vector2.zero, .07f, 0, Mathf.PI * 2); break;
            }
        }
        void Fill(Vector2 a, Vector2 b, Vector2 c)
        {
            int first = helper.currentVertCount;
            helper.AddVert(origin + a * unit, color, Vector2.zero);
            helper.AddVert(origin + b * unit, color, Vector2.zero);
            helper.AddVert(origin + c * unit, color, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2);
        }
        void Rect(float left, float bottom, float right, float top)
        {
            Path(new[] { new Vector2(left, bottom), new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom), new Vector2(left, bottom) });
        }
        void Circle(Vector2 center, float radius, float start, float end)
        {
            Vector2 previous = center + new Vector2(Mathf.Cos(start), Mathf.Sin(start)) * radius;
            for (int i = 1; i <= 32; i++)
            {
                float a = Mathf.Lerp(start, end, i / 32f); Vector2 point = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                Stroke(previous, point); previous = point;
            }
        }
        void Path(Vector2[] points) { for (int i = 1; i < points.Length; i++) Stroke(points[i - 1], points[i]); }
        void Stroke(Vector2 a, Vector2 b)
        {
            Vector2 delta = b - a; if (delta.sqrMagnitude < .000001f) return;
            Vector2 normal = new Vector2(-delta.y, delta.x).normalized * .028f;
            int first = helper.currentVertCount;
            helper.AddVert(origin + (a - normal) * unit, color, Vector2.zero);
            helper.AddVert(origin + (a + normal) * unit, color, Vector2.zero);
            helper.AddVert(origin + (b + normal) * unit, color, Vector2.zero);
            helper.AddVert(origin + (b - normal) * unit, color, Vector2.zero);
            helper.AddTriangle(first, first + 1, first + 2); helper.AddTriangle(first, first + 2, first + 3);
        }
    }
}
