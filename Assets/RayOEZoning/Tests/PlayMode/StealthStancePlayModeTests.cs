using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace RayOEZoning.Tests
{
    public sealed class StealthStancePlayModeTests
    {
        const int WorldLayer = 28;
        const int PlayerLayer = 29;
        readonly List<GameObject> objects = new List<GameObject>();
        Keyboard keyboard;
        StealthGame game;
        StealthStance stance;
        CharacterController body;
        StarterAssetsInputs inputs;
        FirstPersonController controller;
        PlayerInput playerInput;
        Transform cameraRoot;
        InputActionAsset playerActions;
        InputSettings.BackgroundBehavior previousBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif

        GameObject NewObject(string name)
        {
            var value = new GameObject(name);
            objects.Add(value);
            return value;
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previousBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            var floor = NewObject("Movement test floor");
            floor.layer = WorldLayer;
            floor.transform.position = new Vector3(0, -0.15f, 15f);
            floor.AddComponent<BoxCollider>().size = new Vector3(20f, 0.3f, 50f);

            var session = NewObject("Movement test session");
            session.SetActive(false);
            game = session.AddComponent<StealthGame>();

            var player = NewObject("Movement test player");
            player.SetActive(false);
            player.layer = PlayerLayer;
            body = player.AddComponent<CharacterController>();
            body.height = 1.8f;
            body.radius = 0.28f;
            body.center = Vector3.up * 0.9f;
            body.skinWidth = 0.025f;
            body.stepOffset = 0.15f;
            body.minMoveDistance = 0f;
            inputs = player.AddComponent<StarterAssetsInputs>();
            controller = player.AddComponent<FirstPersonController>();
            controller.GroundLayers = 1 << WorldLayer;
            controller.GroundedRadius = 0.28f;
            controller.MoveSpeed = 2.35f;
            controller.SprintSpeed = 4.55f;
            controller.SpeedChangeRate = 12f;

            // FirstPersonController requires PlayerInput, so provide the same SendMessage-based
            // Move path used by the authored scene instead of leaving PlayerInput unconfigured.
            playerActions = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionMap playerMap = playerActions.AddActionMap("Player");
            InputAction moveAction = playerMap.AddAction("Move", InputActionType.Value,
                expectedControlLayout: "Vector2");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w", "KeyboardMouse")
                .With("Down", "<Keyboard>/s", "KeyboardMouse")
                .With("Left", "<Keyboard>/a", "KeyboardMouse")
                .With("Right", "<Keyboard>/d", "KeyboardMouse");
            playerMap.AddAction("Sprint", InputActionType.Button, expectedControlLayout: "Button")
                .AddBinding("<Keyboard>/leftShift", groups: "KeyboardMouse");
            playerActions.AddControlScheme("KeyboardMouse")
                .WithBindingGroup("KeyboardMouse")
                .WithRequiredDevice("<Keyboard>")
                .WithOptionalDevice("<Mouse>");
            playerInput = player.GetComponent<PlayerInput>();
            playerInput.actions = playerActions;
            playerInput.defaultActionMap = "Player";
            playerInput.defaultControlScheme = "KeyboardMouse";
            playerInput.notificationBehavior = PlayerNotifications.SendMessages;

            var root = NewObject("Player camera root").transform;
            root.SetParent(player.transform, false);
            root.localPosition = Vector3.up * 1.65f;
            cameraRoot = root;
            controller.CinemachineCameraTarget = root.gameObject;
            var camera = NewObject("MainCamera");
            camera.tag = "MainCamera";
            camera.transform.SetParent(root, false);
            camera.AddComponent<Camera>();

            stance = player.AddComponent<StealthStance>();
            stance.game = game;
            stance.controller = controller;
            stance.body = body;
            stance.cameraRoot = cameraRoot;
            stance.obstructionMask = 1 << WorldLayer;

            var spawn = NewObject("Safe spawn").transform;
            spawn.position = Vector3.zero;
            game.controller = controller;
            game.inputs = inputs;
            game.body = body;
            game.playerCamera = camera.GetComponent<Camera>();
            game.spawn = spawn;
            game.stance = stance;
            game.hazards = new HazardDetector[0];
            game.clues = new HiddenClue[0];

            keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.EnableDevice(keyboard);
            keyboard.MakeCurrent();
            player.SetActive(true);
            session.SetActive(true);
            Physics.SyncTransforms();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            inputs.move = Vector2.zero;
            inputs.sprint = false;
            game.SetMode(GameMode.Paused);
            if (playerInput != null)
                playerInput.actions = null;
            yield return null;
            foreach (GameObject value in objects)
                if (value) Object.Destroy(value);
            objects.Clear();
            if (keyboard != null)
                InputSystem.RemoveDevice(keyboard);
            keyboard = null;
            if (playerActions != null)
                Object.Destroy(playerActions);
            playerActions = null;
            playerInput = null;
            InputSystem.settings.backgroundBehavior = previousBackground;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        }

        [UnityTest]
        public IEnumerator ShiftMovesFasterOnlyForwardWhileStanding()
        {
            Assert.That(game.Mode, Is.EqualTo(GameMode.Playing));
            Assert.That(controller.enabled, Is.True, "The production FirstPersonController must be enabled.");
            Assert.That(body.enabled, Is.True, "The real CharacterController must be enabled.");
            SetKeyboard(Key.W, Key.LeftShift);
            Assert.That(inputs.move, Is.EqualTo(Vector2.up), "W reaches StarterAssetsInputs through PlayerInput.");
            yield return new WaitForSeconds(0.45f);
            float sprintDistance = body.transform.position.z;

            Assert.That(stance.IsSprinting, Is.True);
            Assert.That(sprintDistance, Is.GreaterThan(0.2f),
                $"The real CharacterController advances while sprinting. enabled={body.enabled}, " +
                $"controllerEnabled={controller.enabled}, grounded={controller.Grounded}, " +
                $"input={inputs.move}, speed={controller.SprintSpeed}, position={body.transform.position}.");

            SetKeyboard(Key.D, Key.LeftShift);
            yield return null;
            Assert.That(stance.IsSprinting, Is.False, "Shift does not sprint while strafing.");

            body.enabled = false;
            body.transform.position = Vector3.zero;
            body.enabled = true;
            controller.ResetView();
            Assert.That(stance.TrySetStance(PlayerStance.Crouched), Is.True);
            SetKeyboard(Key.W, Key.LeftShift);
            Assert.That(inputs.move, Is.EqualTo(Vector2.up), "W reaches StarterAssetsInputs while crouched.");
            yield return new WaitForSeconds(0.45f);
            float crouchDistance = body.transform.position.z;

            Assert.That(stance.IsSprinting, Is.False, "Shift is ignored while crouched.");
            Assert.That(crouchDistance, Is.GreaterThan(0.05f));
            Assert.That(sprintDistance, Is.GreaterThan(crouchDistance + 0.15f),
                "Measured movement through the production controller is faster when standing sprint is held.");
        }

        [UnityTest]
        public IEnumerator CAndZKeysToggleTheCharacterControllerAndCameraStance()
        {
            SetKeyboard(Key.C);
            stance.SendMessage("Update");
            Assert.That(stance.CurrentStance, Is.EqualTo(PlayerStance.Crouched));
            Assert.That(body.height, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(cameraRoot.localPosition.y, Is.EqualTo(0.95f).Within(0.001f));

            SetKeyboard();
            stance.SendMessage("Update");
            SetKeyboard(Key.C);
            stance.SendMessage("Update");
            Assert.That(stance.CurrentStance, Is.EqualTo(PlayerStance.Standing), "C toggles back to standing.");
            Assert.That(body.height, Is.EqualTo(1.8f).Within(0.001f));

            SetKeyboard();
            stance.SendMessage("Update");
            SetKeyboard(Key.Z);
            stance.SendMessage("Update");
            Assert.That(stance.CurrentStance, Is.EqualTo(PlayerStance.Prone));
            Assert.That(body.height, Is.EqualTo(0.56f).Within(0.001f));
            Assert.That(cameraRoot.localPosition.y, Is.EqualTo(0.43f).Within(0.001f));

            SetKeyboard();
            stance.SendMessage("Update");
            SetKeyboard(Key.Z);
            stance.SendMessage("Update");
            Assert.That(stance.CurrentStance, Is.EqualTo(PlayerStance.Standing), "Z toggles back to standing.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator ModalFreezesStanceAndRetryRestoresStandingAtSpawn()
        {
            inputs.move = Vector2.up;
            SetKeyboard(Key.W, Key.LeftShift);
            yield return new WaitForSeconds(0.15f);
            Assert.That(stance.IsSprinting, Is.True, "Forward standing movement with Shift enters sprint state.");
            Assert.That(inputs.sprint, Is.True);

            game.SetMode(GameMode.Paused);
            Vector3 pausedPosition = body.transform.position;

            Assert.That(game.controller.enabled, Is.False);
            Assert.That(inputs.sprint, Is.False);
            Assert.That(stance.IsSprinting, Is.False, "Opening a modal clears the stance sprint state synchronously.");
            Assert.That(stance.TrySetStance(PlayerStance.Standing), Is.False);
            yield return new WaitForSeconds(0.15f);
            Assert.That(stance.CurrentStance, Is.EqualTo(PlayerStance.Standing));
            Assert.That(Vector3.Distance(body.transform.position, pausedPosition), Is.LessThan(0.001f));

            body.enabled = false;
            body.transform.position = new Vector3(0f, 0f, 6f);
            body.enabled = true;
            game.Retry();
            Assert.That(game.Mode, Is.EqualTo(GameMode.Playing));
            Assert.That(stance.CurrentStance, Is.EqualTo(PlayerStance.Standing));
            Assert.That(body.height, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(cameraRoot.localPosition.y, Is.EqualTo(1.65f).Within(0.001f));
            Assert.That(body.transform.position, Is.EqualTo(Vector3.zero));
            Assert.That(inputs.sprint, Is.False);
        }

        void SetKeyboard(params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
    }
}
