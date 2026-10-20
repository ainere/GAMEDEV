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
            Assert.That(game.clues, Has.Length.EqualTo(4));
            Assert.That(game.keypad, Is.Not.Null);
            Assert.That(Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None), Is.Empty);
            foreach (var hazard in game.hazards)
            {
                Assert.That(hazard.player, Is.EqualTo(game.body.transform));
                Assert.That(hazard.rayOrigin, Is.Not.Null, "The surveillance lens must supply its actual sight origin.");
                Assert.That(hazard.rayOrigin.IsChildOf(hazard.transform), Is.True);
                Assert.That(hazard.playerMask.value & (1 << game.body.gameObject.layer), Is.Not.Zero);
                Assert.That(hazard.obstructionMask.value & hazard.playerMask.value, Is.Zero);
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
        public void EachClueCanBeIlluminatedAndReadFromItsAuthoredApproach()
        {
            EditorSceneManager.OpenScene(Scene);
            var game = Object.FindAnyObjectByType<StealthGame>();
            float[] approachZ = { 2.5f, 8.6f, 16.6f, 25.15f };
            for (int i = 0; i < game.clues.Length; i++)
            {
                var clue = game.clues[i];
                var target = clue.anchor ? clue.anchor.position : clue.transform.position;
                game.playerCamera.transform.position = new Vector3(0, 1.71f, approachZ[i]);
                game.playerCamera.transform.LookAt(target);
                Physics.SyncTransforms();
                clue.ResetReading();
                Assert.That(clue.Evaluate(game.playerCamera, true, 8, 50, .81f), Is.True,
                    PuzzleCatalog.Clues[i].title + " needs an unobstructed reading approach within 3 m.");
                clue.Evaluate(game.playerCamera, false, 8, 50, 0);
                Assert.That(clue.hiddenRenderers.All(r => !r.enabled), Is.True);
            }
        }
    }
}
