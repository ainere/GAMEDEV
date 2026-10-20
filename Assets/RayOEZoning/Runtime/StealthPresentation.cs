using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace RayOEZoning
{
    /// <summary>Presentation only. All puzzle, input focus and retry decisions live in StealthGame.</summary>
    public sealed class StealthPresentation : MonoBehaviour
    {
        public StealthGame game;
        public GameObject exitGrid;
        public Transform door;
        public TMP_FontAsset font;
        public Light[] corridorLights;

        static readonly Color Ink = new Color(.035f, .060f, .085f);
        static readonly Color Paper = new Color(.91f, .94f, .93f);
        static readonly Color Quiet = new Color(.53f, .65f, .70f);
        static readonly Color Cyan = new Color(.34f, .78f, .83f);
        static readonly Color Amber = new Color(.95f, .65f, .31f);
        static readonly Color Red = new Color(.94f, .37f, .29f);
        RectTransform canvas;
        TMP_Text objective, sector, torchState, prompt, notice, alertLabel, diagnostic, notebookCount;
        TMP_Text[] noteTitles, noteDigits, noteRelations;
        TMP_Text keypadEntry, keypadNotice;
        UnityEngine.UI.Image alertFill, curtain;
        GameObject alertGroup, notebook, keypad, pause, won, onboarding, caughtMessage;
        Vector3 closedDoorPosition;
        readonly List<Light> sceneLights = new List<Light>();
        readonly List<float> lightLevels = new List<float>();
        bool built;
        float playedTime;

        void Start()
        {
            if (!game) game = GetComponent<StealthGame>();
            if (!font) font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (door) closedDoorPosition = door.localPosition;
            if (corridorLights != null)
                foreach (var lamp in corridorLights)
                    if (lamp) { sceneLights.Add(lamp); lightLevels.Add(lamp.intensity); }
            Build();
        }

        void Build()
        {
            var root = new GameObject("Zoning interface", typeof(RectTransform), typeof(Canvas),
                typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(transform, false);
            canvas = root.GetComponent<RectTransform>();
            var c = root.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 50;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            if (!FindAnyObjectByType<EventSystem>())
            {
                var events = new GameObject("Interface input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(transform, false);
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            Text(canvas, "Project caption", "RAY-OE  /  SECURITY ANNEX", 18, Quiet, new Vector2(58, -45), new Vector2(600, 28), TopLeft);
            objective = Text(canvas, "Objective", "Infiltrate the archive. Recover the maintenance override.", 25, Paper,
                new Vector2(58, -82), new Vector2(920, 38), TopLeft);
            sector = Text(canvas, "Current sector", "SERVICE ENTRY", 18, Cyan, new Vector2(58, -126), new Vector2(760, 28), TopLeft);
            torchState = Text(canvas, "Torch state", "F  /  LIGHT ON", 19, Cyan,
                new Vector2(-58, -48), new Vector2(330, 30), TopRight, TextAlignmentOptions.Right);
            Text(canvas, "Notebook hint", "N  /  MAINTENANCE NOTES      ESC  /  PAUSE", 17, Quiet,
                new Vector2(-58, -86), new Vector2(500, 26), TopRight, TextAlignmentOptions.Right);
            var reticle = Image(canvas, "Sight point", new Color(.88f, .93f, .88f, .6f), Vector2.zero, new Vector2(3, 3), Center);
            reticle.raycastTarget = false;
            prompt = Text(canvas, "Interaction", "", 24, Paper, new Vector2(0, -82), new Vector2(850, 42), Center, TextAlignmentOptions.Center);
            notice = Text(canvas, "Quiet notice", "", 21, Cyan, new Vector2(0, 99), new Vector2(950, 40), BottomCenter, TextAlignmentOptions.Center);

            alertGroup = Box(canvas, "Exposure", new Vector2(0, 158), new Vector2(450, 82), BottomCenter).gameObject;
            var alertRoot = (RectTransform)alertGroup.transform;
            alertLabel = Text(alertRoot, "Exposure caption", "CCTV EXPOSURE / FIND COVER", 17, Amber, new Vector2(0, 15), new Vector2(450, 28), Center, TextAlignmentOptions.Center);
            Image(alertRoot, "Exposure track", new Color(.28f, .31f, .29f, .7f), new Vector2(0, -15), new Vector2(330, 4), Center);
            alertFill = Image(alertRoot, "Exposure amount", Amber, new Vector2(-165, -15), new Vector2(330, 4), Center);
            alertFill.rectTransform.pivot = new Vector2(0, .5f);

            onboarding = Box(canvas, "Entry instructions", new Vector2(58, 58), new Vector2(660, 165), BottomLeft).gameObject;
            var intro = (RectTransform)onboarding.transform;
            Image(intro, "Entry accent", Cyan, new Vector2(0, 0), new Vector2(2, 165), LeftCenter);
            Text(intro, "Entry heading", "INFILTRATE THE ARCHIVE", 22, Paper, new Vector2(28, -14), new Vector2(590, 32), TopLeft);
            Text(intro, "Entry body", "WASD  Move     MOUSE  Look     F  Inspection light\nRead four maintenance stencils. N keeps your notes.\nTime the CCTV zones. Cabinets block their sight.\nE  Use the archive terminal.", 20, Quiet,
                new Vector2(28, -53), new Vector2(610, 102), TopLeft);
            diagnostic = Text(canvas, "Physics diagnostic caption", "", 18, Amber, new Vector2(58, 250), new Vector2(970, 150), BottomLeft);

            notebook = Overlay("Notebook");
            var notesPanel = Panel((RectTransform)notebook.transform, "Field notes panel", new Vector2(1040, 800));
            Header(notesPanel, "MAINTENANCE NOTES", "Read stencils are transcribed here. Notes survive a security reset.", "N / ESC  CLOSE");
            notebookCount = Text(notesPanel, "Notes count", "0 / 4 RECORDED", 18, Cyan, new Vector2(58, -140), new Vector2(800, 30), TopLeft);
            noteTitles = new TMP_Text[4]; noteDigits = new TMP_Text[4]; noteRelations = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                float y = -203 - i * 137;
                Image(notesPanel, "Rule " + i, new Color(.25f, .36f, .37f, .55f), new Vector2(58, y - 98), new Vector2(924, 1), TopLeft);
                noteDigits[i] = Text(notesPanel, "Recorded digit " + i, "—", 49, Quiet, new Vector2(58, y), new Vector2(76, 72), TopLeft);
                noteTitles[i] = Text(notesPanel, "Recorded title " + i, "UNREAD MAINTENANCE STENCIL", 21, Quiet, new Vector2(165, y), new Vector2(750, 34), TopLeft);
                noteRelations[i] = Text(notesPanel, "Recorded relation " + i, "No entry.", 24, Quiet, new Vector2(165, y - 39), new Vector2(750, 57), TopLeft);
            }

            keypad = Overlay("Keypad");
            var keypadPanel = Panel((RectTransform)keypad.transform, "Code panel", new Vector2(650, 910));
            Header(keypadPanel, "ARCHIVE OVERRIDE", "Enter the maintenance code to release the archive door.", "ESC  CLOSE");
            var screen = Image(keypadPanel, "Terminal screen", new Color(.015f, .035f, .038f), new Vector2(0, -226), new Vector2(534, 108), TopCenter);
            keypadEntry = Text(screen.rectTransform, "Terminal digits", "_  _  _  _", 58, Cyan, Vector2.zero, new Vector2(500, 85), Center, TextAlignmentOptions.Center);
            keypadNotice = Text(keypadPanel, "Terminal feedback", "", 20, Amber, new Vector2(0, -312), new Vector2(535, 52), TopCenter, TextAlignmentOptions.Center);
            for (int i = 0; i < 9; i++)
            {
                string digit = (i + 1).ToString();
                Button(keypadPanel, "Digit " + digit, digit, new Vector2(-178 + (i % 3) * 178, -399 - (i / 3) * 99), new Vector2(165, 85), () => game.AppendDigit(digit));
            }
            Button(keypadPanel, "Erase", "BACK", new Vector2(-178, -696), new Vector2(165, 85), () => game.Backspace(), 20);
            Button(keypadPanel, "Digit 0", "0", new Vector2(0, -696), new Vector2(165, 85), () => game.AppendDigit("0"));
            Button(keypadPanel, "Submit", "ENTER", new Vector2(178, -696), new Vector2(165, 85), () => game.SubmitCode(), 20);
            Button(keypadPanel, "Terminal close", "RETURN TO INFILTRATION", new Vector2(0, -809), new Vector2(534, 66), () => game.CloseOverlay(), 18);

            pause = Overlay("Pause");
            var pausePanel = Panel((RectTransform)pause.transform, "Pause panel", new Vector2(910, 650));
            Header(pausePanel, "MISSION PAUSED", "Cross three CCTV sectors. Decode the maintenance override.", "ESC  RESUME");
            Text(pausePanel, "Controls", "WASD   Move\nMOUSE   Look\nF   Inspection flashlight\nN   Maintenance notes\nE   Archive terminal", 27, Paper,
                new Vector2(58, -192), new Vector2(780, 220), TopLeft);
            Text(pausePanel, "Retry explanation", "CCTV identification returns you to service entry. Notes remain.\nNotes and the archive terminal pause the camera patrols.", 22, Quiet,
                new Vector2(58, -425), new Vector2(800, 90), TopLeft);
            Button(pausePanel, "Resume", "RESUME", new Vector2(0, -568), new Vector2(794, 64), () => game.CloseOverlay(), 20);

            won = Overlay("Escaped");
            var winPanel = Panel((RectTransform)won.transform, "Exit panel", new Vector2(920, 540));
            Header(winPanel, "ARCHIVE ACCESSED", "Maintenance override accepted. The secure route is open.", "SECURITY ANNEX");
            Text(winPanel, "Win body", "You crossed the surveillance sectors.\nInfiltration complete.", 29, Paper, new Vector2(58, -200), new Vector2(800, 110), TopLeft);
            Button(winPanel, "New investigation", "RESTART INFILTRATION", new Vector2(0, -414), new Vector2(804, 68), () => game.NewGame(), 20);

            curtain = Image(canvas, "Detected fade", new Color(.025f, .035f, .04f, 0), Vector2.zero, Vector2.zero, Center);
            Stretch(curtain.rectTransform); curtain.raycastTarget = false;
            caughtMessage = Box(curtain.rectTransform, "Detected message", Vector2.zero, new Vector2(800, 160), Center).gameObject;
            Text((RectTransform)caughtMessage.transform, "Detected title", "IDENTIFIED BY CCTV", 34, Paper, new Vector2(0, 19), new Vector2(800, 55), Center, TextAlignmentOptions.Center);
            Text((RectTransform)caughtMessage.transform, "Retained notes", "Security reset. Returning to entry. Your notes remain.", 21, Quiet, new Vector2(0, -33), new Vector2(800, 42), Center, TextAlignmentOptions.Center);
            built = true;
        }

        void LateUpdate()
        {
            if (!built || !game) return;
            bool playing = game.Mode == GameMode.Playing;
            if (playing) playedTime += Time.deltaTime;
            if (exitGrid) exitGrid.SetActive(!game.ExitUnlocked);
            if (door) door.localPosition = Vector3.Lerp(door.localPosition, closedDoorPosition + (game.ExitUnlocked ? Vector3.up * 3.6f : Vector3.zero), Time.unscaledDeltaTime * .8f);
            notebook.SetActive(game.Mode == GameMode.Notebook); keypad.SetActive(game.Mode == GameMode.Keypad);
            pause.SetActive(game.Mode == GameMode.Paused); won.SetActive(game.Mode == GameMode.Won);
            objective.text = game.ExitUnlocked ? "Archive unlocked. Cross the security threshold." : "Infiltrate the archive. Recover the maintenance override.";
            float z = game.body ? game.body.transform.position.z : 0;
            sector.text = z < 3.8f ? "SERVICE ENTRY  /  FOUR MAINTENANCE STENCILS" : z < 10.15f ? "SECTOR 01  /  PERIMETER CCTV" : z < 18.1f ? "SECTOR 02  /  SIGNAL PROCESSING" : z < 25.55f ? "SECTOR 03  /  RECORDS APPROACH" : "ARCHIVE  /  PROTECTED TERMINAL";
            torchState.text = game.TorchOn ? "F  /  LIGHT ON" : "F  /  LIGHT OFF";
            torchState.color = game.TorchOn ? Cyan : Quiet;
            onboarding.SetActive(playing && game.body && game.body.transform.position.z < 3.3f && playedTime < 50);
            prompt.text = playing ? game.Prompt : "";
            notice.text = playing ? game.Notice : "";
            alertGroup.SetActive(playing && game.Alert > .002f);
            alertFill.rectTransform.sizeDelta = new Vector2(330 * game.Alert, 4);
            alertFill.color = Color.Lerp(Amber, Red, game.Alert);
            alertLabel.color = alertFill.color;
            alertLabel.text = string.IsNullOrEmpty(game.DetectorName) ? "CAMERA SIGHT BROKEN" : "CCTV EXPOSURE / FIND COVER";
            curtain.color = new Color(.025f, .035f, .04f, game.Mode == GameMode.Caught ? Mathf.Clamp01(game.CaughtFade) : 0);
            caughtMessage.SetActive(game.Mode == GameMode.Caught);
            if (game.Mode == GameMode.Notebook) RefreshNotes();
            keypadEntry.text = SpacedCode(game.KeypadInput);
            keypadNotice.text = game.Mode == GameMode.Keypad ? game.Notice : "";
            diagnostic.text = game.Diagnostics ? DiagnosticText() : "";
            for (int i = 0; i < sceneLights.Count; i++)
                if (sceneLights[i]) sceneLights[i].intensity = lightLevels[i] * (.94f + .06f * Mathf.Sin(playedTime * .43f + i * .91f));
        }

        string DiagnosticText()
        {
            string text = "F3 / SURVEILLANCE DIAGNOSTICS";
            if (game.hazards == null) return text;
            for (int i = 0; i < game.hazards.Length; i++)
            {
                var detector = game.hazards[i]; if (!detector) continue;
                string query = detector.shape == DetectionShape.Sphere ? "Physics.CheckSphere" : detector.shape == DetectionShape.Box ? "Physics.CheckBox" : "Physics.CheckCapsule";
                string state = !detector.HasPlayerOverlap ? "OUTSIDE / RAY NOT SAMPLED" : detector.IsDetecting ? "DETECTING / RAY CLEAR" : "OCCLUDED / RAY BLOCKED";
                text += "\nCAM 0" + (i + 1) + "  " + query + "  /  " + state;
            }
            return text;
        }

        void RefreshNotes()
        {
            notebookCount.text = game.Notes.Count + " / 4 RECORDED";
            for (int i = 0; i < 4; i++)
            {
                bool known = i < game.Notes.Count;
                noteTitles[i].text = known ? game.Notes[i].title.ToUpperInvariant() : "UNREAD MAINTENANCE STENCIL";
                noteDigits[i].text = known ? game.Notes[i].digit : "—";
                noteRelations[i].text = known ? (string.IsNullOrEmpty(game.Notes[i].relation) ? "No ordering statement on this marking." : game.Notes[i].relation) : "No entry.";
                noteTitles[i].color = noteRelations[i].color = known ? Paper : Quiet;
                noteDigits[i].color = known ? Cyan : Quiet;
            }
        }

        static string SpacedCode(string input)
        {
            var parts = new string[4];
            for (int i = 0; i < 4; i++) parts[i] = input != null && i < input.Length ? input[i].ToString() : "_";
            return string.Join("   ", parts);
        }

        GameObject Overlay(string name)
        {
            var image = Image(canvas, name, new Color(.009f, .019f, .024f, .87f), Vector2.zero, Vector2.zero, Center);
            Stretch(image.rectTransform); image.raycastTarget = true;
            return image.gameObject;
        }
        RectTransform Panel(RectTransform parent, string name, Vector2 size)
        {
            var panel = Image(parent, name, new Color(Ink.r, Ink.g, Ink.b, 1), Vector2.zero, size, Center);
            Image(panel.rectTransform, "Panel top rule", Cyan, Vector2.zero, new Vector2(size.x, 2), TopCenter);
            return panel.rectTransform;
        }
        void Header(RectTransform parent, string title, string body, string action)
        {
            Text(parent, "Panel title", title, 34, Paper, new Vector2(58, -45), new Vector2(parent.sizeDelta.x - 116, 52), TopLeft);
            Text(parent, "Panel description", body, 21, Quiet, new Vector2(58, -108), new Vector2(parent.sizeDelta.x - 116, 65), TopLeft);
            Text(parent, "Panel shortcut", action, 16, Cyan, new Vector2(-30, 20), new Vector2(parent.sizeDelta.x - 60, 26), BottomRight, TextAlignmentOptions.Right);
        }
        void Button(RectTransform parent, string name, string label, Vector2 at, Vector2 size, UnityEngine.Events.UnityAction action, float typeSize = 32)
        {
            var bg = Image(parent, name, new Color(.09f, .14f, .15f), at, size, TopCenter);
            bg.raycastTarget = true;
            var button = bg.gameObject.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = bg;
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(1.5f, 1.8f, 1.7f);
            colors.pressedColor = Cyan; colors.selectedColor = Color.white; colors.fadeDuration = .16f; button.colors = colors;
            var navigation = button.navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = navigation;
            button.onClick.AddListener(action);
            Text(bg.rectTransform, name + " label", label, typeSize, Paper, Vector2.zero, size - new Vector2(10, 10), Center, TextAlignmentOptions.Center);
        }
        static readonly Vector2 TopLeft = new Vector2(0, 1), TopCenter = new Vector2(.5f, 1), TopRight = Vector2.one;
        static readonly Vector2 Center = new Vector2(.5f, .5f), LeftCenter = new Vector2(0, .5f);
        static readonly Vector2 BottomLeft = Vector2.zero, BottomCenter = new Vector2(.5f, 0), BottomRight = new Vector2(1, 0);
        static RectTransform Box(RectTransform parent, string name, Vector2 at, Vector2 size, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor; rt.anchoredPosition = at; rt.sizeDelta = size; return rt;
        }
        static UnityEngine.UI.Image Image(RectTransform parent, string name, Color color, Vector2 at, Vector2 size, Vector2 anchor)
        {
            var rt = Box(parent, name, at, size, anchor); var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
            image.color = color; image.raycastTarget = false; return image;
        }
        TMP_Text Text(RectTransform parent, string name, string value, float size, Color color, Vector2 at, Vector2 area, Vector2 anchor,
            TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var rt = Box(parent, name, at, area, anchor); var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = size; text.color = color; text.alignment = align;
            text.textWrappingMode = TextWrappingModes.Normal; text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false; text.richText = false; return text;
        }
        static void Stretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }
    }
}
