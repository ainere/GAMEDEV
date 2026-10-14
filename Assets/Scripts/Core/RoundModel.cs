using System;
using System.Collections.Generic;

namespace Knockdown
{
    public enum GameMode
    {
        Static,
        Moving
    }

    public enum RoundPhase
    {
        Ready,
        Countdown,
        Playing,
        Resolving,
        Results
    }

    [Serializable]
    public sealed class RoundSettings
    {
        public float CountdownSeconds = 3f;
        public float RoundSeconds = 60f;
        public float ResolutionSeconds = 0.5f;
        public float ShotInterval = 0.30f;
        public int PointsPerPop = 10;
    }

    /// <summary>
    /// Owns round timing and score state without depending on Unity APIs.
    /// </summary>
    public sealed class RoundModel
    {
        private const float DefaultCountdownSeconds = 3f;
        private const float DefaultRoundSeconds = 60f;
        private const float DefaultResolutionSeconds = 0.5f;
        private const float DefaultShotInterval = 0.30f;
        private const int DefaultPointsPerPop = 10;

        private readonly float _countdownSeconds;
        private readonly float _roundSeconds;
        private readonly float _resolutionSeconds;
        private readonly float _shotInterval;
        private readonly int _pointsPerPop;
        private readonly HashSet<int> _outstandingShots = new HashSet<int>();

        private float _countdownRemaining;
        private float _elapsedPlaySeconds;
        private float _elapsedResolutionSeconds;
        private float _nextShotTime;
        private bool _hasFiredThisRound;
        private long _lastShotId;

        public RoundModel(RoundSettings settings = null)
        {
            settings = settings ?? new RoundSettings();
            _countdownSeconds = ValidNonNegative(settings.CountdownSeconds, DefaultCountdownSeconds);
            _roundSeconds = ValidPositive(settings.RoundSeconds, DefaultRoundSeconds);
            _resolutionSeconds = ValidNonNegative(settings.ResolutionSeconds, DefaultResolutionSeconds);
            _shotInterval = ValidPositive(settings.ShotInterval, DefaultShotInterval);
            _pointsPerPop = settings.PointsPerPop >= 0 ? settings.PointsPerPop : DefaultPointsPerPop;

            SecondsRemaining = _roundSeconds;
        }

        public GameMode Mode { get; private set; } = GameMode.Static;
        public RoundPhase Phase { get; private set; } = RoundPhase.Ready;
        public bool IsPaused { get; private set; }
        public float SecondsRemaining { get; private set; }
        public float CountdownRemaining => Phase == RoundPhase.Countdown ? _countdownRemaining : 0f;
        public float ElapsedPlaySeconds => _elapsedPlaySeconds;
        public int Shots { get; private set; }
        public int Pops { get; private set; }
        public int Score { get; private set; }
        public float Accuracy => Shots == 0 ? 0f : (float)Pops / Shots;

        public event Action Changed;

        /// <summary>Starts a round from Ready. Use RestartRound to reset an active/results round.</summary>
        public void StartRound()
        {
            if (Phase != RoundPhase.Ready)
                return;

            BeginRound();
        }

        /// <summary>Clears this round's totals and begins a fresh countdown.</summary>
        public void RestartRound()
        {
            BeginRound();
        }

        /// <summary>Changes mode and resets all round state, even if the mode is unchanged.</summary>
        public void SelectMode(GameMode mode)
        {
            Mode = mode == GameMode.Moving ? GameMode.Moving : GameMode.Static;
            ResetRoundState(RoundPhase.Ready);
            IsPaused = false;
            NotifyChanged();
        }

        public void Pause()
        {
            if (IsPaused || (Phase != RoundPhase.Countdown && Phase != RoundPhase.Playing && Phase != RoundPhase.Resolving))
                return;

            IsPaused = true;
            NotifyChanged();
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            IsPaused = false;
            NotifyChanged();
        }

        /// <summary>
        /// Advances round time using scaled seconds. Any overshoot is carried across phase boundaries.
        /// </summary>
        public void Tick(float scaledDeltaTime)
        {
            if (IsPaused || (Phase != RoundPhase.Countdown && Phase != RoundPhase.Playing && Phase != RoundPhase.Resolving))
                return;
            if (float.IsNaN(scaledDeltaTime) || float.IsInfinity(scaledDeltaTime) || scaledDeltaTime < 0f)
                return;

            var delta = scaledDeltaTime;
            var changed = false;

            // Each successful transition moves forward, so even a zero-length countdown/grace cannot loop forever.
            while (Phase == RoundPhase.Countdown || Phase == RoundPhase.Playing || Phase == RoundPhase.Resolving)
            {
                if (Phase == RoundPhase.Countdown)
                {
                    if (_countdownRemaining > delta)
                    {
                        _countdownRemaining -= delta;
                        changed |= delta > 0f;
                        break;
                    }

                    delta -= _countdownRemaining;
                    _countdownRemaining = 0f;
                    _elapsedPlaySeconds = 0f;
                    SecondsRemaining = _roundSeconds;
                    Phase = RoundPhase.Playing;
                    changed = true;
                    continue;
                }

                if (Phase == RoundPhase.Playing)
                {
                    var remaining = _roundSeconds - _elapsedPlaySeconds;
                    if (remaining > delta)
                    {
                        _elapsedPlaySeconds += delta;
                        SecondsRemaining = Math.Max(0f, _roundSeconds - _elapsedPlaySeconds);
                        changed |= delta > 0f;
                        break;
                    }

                    delta -= remaining;
                    _elapsedPlaySeconds = _roundSeconds;
                    SecondsRemaining = 0f;
                    _elapsedResolutionSeconds = 0f;
                    Phase = RoundPhase.Resolving;
                    changed = true;
                    continue;
                }

                var resolutionRemaining = _resolutionSeconds - _elapsedResolutionSeconds;
                if (resolutionRemaining > delta)
                {
                    _elapsedResolutionSeconds += delta;
                    changed |= delta > 0f;
                    break;
                }

                _elapsedResolutionSeconds = _resolutionSeconds;
                Phase = RoundPhase.Results;
                _outstandingShots.Clear();
                changed = true;
            }

            if (changed)
                NotifyChanged();
        }

        /// <summary>Records one shot when the round is accepting fire and its cadence has elapsed.</summary>
        public bool TryFire(out int shotId)
        {
            shotId = 0;
            if (IsPaused || Phase != RoundPhase.Playing || _elapsedPlaySeconds >= _roundSeconds)
                return false;
            if (_hasFiredThisRound && _elapsedPlaySeconds < _nextShotTime)
                return false;
            if (_lastShotId >= int.MaxValue)
                return false;

            shotId = (int)++_lastShotId;
            Shots++;
            _outstandingShots.Add(shotId);
            _hasFiredThisRound = true;
            // Schedule from the current time so a delayed frame never creates catch-up bursts.
            _nextShotTime = _elapsedPlaySeconds + _shotInterval;
            NotifyChanged();
            return true;
        }

        /// <summary>
        /// Resolves a previously fired shot once. Only a popped target adds score.
        /// </summary>
        public bool ResolveShot(int shotId, bool popped)
        {
            if (IsPaused || (Phase != RoundPhase.Playing && Phase != RoundPhase.Resolving))
                return false;
            if (!_outstandingShots.Remove(shotId))
                return false;

            if (popped)
            {
                Pops++;
                Score = Score > int.MaxValue - _pointsPerPop
                    ? int.MaxValue
                    : Score + _pointsPerPop;
            }

            NotifyChanged();
            return true;
        }

        private void BeginRound()
        {
            ResetRoundState(RoundPhase.Countdown);
            _countdownRemaining = _countdownSeconds;
            IsPaused = false;
            NotifyChanged();
        }

        private void ResetRoundState(RoundPhase phase)
        {
            _countdownRemaining = phase == RoundPhase.Countdown ? _countdownSeconds : 0f;
            _elapsedPlaySeconds = 0f;
            _elapsedResolutionSeconds = 0f;
            _nextShotTime = 0f;
            _hasFiredThisRound = false;
            SecondsRemaining = _roundSeconds;
            Shots = 0;
            Pops = 0;
            Score = 0;
            _outstandingShots.Clear();
            Phase = phase;
        }

        private void NotifyChanged()
        {
            Changed?.Invoke();
        }

        private static float ValidNonNegative(float value, float fallback)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f ? value : fallback;
        }

        private static float ValidPositive(float value, float fallback)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f ? value : fallback;
        }
    }
}
