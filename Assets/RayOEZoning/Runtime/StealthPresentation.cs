using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RayOEZoning
{
    /// <summary>
    /// Minimal play interface: a crosshair dot, a detection ring that only appears while a camera has the player,
    /// one prompt line for the terminal, and plain overlays for the keypad and the end of the run.
    /// </summary>
    [DefaultExecutionOrder(1200)]
    public sealed class StealthPresentation : MonoBehaviour
    {
        public StealthGame game;
        public GameObject exitGrid;
        public Transform door;
        public TMP_FontAsset font;
        public Light[] corridorLights;
        static readonly Color White = new Color(.94f, .95f, .95f), Soft = new Color(.94f, .95f, .95f, .6f);
        /// <summary>Shared by the screen-edge vignette and the top of the detection ring.</summary>
        static readonly Color Red = new Color(.8f, .03f, .02f);
        static readonly Color Amber = new Color(1f, .6f, .2f);
        static readonly Vector2 Center = new Vector2(.5f, .5f);
        const float RingSize = 250;
        RectTransform canvas;
        CanvasGroup ringGroup;
        Image fillLeft, fillRight;
        TMP_Text detected, prompt, toast, keypadNotice;
        TMP_Text[] slots;
        RectTransform[] pips;
        GameObject keypad, won;
        Image vignette, curtain;
        Vector3 closedDoorPosition;
        string lastNotice = string.Empty;
        float toastUntil, ringAlpha;
        bool built;

        void Start()
        {
            if (!game) game = GetComponent<StealthGame>();
            if (!font) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (door) closedDoorPosition = door.localPosition;
            Build();
        }

        void Build()
        {
            var root = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false); canvas = root.GetComponent<RectTransform>();
            var ui = root.GetComponent<Canvas>(); ui.renderMode = RenderMode.ScreenSpaceOverlay; ui.sortingOrder = 50;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            if (!FindAnyObjectByType<EventSystem>())
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            vignette = Image(canvas, "Vignette", Color.clear, Vector2.zero, Vector2.zero); Stretch(vignette.rectTransform);
            vignette.sprite = RadialSprite();
            Image(canvas, "Crosshair", Soft, Vector2.zero, new Vector2(4, 4));

            // Translucent ring that fills from the bottom up both sides at once, white through amber to red.
            var ringRoot = Box(canvas, "Detection", Vector2.zero, Vector2.one * RingSize);
            ringGroup = ringRoot.gameObject.AddComponent<CanvasGroup>(); ringGroup.alpha = 0;
            var track = Image(ringRoot, "Track", new Color(1, 1, 1, .2f), Vector2.zero, Vector2.one * RingSize);
            track.sprite = RingSprite(false);
            fillLeft = RingFill(ringRoot, true);
            fillRight = RingFill(ringRoot, false);
            detected = Text(ringRoot, "Detected", "DETECTED", 28, Red, Vector2.zero, new Vector2(200, 44), TextAlignmentOptions.Center);
            Outline(detected);
            int count = game.hazards != null ? game.hazards.Length : 0;
            pips = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                var pip = Image(ringRoot, "Direction", Red, Vector2.zero, new Vector2(16, 16));
                pip.sprite = ArrowSprite();
                pips[i] = pip.rectTransform;
            }

            prompt = Text(canvas, "Prompt", "", 24, Soft, new Vector2(0, -200), new Vector2(900, 40), TextAlignmentOptions.Center);
            toast = Text(canvas, "Toast", "", 24, White, new Vector2(0, -240), new Vector2(900, 40), TextAlignmentOptions.Center);
            Outline(prompt); Outline(toast);

            keypad = Overlay("Keypad");
            var pad = (RectTransform)keypad.transform;
            slots = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                float x = -150 + i * 100;
                slots[i] = Text(pad, "Digit", "", 64, White, new Vector2(x, 250), new Vector2(80, 90), TextAlignmentOptions.Center);
                Image(pad, "Underline", Soft, new Vector2(x, 200), new Vector2(64, 2));
            }
            keypadNotice = Text(pad, "Notice", "", 22, Red, new Vector2(0, 160), new Vector2(500, 32), TextAlignmentOptions.Center);
            for (int i = 0; i < 12; i++)
            {
                string label = i < 9 ? (i + 1).ToString() : i == 9 ? "DEL" : i == 10 ? "0" : "OK";
                var at = new Vector2(-110 + (i % 3) * 110, 70 - (i / 3) * 90);
                UnityEngine.Events.UnityAction action;
                if (i == 9) action = game.Backspace;
                else if (i == 11) action = () => game.SubmitCode();
                else action = () => game.AppendDigit(label);
                TextButton(pad, label, at, new Vector2(96, 76), i == 9 || i == 11 ? 22 : 34, action);
            }
            Text(pad, "Hint", "Type the code   ENTER submit   ESC cancel", 20, Soft, new Vector2(0, -330), new Vector2(900, 30), TextAlignmentOptions.Center);

            won = Overlay("Complete");
            var end = (RectTransform)won.transform;
            Text(end, "Title", "ARCHIVE OPEN", 54, White, new Vector2(0, 60), new Vector2(900, 80), TextAlignmentOptions.Center);
            TextButton(end, "R  play again", new Vector2(0, -50), new Vector2(260, 56), 24, game.NewGame);

            curtain = Image(canvas, "Fade", Color.clear, Vector2.zero, Vector2.zero); Stretch(curtain.rectTransform);
            ringRoot.SetAsLastSibling();
            built = true;
        }

        void LateUpdate()
        {
            if (!built || !game) return;
            bool playing = game.Mode == GameMode.Playing, caught = game.Mode == GameMode.Caught;
            float now = Time.unscaledTime, step = Time.unscaledDeltaTime;
            if (exitGrid) exitGrid.SetActive(!game.ExitUnlocked);
            if (door) door.localPosition = Vector3.Lerp(door.localPosition, closedDoorPosition + (game.ExitUnlocked ? Vector3.up * 3.6f : Vector3.zero), step * .8f);
            keypad.SetActive(game.Mode == GameMode.Keypad);
            won.SetActive(game.Mode == GameMode.Won);

            // The ring only exists while a camera has the player or the meter is still draining.
            bool seen = false;
            for (int i = 0; i < pips.Length; i++)
            {
                var hazard = game.hazards[i];
                bool active = playing && hazard && hazard.IsDetecting;
                seen |= active;
                pips[i].gameObject.SetActive(active && game.playerCamera);
                if (!pips[i].gameObject.activeSelf) continue;
                Vector3 local = game.playerCamera.transform.InverseTransformPoint(hazard.SightOrigin);
                float angle = Mathf.Atan2(local.x, local.z);
                pips[i].anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * (RingSize * .5f + 14);
                pips[i].localEulerAngles = new Vector3(0, 0, -angle * Mathf.Rad2Deg);
            }
            float target = caught || (playing && (seen || game.Alert > .001f)) ? 1 : 0;
            ringAlpha = Mathf.MoveTowards(ringAlpha, target, step * (target > ringAlpha ? 8 : 2.5f));
            ringGroup.alpha = ringAlpha;
            float fill = caught ? 1 : game.Alert;
            fillLeft.fillAmount = fillRight.fillAmount = fill * .5f;
            detected.gameObject.SetActive(caught);
            float danger = playing || caught ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.2f, 1, game.Alert)) : 0;
            vignette.color = new Color(Red.r, Red.g, Red.b, danger * (.42f + .14f * Mathf.Sin(now * (5 + 9 * game.Alert))));
            curtain.color = new Color(0, 0, 0, caught ? Mathf.Clamp01(game.CaughtFade / .65f) * .85f : 0);

            prompt.text = playing ? game.Prompt : "";
            if (game.Notice != lastNotice)
            {
                lastNotice = game.Notice;
                if (game.ExitUnlocked && !string.IsNullOrEmpty(lastNotice) && playing) { toast.text = lastNotice; toastUntil = now + 2.6f; }
            }
            toast.alpha = playing ? Mathf.Clamp01((toastUntil - now) / .6f) : 0;

            for (int i = 0; i < 4; i++) slots[i].text = i < game.KeypadInput.Length ? game.KeypadInput[i].ToString() : "";
            keypadNotice.text = game.Mode == GameMode.Keypad && game.Notice == "Wrong code" ? "Wrong code" : "";
        }

        Image RingFill(RectTransform parent, bool left)
        {
            var fill = Image(parent, "Fill", new Color(1, 1, 1, .85f), Vector2.zero, Vector2.one * RingSize);
            fill.sprite = RingSprite(true);
            fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
            fill.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Bottom;
            // Clockwise from the bottom runs up the left side; counter-clockwise runs up the right.
            fill.fillClockwise = left; fill.fillAmount = 0;
            return fill;
        }

        static Sprite trackSprite, fillSprite, arrowSprite, radial;

        /// <summary>Anti-aliased ring. The fill version bakes the gradient by height: white at the bottom, red at the top.</summary>
        static Sprite RingSprite(bool gradient)
        {
            if (gradient ? fillSprite : trackSprite) return gradient ? fillSprite : trackSprite;
            const int size = 512;
            const float outer = size * .5f - 4, thickness = 22;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = gradient ? "Ring fill" : "Ring track" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x + .5f - size * .5f, dy = y + .5f - size * .5f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float coverage = Mathf.Clamp01(outer - r + .5f) * Mathf.Clamp01(r - (outer - thickness) + .5f);
                    Color c = White;
                    if (gradient)
                    {
                        float t = Mathf.Acos(Mathf.Clamp(-dy / Mathf.Max(r, .001f), -1, 1)) / Mathf.PI;
                        c = t < .5f ? Color.Lerp(White, Amber, t / .5f) : Color.Lerp(Amber, Red, (t - .5f) / .5f);
                    }
                    c.a = coverage;
                    pixels[y * size + x] = c;
                }
            texture.SetPixels32(pixels); texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            if (gradient) fillSprite = sprite; else trackSprite = sprite;
            return sprite;
        }

        /// <summary>Small arrow pointing up, rotated to face the watching camera.</summary>
        static Sprite ArrowSprite()
        {
            if (arrowSprite) return arrowSprite;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Direction arrow" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + .5f) / size, v = (y + .5f) / size;
                    float halfWidth = (1 - v) * .45f;
                    float edge = Mathf.Min(halfWidth - Mathf.Abs(u - .5f), Mathf.Min(v - .1f, .92f - v)) * size;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.Clamp01(edge + .5f)));
                }
            texture.SetPixels32(pixels); texture.Apply();
            arrowSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            return arrowSprite;
        }

        static Sprite RadialSprite()
        {
            if (radial) return radial;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Vignette" };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + .5f) / size * 2 - 1, dy = (y + .5f) / size * 2 - 1;
                    float r = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255 * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.42f, 1f, r))));
                }
            texture.SetPixels32(pixels); texture.Apply();
            radial = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f));
            return radial;
        }

        GameObject Overlay(string name)
        {
            var overlay = Image(canvas, name, new Color(0, 0, 0, .96f), Vector2.zero, Vector2.zero);
            Stretch(overlay.rectTransform); overlay.raycastTarget = true; return overlay.gameObject;
        }
        void TextButton(RectTransform parent, string label, Vector2 at, Vector2 size, float fontSize, UnityEngine.Events.UnityAction action)
        {
            var background = Image(parent, "Button", new Color(1, 1, 1, .06f), at, size); background.raycastTarget = true;
            var button = background.gameObject.AddComponent<Button>(); button.targetGraphic = background;
            var colors = button.colors; colors.highlightedColor = new Color(2.4f, 2.4f, 2.4f); colors.pressedColor = new Color(4, 4, 4); colors.selectedColor = Color.white; colors.fadeDuration = .08f; button.colors = colors;
            var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            button.onClick.AddListener(action);
            Text(background.rectTransform, "Text (TMP)", label, fontSize, White, Vector2.zero, size, TextAlignmentOptions.Center);
        }
        static RectTransform Box(RectTransform parent, string name, Vector2 at, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = Center; rt.pivot = Center; rt.anchoredPosition = at; rt.sizeDelta = size; return rt;
        }
        static Image Image(RectTransform parent, string name, Color color, Vector2 at, Vector2 size)
        {
            var rt = Box(parent, name, at, size); var image = rt.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        TMP_Text Text(RectTransform parent, string name, string value, float size, Color color, Vector2 at, Vector2 area, TextAlignmentOptions align)
        {
            var rt = Box(parent, name, at, area); var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color; text.alignment = align; text.fontStyle = FontStyles.Bold;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false; text.richText = false; return text;
        }
        /// <summary>A thin dark edge keeps a line legible over the flashlight's bright pool.</summary>
        static void Outline(TMP_Text text) { text.outlineWidth = .22f; text.outlineColor = new Color32(0, 0, 0, 200); }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
    }
}
