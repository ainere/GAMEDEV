using System.Collections.Generic;
using NUnit.Framework;
using StarterAssets;
using UnityEngine;

namespace RayOEZoning.Tests
{
    public sealed class StealthStancePhysicsTests
    {
        const int PlayerLayer = 29;
        const int WorldLayer = 28;
        readonly List<GameObject> objects = new List<GameObject>();
        StealthGame game;
        StealthStance stance;
        CharacterController body;
        FirstPersonController controller;
        Transform cameraRoot;

        GameObject NewObject(string name)
        {
            var value = new GameObject(name);
            objects.Add(value);
            return value;
        }

        [SetUp]
        public void SetUp()
        {
            var session = NewObject("Stance test session");
            session.SetActive(false);
            game = session.AddComponent<StealthGame>();

            var player = NewObject("Stance test player");
            player.SetActive(false);
            player.layer = PlayerLayer;
            body = player.AddComponent<CharacterController>();
            body.height = 1.8f;
            body.radius = 0.5f;
            body.center = Vector3.up * 0.9f;
            var inputs = player.AddComponent<StarterAssetsInputs>();
            controller = player.AddComponent<FirstPersonController>();
            var root = NewObject("Camera root").transform;
            root.SetParent(player.transform, false);
            root.localPosition = Vector3.up * 1.65f;
            controller.CinemachineCameraTarget = root.gameObject;
            root.gameObject.AddComponent<Camera>();
            stance = player.AddComponent<StealthStance>();
            stance.game = game;
            stance.controller = controller;
            stance.body = body;
            stance.cameraRoot = root;
            stance.obstructionMask = 1 << WorldLayer;

            game.controller = controller;
            game.inputs = inputs;
            game.body = body;
            game.playerCamera = root.GetComponent<Camera>();
            game.stance = stance;
            game.spawn = player.transform;
            game.hazards = new HazardDetector[0];
            game.clues = new HiddenClue[0];

            player.SetActive(true);
            session.SetActive(true);
            cameraRoot = root;
            // EditMode fixtures invoke the production bootstrap explicitly; ordinary
            // MonoBehaviour Awake is only called by the player lifecycle in PlayMode.
            stance.Bind(game, controller, body, cameraRoot);
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject value in objects)
                if (value) Object.DestroyImmediate(value);
            objects.Clear();
            Physics.SyncTransforms();
        }

        [Test]
        public void StancesKeepFeetFixedAndMoveTheViewAndSpeedsTogether()
        {
            float standingFootY = body.center.y - body.height * 0.5f;
            Assert.That(body.height, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(body.radius, Is.EqualTo(0.28f).Within(0.001f));
            Assert.That(stance.EyeHeight, Is.EqualTo(1.65f).Within(0.001f));
            Assert.That(controller.MoveSpeed, Is.EqualTo(2.35f).Within(0.001f));
            Assert.That(controller.SprintSpeed, Is.EqualTo(4.55f).Within(0.001f));

            Assert.That(stance.TrySetStance(PlayerStance.Crouched), Is.True);
            AssertStance(PlayerStance.Crouched, 1.10f, 0.95f, 1.35f, standingFootY);
            Assert.That(stance.TrySetStance(PlayerStance.Prone), Is.True);
            AssertStance(PlayerStance.Prone, 0.56f, 0.43f, 0.70f, standingFootY);
            Assert.That(stance.TrySetStance(PlayerStance.Standing), Is.True);
            AssertStance(PlayerStance.Standing, 1.8f, 1.65f, 2.35f, standingFootY);
        }

        [Test]
        public void LowCeilingBlocksStandingUntilTheCapsuleFits()
        {
            Assert.That(stance.TrySetStance(PlayerStance.Crouched), Is.True);
            var ceiling = NewObject("Low overhead cover");
            ceiling.layer = WorldLayer;
            ceiling.transform.position = new Vector3(0, 1.40f, 0);
            ceiling.AddComponent<BoxCollider>().size = new Vector3(4, 0.2f, 4);
            Physics.SyncTransforms();

            Assert.That(stance.TrySetStance(PlayerStance.Standing), Is.False);
            Assert.That(stance.CurrentStance, Is.EqualTo(PlayerStance.Crouched));
            Assert.That(body.height, Is.EqualTo(1.10f).Within(0.001f));

            ceiling.transform.position = Vector3.up * 3f;
            Physics.SyncTransforms();
            Assert.That(stance.TrySetStance(PlayerStance.Standing), Is.True);
            Assert.That(body.height, Is.EqualTo(1.8f).Within(0.001f));
        }

        [Test]
        public void StandingExpansionSucceedsWhenGroundedOnTheFloor()
        {
            var floor = NewObject("Grounded floor");
            floor.layer = WorldLayer;
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.AddComponent<BoxCollider>().size = new Vector3(4f, 0.2f, 4f);
            Physics.SyncTransforms();

            Assert.That(stance.TrySetStance(PlayerStance.Crouched), Is.True);
            Assert.That(stance.TrySetStance(PlayerStance.Prone), Is.True);
            Assert.That(stance.TrySetStance(PlayerStance.Standing), Is.True,
                "The shrunken clearance capsule leaves a skin-width gap above the floor.");
            Assert.That(body.height, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(body.center.y - body.height * 0.5f, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void BindAfterAwakeCapturesLateReferencesWithoutChangingTheChosenStance()
        {
            var lateRoot = NewObject("Late-bound stance");
            var lateStance = lateRoot.AddComponent<StealthStance>();
            Assert.That(lateStance.TrySetStance(PlayerStance.Crouched), Is.True);

            var latePlayer = NewObject("Late-bound player");
            var lateBody = latePlayer.AddComponent<CharacterController>();
            lateBody.height = 2f;
            lateBody.center = new Vector3(0.12f, 1.2f, -0.08f);
            var lateInputs = latePlayer.AddComponent<StarterAssetsInputs>();
            var lateController = latePlayer.AddComponent<FirstPersonController>();
            var lateCameraRoot = NewObject("Late-bound camera root").transform;
            lateCameraRoot.SetParent(latePlayer.transform, false);
            lateCameraRoot.localPosition = new Vector3(0.16f, 1.7f, -0.12f);

            lateStance.Bind(game, lateController, lateBody, lateCameraRoot);

            Assert.That(lateStance.CurrentStance, Is.EqualTo(PlayerStance.Crouched));
            Assert.That(lateBody.height, Is.EqualTo(1.1f).Within(0.001f));
            Assert.That(lateBody.center.x, Is.EqualTo(0.12f).Within(0.001f));
            Assert.That(lateBody.center.z, Is.EqualTo(-0.08f).Within(0.001f));
            Assert.That(lateBody.center.y - lateBody.height * 0.5f, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(lateCameraRoot.localPosition.x, Is.EqualTo(0.16f).Within(0.001f));
            Assert.That(lateCameraRoot.localPosition.y, Is.EqualTo(0.95f).Within(0.001f));
            Assert.That(lateCameraRoot.localPosition.z, Is.EqualTo(-0.12f).Within(0.001f));
            Assert.That(lateController.MoveSpeed, Is.EqualTo(1.35f).Within(0.001f));
            Assert.That(lateInputs.sprint, Is.False);
        }

        [Test]
        public void CrouchingLowersActualDetectorSamplesWithoutRemovingBroadphaseOverlap()
        {
            var rig = NewObject("Directional detector");
            rig.transform.position = new Vector3(0, 2.7f, 0);
            var detector = rig.AddComponent<HazardDetector>();
            detector.shape = DetectionShape.Sphere;
            detector.playerMask = 1 << PlayerLayer;
            detector.obstructionMask = 1 << WorldLayer;
            detector.useViewCone = true;
            detector.viewAngle = 90f;
            detector.viewRange = 10f;
            detector.player = body.transform;
            var lens = NewObject("Detector lens").transform;
            lens.SetParent(rig.transform, false);
            detector.rayOrigin = lens;
            body.transform.position = new Vector3(0, 0, 4);

            var cover = NewObject("Waist-high opaque cover");
            cover.layer = WorldLayer;
            cover.transform.position = new Vector3(0, 1f, 2f);
            cover.AddComponent<BoxCollider>().size = new Vector3(4f, 2f, 0.12f);
            Physics.SyncTransforms();

            Assert.That(detector.Evaluate(), Is.True, "The standing head sample clears the cover.");
            Assert.That(detector.LastRayTarget.y, Is.GreaterThan(1.5f));
            Assert.That(stance.TrySetStance(PlayerStance.Crouched), Is.True);
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.False, "Both lower posture samples fall behind cover.");
            Assert.That(detector.HasPlayerOverlap, Is.True, "The detector still overlaps the real player capsule.");
            Assert.That(detector.LastRayBlocked, Is.True);
            Assert.That(detector.LastRayTarget.y, Is.EqualTo(0.55f).Within(0.001f));
        }

        void AssertStance(PlayerStance expected, float height, float eye, float speed, float footY)
        {
            Assert.That(stance.CurrentStance, Is.EqualTo(expected));
            Assert.That(body.height, Is.EqualTo(height).Within(0.001f));
            Assert.That(body.center.y - body.height * 0.5f, Is.EqualTo(footY).Within(0.001f));
            Assert.That(cameraRoot.localPosition.y, Is.EqualTo(eye).Within(0.001f));
            Assert.That(stance.EyeHeight, Is.EqualTo(eye).Within(0.001f));
            Assert.That(controller.MoveSpeed, Is.EqualTo(speed).Within(0.001f));
            Assert.That(body.stepOffset, Is.GreaterThan(0f));
        }
    }
}
