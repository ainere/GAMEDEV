using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RayOEZoning
{
    /// <summary>Icon-only play interface. Written language is reserved for the actual puzzle clues.</summary>
    [DefaultExecutionOrder(1200)]
    public sealed class StealthPresentation : MonoBehaviour
    {
        public StealthGame game;
        public GameObject exitGrid;
        public Transform door;
        public TMP_FontAsset font;
        public Light[] corridorLights;
        static readonly Color Ink = new Color(.018f, .030f, .040f), Paper = new Color(.84f, .90f, .87f);
        static readonly Color Quiet = new Color(.38f, .50f, .54f), Cyan = new Color(.26f, .65f, .70f), Amber = new Color(.94f, .56f, .18f), Red = new Color(.98f, .09f, .12f);
        static readonly Vector2 Center = new Vector2(.5f, .5f), Top = new Vector2(.5f, 1), Bottom = new Vector2(.5f, 0), BottomLeft = Vector2.zero, BottomRight = Vector2.right;
        RectTransform canvas;
        GameObject notebook, keypad, pause, won, caught, exposure, interaction, reading;
        TMP_Text[] noteTitles, noteDigits, noteRelations, entryDigits;
        UnityEngine.UI.Image alertFill, readFill, curtain;
        UnityEngine.UI.Image[] noteDots;
        ZoningIcon torchIcon, noteIcon, rejected, exposureIcon, stanceIcon;
        ZoningIcon[] threats;
        float[] threatAlpha;
        UnityEngine.UI.Image vignette;
        CanvasGroup controls;
        float controlsUntil;
        Vector3 closedDoorPosition;
        int lastNoteCount;
        float recordedUntil;
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
            var root = new GameObject("Icon interface", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(transform, false); canvas = root.GetComponent<RectTransform>();
            var ui = root.GetComponent<Canvas>(); ui.renderMode = RenderMode.ScreenSpaceOverlay; ui.sortingOrder = 50;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            if (!FindAnyObjectByType<EventSystem>())
            {
                var events = new GameObject("Interface input", typeof(EventSystem), typeof(InputSystemUIInputModule)); events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            vignette = Image(canvas, "Exposure vignette", Color.clear, Vector2.zero, Vector2.zero, Center); Stretch(vignette.rectTransform);
            vignette.sprite = RadialSprite();
            Image(canvas, "Sight point", new Color(.8f, .9f, .87f, .65f), Vector2.zero, new Vector2(3, 3), Center);
            // One chevron per camera; each points at the lens that currently sees the player.
            var ring = Box(canvas, "Threat direction ring", Vector2.zero, new Vector2(10, 10), Center);
            int count = game.hazards != null ? game.hazards.Length : 0;
            threats = new ZoningIcon[count]; threatAlpha = new float[count];
            for (int i = 0; i < count; i++) { threats[i] = Icon(ring, ZoningSymbol.Chevron, Vector2.zero, 46, Red, Center); threats[i].gameObject.SetActive(false); }
            torchIcon = Icon(canvas, ZoningSymbol.Flashlight, new Vector2(58, 58), 32, Cyan, BottomLeft);
            noteIcon = Icon(canvas, ZoningSymbol.Notebook, new Vector2(119, 58), 32, Quiet, BottomLeft);
            stanceIcon = Icon(canvas, ZoningSymbol.Standing, new Vector2(-66, 58), 36, Paper, BottomRight);
            noteDots = new UnityEngine.UI.Image[4];
            for (int i = 0; i < 4; i++) noteDots[i] = Image(canvas, "Recorded clue indicator", Quiet, new Vector2(150 + i * 13, 58), new Vector2(5, 5), BottomLeft);
            interaction = Box(canvas, "Terminal affordance", new Vector2(0, -66), new Vector2(36, 36), Center).gameObject;
            Icon((RectTransform)interaction.transform, ZoningSymbol.Keypad, Vector2.zero, 32, Cyan, Center);
            reading = Box(canvas, "Clue reading", new Vector2(0, -190), new Vector2(90, 36), Center).gameObject;
            Icon((RectTransform)reading.transform, ZoningSymbol.Eye, Vector2.zero, 26, Paper, Center);
            Image((RectTransform)reading.transform, "Read track", Quiet, new Vector2(0, -24), new Vector2(60, 2), Center);
            readFill = Image((RectTransform)reading.transform, "Read amount", Cyan, new Vector2(-30, -24), new Vector2(0, 2), Center); readFill.rectTransform.pivot = new Vector2(0, .5f);
            exposure = Box(canvas, "Exposure", new Vector2(0, 64), new Vector2(468, 70), Bottom).gameObject;
            var meter = (RectTransform)exposure.transform;
            Image(meter, "Exposure instrument backing", new Color(.009f, .021f, .028f, .94f), Vector2.zero, new Vector2(468, 70), Center);
            Image(meter, "Exposure instrument top edge", new Color(.24f, .43f, .46f, .8f), new Vector2(0, 35), new Vector2(468, 1), Center);
            Image(meter, "Exposure instrument lower edge", new Color(.12f, .25f, .29f, .8f), new Vector2(0, -35), new Vector2(468, 1), Center);
            exposureIcon = Icon(meter, ZoningSymbol.Camera, new Vector2(-194, 0), 34, Paper, Center);
            Image(meter, "Exposure track border", new Color(.40f, .54f, .58f), new Vector2(26, 0), new Vector2(340, 18), Center);
            Image(meter, "Exposure track", new Color(.065f, .10f, .12f), new Vector2(26, 0), new Vector2(336, 14), Center);
            alertFill = Image(meter, "Exposure amount", Paper, new Vector2(-142, 0), new Vector2(0, 14), Center); alertFill.rectTransform.pivot = new Vector2(0, .5f);
            for (int i = 1; i < 12; i++)
                Image(meter, "Exposure division", new Color(.009f, .021f, .028f), new Vector2(-142 + i * 28, 0), new Vector2(3, 14), Center);
            Image(meter, "Full exposure limit", Red, new Vector2(202, 0), new Vector2(3, 24), Center);
            Image(meter, "Caution zone", new Color(Amber.r, Amber.g, Amber.b, .55f), new Vector2(-142 + 336 * .45f + 336 * .2f, -12), new Vector2(336 * .4f, 2), Center);
            Image(meter, "Danger zone", new Color(Red.r, Red.g, Red.b, .7f), new Vector2(-142 + 336 * .85f + 336 * .075f, -12), new Vector2(336 * .15f, 2), Center);

            notebook = Overlay("Clue notebook");
            var notes = Panel((RectTransform)notebook.transform, new Vector2(1060, 760));
            IconButton(notes, ZoningSymbol.Close, new Vector2(470, 325), new Vector2(56, 56), () => game.CloseOverlay());
            noteTitles = new TMP_Text[4]; noteDigits = new TMP_Text[4]; noteRelations = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                float y = 230 - i * 155;
                noteDigits[i] = Text(notes, "Recorded digit", "", 54, Cyan, new Vector2(-410, y), new Vector2(100, 80));
                noteTitles[i] = Text(notes, "Recorded clue", "", 23, Paper, new Vector2(80, y + 20), new Vector2(720, 38));
                noteRelations[i] = Text(notes, "Recorded statement", "", 26, Paper, new Vector2(80, y - 30), new Vector2(720, 76));
            }

            keypad = Overlay("Terminal"); var pad = Panel((RectTransform)keypad.transform, new Vector2(568, 750));
            IconButton(pad, ZoningSymbol.Close, new Vector2(232, 323), new Vector2(54, 54), () => game.CloseOverlay());
            entryDigits = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                float x = -163.5f + i * 109;
                Image(pad, "Digit slot", new Color(.007f, .018f, .022f), new Vector2(x, 235), new Vector2(91, 94), Center);
                Image(pad, "Slot baseline", Quiet, new Vector2(x, 188), new Vector2(91, 2), Center);
                entryDigits[i] = Text(pad, "Entered digit", "", 54, Cyan, new Vector2(x, 236), new Vector2(85, 90), TextAlignmentOptions.Center);
            }
            rejected = Icon(pad, ZoningSymbol.Close, new Vector2(0, 154), 20, Red, Center);
            for (int i = 0; i < 9; i++)
            {
                string digit = (i + 1).ToString();
                var button = Button(pad, new Vector2(-153 + (i % 3) * 153, 78 - (i / 3) * 110), new Vector2(136, 92), () => game.AppendDigit(digit));
                Text(button, "Numeric button", digit, 37, Paper, Vector2.zero, new Vector2(120, 80), TextAlignmentOptions.Center);
            }
            IconButton(pad, ZoningSymbol.Backspace, new Vector2(-153, -252), new Vector2(136, 92), () => game.Backspace());
            var zero = Button(pad, new Vector2(0, -252), new Vector2(136, 92), () => game.AppendDigit("0"));
            Text(zero, "Numeric button", "0", 37, Paper, Vector2.zero, new Vector2(120, 80), TextAlignmentOptions.Center);
            IconButton(pad, ZoningSymbol.Check, new Vector2(153, -252), new Vector2(136, 92), () => game.SubmitCode());

            pause = Overlay("Paused"); var paused = Panel((RectTransform)pause.transform, new Vector2(560, 560));
            IconButton(paused, ZoningSymbol.Play, new Vector2(0, 190), new Vector2(96, 96), () => game.CloseOverlay());
            ControlsLegend(paused, new Vector2(0, -70), 1);
            var intro = Box(canvas, "Controls reminder", new Vector2(150, 0), new Vector2(260, 420), new Vector2(0, .5f));
            controls = intro.gameObject.AddComponent<CanvasGroup>();
            Image(intro, "Reminder backing", new Color(.006f, .016f, .022f, .72f), Vector2.zero, new Vector2(260, 420), Center);
            ControlsLegend(intro, Vector2.zero, .82f);
            controlsUntil = Time.unscaledTime + 9;
            won = Overlay("Archive accessed"); var complete = Panel((RectTransform)won.transform, new Vector2(380, 330));
            Icon(complete, ZoningSymbol.Check, new Vector2(0, 70), 90, Cyan, Center);
            IconButton(complete, ZoningSymbol.Restart, new Vector2(0, -68), new Vector2(90, 78), () => game.NewGame());
            curtain = Image(canvas, "Security reset fade", Color.clear, Vector2.zero, Vector2.zero, Center); Stretch(curtain.rectTransform);
            caught = Box(curtain.rectTransform, "Security reset icon", Vector2.zero, new Vector2(100, 100), Center).gameObject;
            Icon((RectTransform)caught.transform, ZoningSymbol.Camera, Vector2.zero, 82, Red, Center);
            built = true;
        }

        void LateUpdate()
        {
            if (!built || !game) return;
            bool playing = game.Mode == GameMode.Playing;
            if (exitGrid) exitGrid.SetActive(!game.ExitUnlocked);
            if (door) door.localPosition = Vector3.Lerp(door.localPosition, closedDoorPosition + (game.ExitUnlocked ? Vector3.up * 3.6f : Vector3.zero), Time.unscaledDeltaTime * .8f);
            notebook.SetActive(game.Mode == GameMode.Notebook); keypad.SetActive(game.Mode == GameMode.Keypad);
            pause.SetActive(game.Mode == GameMode.Paused); won.SetActive(game.Mode == GameMode.Won);
            torchIcon.gameObject.SetActive(playing); noteIcon.gameObject.SetActive(playing);
            stanceIcon.gameObject.SetActive(playing);
            if (game.stance)
            {
                ZoningSymbol symbol = game.stance.IsSprinting ? ZoningSymbol.Sprinting :
                    game.stance.CurrentStance == PlayerStance.Prone ? ZoningSymbol.Prone :
                    game.stance.CurrentStance == PlayerStance.Crouched ? ZoningSymbol.Crouched : ZoningSymbol.Standing;
                if (stanceIcon.symbol != symbol) { stanceIcon.symbol = symbol; stanceIcon.SetVerticesDirty(); }
                stanceIcon.color = game.stance.IsSprinting ? Cyan : Paper;
            }
            torchIcon.color = game.TorchOn ? Cyan : Quiet;
            if (game.Notes.Count > lastNoteCount) recordedUntil = Time.unscaledTime + 1.7f;
            lastNoteCount = game.Notes.Count; noteIcon.color = Time.unscaledTime < recordedUntil ? Cyan : Quiet;
            for (int i = 0; i < noteDots.Length; i++) { noteDots[i].gameObject.SetActive(playing); noteDots[i].color = i < game.Notes.Count ? Cyan : Quiet; }
            interaction.SetActive(playing && game.CanUseKeypad);
            float progress = 0; bool visibleUnread = false;
            if (playing && game.clues != null)
                foreach (var clue in game.clues)
                {
                    if (!clue || !clue.IsInSight) continue;
                    bool known = false;
                    foreach (var note in game.Notes) if (note.id == PuzzleCatalog.Clues[clue.clueIndex].id) { known = true; break; }
                    if (!known) { visibleUnread = true; progress = Mathf.Max(progress, clue.ReadProgress / .8f); }
                }
            reading.SetActive(visibleUnread && !game.CanUseKeypad); readFill.rectTransform.sizeDelta = new Vector2(60 * progress, 2);
            exposure.SetActive(playing);
            alertFill.rectTransform.sizeDelta = new Vector2(336 * game.Alert, 14);
            alertFill.color = game.Alert < .45f ? Color.Lerp(Paper, Amber, game.Alert / .45f) : Color.Lerp(Amber, Red, (game.Alert - .45f) / .55f);
            bool seen = false;
            float now = Time.unscaledTime, step = Time.unscaledDeltaTime;
            for (int i = 0; i < threats.Length; i++)
            {
                var hazard = game.hazards[i];
                bool active = playing && hazard && hazard.IsDetecting;
                bool covered = playing && hazard && !hazard.IsDetecting && hazard.HasSightSample && hazard.LastRayBlocked;
                seen |= active;
                threatAlpha[i] = Mathf.MoveTowards(threatAlpha[i], active ? 1 : covered ? .55f : 0, step * (active ? 8 : 2.5f));
                threats[i].gameObject.SetActive(threatAlpha[i] > .01f && playing);
                if (!threats[i].gameObject.activeSelf || !game.playerCamera) continue;
                Vector3 local = game.playerCamera.transform.InverseTransformPoint(hazard.SightOrigin);
                float angle = Mathf.Atan2(local.x, local.z);
                var rt = threats[i].rectTransform;
                rt.anchoredPosition = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * 150;
                rt.localEulerAngles = new Vector3(0, 0, -angle * Mathf.Rad2Deg);
                threats[i].color = active ? new Color(Red.r, Red.g, Red.b, threatAlpha[i] * (.75f + .25f * Mathf.Sin(now * 14)))
                    : new Color(Cyan.r, Cyan.g, Cyan.b, threatAlpha[i]);
            }
            exposureIcon.color = seen ? Color.Lerp(Red, Paper, .5f + .5f * Mathf.Sin(now * 16)) : game.Alert > .002f ? Color.Lerp(Paper, Amber, game.Alert) : Quiet;
            float danger = playing || game.Mode == GameMode.Caught ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.2f, 1, game.Alert)) : 0;
            vignette.color = new Color(.8f, .03f, .02f, danger * (.42f + .14f * Mathf.Sin(now * (5 + 9 * game.Alert))));
            if (controls)
            {
                if (playing && game.inputs && game.inputs.move.sqrMagnitude > .01f && controlsUntil > now + 2.5f) controlsUntil = now + 2.5f;
                controls.alpha = playing ? Mathf.Clamp01((controlsUntil - now) / 1.2f) : 0;
                controls.gameObject.SetActive(controls.alpha > .001f);
            }
            curtain.color = new Color(.006f, .010f, .014f, game.Mode == GameMode.Caught ? Mathf.Clamp01(game.CaughtFade / .65f) : 0);
            caught.SetActive(game.Mode == GameMode.Caught);
            if (game.Mode == GameMode.Notebook)
                for (int i = 0; i < 4; i++)
                {
                    bool known = i < game.Notes.Count;
                    noteDigits[i].text = known ? game.Notes[i].digit : "";
                    noteTitles[i].text = known ? game.Notes[i].title : "";
                    noteRelations[i].text = known ? game.Notes[i].relation : "";
                }
            for (int i = 0; i < 4; i++) entryDigits[i].text = i < game.KeypadInput.Length ? game.KeypadInput[i].ToString() : "";
            rejected.gameObject.SetActive(game.Mode == GameMode.Keypad && !string.IsNullOrEmpty(game.Notice));
        }

        static Sprite radial;
        static Sprite RadialSprite()
        {
            if (radial) return radial;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Exposure vignette" };
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

        /// <summary>Key caps paired with the interface icons for each action.</summary>
        void ControlsLegend(RectTransform parent, Vector2 at, float scale)
        {
            var legend = Box(parent, "Controls legend", at, new Vector2(240, 380), Center);
            legend.localScale = Vector3.one * scale;
            var rows = new (string key, ZoningSymbol icon)[] {
                ("WASD", ZoningSymbol.Move), ("SHIFT", ZoningSymbol.Sprinting), ("C", ZoningSymbol.Crouched), ("Z", ZoningSymbol.Prone),
                ("F", ZoningSymbol.Flashlight), ("N", ZoningSymbol.Notebook), ("E", ZoningSymbol.Keypad), ("F3", ZoningSymbol.Eye) };
            for (int i = 0; i < rows.Length; i++)
            {
                float y = 168 - i * 48;
                float width = rows[i].key.Length > 2 ? 96 : 48;
                Image(legend, "Key cap edge", new Color(.30f, .44f, .48f), new Vector2(-40 - (width - 48) * .5f, y), new Vector2(width, 38), Center);
                Image(legend, "Key cap", new Color(.03f, .06f, .075f), new Vector2(-40 - (width - 48) * .5f, y), new Vector2(width - 4, 34), Center);
                Text(legend, "Key label", rows[i].key, 18, Paper, new Vector2(-40 - (width - 48) * .5f, y), new Vector2(width, 34), TextAlignmentOptions.Center);
                Icon(legend, rows[i].icon, new Vector2(60, y), 34, Cyan, Center);
            }
        }

        GameObject Overlay(string name)
        {
            var overlay = Image(canvas, name, new Color(.005f, .010f, .014f, .94f), Vector2.zero, Vector2.zero, Center);
            Stretch(overlay.rectTransform); overlay.raycastTarget = true; return overlay.gameObject;
        }
        RectTransform Panel(RectTransform parent, Vector2 size)
        {
            var panel = Image(parent, "Modal panel", Ink, Vector2.zero, size, Center);
            Image(panel.rectTransform, "Panel edge", new Color(.14f, .28f, .31f), new Vector2(0, size.y * .5f), new Vector2(size.x, 2), Center); return panel.rectTransform;
        }
        RectTransform Button(RectTransform parent, Vector2 at, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var bg = Image(parent, "Icon button", new Color(.055f, .09f, .11f), at, size, Center); bg.raycastTarget = true;
            var button = bg.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = bg;
            var colors = button.colors; colors.highlightedColor = new Color(1.6f, 1.8f, 1.8f); colors.pressedColor = Cyan; colors.selectedColor = Color.white; colors.fadeDuration = .12f; button.colors = colors;
            var navigation = button.navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = navigation;
            button.onClick.AddListener(action); return bg.rectTransform;
        }
        void IconButton(RectTransform parent, ZoningSymbol symbol, Vector2 at, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var button = Button(parent, at, size, action); Icon(button, symbol, Vector2.zero, Mathf.Min(size.x, size.y) * .48f, Cyan, Center);
        }
        ZoningIcon Icon(RectTransform parent, ZoningSymbol symbol, Vector2 at, float size, Color color, Vector2 anchor)
        {
            var rt = Box(parent, symbol.ToString(), at, Vector2.one * size, anchor); var icon = rt.gameObject.AddComponent<ZoningIcon>(); icon.symbol = symbol; icon.color = color; icon.raycastTarget = false; return icon;
        }
        static RectTransform Box(RectTransform parent, string name, Vector2 at, Vector2 size, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform)); var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = Center; rt.anchoredPosition = at; rt.sizeDelta = size; return rt;
        }
        static UnityEngine.UI.Image Image(RectTransform parent, string name, Color color, Vector2 at, Vector2 size, Vector2 anchor)
        {
            var rt = Box(parent, name, at, size, anchor); var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false; return image;
        }
        TMP_Text Text(RectTransform parent, string name, string value, float size, Color color, Vector2 at, Vector2 area, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var rt = Box(parent, name, at, area, Center); var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color; text.alignment = align;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow; text.raycastTarget = false; text.richText = false; return text;
        }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
    }
}
