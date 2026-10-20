using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning.Tests
{
    public sealed class DetectionAndRevealTests
    {
        readonly List<GameObject> objects = new List<GameObject>();

        GameObject Object(string name)
        {
            var value = new GameObject(name);
            objects.Add(value);
            return value;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var value in objects) if (value) UnityEngine.Object.DestroyImmediate(value);
            objects.Clear();
            Physics.SyncTransforms();
        }

        HazardDetector Detector(DetectionShape shape, Vector3 playerPosition)
        {
            var player = Object("Actual player collider");
            player.layer = 29;
            player.transform.position = playerPosition;
            var capsule = player.AddComponent<CharacterController>();
            capsule.height = 1.8f;
            capsule.radius = .3f;
            capsule.center = Vector3.up * .9f;
            var hazard = Object("Detector").AddComponent<HazardDetector>();
            hazard.shape = shape;
            hazard.home = new Vector3(0, 1.1f, 0);
            hazard.transform.position = hazard.home;
            hazard.radius = 1.55f;
            hazard.halfExtents = new Vector3(1.3f, 1.2f, 2);
            hazard.capsulePointA = new Vector3(0, 0, -2);
            hazard.capsulePointB = new Vector3(0, 0, 2);
            hazard.lateralAmplitude = 1.15f;
            hazard.period = 8;
            hazard.playerMask = 1 << 29;
            hazard.obstructionMask = 1 << 28;
            hazard.player = player.transform;
            Physics.SyncTransforms();
            return hazard;
        }

        [TestCase(DetectionShape.Sphere)]
        [TestCase(DetectionShape.Box)]
        [TestCase(DetectionShape.Capsule)]
        public void SpatialQueriesDetectPlayerAndRejectOutsidePlayer(DetectionShape shape)
        {
            var detector = Detector(shape, new Vector3(.4f, 0, .4f));
            Assert.That(detector.Evaluate(), Is.True, "The real player collider overlaps this query.");
            detector.player.position = new Vector3(8, 0, 0);
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.False);
        }

        [TestCase(DetectionShape.Sphere)]
        [TestCase(DetectionShape.Box)]
        [TestCase(DetectionShape.Capsule)]
        public void OpaqueCoverInterruptsActualRaycastDetection(DetectionShape shape)
        {
            var detector = Detector(shape, new Vector3(1, 0, 0));
            Assert.That(detector.Evaluate(), Is.True);
            var cover = Object("Opaque cover");
            cover.layer = 28;
            cover.transform.position = new Vector3(.5f, 1.3f, 0);
            cover.AddComponent<BoxCollider>().size = new Vector3(.15f, 3, 4);
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.False);
            cover.transform.position += Vector3.forward * 10;
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.True, "Moving cover away restores sight.");
        }

        [Test]
        public void BoxUsesOrientedExtentsRatherThanSphereApproximation()
        {
            var detector = Detector(DetectionShape.Box, new Vector3(2.4f, 0, 0));
            detector.halfExtents = new Vector3(.4f, 1.2f, 3);
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.False);
            detector.transform.rotation = Quaternion.Euler(0, 90, 0);
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.True);
        }

        [Test]
        public void PlayerMaskIgnoresUnrelatedOverlappingGeometry()
        {
            var detector = Detector(DetectionShape.Sphere, new Vector3(8, 0, 0));
            Object("Unrelated geometry").AddComponent<BoxCollider>().size = Vector3.one * 2;
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.False);
        }

        [Test]
        public void PatrolIsPeriodicAndMovesTheDetectionVolume()
        {
            var detector = Detector(DetectionShape.Sphere, new Vector3(8, 0, 0));
            detector.SetPatrolTime(0);
            var initial = detector.transform.position;
            detector.SetPatrolTime(2);
            Assert.That(Vector3.Distance(initial, detector.transform.position), Is.GreaterThan(.5f));
            detector.SetPatrolTime(8);
            Assert.That(Vector3.Distance(initial, detector.transform.position), Is.LessThan(.001f));
        }

        HiddenClue Clue(out Camera camera)
        {
            camera = Object("Flashlight camera").AddComponent<Camera>();
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            objects.Add(target);
            target.name = "Hidden physical clue";
            target.layer = 11;
            target.transform.position = new Vector3(0, 0, 2);
            var clue = target.AddComponent<HiddenClue>();
            clue.clueIndex = 0;
            clue.anchor = target.transform;
            clue.hiddenRenderers = new Renderer[] { target.GetComponent<Renderer>() };
            clue.obstructionMask = 1 << 28;
            Physics.SyncTransforms();
            return clue;
        }

        [Test]
        public void ClueRequiresTorchConeRangeAndClearSightEveryFrame()
        {
            var clue = Clue(out var camera);
            clue.Evaluate(camera, true, 8, 50, .02f);
            Assert.That(clue.IsVisible, Is.True);
            clue.Evaluate(camera, false, 8, 50, .02f);
            Assert.That(clue.IsVisible, Is.False);
            Assert.That(clue.hiddenRenderers[0].enabled, Is.False);
            Assert.That(clue.hiddenRenderers[0].shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            camera.transform.rotation = Quaternion.Euler(0, 90, 0);
            clue.Evaluate(camera, true, 8, 50, .02f);
            Assert.That(clue.IsVisible, Is.False);
            camera.transform.rotation = Quaternion.identity;
            clue.transform.position = new Vector3(0, 0, 9);
            clue.Evaluate(camera, true, 8, 50, .02f);
            Assert.That(clue.IsVisible, Is.False);
            clue.transform.position = new Vector3(0, 0, 2);
            var blocker = Object("Solid obstruction");
            blocker.layer = 28;
            blocker.transform.position = new Vector3(0, 0, 1);
            blocker.AddComponent<BoxCollider>().size = new Vector3(2, 2, .2f);
            Physics.SyncTransforms();
            clue.Evaluate(camera, true, 8, 50, .02f);
            Assert.That(clue.IsVisible, Is.False);
        }

        [Test]
        public void NotebookReadRequiresContinuousCloseIllumination()
        {
            var clue = Clue(out var camera);
            Assert.That(clue.Evaluate(camera, true, 8, 50, .4f), Is.False);
            clue.Evaluate(camera, false, 8, 50, .1f);
            Assert.That(clue.Evaluate(camera, true, 8, 50, .4f), Is.False,
                "An interrupted glance cannot accumulate read time.");
            Assert.That(clue.Evaluate(camera, true, 8, 50, .41f), Is.True);
            clue.ResetReading();
            clue.transform.position = new Vector3(0, 0, 4);
            Assert.That(clue.Evaluate(camera, true, 8, 50, 2), Is.False,
                "A visible clue beyond reading range cannot be automatically transcribed.");
        }

        [Test]
        public void EnvironmentalCluesHaveExactlyOneSolution()
        {
            var solutions = new List<string>();
            var names = new[] { "Eye", "Moon", "Gate", "Crown" };
            foreach (var a in names) foreach (var b in names)
            foreach (var c in names) foreach (var d in names)
            {
                var order = new[] { a, b, c, d };
                if (new HashSet<string>(order).Count != 4) continue;
                if (a == "Eye" && d != "Gate" &&
                    System.Array.IndexOf(order, "Crown") == System.Array.IndexOf(order, "Moon") + 1)
                    solutions.Add(string.Join(",", order));
            }
            Assert.That(solutions, Is.EqualTo(new[] { "Eye,Gate,Moon,Crown" }));
            Assert.That(PuzzleCatalog.Clues.Length, Is.EqualTo(4));
            Assert.That(PuzzleCatalog.Code, Is.EqualTo("2469"));
        }
    }
}
