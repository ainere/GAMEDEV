using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Knockdown.Tests
{
    public sealed class KnockdownScenePlayModeTests
    {
        private const string SceneName = "Knockdown";
        private const float BoardMinX = -4.8f;
        private const float BoardMaxX = 4.8f;
        private const float BoardMinY = 0.65f;
        private const float BoardMaxY = 4.85f;

        private KnockdownGameplayController _controller;
        private Mouse _testMouse;
        private float _originalTimeScale;
        private bool _originalAudioPause;
        private bool _originalCursorVisible;
        private CursorLockMode _originalCursorLock;
        private InputSettings.BackgroundBehavior _originalBackgroundBehavior;
        private InputSettings.EditorInputBehaviorInPlayMode _originalEditorInputBehavior;
        private bool _inputSettingsSaved;
        private RectTransform _relocatedPauseButton;
        private Vector2 _pauseButtonOriginalAnchorMin;
        private Vector2 _pauseButtonOriginalAnchorMax;
        private Vector2 _pauseButtonOriginalAnchoredPosition;

        [UnitySetUp]
        public IEnumerator LoadAuthoredScene()
        {
            _originalTimeScale = Time.timeScale;
            _originalAudioPause = AudioListener.pause;
            _originalCursorVisible = Cursor.visible;
            _originalCursorLock = Cursor.lockState;
            Time.timeScale = 1f;
            AudioListener.pause = true;

            var load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            Assert.That(load, Is.Not.Null,
                $"Scene '{SceneName}' must be present in EditorBuildSettings for PlayMode integration tests.");
            yield return load;
            yield return null;
            yield return null;

            _controller = UnityEngine.Object.FindFirstObjectByType<KnockdownGameplayController>();
            Assert.That(_controller, Is.Not.Null,
                $"Loaded scene '{SceneName}' must contain a KnockdownGameplayController.");
            Assert.That(_controller.Model, Is.Not.Null,
                "KnockdownGameplayController.Awake must initialize its RoundModel before tests continue.");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditorState()
        {
            RestorePauseButtonPosition();
            Time.timeScale = 1f;
            if (_testMouse != null && _testMouse.added)
                InputSystem.RemoveDevice(_testMouse);
            RestoreTestInputSettings();

            AudioListener.pause = _originalAudioPause;
            Cursor.visible = _originalCursorVisible;
            Cursor.lockState = _originalCursorLock;
            Time.timeScale = _originalTimeScale;
            yield return null;
        }

        [Test]
        public void AuthoredSceneHasCameraHudAndTwelveConfiguredTargets()
        {
            Assert.That(_controller.GameplayCamera, Is.Not.Null, "Gameplay camera is not assigned.");
            Assert.That(_controller.Balloons.Count, Is.EqualTo(12), "The scene must contain exactly twelve targets.");
            Assert.That(UnityEngine.Object.FindFirstObjectByType<Canvas>(), Is.Not.Null,
                "The authored scene must contain its runtime HUD canvas.");
            Assert.That(UnityEngine.Object.FindFirstObjectByType<EventSystem>(), Is.Not.Null,
                "The authored scene must contain an EventSystem for UI interaction.");

            var boothRoots = new List<Transform>();
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hasTargetBacking = false;
                var hasCounterTop = false;
                foreach (Transform child in root.transform)
                {
                    hasTargetBacking |= child.name == "Navy target backing";
                    hasCounterTop |= child.name == "Counter top";
                }
                if (hasTargetBacking || hasCounterTop)
                    boothRoots.Add(root.transform);
            }

            Assert.That(boothRoots.Count, Is.EqualTo(1),
                "The authored scene must contain exactly one booth root with its target backing and counter geometry.");
            var booth = boothRoots[0];
            var targetBackings = 0;
            var counterTops = 0;
            foreach (Transform child in booth)
            {
                if (child.name == "Navy target backing") targetBackings++;
                if (child.name == "Counter top") counterTops++;
            }
            Assert.That(targetBackings, Is.EqualTo(1), "The booth must contain exactly one 'Navy target backing'.");
            Assert.That(counterTops, Is.EqualTo(1), "The booth must contain exactly one 'Counter top'.");

            foreach (var balloon in _controller.Balloons)
            {
                Assert.That(balloon, Is.Not.Null, "The controller target list contains a null entry.");
                Assert.That(balloon.BodyCollider, Is.Not.Null, $"Target '{balloon.name}' has no body collider.");
                Assert.That(balloon.BodyCollider.enabled, Is.True, $"Target '{balloon.name}' starts without an enabled collider.");
                Assert.That(balloon.IsAvailable, Is.True, $"Target '{balloon.name}' should start available.");
                Assert.That(balloon.BodyCollider.attachedRigidbody, Is.Not.Null,
                    $"Target '{balloon.name}' needs a Rigidbody for physical dart collisions.");
            }
        }

        [UnityTest]
        public IEnumerator CameraFramingKeepsTheWholeBoothInsideCommonAspectRatios()
        {
            var camera = _controller.GameplayCamera;
            var framing = camera.GetComponent<KnockdownCameraFraming>();
            Assert.That(framing, Is.Not.Null, "The gameplay camera must keep the authored booth framing component.");
            framing.ResetLook();
            var originalAspect = camera.aspect;
            var originalPosition = camera.transform.position;
            Transform booth = null;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                var hasCounter = false;
                foreach (Transform child in root.transform)
                    hasCounter |= child.name == "Counter top";
                if (hasCounter)
                {
                    booth = root.transform;
                    break;
                }
            }
            Assert.That(booth, Is.Not.Null, "The authored booth root must include its attached counter geometry.");
            var renderers = booth.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0), "The booth should have visible geometry to frame.");

            try
            {
                foreach (var aspect in new[] { 4f / 3f, 16f / 9f, 21f / 9f })
                {
                    camera.aspect = aspect;
                    yield return null;
                    yield return null;
                    Assert.That(camera.fieldOfView, Is.GreaterThanOrEqualTo(45f));
                    Assert.That(Vector3.Distance(camera.transform.position, originalPosition), Is.LessThan(0.001f),
                        "Aspect fitting and mouse look must keep the camera at its booth anchor.");
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null || !renderer.enabled) continue;
                        var bounds = renderer.bounds;
                        for (var corner = 0; corner < 8; corner++)
                        {
                            var point = new Vector3(
                                (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                                (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                                (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                            var viewport = camera.WorldToViewportPoint(point);
                            Assert.That(viewport.z, Is.GreaterThan(0f),
                                $"Booth geometry '{renderer.name}' moved behind the camera at aspect {aspect:0.00}.");
                            Assert.That(viewport.x, Is.InRange(-0.01f, 1.01f),
                                $"Booth geometry '{renderer.name}' is horizontally cropped at aspect {aspect:0.00}.");
                            Assert.That(viewport.y, Is.InRange(-0.01f, 1.01f),
                                $"Booth geometry '{renderer.name}' is vertically cropped at aspect {aspect:0.00}.");
                        }
                    }
                }
            }
            finally
            {
                camera.aspect = originalAspect;
                framing.ResetLook();
            }
        }

        [UnityTest]
        public IEnumerator StaticTargetsStayFixedAndMovingRowsTraverseFullWidthWithBobAndWrapTeleports()
        {
            _controller.SelectMode(GameMode.Static);
            yield return EnterPlaying();

            var staticPositions = new Vector3[_controller.Balloons.Count];
            for (var i = 0; i < _controller.Balloons.Count; i++)
            {
                staticPositions[i] = GetPhysicsPosition(_controller.Balloons[i]);
                Assert.That(staticPositions[i].x, Is.InRange(BoardMinX, BoardMaxX),
                    $"Static target '{_controller.Balloons[i].name}' must stay within the board's horizontal limits.");
                Assert.That(staticPositions[i].y, Is.InRange(BoardMinY, BoardMaxY),
                    $"Static target '{_controller.Balloons[i].name}' must stay within the board's vertical limits.");
            }

            for (var i = 0; i < 12; i++)
                yield return new WaitForFixedUpdate();

            Physics.SyncTransforms();
            for (var i = 0; i < _controller.Balloons.Count; i++)
            {
                Assert.That(Vector3.Distance(GetPhysicsPosition(_controller.Balloons[i]), staticPositions[i]),
                    Is.LessThan(0.02f), $"Static target '{_controller.Balloons[i].name}' drifted from its center.");
            }

            _controller.SelectMode(GameMode.Moving);
            yield return EnterPlaying();

            var rowHeights = GetLaneRowHeights();
            Assert.That(rowHeights.Length, Is.EqualTo(3), "Moving mode must retain three target rows.");
            var laneLength = _controller.Balloons[0].LaneMaxX - _controller.Balloons[0].LaneMinX;
            var rowStarts = new List<float>[rowHeights.Length];
            foreach (var balloon in _controller.Balloons)
            {
                var row = FindRowIndex(balloon.SlotPosition.y, rowHeights);
                var expectedDirection = row % 2 == 0 ? 1 : -1;
                Assert.That(balloon.LaneDirection, Is.EqualTo(expectedDirection),
                    $"Row {row} on '{balloon.name}' must alternate its lane direction.");
                Assert.That(balloon.LaneMinX, Is.EqualTo(-5.6f).Within(0.001f));
                Assert.That(balloon.LaneMaxX, Is.EqualTo(5.6f).Within(0.001f));
                Assert.That(balloon.HorizontalSpeed, Is.EqualTo(1.4f).Within(0.001f));
                Assert.That(balloon.MovingCycleSeconds, Is.EqualTo(laneLength / balloon.HorizontalSpeed).Within(0.001f));

                if (rowStarts[row] == null)
                    rowStarts[row] = new List<float>();
                rowStarts[row].Add(balloon.PositionAt(GameMode.Moving, 0f).x);

                var start = balloon.PositionAt(GameMode.Moving, _controller.Model.ElapsedPlaySeconds);
                var shortStep = balloon.PositionAt(GameMode.Moving, _controller.Model.ElapsedPlaySeconds + 0.2f);
                Assert.That((shortStep.x - start.x) * balloon.LaneDirection, Is.GreaterThan(0.25f),
                    $"Moving target '{balloon.name}' should follow its row direction away from the seam.");
                Assert.That(balloon.PositionAt(GameMode.Static, 25f), Is.EqualTo(balloon.SlotPosition),
                    $"Static mode must continue to ignore lane time for '{balloon.name}'.");
            }

            foreach (var starts in rowStarts)
            {
                Assert.That(starts, Is.Not.Null);
                starts.Sort();
                Assert.That(starts.Count, Is.EqualTo(4), "Each moving row must contain four lane positions.");
                for (var column = 0; column < starts.Count; column++)
                {
                    var expectedX = _controller.Balloons[0].LaneMinX + (column + 0.5f) * laneLength / 4f;
                    Assert.That(starts[column], Is.EqualTo(expectedX).Within(0.02f),
                        "Moving lane columns must begin evenly spaced across the field.");
                }
            }

            var minX = new float[_controller.Balloons.Count];
            var maxX = new float[_controller.Balloons.Count];
            var minBob = new float[_controller.Balloons.Count];
            var maxBob = new float[_controller.Balloons.Count];
            var previousX = new float[_controller.Balloons.Count];
            for (var i = 0; i < _controller.Balloons.Count; i++)
            {
                var balloon = _controller.Balloons[i];
                var center = GetPhysicsPosition(balloon);
                minX[i] = maxX[i] = center.x;
                minBob[i] = maxBob[i] = center.y - balloon.SlotPosition.y;
                previousX[i] = center.x;
            }

            var observedWraps = 0;
            var observedHiddenCenters = 0;
            var observedPartialCenters = 0;
            var sampleEnd = _controller.Model.ElapsedPlaySeconds +
                            _controller.Balloons[0].MovingCycleSeconds + 0.25f;
            while (_controller.Model.ElapsedPlaySeconds + 0.001f < sampleEnd)
            {
                _controller.Model.Tick(Mathf.Min(0.1f, sampleEnd - _controller.Model.ElapsedPlaySeconds));
                yield return new WaitForFixedUpdate();
                yield return null;
                Physics.SyncTransforms();

                for (var i = 0; i < _controller.Balloons.Count; i++)
                {
                    var balloon = _controller.Balloons[i];
                    var seconds = _controller.Model.ElapsedPlaySeconds;
                    var predicted = balloon.PositionAt(GameMode.Moving, seconds);
                    var actual = GetPhysicsPosition(balloon);
                    Assert.That(Vector3.Distance(actual, predicted), Is.LessThan(0.12f),
                        $"Moving target '{balloon.name}' did not settle at its current lane position.");
                    Assert.That(actual.y, Is.InRange(BoardMinY, BoardMaxY),
                        $"Moving target '{balloon.name}' left the vertical playfield during its bob.");

                    var hidden = balloon.IsHiddenAt(GameMode.Moving, seconds);
                    if (hidden)
                    {
                        observedHiddenCenters++;
                    }
                    else
                    {
                        Assert.That(balloon.BodyCollider.enabled, Is.True,
                            $"Visible target '{balloon.name}' must remain physically targetable.");
                        if (Mathf.Abs(predicted.x) > 4.9f)
                            observedPartialCenters++;
                    }

                    minX[i] = Mathf.Min(minX[i], actual.x);
                    maxX[i] = Mathf.Max(maxX[i], actual.x);
                    var bob = actual.y - balloon.SlotPosition.y;
                    minBob[i] = Mathf.Min(minBob[i], bob);
                    maxBob[i] = Mathf.Max(maxBob[i], bob);

                    if (Mathf.Abs(actual.x - previousX[i]) > laneLength * 0.75f)
                    {
                        observedWraps++;
                        Assert.That(Mathf.Abs(actual.x), Is.GreaterThan(balloon.CollisionHalfWidth),
                            $"Wrapped target '{balloon.name}' must land behind a side mask.");
                    }
                    previousX[i] = actual.x;
                }

                AssertVisibleTargetsDoNotOverlap(_controller.Model.ElapsedPlaySeconds);
            }

            Assert.That(observedWraps, Is.GreaterThanOrEqualTo(8),
                "A complete lane cycle must teleport targets at the hidden seams.");
            Assert.That(observedHiddenCenters, Is.GreaterThan(0),
                "Targets should leave the visible field while crossing behind the booth wings.");
            Assert.That(observedPartialCenters, Is.GreaterThan(0),
                "The moving path should include partially visible, still-collidable targets.");
            for (var i = 0; i < _controller.Balloons.Count; i++)
            {
                Assert.That(minX[i], Is.LessThanOrEqualTo(-5.25f),
                    $"Target '{_controller.Balloons[i].name}' never traversed the left side of its lane.");
                Assert.That(maxX[i], Is.GreaterThanOrEqualTo(5.25f),
                    $"Target '{_controller.Balloons[i].name}' never traversed the right side of its lane.");
                Assert.That(minBob[i], Is.LessThanOrEqualTo(-0.19f),
                    $"Target '{_controller.Balloons[i].name}' missed the lower half of its vertical bob.");
                Assert.That(maxBob[i], Is.GreaterThanOrEqualTo(0.19f),
                    $"Target '{_controller.Balloons[i].name}' missed the upper half of its vertical bob.");
            }
        }

        [UnityTest]
        public IEnumerator PhysicalDartPopsOneTargetOnceAndColliderReturnsAfterRespawnDelay()
        {
            _controller.SelectMode(GameMode.Static);
            yield return EnterPlaying();

            var target = FindCentralTarget();
            var aim = target.BodyCollider.bounds.center;
            Assert.That(_controller.FireAt(aim), Is.True, "The first in-range shot should launch.");
            Assert.That(_controller.Model.Shots, Is.EqualTo(1));
            Assert.That(_controller.ActiveProjectileCount, Is.EqualTo(1));

            yield return WaitForTargetToPop(target, 2f);

            Assert.That(target.IsAvailable, Is.False,
                $"The physical dart did not pop '{target.name}' within 2 scaled seconds; active darts: {_controller.ActiveProjectileCount}.");
            Assert.That(target.BodyCollider.enabled, Is.False, "A popped target must disable its physical collider.");
            Assert.That(_controller.Model.Shots, Is.EqualTo(1), "One projectile must count as exactly one shot.");
            Assert.That(_controller.Model.Pops, Is.EqualTo(1), "One collision must resolve as one pop.");
            Assert.That(_controller.Model.Score, Is.EqualTo(10), "One pop must award one target's points exactly once.");
            Assert.That(_controller.ActiveProjectileCount, Is.Zero, "A resolved projectile must leave the active set.");

            var popTime = Time.time;
            while (Time.time < popTime + 0.55f)
                yield return new WaitForFixedUpdate();
            Assert.That(target.IsAvailable, Is.False,
                "A popped target respawned noticeably before its 0.75-second delay elapsed.");
            Assert.That(target.BodyCollider.enabled, Is.False);

            var respawnDeadline = popTime + 1.0f;
            while (!target.IsAvailable && Time.time < respawnDeadline)
                yield return new WaitForFixedUpdate();
            Assert.That(target.IsAvailable, Is.True, "A popped target did not return after its respawn delay.");
            Assert.That(target.BodyCollider.enabled, Is.True, "The respawned target collider was not re-enabled.");
        }

        [UnityTest]
        public IEnumerator PhysicalDartCanLeadAVisibleLaneTargetAndWrapDoesNotSweepAcrossTheBoard()
        {
            _controller.SelectMode(GameMode.Moving);
            yield return EnterPlaying();

            var target = FindTargetWithDirection(1);
            var targetTime = FindTimeAtX(target, -5.1f, _controller.Model.ElapsedPlaySeconds,
                target.MovingCycleSeconds * 2f);
            yield return AdvancePlayingTime(targetTime);

            var elapsed = _controller.Model.ElapsedPlaySeconds;
            var currentPosition = target.BodyCollider.bounds.center;
            Assert.That(currentPosition.x, Is.InRange(-5.25f, -4.95f),
                "The lead-fire check should start with a partially visible target entering from the left.");
            Assert.That(target.IsHiddenAt(GameMode.Moving, elapsed), Is.False,
                "A partially visible target must not be hidden by the booth mask.");
            Assert.That(target.BodyCollider.enabled, Is.True,
                "A partially visible target must remain physically targetable.");

            var origin = _controller.GameplayCamera.transform.TransformPoint(new Vector3(0f, -0.32f, 0.45f));
            var predicted = target.PositionAt(GameMode.Moving, elapsed);
            for (var iteration = 0; iteration < 8; iteration++)
            {
                var flightSeconds = Vector3.Distance(origin, predicted) / 28f;
                predicted = target.PositionAt(GameMode.Moving, elapsed + flightSeconds);
            }

            Assert.That(Mathf.Abs(predicted.x), Is.LessThanOrEqualTo(BoardMaxX),
                "The lead point should remain inside the authored aim limits.");
            const float wingPlaneZ = 2.41f;
            const float wingOpeningHalfWidth = 4.4f;
            var wingFraction = (wingPlaneZ - origin.z) / (predicted.z - origin.z);
            Assert.That(wingFraction, Is.InRange(0f, 1f),
                "The lead shot path must pass the booth wing plane before reaching the target.");
            var shotAtWing = Vector3.Lerp(origin, predicted, wingFraction);
            Assert.That(Mathf.Abs(shotAtWing.x), Is.LessThan(wingOpeningHalfWidth),
                "The partially visible lead shot must pass through the physical booth opening.");

            Assert.That(_controller.FireAt(predicted), Is.True,
                "A lead-predicted shot at a moving target should launch without relying on a reversal point.");
            yield return WaitForTargetToPop(target, 2f);

            Assert.That(target.IsAvailable, Is.False,
                $"The partially visible moving target was not hit; active darts: {_controller.ActiveProjectileCount}.");
            Assert.That(_controller.Model.Shots, Is.EqualTo(1));
            Assert.That(_controller.Model.Pops, Is.EqualTo(1));
            Assert.That(_controller.Model.Score, Is.EqualTo(10));

            var popTime = Time.time;
            var respawnDeadline = popTime + 1.1f;
            while (!target.IsAvailable && Time.time < respawnDeadline)
                yield return new WaitForFixedUpdate();
            Assert.That(target.IsAvailable, Is.True,
                "The moving target did not return after its normal respawn delay.");
            Assert.That(target.BodyCollider.enabled, Is.True,
                "The respawned moving target must become physically targetable again.");
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();

            var resumedPosition = GetPhysicsPosition(target);
            var expectedResumedPosition = target.PositionAt(GameMode.Moving, _controller.Model.ElapsedPlaySeconds);
            Assert.That(Vector3.Distance(resumedPosition, expectedResumedPosition), Is.LessThan(0.12f),
                "Respawn must resume at the current lane phase instead of resetting lane time.");
            Assert.That(Vector3.Distance(resumedPosition, target.PositionAt(GameMode.Moving, 0f)), Is.GreaterThan(0.2f),
                "A respawned moving target must not reset to its lane start.");

            var wrapTarget = FindAvailableTargetWithDirection(1, target);
            var wrapTime = FindNextWrapTime(wrapTarget, _controller.Model.ElapsedPlaySeconds + 0.1f,
                wrapTarget.MovingCycleSeconds * 2f);
            var wrapPosition = wrapTarget.PositionAt(GameMode.Moving, wrapTime);
            var gapPoint = new Vector3(1.4f, wrapPosition.y, wrapPosition.z);
            var shotOrigin = _controller.GameplayCamera.transform.TransformPoint(new Vector3(0f, -0.32f, 0.45f));
            var wrapFlightSeconds = Vector3.Distance(shotOrigin, gapPoint) / 28f;
            var launchTime = wrapTime - wrapFlightSeconds;
            if (launchTime <= _controller.Model.ElapsedPlaySeconds + 0.05f)
            {
                wrapTime += wrapTarget.MovingCycleSeconds;
                wrapPosition = wrapTarget.PositionAt(GameMode.Moving, wrapTime);
                gapPoint = new Vector3(1.4f, wrapPosition.y, wrapPosition.z);
                wrapFlightSeconds = Vector3.Distance(shotOrigin, gapPoint) / 28f;
                launchTime = wrapTime - wrapFlightSeconds;
            }

            foreach (var other in _controller.Balloons)
            {
                if (other == wrapTarget || Mathf.Abs(other.SlotPosition.y - wrapTarget.SlotPosition.y) > 0.01f)
                    continue;
                Assert.That(Mathf.Abs(other.PositionAt(GameMode.Moving, wrapTime).x - gapPoint.x),
                    Is.GreaterThan(1f), "The no-sweep shot must pass through a gap between the other lane targets.");
            }

            yield return AdvancePlayingTime(launchTime);
            Assert.That(_controller.FireAt(gapPoint), Is.True,
                "The wrap-collision check should launch a real dart through the empty lane gap.");
            yield return new WaitForSeconds(wrapFlightSeconds + 0.5f);
            Assert.That(_controller.Model.Pops, Is.EqualTo(1),
                "A target teleported behind the side mask and must not sweep through a dart in the visible lane.");
            Assert.That(_controller.Model.Score, Is.EqualTo(10));
            Assert.That(_controller.Model.Shots, Is.EqualTo(2));
            Assert.That(_controller.ActiveProjectileCount, Is.Zero,
                "The no-sweep test dart should resolve as a miss after crossing the playfield.");
        }

        [Test]
        public void FireAtRejectsInvalidAimAndRoundPhasesThatDoNotAcceptShots()
        {
            var validPoint = FindCentralTarget().BodyCollider.bounds.center;
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Ready));
            Assert.That(_controller.FireAt(validPoint), Is.False, "Ready state must reject fire.");
            Assert.That(_controller.FireAt(new Vector3(100f, 3f, _controller.AimPlaneZ)), Is.False,
                "FireAt must reject points outside the board bounds.");
            Assert.That(_controller.TryGetAim(new Vector2(-1f, -1f), out _), Is.False,
                "The camera must reject pointer coordinates outside the screen.");

            _controller.StartRound();
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Countdown));
            Assert.That(_controller.FireAt(validPoint), Is.False, "Countdown state must reject fire.");
            Assert.That(_controller.Model.Shots, Is.Zero);

            _controller.Model.Tick(_controller.Model.CountdownRemaining + 0.05f);
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Playing));
            _controller.Model.Tick(1000f);
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Results));
            Assert.That(_controller.FireAt(validPoint), Is.False, "Results state must reject fire.");
            Assert.That(_controller.Model.Shots, Is.Zero);
        }

        [UnityTest]
        public IEnumerator FireCadenceBlocksImmediateRepeatAndAllowsTheNextShot()
        {
            _controller.SelectMode(GameMode.Static);
            yield return EnterPlaying();
            var emptyBoardPoint = new Vector3(4.65f, 4.7f, _controller.AimPlaneZ);

            Assert.That(_controller.FireAt(emptyBoardPoint), Is.True, "The first shot should launch immediately.");
            Assert.That(_controller.FireAt(emptyBoardPoint), Is.False, "The 0.30-second cadence should block a repeat.");
            Assert.That(_controller.Model.Shots, Is.EqualTo(1));

            _controller.Model.Tick(0.31f);
            Assert.That(_controller.FireAt(emptyBoardPoint), Is.True, "A shot should be accepted after the cadence.");
            Assert.That(_controller.Model.Shots, Is.EqualTo(2));
            Assert.That(_controller.ActiveProjectileCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RelativeMouseDeltaTurnsCameraAndClampsLookWithoutTranslatingIt()
        {
            _controller.SelectMode(GameMode.Static);
            yield return EnterPlaying();

            var camera = _controller.GameplayCamera;
            var framing = camera.GetComponent<KnockdownCameraFraming>();
            Assert.That(framing, Is.Not.Null, "The gameplay camera must expose its mouse-look framing component.");
            var startingPosition = camera.transform.position;
            var startingRotation = camera.transform.rotation;
            AssertLockedCursor();

            AddTestMouse();
            yield return WarmLockedMouse();
            QueueMouseDelta(new Vector2(20f, -10f), false);
            yield return null;

            Assert.That(framing.LookAngles.x, Is.EqualTo(2f).Within(0.02f),
                "Relative horizontal mouse movement should turn the view by the configured sensitivity.");
            Assert.That(framing.LookAngles.y, Is.EqualTo(1f).Within(0.02f),
                "Moving the mouse down should pitch the view downward.");
            Assert.That(Vector3.Distance(camera.transform.position, startingPosition), Is.LessThan(0.001f),
                "FPS mouse look should rotate the camera around its fixed booth position.");
            Assert.That(Quaternion.Angle(camera.transform.rotation, startingRotation), Is.GreaterThan(0.5f),
                "Relative mouse movement did not rotate the camera.");

            QueueMouseDelta(new Vector2(1000f, 1000f), false);
            yield return null;
            Assert.That(framing.LookAngles.x, Is.EqualTo(framing.YawLimits.y).Within(0.02f),
                "Yaw should stop at its positive limit.");
            Assert.That(framing.LookAngles.y, Is.EqualTo(framing.PitchLimits.x).Within(0.02f),
                "Pitch should stop at its lower limit.");

            QueueMouseDelta(new Vector2(-1000f, -1000f), false);
            yield return null;
            Assert.That(framing.LookAngles.x, Is.EqualTo(framing.YawLimits.x).Within(0.02f),
                "Yaw should stop at its negative limit.");
            Assert.That(framing.LookAngles.y, Is.EqualTo(framing.PitchLimits.y).Within(0.02f),
                "Pitch should stop at its upper limit.");
            Assert.That(Vector3.Distance(camera.transform.position, startingPosition), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator CenteredCrosshairMouseFireHitsTheBalloonUnderTheSight()
        {
            _controller.SelectMode(GameMode.Static);
            var target = FindCentralTarget();
            var camera = _controller.GameplayCamera;
            var framing = camera.GetComponent<KnockdownCameraFraming>();
            var restRotation = camera.transform.rotation;
            var localDirection = Quaternion.Inverse(restRotation) *
                                 (target.BodyCollider.bounds.center - camera.transform.position).normalized;
            var wantedYaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            var wantedPitch = Mathf.Atan2(localDirection.y,
                Mathf.Sqrt(localDirection.x * localDirection.x + localDirection.z * localDirection.z)) * Mathf.Rad2Deg;
            Assert.That(wantedYaw, Is.InRange(framing.YawLimits.x, framing.YawLimits.y),
                "The selected target should be reachable inside the camera yaw limits.");
            Assert.That(wantedPitch, Is.InRange(framing.PitchLimits.x, framing.PitchLimits.y),
                "The selected target should be reachable inside the camera pitch limits.");

            yield return EnterPlaying();
            var reticle = FindReticle();
            Assert.That(reticle, Is.Not.Null, "The authored centered aiming reticle is missing.");
            Assert.That(reticle.gameObject.activeInHierarchy, Is.True,
                "The centered reticle should be visible while the player aims.");
            Assert.That(reticle.anchoredPosition, Is.EqualTo(Vector2.zero),
                "The FPS reticle should stay centered as the camera follows relative mouse movement.");
            AddTestMouse();
            yield return WarmLockedMouse();
            var relativeDelta = new Vector2(wantedYaw / framing.MouseSensitivity,
                wantedPitch / framing.MouseSensitivity);
            QueueMouseDelta(relativeDelta, false);
            yield return null;

            var sight = camera.WorldToViewportPoint(target.BodyCollider.bounds.center);
            Assert.That(sight.x, Is.EqualTo(0.5f).Within(0.015f),
                "The camera should turn until the selected balloon sits under the centered crosshair.");
            Assert.That(sight.y, Is.EqualTo(0.5f).Within(0.015f));
            Assert.That(_controller.TryGetCenterAim(out var aimPoint), Is.True,
                "The center crosshair should aim inside the target board bounds.");
            Assert.That(Vector3.Distance(aimPoint, target.BodyCollider.bounds.center), Is.LessThan(0.16f),
                "The center ray should intersect close to the balloon selected for the shot.");

            var center = camera.pixelRect.center;
            QueueMouseState(center, true);
            yield return null;
            Assert.That(_controller.Model.Shots, Is.EqualTo(1),
                "A left click with the reticle centered on a balloon should launch one physical dart.");
            QueueMouseState(center, false);
            yield return null;
            yield return null;
            yield return WaitForTargetToPop(target, 2f);

            Assert.That(target.IsAvailable, Is.False,
                "The physical dart launched from centered mouse-look did not hit the balloon under the sight.");
            Assert.That(_controller.Model.Pops, Is.EqualTo(1));
            Assert.That(_controller.Model.Score, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator StartRestartAndModeSelectionResetLookAndOnlyLockCursorDuringPlay()
        {
            _controller.SelectMode(GameMode.Static);
            var framing = _controller.GameplayCamera.GetComponent<KnockdownCameraFraming>();
            framing.ApplyMouseLook(new Vector2(80f, -40f));
            Assert.That(framing.LookAngles.sqrMagnitude, Is.GreaterThan(0.1f));

            _controller.StartRound();
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Countdown));
            Assert.That(framing.LookAngles, Is.EqualTo(Vector2.zero), "Starting a round should restore the centered view.");
            AssertUnlockedCursor();

            _controller.Model.Tick(_controller.Model.CountdownRemaining + 0.05f);
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Playing));
            AssertLockedCursor();
            yield return null;

            framing.ApplyMouseLook(new Vector2(-60f, 30f));
            Assert.That(framing.LookAngles.sqrMagnitude, Is.GreaterThan(0.1f));
            _controller.RestartRound();
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Countdown));
            Assert.That(framing.LookAngles, Is.EqualTo(Vector2.zero), "Restart should restore the centered view.");
            AssertUnlockedCursor();

            _controller.Model.Tick(_controller.Model.CountdownRemaining + 0.05f);
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Playing));
            AssertLockedCursor();
            framing.ApplyMouseLook(new Vector2(60f, -30f));
            _controller.SelectMode(GameMode.Moving);
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Ready));
            Assert.That(_controller.Model.Mode, Is.EqualTo(GameMode.Moving));
            Assert.That(framing.LookAngles, Is.EqualTo(Vector2.zero), "Changing mode should restore the centered view.");
            AssertUnlockedCursor();
        }

        [UnityTest]
        public IEnumerator HoldingMouseButtonLaunchesRepeatedShotsAtCadenceUntilReleased()
        {
            _controller.SelectMode(GameMode.Static);
            yield return EnterPlaying();

            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null, "The authored scene must include an EventSystem for mouse input.");
            AddTestMouse();
            var screenPoint = _controller.GameplayCamera.WorldToScreenPoint(
                new Vector3(4.65f, 4.7f, _controller.AimPlaneZ));
            Assert.That(_controller.TryGetAim(screenPoint, out _), Is.True,
                "The held-fire test point must be inside the valid aiming region.");

            QueueMouseState(screenPoint, false);
            yield return null;
            yield return null;
            Assert.That(Mouse.current, Is.SameAs(_testMouse), "The temporary test mouse did not become the active mouse.");
            Assert.That(_testMouse.leftButton.isPressed, Is.False, "The queued release state was not delivered to the test mouse.");
            Assert.That(eventSystem.IsPointerOverGameObject(), Is.False,
                "The held-fire aim point must not overlap an authored UI control.");

            QueueMouseState(screenPoint, true);
            yield return null;
            yield return null;
            Assert.That(Mouse.current, Is.SameAs(_testMouse), "A native mouse event replaced the simulated mouse during the press.");
            Assert.That(_testMouse.leftButton.isPressed, Is.True, "The queued press state was not delivered to the test mouse.");
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(_controller.Model.IsPaused, Is.False);
            Assert.That(_controller.TryGetAim(_testMouse.position.ReadValue(), out _), Is.True,
                "The delivered mouse position should still be inside the valid aiming region.");
            Assert.That(_controller.Model.Shots, Is.EqualTo(1),
                "A held mouse button should launch its first dart immediately after the start-round release guard clears.");

            yield return new WaitForSecondsRealtime(0.7f);
            Assert.That(_controller.Model.Shots, Is.EqualTo(3),
                "Holding for 0.7 seconds should launch at t=0, t=0.3, and t=0.6 under the 0.30-second cadence.");

            QueueMouseState(screenPoint, false);
            yield return null;
            yield return null;
            var shotsAfterRelease = _controller.Model.Shots;
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(_controller.Model.Shots, Is.EqualTo(shotsAfterRelease),
                "Releasing the mouse button should stop held-fire shots.");
        }

        [UnityTest]
        public IEnumerator PauseFreezesClockTargetsAndInFlightDartThenResumesThem()
        {
            _controller.SelectMode(GameMode.Moving);
            yield return EnterPlaying();
            AddTestMouse();
            yield return WarmLockedMouse();
            QueueMouseDelta(new Vector2(20f, 0f), false);
            yield return null;
            var framing = _controller.GameplayCamera.GetComponent<KnockdownCameraFraming>();
            var lookAtPause = framing.LookAngles;

            var target = _controller.Balloons[0];
            Assert.That(_controller.FireAt(target.BodyCollider.bounds.center), Is.True);
            var dart = UnityEngine.Object.FindFirstObjectByType<KnockdownDart>();
            Assert.That(dart, Is.Not.Null, "A shot must create a physical dart before pause.");

            _controller.PauseRound();
            Assert.That(_controller.Model.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            AssertUnlockedCursor();
            yield return null;
            yield return null;

            var elapsedAtPause = _controller.Model.ElapsedPlaySeconds;
            var targetAtPause = target.transform.position;
            var dartAtPause = dart.transform.position;
            _controller.Model.Tick(10f);
            QueueMouseDelta(new Vector2(-150f, 90f), false);
            yield return new WaitForSecondsRealtime(0.15f);

            Assert.That(_controller.Model.ElapsedPlaySeconds, Is.EqualTo(elapsedAtPause).Within(0.0001f));
            Assert.That(framing.LookAngles, Is.EqualTo(lookAtPause),
                "Relative mouse motion should not turn the camera while the pause menu owns the cursor.");
            Assert.That(Vector3.Distance(target.transform.position, targetAtPause), Is.LessThan(0.001f),
                "A moving target continued while paused.");
            Assert.That(dart == null, Is.False, "An in-flight dart was destroyed while paused.");
            Assert.That(Vector3.Distance(dart.transform.position, dartAtPause), Is.LessThan(0.001f),
                "An in-flight dart continued while paused.");
            Assert.That(_controller.ActiveProjectileCount, Is.EqualTo(1));

            _controller.ResumeRound();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            AssertLockedCursor();
            QueueMouseDelta(new Vector2(100f, 0f), false);
            yield return null;
            Assert.That(framing.LookAngles, Is.EqualTo(lookAtPause),
                "The first mouse delta after Resume should be consumed to prevent menu movement from snapping the view.");
            QueueMouseDelta(new Vector2(10f, 0f), false);
            yield return null;
            Assert.That(framing.LookAngles.x, Is.EqualTo(lookAtPause.x + 1f).Within(0.02f),
                "Fresh relative mouse movement should turn the camera after the resume guard has passed.");
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(_controller.Model.ElapsedPlaySeconds, Is.GreaterThan(elapsedAtPause + 0.1f),
                "The round clock did not resume.");
            Assert.That(Vector3.Distance(target.transform.position, targetAtPause), Is.GreaterThan(0.001f),
                "Moving targets did not resume after unpausing.");
        }

        [UnityTest]
        public IEnumerator SelectingAnotherModeInTheSameSceneClearsRoundAndOutstandingDarts()
        {
            _controller.SelectMode(GameMode.Static);
            yield return EnterPlaying();
            var target = FindCentralTarget();
            Assert.That(_controller.FireAt(target.BodyCollider.bounds.center), Is.True);

            yield return WaitForTargetToPop(target, 2f);
            Assert.That(target.IsAvailable, Is.False, "The setup shot did not produce a score to clear.");
            Assert.That(_controller.Model.Score, Is.EqualTo(10));

            _controller.Model.Tick(0.31f);
            Assert.That(_controller.FireAt(new Vector3(4.65f, 4.7f, _controller.AimPlaneZ)), Is.True,
                "The second shot should be in flight when mode changes.");
            Assert.That(_controller.ActiveProjectileCount, Is.EqualTo(1));

            var activeScene = SceneManager.GetActiveScene();
            _controller.SelectMode(GameMode.Moving);

            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(activeScene.handle),
                "Changing modes should reset the current scene rather than load a different scene.");
            Assert.That(_controller.Model.Mode, Is.EqualTo(GameMode.Moving));
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Ready));
            Assert.That(_controller.Model.IsPaused, Is.False);
            Assert.That(_controller.Model.Shots, Is.Zero);
            Assert.That(_controller.Model.Pops, Is.Zero);
            Assert.That(_controller.Model.Score, Is.Zero);
            Assert.That(_controller.ActiveProjectileCount, Is.Zero);

            foreach (var balloon in _controller.Balloons)
            {
                Assert.That(balloon.IsAvailable, Is.True, $"Target '{balloon.name}' did not reset for the new mode.");
                Assert.That(balloon.BodyCollider.enabled, Is.True,
                    $"Target '{balloon.name}' collider did not reset for the new mode.");
                Assert.That(Vector3.Distance(balloon.transform.position, balloon.PositionAt(GameMode.Moving, 0f)),
                    Is.LessThan(0.03f), $"Target '{balloon.name}' did not reset to its moving-mode start position.");
            }
        }

        [UnityTest]
        public IEnumerator LastShotCanResolveDuringTheRoundEndGracePeriod()
        {
            _controller.SelectMode(GameMode.Static);
            yield return EnterPlaying();

            _controller.Model.Tick(59.5f);
            var target = FindCentralTarget();
            Assert.That(_controller.FireAt(target.BodyCollider.bounds.center), Is.True,
                "The last in-range shot should launch before round time expires.");
            _controller.Model.Tick(_controller.Model.SecondsRemaining + 0.01f);
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Resolving),
                "The test must let the round clock reach its resolution phase with a dart in flight.");

            var resolutionDeadline = Time.time + 0.4f;
            while (_controller.ActiveProjectileCount > 0 &&
                   _controller.Model.Phase == RoundPhase.Resolving && Time.time < resolutionDeadline)
                yield return new WaitForFixedUpdate();

            Assert.That(_controller.Model.Pops, Is.EqualTo(1),
                "A dart already in flight at round end should still score during the resolution grace period.");
            Assert.That(_controller.Model.Score, Is.EqualTo(10));
        }

        [UnityTest]
        public IEnumerator CenterUIBlocksLockedFireAndPauseMenuButtonsWorkWithUnlockedCursor()
        {
            _controller.SelectMode(GameMode.Static);
            yield return EnterPlaying();
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            Assert.That(eventSystem, Is.Not.Null);
            AssertLockedCursor();

            var pauseButton = FindButton("PAUSE");
            Assert.That(pauseButton, Is.Not.Null, "The playing HUD must expose a pause button.");
            var canvas = pauseButton.GetComponentInParent<Canvas>();
            Assert.That(canvas, Is.Not.Null, "The pause button must belong to the authored HUD canvas.");
            var buttonRect = pauseButton.GetComponent<RectTransform>();
            var screenPoint = _controller.GameplayCamera.pixelRect.center;

            var parentRect = buttonRect.parent as RectTransform;
            Assert.That(parentRect, Is.Not.Null, "The pause button must have a RectTransform parent.");
            _relocatedPauseButton = buttonRect;
            _pauseButtonOriginalAnchorMin = buttonRect.anchorMin;
            _pauseButtonOriginalAnchorMax = buttonRect.anchorMax;
            _pauseButtonOriginalAnchoredPosition = buttonRect.anchoredPosition;
            Assert.That(RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentRect,
                    screenPoint,
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                    out var localPoint),
                Is.True,
                "The valid aim point could not be mapped into the pause button's parent canvas.");
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = localPoint - parentRect.rect.min -
                Vector2.Scale(parentRect.rect.size, buttonRect.anchorMin);
            Canvas.ForceUpdateCanvases();
            yield return null;

            var pointerData = new PointerEventData(eventSystem) { position = screenPoint };
            var raycastResults = new List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, raycastResults);
            var pauseButtonIsUnderAimPoint = false;
            foreach (var result in raycastResults)
            {
                if (result.gameObject == pauseButton.gameObject)
                {
                    pauseButtonIsUnderAimPoint = true;
                    break;
                }
            }
            Assert.That(pauseButtonIsUnderAimPoint, Is.True,
                "The temporarily relocated pause button must cover the centered crosshair for this UI-blocking check.");

            AddTestMouse();
            yield return WarmLockedMouse();
            QueueMouseState(screenPoint, true);
            yield return null;
            Assert.That(_testMouse.leftButton.isPressed, Is.True, "The queued left click was not delivered.");
            Assert.That(_controller.Model.Shots, Is.Zero,
                "An active UI graphic under the centered crosshair must block the fire action.");
            QueueMouseState(screenPoint, false);
            yield return null;
            yield return null;

            RestorePauseButtonPosition();
            _controller.PauseRound();
            Assert.That(_controller.Model.IsPaused, Is.True);
            AssertUnlockedCursor();
            var resumeButton = FindButton("RESUME");
            Assert.That(resumeButton, Is.Not.Null, "The unlocked pause menu must expose its Resume button.");
            yield return ClickButton(resumeButton);
            Assert.That(_controller.Model.IsPaused, Is.False,
                "Clicking Resume while the pause cursor is unlocked should return to the round.");
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Playing));
            AssertLockedCursor();
            Assert.That(_controller.Model.Shots, Is.Zero,
                "The blocked crosshair click must not count as a dart after the pause menu closes.");
        }

        private IEnumerator EnterPlaying()
        {
            _controller.StartRound();
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Countdown));
            _controller.Model.Tick(_controller.Model.CountdownRemaining + 0.05f);
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(_controller.Model.Phase, Is.EqualTo(RoundPhase.Playing));
        }

        private KnockdownBalloon FindCentralTarget()
        {
            KnockdownBalloon best = null;
            var bestDistance = float.PositiveInfinity;
            foreach (var balloon in _controller.Balloons)
            {
                var point = balloon.BodyCollider.bounds.center;
                var screenPoint = _controller.GameplayCamera.WorldToScreenPoint(point);
                if (!_controller.TryGetAim(screenPoint, out _))
                    continue;
                var distance = Mathf.Abs(point.x) + Mathf.Abs(point.y - 2.75f);
                if (distance < bestDistance)
                {
                    best = balloon;
                    bestDistance = distance;
                }
            }

            Assert.That(best, Is.Not.Null, "No target center is visible inside the board's valid aim region.");
            return best;
        }

        private void AssertVisibleTargetsDoNotOverlap(float seconds)
        {
            for (var i = 0; i < _controller.Balloons.Count; i++)
            {
                var leftTarget = _controller.Balloons[i];
                if (leftTarget.IsHiddenAt(GameMode.Moving, seconds) || !leftTarget.BodyCollider.enabled)
                    continue;

                for (var j = i + 1; j < _controller.Balloons.Count; j++)
                {
                    var rightTarget = _controller.Balloons[j];
                    if (rightTarget.IsHiddenAt(GameMode.Moving, seconds) || !rightTarget.BodyCollider.enabled)
                        continue;

                    var left = leftTarget.BodyCollider;
                    var right = rightTarget.BodyCollider;
                    Assert.That(left.bounds.Intersects(right.bounds), Is.False,
                        $"Moving target colliders '{left.name}' and '{right.name}' overlap.");
                }
            }
        }

        private static Vector3 GetPhysicsPosition(KnockdownBalloon balloon) =>
            balloon.GetComponent<Rigidbody>().position;

        private float[] GetLaneRowHeights()
        {
            var heights = new List<float>();
            foreach (var balloon in _controller.Balloons)
            {
                var y = balloon.SlotPosition.y;
                var alreadyFound = false;
                foreach (var height in heights)
                {
                    if (Mathf.Abs(height - y) < 0.01f)
                    {
                        alreadyFound = true;
                        break;
                    }
                }
                if (!alreadyFound)
                    heights.Add(y);
            }
            heights.Sort();
            return heights.ToArray();
        }

        private static int FindRowIndex(float y, float[] rowHeights)
        {
            for (var row = 0; row < rowHeights.Length; row++)
            {
                if (Mathf.Abs(y - rowHeights[row]) < 0.01f)
                    return row;
            }

            Assert.Fail($"No configured lane row matches y={y}.");
            return -1;
        }

        private KnockdownBalloon FindTargetWithDirection(int direction)
        {
            foreach (var balloon in _controller.Balloons)
            {
                if (balloon.LaneDirection == direction)
                    return balloon;
            }

            Assert.Fail($"No target uses lane direction {direction}.");
            return null;
        }

        private KnockdownBalloon FindAvailableTargetWithDirection(int direction, KnockdownBalloon excluded)
        {
            foreach (var balloon in _controller.Balloons)
            {
                if (balloon != excluded && balloon.IsAvailable && balloon.LaneDirection == direction)
                    return balloon;
            }

            Assert.Fail($"No available target uses lane direction {direction}.");
            return null;
        }

        private static float FindTimeAtX(KnockdownBalloon balloon, float targetX, float startTime, float searchSeconds)
        {
            const float sampleSeconds = 0.01f;
            var endTime = startTime + searchSeconds;
            for (var time = startTime; time <= endTime; time += sampleSeconds)
            {
                var startX = balloon.PositionAt(GameMode.Moving, time).x;
                var nextX = balloon.PositionAt(GameMode.Moving, time + sampleSeconds).x;
                if (Mathf.Abs(startX - targetX) <= balloon.HorizontalSpeed * sampleSeconds + 0.002f &&
                    Mathf.Sign(nextX - startX) == balloon.LaneDirection)
                    return time;
            }

            Assert.Fail($"Target '{balloon.name}' did not pass x={targetX} in the requested direction.");
            return startTime;
        }

        private static float FindNextWrapTime(KnockdownBalloon balloon, float startTime, float searchSeconds)
        {
            const float sampleSeconds = 0.01f;
            var laneLength = balloon.LaneMaxX - balloon.LaneMinX;
            var previousX = balloon.PositionAt(GameMode.Moving, startTime).x;
            var endTime = startTime + searchSeconds;
            for (var time = startTime + sampleSeconds; time <= endTime; time += sampleSeconds)
            {
                var currentX = balloon.PositionAt(GameMode.Moving, time).x;
                if (Mathf.Abs(currentX - previousX) > laneLength * 0.5f)
                    return time;
                previousX = currentX;
            }

            Assert.Fail($"Target '{balloon.name}' did not wrap within {searchSeconds} seconds.");
            return startTime;
        }

        private IEnumerator AdvancePlayingTime(float targetTime)
        {
            var iterations = 0;
            while (_controller.Model.ElapsedPlaySeconds + 0.002f < targetTime)
            {
                if (++iterations > 200)
                    Assert.Fail("The moving round did not advance to the requested test time.");

                var step = Mathf.Min(0.1f, targetTime - _controller.Model.ElapsedPlaySeconds);
                _controller.Model.Tick(step);
                yield return new WaitForFixedUpdate();
                yield return null;
                Physics.SyncTransforms();
            }
        }

        private static Button FindButton(string label)
        {
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var button in buttons)
            {
                if (button == null || !button.isActiveAndEnabled || !button.interactable)
                    continue;
                var text = button.GetComponentInChildren<TMP_Text>(true);
                if ((button.name.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (text != null && text.text.IndexOf(label, StringComparison.OrdinalIgnoreCase) >= 0))
                    return button;
            }
            return null;
        }

        private static RectTransform FindReticle()
        {
            var rects = UnityEngine.Object.FindObjectsByType<RectTransform>(FindObjectsSortMode.None);
            foreach (var rect in rects)
                if (rect != null && rect.name == "Aiming reticle")
                    return rect;
            return null;
        }

        private void QueueMouseState(Vector2 position, bool pressed)
        {
            var state = new MouseState { position = position };
            if (pressed)
                state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(_testMouse, state);
        }

        private void QueueMouseDelta(Vector2 delta, bool pressed)
        {
            var state = new MouseState
            {
                position = _controller.GameplayCamera.pixelRect.center,
                delta = delta
            };
            if (pressed)
                state = state.WithButton(MouseButton.Left);
            InputSystem.QueueStateEvent(_testMouse, state);
        }

        private IEnumerator WarmLockedMouse()
        {
            QueueMouseState(_controller.GameplayCamera.pixelRect.center, false);
            yield return null;
            yield return null;
        }

        private IEnumerator ClickButton(Button button)
        {
            var rect = button.GetComponent<RectTransform>();
            var canvas = button.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, rect.position);

            QueueMouseState(screenPoint, false);
            yield return null;
            yield return null;
            QueueMouseState(screenPoint, true);
            yield return null;
            QueueMouseState(screenPoint, false);
            yield return null;
            yield return null;
        }

        private void AssertLockedCursor()
        {
            Assert.That(_controller.IsMouseLookActive, Is.True,
                "The controller should request captured mouse-look only during active play.");
            if (Application.isBatchMode)
            {
                TestContext.WriteLine("Batch mode cannot verify native OS cursor capture or visibility.");
                return;
            }

            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.Locked));
            Assert.That(Cursor.visible, Is.False,
                "The native pointer should be hidden while the centered crosshair is active.");
        }

        private void AssertUnlockedCursor()
        {
            Assert.That(_controller.IsMouseLookActive, Is.False,
                "The controller should release mouse-look outside active unpaused play.");
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(Cursor.visible, Is.True);
        }

        private Mouse AddTestMouse()
        {
            if (!_inputSettingsSaved)
            {
                _originalBackgroundBehavior = InputSystem.settings.backgroundBehavior;
                _originalEditorInputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
                _inputSettingsSaved = true;
            }

            // Batch-mode PlayMode tests may have no focused Game view. Keep the synthetic
            // pointer routed to the game so the real controller and UI input path can run.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _testMouse = InputSystem.AddDevice<Mouse>();
            return _testMouse;
        }

        private void RestoreTestInputSettings()
        {
            if (!_inputSettingsSaved)
                return;

            InputSystem.settings.backgroundBehavior = _originalBackgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = _originalEditorInputBehavior;
            _inputSettingsSaved = false;
        }

        private IEnumerator WaitForTargetToPop(KnockdownBalloon target, float timeoutSeconds)
        {
            var deadline = Time.time + timeoutSeconds;
            while (target.IsAvailable && Time.time < deadline)
                yield return new WaitForFixedUpdate();
        }

        private void RestorePauseButtonPosition()
        {
            if (_relocatedPauseButton == null)
                return;

            _relocatedPauseButton.anchorMin = _pauseButtonOriginalAnchorMin;
            _relocatedPauseButton.anchorMax = _pauseButtonOriginalAnchorMax;
            _relocatedPauseButton.anchoredPosition = _pauseButtonOriginalAnchoredPosition;
            _relocatedPauseButton = null;
            Canvas.ForceUpdateCanvases();
        }
    }
}
