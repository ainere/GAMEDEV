using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarterAssets;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RayOEZoning.Tests
{
    public sealed class StealthStateTests
    {
        readonly List<GameObject> objects = new List<GameObject>();
        Keyboard keyboard;
        InputSettings.BackgroundBehavior? previousBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode? previousEditorInput;
#endif

        GameObject Object(string name)
        {
            var value = new GameObject(name);
            objects.Add(value);
            return value;
        }

        StealthGame Game(bool withClue = false)
        {
            var root = Object("Real stealth state");
            root.SetActive(false);
            var game = root.AddComponent<StealthGame>();
            var player = Object("Player");
            player.layer = 29;
            game.body = player.AddComponent<CharacterController>();
            game.body.height = 1.8f;
            game.body.radius = .3f;
            game.body.center = Vector3.up * .9f;
            game.inputs = player.AddComponent<StarterAssetsInputs>();
            game.spawn = Object("Safe spawn").transform;
            game.keypad = Object("Keypad").transform;
            game.keypad.position = Vector3.forward;
            game.playerCamera = Object("Player view").AddComponent<Camera>();
            game.playerCamera.transform.position = Vector3.up * 1.55f;
            game.torch = Object("Actual spotlight").AddComponent<Light>();
            game.torch.type = LightType.Spot;
            game.hazards = new HazardDetector[0];
            game.clues = new HiddenClue[0];
            if (withClue)
            {
                var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
                objects.Add(target);
                target.layer = 11;
                target.transform.position = new Vector3(0, 1.55f, 2);
                var clue = target.AddComponent<HiddenClue>();
                clue.clueIndex = 0;
                clue.anchor = target.transform;
                clue.hiddenRenderers = new Renderer[] { target.GetComponent<Renderer>() };
                clue.obstructionMask = 1 << 28;
                game.clues = new[] { clue };
            }
            root.SetActive(true);
            Physics.SyncTransforms();
            return game;
        }

        HazardDetector Detector(StealthGame game, DetectionShape shape)
        {
            var detector = Object("Actual query " + shape).AddComponent<HazardDetector>();
            detector.shape = shape;
            detector.home = new Vector3(0, 1.1f, 1);
            detector.transform.position = detector.home;
            detector.lateralAmplitude = 0;
            detector.radius = 2;
            detector.halfExtents = new Vector3(2, 2, 2);
            detector.capsulePointA = Vector3.back;
            detector.capsulePointB = Vector3.forward;
            detector.playerMask = 1 << 29;
            detector.obstructionMask = 1 << 28;
            detector.player = game.body.transform;
            Physics.SyncTransforms();
            return detector;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var value in objects) if (value) UnityEngine.Object.DestroyImmediate(value);
            objects.Clear();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            if (previousBackground.HasValue) InputSystem.settings.backgroundBehavior = previousBackground.Value;
            previousBackground = null;
#if UNITY_EDITOR
            if (previousEditorInput.HasValue) InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput.Value;
            previousEditorInput = null;
#endif
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        [Test]
        public void MultipleDetectorsShareOneExposureRateAndModalStatesFreezeIt()
        {
            var game = Game();
            game.hazards = new[] { Detector(game, DetectionShape.Sphere),
                Detector(game, DetectionShape.Box), Detector(game, DetectionShape.Capsule) };
            var pivot = Object("Panning camera head").transform;
            pivot.SetParent(game.hazards[0].transform, false);
            game.hazards[0].panPivot = pivot;
            game.hazards[0].panAmplitude = 30;
            game.Advance(.675f);
            Assert.That(game.Alert, Is.EqualTo(.5f).Within(.002f));
            foreach (var mode in new[] { GameMode.Notebook, GameMode.Keypad, GameMode.Paused })
            {
                game.SetMode(mode);
                float patrolTime = game.PatrolTime;
                float alert = game.Alert;
                Quaternion rotation = pivot.rotation;
                game.Advance(5);
                Assert.That(game.Alert, Is.EqualTo(alert));
                Assert.That(game.PatrolTime, Is.EqualTo(patrolTime));
                Assert.That(Quaternion.Angle(pivot.rotation, rotation), Is.LessThan(.001f));
            }
            game.SetMode(GameMode.Playing);
            game.Advance(.676f);
            Assert.That(game.Mode, Is.EqualTo(GameMode.Caught));
            game.Advance(.66f);
            Assert.That(game.Mode, Is.EqualTo(GameMode.Playing));
            Assert.That(game.Alert, Is.Zero);
            Assert.That(game.PatrolTime, Is.Zero);
        }

        [Test]
        public void RevealUsesActualSpotlightSettingsAndHeldMuzzlePose()
        {
            var game = Game(true);
            game.torch.range = 1;
            game.Advance(.02f);
            Assert.That(game.clues[0].IsVisible, Is.False);
            game.torch.range = 8;
            game.torch.spotAngle = 50;
            game.Advance(.02f);
            Assert.That(game.clues[0].IsVisible, Is.True);
            game.torchOrigin = Object("Held light emitter").transform;
            game.torchOrigin.position = new Vector3(1, 1.55f, 0);
            game.torchOrigin.rotation = Quaternion.Euler(0, 90, 0);
            game.Advance(.02f);
            Assert.That(game.clues[0].IsVisible, Is.False);
            game.torchOrigin.LookAt(game.clues[0].transform);
            game.Advance(.02f);
            Assert.That(game.clues[0].IsVisible, Is.True);
            Assert.That(game.torch.transform.position, Is.EqualTo(game.torchOrigin.position));
            Assert.That(Quaternion.Angle(game.torch.transform.rotation, game.torchOrigin.rotation), Is.LessThan(.001f));
        }

        [Test]
        public void LosingDetectionDrainsAlertRatherThanInstantlyClearingIt()
        {
            var game = Game();
            game.hazards = new[] { Detector(game, DetectionShape.Sphere) };
            game.Advance(.675f);
            game.body.enabled = false;
            game.body.transform.position = Vector3.back * 10;
            game.body.enabled = true;
            game.Advance(.375f);
            Assert.That(game.Alert, Is.EqualTo(.25f).Within(.002f));
            game.Advance(.375f);
            Assert.That(game.Alert, Is.Zero);
        }

        [Test]
        public void KeypadAcceptsKnowledgeWithoutDiscoveryGateAndRetryRelocksExit()
        {
            var game = Game();
            game.OpenKeypad();
            foreach (char digit in "1111") game.AppendDigit(digit.ToString());
            Assert.That(game.SubmitCode(), Is.False);
            Assert.That(game.KeypadInput, Is.EqualTo("1111"));
            Assert.That(game.ExitUnlocked, Is.False);
            for (int i = 0; i < 4; i++) game.Backspace();
            foreach (char digit in "2469") game.AppendDigit(digit.ToString());
            Assert.That(game.Notes.Count, Is.Zero);
            Assert.That(game.SubmitCode(), Is.True);
            Assert.That(game.ExitUnlocked, Is.True);
            Assert.That(game.Mode, Is.EqualTo(GameMode.Playing));
            game.body.enabled = false;
            game.body.transform.position = Vector3.forward * 28;
            game.body.enabled = true;
            game.Advance(.02f);
            Assert.That(game.Mode, Is.EqualTo(GameMode.Won));
            game.Retry();
            Assert.That(game.ExitUnlocked, Is.False);
            Assert.That(game.KeypadInput, Is.Empty);
            Assert.That(game.TorchOn, Is.True);
            Assert.That(game.body.transform.position, Is.EqualTo(game.spawn.position));
        }

        [Test]
        public void ExclusiveModesClearInputAndOnlyNearbyKeypadCanOpen()
        {
            var game = Game();
            game.inputs.move = Vector2.one;
            game.inputs.look = Vector2.one;
            game.SetMode(GameMode.Paused);
            Assert.That(game.inputs.move, Is.EqualTo(Vector2.zero));
            Assert.That(game.inputs.look, Is.EqualTo(Vector2.zero));
            game.ToggleNotebook();
            Assert.That(game.Mode, Is.EqualTo(GameMode.Paused));
            game.CloseOverlay();
            game.keypad.position = Vector3.forward * 20;
            game.OpenKeypad();
            Assert.That(game.Mode, Is.EqualTo(GameMode.Playing));
            game.ToggleNotebook();
            game.OpenKeypad();
            Assert.That(game.Mode, Is.EqualTo(GameMode.Notebook));
            game.CloseOverlay();
            Assert.That(game.Mode, Is.EqualTo(GameMode.Playing));
        }

        [UnityTest]
        public IEnumerator AVisibleDistantClueAsksPlayerToMoveIntoReadingRange()
        {
            var game = Game(true);
            game.clues[0].transform.position = new Vector3(0, 1.55f, 4);
            yield return new WaitForSeconds(.1f);
            Assert.That(game.clues[0].IsVisible, Is.True);
            Assert.That(game.Notes.Count, Is.Zero);
            Assert.That(game.Prompt, Does.Contain("closer"));
            game.clues[0].transform.position = new Vector3(0, 1.55f, 2);
            yield return new WaitForSeconds(.1f);
            Assert.That(game.Prompt, Does.Contain("steady"));
        }

        [UnityTest]
        public IEnumerator TopRowDigitsAndEnterReachTheRealKeypadInputPath()
        {
            var game = Game();
            game.OpenKeypad();
            previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard);
            keyboard.MakeCurrent();
            for (int digit = 0; digit < 10; digit++)
            {
                Key key = digit == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + digit - 1);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                InputSystem.Update();
                Assert.That(keyboard[key].wasPressedThisFrame, Is.True, "Queued keyboard press must reach the test device.");
                Assert.That(Keyboard.current, Is.SameAs(keyboard));
                Assert.That(game.Mode, Is.EqualTo(GameMode.Keypad));
                game.SendMessage("Update");
                Assert.That(game.KeypadInput, Is.EqualTo(digit.ToString()), "Top-row digit " + digit);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return null;
                game.Backspace();
            }
            foreach (Key key in new[] { Key.Digit2, Key.Digit4, Key.Digit6, Key.Digit9 })
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
                InputSystem.Update();
                game.SendMessage("Update");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Enter));
            InputSystem.Update();
            game.SendMessage("Update");
            Assert.That(game.ExitUnlocked, Is.True);
            Assert.That(game.Mode, Is.EqualTo(GameMode.Playing));
        }

        [UnityTest]
        public IEnumerator RealReadingRecordsOnceAndKnowledgeSurvivesCapture()
        {
            var game = Game(true);
            yield return new WaitForSeconds(1.0f);
            Assert.That(game.Notes.Count, Is.EqualTo(1));
            Assert.That(game.Notes[0].id, Is.EqualTo("Eye"));
            game.SetTorch(false);
            yield return null;
            Assert.That(game.clues[0].IsVisible, Is.False);
            Assert.That(game.Notes.Count, Is.EqualTo(1));
            game.hazards = new[] { Detector(game, DetectionShape.Sphere) };
            game.Advance(1.36f);
            Assert.That(game.Mode, Is.EqualTo(GameMode.Caught));
            game.Advance(.66f);
            Assert.That(game.Notes.Count, Is.EqualTo(1));
            Assert.That(game.Mode, Is.EqualTo(GameMode.Playing));
            yield return new WaitForSeconds(.9f);
            Assert.That(game.Notes.Count, Is.EqualTo(1), "Repeated readings must not duplicate notes.");
            game.NewGame();
            Assert.That(game.Notes.Count, Is.Zero);
        }
    }
}
