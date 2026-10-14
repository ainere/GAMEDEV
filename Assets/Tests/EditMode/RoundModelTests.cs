using NUnit.Framework;

namespace Knockdown.Tests
{
    public sealed class RoundModelTests
    {
        [Test]
        public void StartsReadyAndRunsCountdownThenRoundAndResults()
        {
            var model = new RoundModel(new RoundSettings
            {
                CountdownSeconds = 1f,
                RoundSeconds = 2f,
                ResolutionSeconds = 0.5f
            });

            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Ready));
            Assert.That(model.SecondsRemaining, Is.EqualTo(2f));
            model.StartRound();
            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Countdown));
            model.Tick(1f);
            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Playing));
            model.Tick(2f);
            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Resolving));
            Assert.That(model.ElapsedPlaySeconds, Is.EqualTo(2f));
            Assert.That(model.SecondsRemaining, Is.Zero);
            model.Tick(0.5f);
            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Results));
        }

        [Test]
        public void TickCarriesOvershootAcrossCountdownRoundAndGrace()
        {
            var model = new RoundModel(new RoundSettings
            {
                CountdownSeconds = 0.5f,
                RoundSeconds = 1f,
                ResolutionSeconds = 0.25f
            });
            model.StartRound();

            model.Tick(1.75f);

            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Results));
            Assert.That(model.ElapsedPlaySeconds, Is.EqualTo(1f));
        }

        [Test]
        public void ResolvingAcceptsEarlierShotsButStopsNewShotsAndRejectsAfterGrace()
        {
            var model = PlayingModel(roundSeconds: 1f, resolutionSeconds: 0.5f);
            Assert.That(model.TryFire(out var shotId), Is.True);
            model.Tick(1f);

            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Resolving));
            Assert.That(model.TryFire(out _), Is.False);
            Assert.That(model.ResolveShot(shotId, true), Is.True);
            model.Tick(0.5f);

            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Results));
            Assert.That(model.ResolveShot(shotId, true), Is.False);
            Assert.That(model.Score, Is.EqualTo(10));
        }

        [Test]
        public void ShotIdsStayUniqueAcrossResetsAndOldShotsCannotScore()
        {
            var model = PlayingModel(roundSeconds: 5f);
            Assert.That(model.TryFire(out var firstId), Is.True);
            model.RestartRound();
            model.Tick(3f);
            Assert.That(model.TryFire(out var secondId), Is.True);

            Assert.That(secondId, Is.GreaterThan(firstId));
            Assert.That(model.ResolveShot(firstId, true), Is.False);
            Assert.That(model.Score, Is.Zero);

            model.SelectMode(GameMode.Moving);
            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Ready));
            Assert.That(model.Shots, Is.Zero);
            Assert.That(model.ResolveShot(secondId, true), Is.False);
            model.StartRound();
            model.Tick(3f);
            Assert.That(model.TryFire(out var thirdId), Is.True);
            Assert.That(thirdId, Is.GreaterThan(secondId));
        }

        [Test]
        public void DuplicateAndMissedShotResolutionsDoNotInflateScore()
        {
            var model = PlayingModel(roundSeconds: 5f);
            Assert.That(model.TryFire(out var hit), Is.True);
            model.Tick(0.3f);
            Assert.That(model.TryFire(out var miss), Is.True);

            Assert.That(model.ResolveShot(hit, true), Is.True);
            Assert.That(model.ResolveShot(hit, true), Is.False);
            Assert.That(model.ResolveShot(miss, false), Is.True);
            Assert.That(model.ResolveShot(miss, true), Is.False);
            Assert.That(model.Score, Is.EqualTo(10));
            Assert.That(model.Pops, Is.EqualTo(1));
            Assert.That(model.Shots, Is.EqualTo(2));
            Assert.That(model.Accuracy, Is.EqualTo(0.5f));
        }

        [Test]
        public void FireCadenceAllowsImmediateFirstShotAndDoesNotCatchUpBursts()
        {
            var model = PlayingModel(roundSeconds: 5f, shotInterval: 0.3f);
            Assert.That(model.TryFire(out _), Is.True);
            Assert.That(model.TryFire(out _), Is.False);

            model.Tick(0.29f);
            Assert.That(model.TryFire(out _), Is.False);
            model.Tick(0.02f);
            Assert.That(model.TryFire(out _), Is.True);
            Assert.That(model.TryFire(out _), Is.False);

            model.Tick(1f);
            Assert.That(model.TryFire(out _), Is.True);
            Assert.That(model.TryFire(out _), Is.False);
        }

        [TestCase(RoundPhase.Countdown)]
        [TestCase(RoundPhase.Playing)]
        [TestCase(RoundPhase.Resolving)]
        public void PauseFreezesRoundClockAndResolution(RoundPhase phase)
        {
            var model = new RoundModel(new RoundSettings
            {
                CountdownSeconds = 1f,
                RoundSeconds = 2f,
                ResolutionSeconds = 1f
            });
            model.StartRound();
            if (phase == RoundPhase.Playing)
                model.Tick(1f);
            else if (phase == RoundPhase.Resolving)
                model.Tick(3f);

            var beforeElapsed = model.ElapsedPlaySeconds;
            var beforeCountdown = model.CountdownRemaining;
            model.Pause();
            model.Tick(10f);

            Assert.That(model.IsPaused, Is.True);
            Assert.That(model.Phase, Is.EqualTo(phase));
            Assert.That(model.ElapsedPlaySeconds, Is.EqualTo(beforeElapsed));
            Assert.That(model.CountdownRemaining, Is.EqualTo(beforeCountdown));

            model.Resume();
            Assert.That(model.IsPaused, Is.False);
            if (phase == RoundPhase.Countdown)
            {
                model.Tick(0.1f);
                Assert.That(model.CountdownRemaining, Is.EqualTo(beforeCountdown - 0.1f).Within(0.0001f));
            }
            else if (phase == RoundPhase.Playing)
            {
                model.Tick(0.1f);
                Assert.That(model.ElapsedPlaySeconds, Is.EqualTo(beforeElapsed + 0.1f).Within(0.0001f));
            }
            else
            {
                model.Tick(1f);
                Assert.That(model.Phase, Is.EqualTo(RoundPhase.Results));
            }
        }

        [Test]
        public void ModeSelectionAlwaysResetsAndReadyResultsCannotBePaused()
        {
            var model = PlayingModel(roundSeconds: 5f);
            Assert.That(model.TryFire(out var shotId), Is.True);
            Assert.That(model.ResolveShot(shotId, true), Is.True);
            Assert.That(model.Score, Is.EqualTo(10));
            model.SelectMode(GameMode.Moving);

            Assert.That(model.Mode, Is.EqualTo(GameMode.Moving));
            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Ready));
            Assert.That(model.Shots, Is.Zero);
            Assert.That(model.Score, Is.Zero);
            Assert.That(model.Pops, Is.Zero);
            Assert.That(model.ElapsedPlaySeconds, Is.Zero);
            model.Pause();
            Assert.That(model.IsPaused, Is.False);

            model.StartRound();
            model.Tick(63.5f);
            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Results));
            model.Pause();
            Assert.That(model.IsPaused, Is.False);
        }

        [Test]
        public void InvalidSettingsFallBackToDefaultsAndInvalidTicksAreIgnored()
        {
            var model = new RoundModel(new RoundSettings
            {
                CountdownSeconds = float.NaN,
                RoundSeconds = 0f,
                ResolutionSeconds = float.PositiveInfinity,
                ShotInterval = -1f,
                PointsPerPop = -1
            });

            Assert.That(model.SecondsRemaining, Is.EqualTo(60f));
            model.StartRound();
            model.Tick(float.NaN);
            model.Tick(float.PositiveInfinity);
            model.Tick(-1f);
            Assert.That(model.CountdownRemaining, Is.EqualTo(3f));
            model.Tick(3f);
            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Playing));
            Assert.That(model.TryFire(out var shotId), Is.True);
            Assert.That(model.ResolveShot(shotId, true), Is.True);
            Assert.That(model.Score, Is.EqualTo(10));
        }

        [Test]
        public void ChangedFiresWhenPublicStateChanges()
        {
            var model = new RoundModel();
            var changes = 0;
            model.Changed += () => changes++;

            model.StartRound();
            model.Pause();
            model.Tick(1f);
            model.Resume();
            model.Tick(3f);

            Assert.That(changes, Is.EqualTo(4));
        }

        private static RoundModel PlayingModel(float roundSeconds, float resolutionSeconds = 0.5f, float shotInterval = 0.3f)
        {
            var model = new RoundModel(new RoundSettings
            {
                CountdownSeconds = 0f,
                RoundSeconds = roundSeconds,
                ResolutionSeconds = resolutionSeconds,
                ShotInterval = shotInterval
            });
            model.StartRound();
            model.Tick(0f);
            Assert.That(model.Phase, Is.EqualTo(RoundPhase.Playing));
            return model;
        }
    }
}
