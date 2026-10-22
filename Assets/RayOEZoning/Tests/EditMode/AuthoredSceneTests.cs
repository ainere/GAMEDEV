using System.Linq;
using NUnit.Framework;
using StarterAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RayOEZoning.Tests
{
    public sealed class AuthoredSceneTests
    {
        const string Scene = "Assets/RayOEZoning/Scenes/RayOEZoning.unity";

        [Test]
        public void NewSceneHasOneControllerAndCompleteIndependentStealthWiring()
        {
            var scene = EditorSceneManager.OpenScene(Scene);
            var game = Object.FindAnyObjectByType<StealthGame>();
            Assert.That(game, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<FirstPersonController>(FindObjectsSortMode.None), Has.Length.EqualTo(1));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c => c.CompareTag("MainCamera")), Is.EqualTo(1));
            Assert.That(game.controller.GetComponent<PlayerInput>().actions, Is.Not.Null);
            Assert.That(game.playerCamera.transform.IsChildOf(game.controller.transform), Is.True);
            Assert.That(game.spawn, Is.Not.EqualTo(game.body.transform));
            Assert.That(game.hazards.Select(h => h.shape).Distinct().Count(), Is.EqualTo(3));
            Assert.That(game.clues, Has.Length.EqualTo(6));
            Assert.That(game.director, Is.Not.Null);
            Assert.That(game.director.spots.Select(s => s.zone).Distinct().Count(), Is.EqualTo(4));
            Assert.That(game.keypad, Is.Not.Null);
            Assert.That(game.escapeZ, Is.InRange(55f, 65f), "The laboratory is a modest expansion of the previous 48 m hall.");
            Assert.That(game.hazards.Length, Is.InRange(5, 8));
            Assert.That(game.hazards.Max(h => h.VisionAngle) - game.hazards.Min(h => h.VisionAngle), Is.GreaterThan(20f));
            Assert.That(game.stance, Is.Not.Null);
            Assert.That(game.stance.body, Is.EqualTo(game.body));
            Assert.That(game.stance.cameraRoot, Is.EqualTo(game.controller.CinemachineCameraTarget.transform));
            Assert.That(game.torchOrigin, Is.Not.Null, "The held flashlight needs a physical emitter.");
            Assert.That(game.torchOrigin.IsChildOf(game.playerCamera.transform), Is.True);
            Assert.That(game.torchOrigin.IsChildOf(game.torch.transform), Is.False,
                "Moving the spotlight to its own descendant would accumulate pose drift.");
            Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None), Is.Empty);
            foreach (var hazard in game.hazards)
            {
                Assert.That(hazard.player, Is.EqualTo(game.body.transform));
                Assert.That(hazard.rayOrigin, Is.Not.Null, "The surveillance lens must supply its actual sight origin.");
                Assert.That(hazard.rayOrigin.IsChildOf(hazard.transform), Is.True);
                Assert.That(hazard.useViewCone, Is.True, "Visible CCTV vision must gate detection.");
                Assert.That(hazard.panPivot, Is.Not.Null);
                Assert.That(hazard.playerMask.value & (1 << game.body.gameObject.layer), Is.Not.Zero);
                Assert.That(hazard.obstructionMask.value & hazard.playerMask.value, Is.Zero);
                var visual = hazard.GetComponent<ScannerVisual>();
                Assert.That(visual, Is.Not.Null);
                Assert.That(visual.lamp, Is.Not.Null, "Surveillance must illuminate surfaces with an actual spotlight.");
                Assert.That(visual.lamp.type, Is.EqualTo(LightType.Spot));
                Assert.That(visual.lamp.shadows, Is.Not.EqualTo(LightShadows.None));
                Assert.That(visual.lamp.transform.IsChildOf(hazard.panPivot), Is.True);
                Assert.That(visual.lens.transform.IsChildOf(hazard.panPivot), Is.True);
                Assert.That(visual.lamp.range, Is.EqualTo(hazard.VisionRange).Within(.001f));
                Assert.That(visual.lamp.spotAngle, Is.EqualTo(hazard.VisionAngle).Within(.001f));
                foreach (float time in new[] { 0f, hazard.panPeriod * .23f, hazard.panPeriod * .61f })
                {
                    hazard.SetPatrolTime(time);
                    Assert.That(Vector3.Distance(visual.lamp.transform.position, hazard.SightOrigin), Is.LessThan(.001f));
                    Assert.That(Vector3.Angle(visual.lamp.transform.forward, hazard.SightForward), Is.LessThan(.01f));
                }
            }
            foreach (var root in scene.GetRootGameObjects())
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            {
                Assert.That(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), Is.Zero, transform.name);
                foreach (var behavior in transform.GetComponents<MonoBehaviour>())
                    Assert.That(behavior.GetType().Namespace, Is.Not.EqualTo("LaserCorridor"), "Legacy gameplay is not active: " + transform.name);
            }
        }

        [Test]
        public void ExitBeamsConnectToPhysicalPortsThatRemainAfterUnlock()
        {
            EditorSceneManager.OpenScene(Scene);
            var presentation = Object.FindAnyObjectByType<StealthPresentation>();
            var ports = presentation.exitGrid.transform.parent.GetComponentsInChildren<Transform>()
                .Where(t => t.name.StartsWith("Sphere")).ToArray();
            var beams = presentation.exitGrid.GetComponentsInChildren<LineRenderer>();
            Assert.That(beams, Is.Not.Empty);
            Assert.That(ports.Length, Is.EqualTo(beams.Length * 2));
            foreach (var beam in beams)
            {
                Assert.That(beam.positionCount, Is.EqualTo(2));
                for (int endpoint = 0; endpoint < 2; endpoint++)
                {
                    Vector3 point = beam.useWorldSpace ? beam.GetPosition(endpoint) : beam.transform.TransformPoint(beam.GetPosition(endpoint));
                    Assert.That(ports.Min(p => Vector3.Distance(p.position, point)), Is.LessThan(.04f),
                        "Every beam must visibly terminate on an emitter or receiver.");
                }
            }
            presentation.exitGrid.SetActive(false);
            Assert.That(ports.All(p => p.gameObject.activeInHierarchy), Is.True,
                "Unlocking disables the laser, not the bolted door hardware.");
        }

        [Test]
        public void EachClueSurfaceCanBeIlluminatedAndReadFromItsApproach()
        {
            EditorSceneManager.OpenScene(Scene);
            var game = Object.FindAnyObjectByType<StealthGame>();
            foreach (var spot in game.director.spots)
            {
                var clue = spot.clue;
                var target = clue.anchor.position;
                // The clue face's +Z points into the surface it is written on.
                Vector3 approach = target - clue.anchor.forward * 1.5f;
                approach.y = 1.71f;
                game.playerCamera.transform.position = approach;
                game.playerCamera.transform.LookAt(target);
                game.torch.transform.SetPositionAndRotation(game.torchOrigin.position, game.torchOrigin.rotation);
                Physics.SyncTransforms();
                clue.ResetReading();
                Assert.That(clue.Evaluate(game.playerCamera, true, game.torch.range, game.torch.spotAngle, .81f,
                    game.torch.transform), Is.True, spot.name + " needs an unobstructed reading approach within 3 m.");
                clue.Evaluate(game.playerCamera, false, 8, 50, 0);
                Assert.That(clue.hiddenRenderers.All(r => !r.enabled), Is.True);
            }
        }
    }
}
