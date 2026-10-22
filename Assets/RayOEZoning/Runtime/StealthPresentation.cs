using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RayOEZoning
{
    /// <summary>
    /// Minimal play interface: a crosshair dot, a detection ring that only appears while a camera has the player,
    /// one prompt line, and plain overlays for the notebook, the keypad and the end of the run.
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
        /// <summary>Shared by the screen-edge vignette and the top half of the detection ring.</summary>
        static readonly Color Red = new Color(.8f, .03f, .02f);
        static readonly Vector2 Center = new Vector2(.5f, .5f);
        const string Controls = "WASD move   SHIFT sprint   C crouch   Z prone   F flashlight   N notebook   E use   F3 debug";
        RectTransform canvas;
        CanvasGroup ringGroup;
        DetectionRing ring;
        TMP_Text detected, prompt, toast, notebookBody, keypadNotice;
        TMP_Text[] slots;
        RectTransform[] pips;
        GameObject notebook, keypad, won;
        Image vignette, curtain;
        Vector3 closedDoorPosition;
        int lastNoteCount;
        string lastNotice = string.Empty;
        float toastUntil, ringAlpha;
        bool built, explainedNotebook;

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

            var ringRoot = Box(canvas, "Detection", Vector2.zero, new Vector2(320, 320));
            ringGroup = ringRoot.gameObject.AddComponent<CanvasGroup>(); ringGroup.alpha = 0;
            ring = Box(ringRoot, "Ring", Vector2.zero, new Vector2(300, 300)).gameObject.AddComponent<DetectionRing>();
            ring.radius = 118; ring.thickness = 7; ring.lowColor = White; ring.highColor = Red; ring.raycastTarget = false;
            Text(ringRoot, "?", "?", 34, White, new Vector2(0, -118), new Vector2(40, 44), TextAlignmentOptions.Center);
            Text(ringRoot, "!", "!", 36, Red, new Vector2(0, 118), new Vector2(40, 44), TextAlignmentOptions.Center);
            detected = Text(ringRoot, "Detected", "DETECTED", 30, Red, Vector2.zero, new Vector2(220, 50), TextAlignmentOptions.Center);
            Outline(detected);
            int count = game.hazards != null ? game.hazards.Length : 0;
            pips = new RectTransform[count];
            for (int i = 0; i < count; i++)
            {
                var pip = Image(ringRoot, "Direction", Red, Vector2.zero, new Vector2(11, 11));
                pip.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
                pips[i] = pip.rectTransform;
            }

            prompt = Text(canvas, "Prompt", "", 24, Soft, new Vector2(0, -200), new Vector2(900, 40), TextAlignmentOptions.Center);
            toast = Text(canvas, "Toast", "", 24, White, new Vector2(0, -240), new Vector2(900, 40), TextAlignmentOptions.Center);
            Outline(prompt); Outline(toast);

            notebook = Overlay("Notebook");
            var page = (RectTransform)notebook.transform;
            Text(page, "Title", "NOTEBOOK", 22, Soft, new Vector2(0, 330), new Vector2(760, 40), TextAlignmentOptions.Center);
            notebookBody = Text(page, "Entries", "", 30, White, new Vector2(0, 40), new Vector2(760, 520), TextAlignmentOptions.Top);
            notebookBody.lineSpacing = 18;
            Text(page, "Close", "N  close", 20, Soft, new Vector2(0, -300), new Vector2(760, 30), TextAlignmentOptions.Center);
            Text(page, "Controls", Controls, 18, new Color(1, 1, 1, .38f), new Vector2(0, -420), new Vector2(1500, 30), TextAlignmentOptions.Center);

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
            notebook.SetActive(game.Mode == GameMode.Notebook);
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
                pips[i].anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 162;
            }
            float target = caught || (playing && (seen || game.Alert > .001f)) ? 1 : 0;
            ringAlpha = Mathf.MoveTowards(ringAlpha, target, step * (target > ringAlpha ? 8 : 2.5f));
            ringGroup.alpha = ringAlpha;
            ring.SetFill(caught ? 1 : game.Alert);
            detected.gameObject.SetActive(caught);
            float danger = playing || caught ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.2f, 1, game.Alert)) : 0;
            vignette.color = new Color(Red.r, Red.g, Red.b, danger * (.42f + .14f * Mathf.Sin(now * (5 + 9 * game.Alert))));
            curtain.color = new Color(0, 0, 0, caught ? Mathf.Clamp01(game.CaughtFade / .65f) * .85f : 0);

            prompt.text = playing ? game.Prompt : "";
            if (game.Notes.Count > lastNoteCount)
            {
                var note = game.Notes[game.Notes.Count - 1];
                string text = note.kind == ClueKind.Code ? "Noted  " + note.symbol.ToUpperInvariant() + "  " + note.digit : "Noted  order note";
                if (!explainedNotebook) { text += "   (N  notebook)"; explainedNotebook = true; }
                Toast(text, now);
            }
            lastNoteCount = game.Notes.Count;
            if (game.Notice != lastNotice)
            {
                lastNotice = game.Notice;
                if (game.ExitUnlocked && !string.IsNullOrEmpty(lastNotice) && playing) Toast(lastNotice, now);
            }
            toast.alpha = playing ? Mathf.Clamp01((toastUntil - now) / .6f) : 0;

            if (game.Mode == GameMode.Notebook) notebookBody.text = NotebookText();
            for (int i = 0; i < 4; i++) slots[i].text = i < game.KeypadInput.Length ? game.KeypadInput[i].ToString() : "";
            keypadNotice.text = game.Mode == GameMode.Keypad && game.Notice == "Wrong code" ? "Wrong code" : "";
        }

        void Toast(string text, float now) { toast.text = text; toastUntil = now + 2.6f; }

        string NotebookText()
        {
            if (game.Notes.Count == 0) return "<alpha=#80>Nothing yet. Shine the flashlight on notes, screens and signs.";
            var text = new StringBuilder();
            foreach (var note in game.Notes)
                if (note.kind == ClueKind.Code) text.Append(note.symbol.ToUpperInvariant()).Append("   ").Append(note.digit).Append('\n');
            bool header = false;
            foreach (var note in game.Notes)
            {
                if (note.kind != ClueKind.Order) continue;
                if (!header) { text.Append("\n<alpha=#99>ORDER<alpha=#FF>\n"); header = true; }
                foreach (string statement in note.statements) text.Append(statement).Append('\n');
            }
            return text.ToString();
        }

        static Sprite radial;
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
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false; text.richText = true; return text;
        }
        /// <summary>A thin dark edge keeps a line legible over the flashlight's bright pool.</summary>
        static void Outline(TMP_Text text) { text.outlineWidth = .22f; text.outlineColor = new Color32(0, 0, 0, 200); }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
    }
}
