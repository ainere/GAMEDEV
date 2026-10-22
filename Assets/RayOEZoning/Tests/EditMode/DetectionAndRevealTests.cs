using System.Linq;
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

        HazardDetector DirectionalDetector(DetectionShape shape, Vector3 playerPosition)
        {
            var hazard = Detector(shape, playerPosition);
            hazard.transform.position = new Vector3(0, 1.55f, 0);
            hazard.home = hazard.transform.position;
            hazard.lateralAmplitude = 0f;
            hazard.useViewCone = true;
            hazard.viewAngle = 60f;
            hazard.viewRange = 5f;

            var pan = Object("CCTV pan pivot").transform;
            pan.SetParent(hazard.transform, false);
            var lens = Object("Directional CCTV lens").transform;
            lens.SetParent(pan, false);
            hazard.panPivot = pan;
            hazard.rayOrigin = lens;
            hazard.panBaseEuler = Vector3.zero;
            hazard.panAmplitude = 0f;
            hazard.panPeriod = 8f;
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

        [TestCase(DetectionShape.Sphere)]
        [TestCase(DetectionShape.Box)]
        [TestCase(DetectionShape.Capsule)]
        public void DirectionalQueryDetectsInConeAndRealCoverBlocksSight(DetectionShape shape)
        {
            var detector = DirectionalDetector(shape, new Vector3(0, 0, 4));
            Assert.That(detector.Evaluate(), Is.True, "An in-range player in front of the lens is detected.");
            Assert.That(detector.HasPlayerOverlap, Is.True);
            Assert.That(detector.HasSightSample, Is.True);

            var cover = Object("Opaque CCTV cover");
            cover.layer = 28;
            cover.transform.position = new Vector3(0, 1.2f, 2);
            cover.AddComponent<BoxCollider>().size = new Vector3(4, 3.2f, .2f);
            Physics.SyncTransforms();

            Assert.That(detector.Evaluate(), Is.False, "Cover between the lens and both player samples blocks sight.");
            Assert.That(detector.HasPlayerOverlap, Is.True, "Occlusion must not alter the broadphase player query.");
            Assert.That(detector.HasSightSample, Is.True);
            Assert.That(detector.LastRayBlocked, Is.True);
        }

        [TestCase(DetectionShape.Sphere, 0f, -.1f, "behind the camera")]
        [TestCase(DetectionShape.Box, 0f, -.1f, "behind the camera")]
        [TestCase(DetectionShape.Capsule, 0f, -.1f, "behind the camera")]
        [TestCase(DetectionShape.Sphere, 2.2f, 3.5f, "outside its viewing angle")]
        [TestCase(DetectionShape.Box, 2.2f, 3.5f, "outside its viewing angle")]
        [TestCase(DetectionShape.Capsule, 2.2f, 3.5f, "outside its viewing angle")]
        [TestCase(DetectionShape.Sphere, 0f, 5.05f, "outside its radial range")]
        [TestCase(DetectionShape.Box, 0f, 5.05f, "outside its radial range")]
        [TestCase(DetectionShape.Capsule, 0f, 5.05f, "outside its radial range")]
        public void DirectionalConeRejectsPlayerEvenWhenConservativeQueryOverlaps(
            DetectionShape shape, float x, float z, string reason)
        {
            var detector = DirectionalDetector(shape, new Vector3(x, 0, z));
            Assert.That(detector.Evaluate(), Is.False, "A player " + reason + " must not be detected.");
            Assert.That(detector.HasPlayerOverlap, Is.True,
                "The shape-specific broadphase should conservatively overlap this player's collider.");
            Assert.That(detector.HasSightSample, Is.False, "Out-of-cone samples must not reach the LOS raycast stage.");
            Assert.That(detector.LastRayBlocked, Is.False, "Cone rejection is not an obstruction hit.");
        }

        [TestCase(DetectionShape.Sphere)]
        [TestCase(DetectionShape.Box)]
        [TestCase(DetectionShape.Capsule)]
        public void DirectionalQueryIncludesPlayerNearTheConeEdge(DetectionShape shape)
        {
            // The lens sits 1.5 m above the player's footprint sample, so straight ahead the footprint
            // reaches the 30 degree half-angle at 1.5 / tan(29.5) metres.
            float distance = 1.5f / Mathf.Tan(29.5f * Mathf.Deg2Rad);
            var detector = DirectionalDetector(shape, new Vector3(0, 0, distance));

            Assert.That(detector.Evaluate(), Is.True, "A footprint just inside the 30 degree half-angle is visible.");
            Assert.That(detector.HasPlayerOverlap, Is.True,
                "The CheckSphere/CheckBox/CheckCapsule broadphase must include the vision-cone edge.");
            Assert.That(detector.HasSightSample, Is.True);

            detector.player.position = new Vector3(0, 0, 1.5f / Mathf.Tan(31f * Mathf.Deg2Rad));
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.False, "A footprint just outside the half-angle is not lit, so it is not seen.");
        }

        [TestCase(DetectionShape.Sphere)]
        [TestCase(DetectionShape.Box)]
        [TestCase(DetectionShape.Capsule)]
        public void MountedSearchlightOnlySeesPlayersStandingInItsLitPool(DetectionShape shape)
        {
            var detector = DirectionalDetector(shape, new Vector3(0, 0, 1.2f));
            detector.transform.position = new Vector3(0, 2.9f, 0);
            detector.home = detector.transform.position;
            detector.rayOrigin.localRotation = Quaternion.Euler(30, 0, 0);
            detector.viewAngle = 40f;
            detector.viewRange = 10f;
            Physics.SyncTransforms();
            Vector3 head = detector.player.position + Vector3.up * 1.66f;
            Assert.That(detector.IsWithinView(head), Is.True, "Close beneath the lens the head is inside the 3D cone...");
            Assert.That(detector.Evaluate(), Is.False, "...but the feet are outside the lit floor pool, so the player is unseen.");
            Assert.That(detector.HasPlayerOverlap, Is.True);
            Assert.That(detector.HasSightSample, Is.False);

            detector.player.position = new Vector3(0, 0, 2.85f / Mathf.Tan(30f * Mathf.Deg2Rad));
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.True, "Standing in the centre of the floor pool is seen.");
        }

        [TestCase(DetectionShape.Sphere)]
        [TestCase(DetectionShape.Box)]
        [TestCase(DetectionShape.Capsule)]
        public void PanningLensRotatesTheActualSightDirection(DetectionShape shape)
        {
            var detector = DirectionalDetector(shape, new Vector3(0, 0, 3));
            detector.panAmplitude = 45f;
            Assert.That(detector.Evaluate(), Is.True);
            Vector3 originalForward = detector.SightForward;

            detector.SetPatrolTime(2f);
            Physics.SyncTransforms();
            Assert.That(Vector3.Angle(originalForward, detector.SightForward), Is.EqualTo(45f).Within(.1f));
            Assert.That(detector.Evaluate(), Is.False, "The former target leaves the panned 60 degree cone.");
            Assert.That(detector.HasPlayerOverlap, Is.True,
                "Panning may not shrink the conservative query so far that it hides the cone rejection.");
            Assert.That(detector.HasSightSample, Is.False);

            Vector3 targetPosition = detector.SightOrigin + detector.SightForward * 3f;
            targetPosition.y = 0f;
            detector.player.position = targetPosition;
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.True, "A player along the updated lens direction is detected.");
        }

        [Test]
        public void MountedCameraSeesOverLowCoverButTallCoverBreaksSight()
        {
            var detector = Detector(DetectionShape.Sphere, new Vector3(1, 0, 0));
            var cover = Object("Server cover");
            cover.layer = 28;
            cover.transform.position = new Vector3(.5f, .75f, 0);
            var solid = cover.AddComponent<BoxCollider>();
            solid.size = new Vector3(.15f, 1.5f, 4);
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.False, "The default lower sensor is occluded.");

            var lens = Object("Rail camera lens");
            lens.transform.SetParent(detector.transform, false);
            lens.transform.localPosition = Vector3.up * 1.75f;
            detector.rayOrigin = lens.transform;
            Assert.That(detector.Evaluate(), Is.True, "The actual elevated lens sees over low cover.");
            Assert.That(detector.HasPlayerOverlap, Is.True);
            Assert.That(detector.LastRayOrigin, Is.EqualTo(lens.transform.position));

            cover.transform.position = new Vector3(.5f, 1.5f, 0);
            solid.size = new Vector3(.15f, 3f, 4);
            Physics.SyncTransforms();
            Assert.That(detector.Evaluate(), Is.False, "Tall cabinets must block both sight rays.");
            Assert.That(detector.HasPlayerOverlap, Is.True, "Cover affects sight, not the spatial query.");
            Assert.That(detector.LastRayBlocked, Is.True);
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
            var lamp = camera.transform;
            clue.Evaluate(true, 8, 50, lamp);
            Assert.That(clue.IsVisible, Is.True);
            clue.Evaluate(false, 8, 50, lamp);
            Assert.That(clue.IsVisible, Is.False);
            Assert.That(clue.hiddenRenderers[0].enabled, Is.False);
            Assert.That(clue.hiddenRenderers[0].shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            camera.transform.rotation = Quaternion.Euler(0, 90, 0);
            clue.Evaluate(true, 8, 50, lamp);
            Assert.That(clue.IsVisible, Is.False);
            camera.transform.rotation = Quaternion.identity;
            clue.transform.position = new Vector3(0, 0, 9);
            clue.Evaluate(true, 8, 50, lamp);
            Assert.That(clue.IsVisible, Is.False);
            clue.transform.position = new Vector3(0, 0, 2);
            var blocker = Object("Solid obstruction");
            blocker.layer = 28;
            blocker.transform.position = new Vector3(0, 0, 1);
            blocker.AddComponent<BoxCollider>().size = new Vector3(2, 2, .2f);
            Physics.SyncTransforms();
            clue.Evaluate(true, 8, 50, lamp);
            Assert.That(clue.IsVisible, Is.False);
        }

        [Test]
        public void ClueUsesFlashlightMuzzleForConeAndOcclusionInsteadOfCameraOrigin()
        {
            var clue = Clue(out var camera);
            var emitter = Object("Flashlight muzzle").transform;
            emitter.position = new Vector3(1, 0, 0);
            emitter.rotation = Quaternion.identity;

            var blocker = Object("Camera-only ray obstruction");
            blocker.layer = 28;
            blocker.transform.position = new Vector3(0, 0, 1);
            var collider = blocker.AddComponent<BoxCollider>();
            collider.size = new Vector3(.2f, 2, .2f);
            Physics.SyncTransforms();

            clue.Evaluate(true, 8, 60, camera.transform);
            Assert.That(clue.IsVisible, Is.False, "A lamp at the eye would be blocked by the obstacle directly ahead.");
            emitter.rotation = Quaternion.Euler(0, 90, 0);
            clue.Evaluate(true, 8, 60, emitter);
            Assert.That(clue.IsVisible, Is.False, "The emitter's own forward direction controls its cone.");
            emitter.rotation = Quaternion.identity;
            clue.Evaluate(true, 8, 60, emitter);
            Assert.That(clue.IsVisible, Is.True,
                "The offset muzzle ray passes beside the camera-only obstacle and illuminates the clue.");

            blocker.transform.position = new Vector3(.5f, 0, 1);
            Physics.SyncTransforms();
            clue.Evaluate(true, 8, 60, emitter);
            Assert.That(clue.IsVisible, Is.False, "An obstacle crossing the actual muzzle ray hides the clue.");
            clue.Evaluate(true, 8, 60, camera.transform);
            Assert.That(clue.IsVisible, Is.True, "The eye-line ray remains clear after moving the blocker.");
        }

        [Test]
        public void WritingWhoseEdgeIsLitStaysOnForThePerPixelReveal()
        {
            var clue = Clue(out var camera);
            // 27.7 degrees off axis: the centre is outside a 50 degree cone, but a 0.2 m wide sheet reaches into it.
            clue.transform.position = new Vector3(1.05f, 0, 2);
            Physics.SyncTransforms();
            clue.radius = .2f;
            Assert.That(clue.Evaluate(true, 8, 50, camera.transform), Is.True);
            clue.radius = 0;
            Assert.That(clue.Evaluate(true, 8, 50, camera.transform), Is.False);
        }

        [Test]
        public void EveryRolledPuzzleHasExactlyOneSolutionSplitAcrossTwoNotes()
        {
            Assert.That(PuzzleCatalog.Solutions(PuzzleCatalog.Classic), Is.EqualTo(1));
            Assert.That(PuzzleCatalog.Classic.code, Is.EqualTo("2469"));
            var codes = new HashSet<string>();
            for (int seed = 1; seed <= 200; seed++)
            {
                var run = PuzzleCatalog.Generate(seed);
                codes.Add(run.code);
                Assert.That(run.code, Has.Length.EqualTo(4), "Seed " + seed);
                Assert.That(run.order, Has.Length.EqualTo(4));
                Assert.That(new HashSet<string>(run.order).Count, Is.EqualTo(4), "Symbols in one run are distinct.");
                Assert.That(run.clues.Count(c => c.kind == ClueKind.Code), Is.EqualTo(4));
                Assert.That(run.clues.Count(c => c.kind == ClueKind.Order), Is.EqualTo(2));
                Assert.That(PuzzleCatalog.Solutions(run), Is.EqualTo(1), "Seed " + seed + " must have one keypad order.");
                for (int i = 0; i < 4; i++)
                    Assert.That(run.clues.First(c => c.symbol == run.order[i]).digit, Is.EqualTo(run.code[i].ToString()));
                foreach (var note in run.clues.Where(c => c.kind == ClueKind.Order))
                {
                    var alone = new PuzzleRun(run.seed, run.code, run.order, new[] { note });
                    Assert.That(PuzzleCatalog.Solutions(alone), Is.GreaterThan(1), "One order note alone must not give the answer away.");
                }
            }
            Assert.That(codes.Count, Is.GreaterThan(150), "Codes must change from run to run.");
        }
    }
}
