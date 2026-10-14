using UnityEngine;

namespace Knockdown
{
    public sealed class KnockdownBalloon : MonoBehaviour
    {
        [SerializeField] private Vector3 slotPosition;
        [SerializeField] private float motionPhase;
        [SerializeField] private GameObject visual;
        [SerializeField] private Rigidbody body;
        [SerializeField] private CapsuleCollider bodyCollider;
        [Header("Moving row")]
        [SerializeField] private float laneMinX = -5.6f;
        [SerializeField] private float laneMaxX = 5.6f;
        [SerializeField, Range(0, 3)] private int laneColumn;
        [SerializeField] private int laneDirection = 1;
        [SerializeField, Min(0.1f)] private float horizontalSpeed = 1.4f;
        [SerializeField, Min(0f)] private float collisionHalfWidth = 5.3f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.22f;
        [SerializeField, Min(0.1f)] private float bobPeriod = 2.8f;
        [SerializeField, Min(0f)] private float respawnDelay = 0.75f;
        private bool _available = true;
        private float _respawnRemaining;
        private float _settleRemaining;
        private GameMode _lastMode;
        private int _lastLaneCycle;
        private float _lastMoveSeconds;

        public bool IsAvailable => _available;
        public Vector3 SlotPosition => slotPosition;
        public Collider BodyCollider => bodyCollider;
        public float LaneMinX => laneMinX;
        public float LaneMaxX => laneMaxX;
        public float HorizontalSpeed => horizontalSpeed;
        public int LaneDirection => laneDirection < 0 ? -1 : 1;
        public float MovingCycleSeconds => LaneLength / Mathf.Max(0.1f, horizontalSpeed);
        public float CollisionHalfWidth => collisionHalfWidth;
        private float LaneLength => Mathf.Max(0.1f, laneMaxX - laneMinX);
        private float LaneStartOffset => (Mathf.Clamp(laneColumn, 0, 3) + 0.5f) * LaneLength / 4f;
        public void Initialize(KnockdownGameplayController controller) { }

        public void Configure(Vector3 slot, float phase, GameObject meshRoot, Rigidbody rigidbody, CapsuleCollider collider)
        {
            slotPosition = slot;
            motionPhase = phase;
            visual = meshRoot;
            body = rigidbody;
            bodyCollider = collider;
        }

        public void ConfigureLane(int row, int column)
        {
            laneColumn = Mathf.Clamp(column, 0, 3);
            laneDirection = row % 2 == 0 ? 1 : -1;
            laneMinX = -5.6f;
            laneMaxX = 5.6f;
            horizontalSpeed = 1.4f;
            collisionHalfWidth = 5.3f;
            bobAmplitude = 0.22f;
            bobPeriod = 2.8f;
            // A common bob keeps the three rows separated throughout their crossing paths.
            motionPhase = 0f;
        }

        public Vector3 PositionAt(GameMode mode, float seconds)
        {
            if (mode == GameMode.Static) return slotPosition;
            var x = laneMinX + Mathf.Repeat(LaneStartOffset + LaneDirection * horizontalSpeed * seconds, LaneLength);
            var y = slotPosition.y + Mathf.Sin(seconds * Mathf.PI * 2f / Mathf.Max(0.1f, bobPeriod) + motionPhase) * bobAmplitude;
            return new Vector3(x, y, slotPosition.z);
        }

        public bool IsHiddenAt(GameMode mode, float seconds) =>
            mode == GameMode.Moving && Mathf.Abs(PositionAt(mode, seconds).x) >= collisionHalfWidth;

        private int LaneCycleAt(float seconds) =>
            Mathf.FloorToInt((LaneStartOffset + LaneDirection * horizontalSpeed * seconds) / LaneLength);

        private void SetCollisionState(GameMode mode, Vector3 position)
        {
            var canCollide = _available && (mode == GameMode.Static || Mathf.Abs(position.x) < collisionHalfWidth);
            bodyCollider.enabled = canCollide;
            body.detectCollisions = canCollide;
        }

        private void Teleport(Vector3 position)
        {
            // The seam is hidden by the booth wings. Never sweep a kinematic collider
            // across the board or interpolate through the visible opening when wrapping.
            bodyCollider.enabled = false;
            body.detectCollisions = false;
            var interpolation = body.interpolation;
            body.interpolation = RigidbodyInterpolation.None;
            body.position = position;
            body.rotation = Quaternion.identity;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            body.PublishTransform();
            body.interpolation = interpolation;
        }

        public void ResetTarget(GameMode mode)
        {
            _available = true;
            _respawnRemaining = 0;
            _settleRemaining = 0;
            visual.SetActive(true);
            visual.transform.localScale = Vector3.one;
            var position = PositionAt(mode, 0);
            Teleport(position);
            _lastMode = mode;
            _lastMoveSeconds = 0f;
            _lastLaneCycle = LaneCycleAt(0f);
            SetCollisionState(mode, position);
        }

        public void Move(GameMode mode, float seconds)
        {
            var position = PositionAt(mode, seconds);
            var cycle = LaneCycleAt(seconds);
            if (mode != _lastMode || seconds < _lastMoveSeconds ||
                (mode == GameMode.Moving && cycle != _lastLaneCycle)) Teleport(position);
            else body.MovePosition(position);
            _lastMode = mode;
            _lastLaneCycle = cycle;
            _lastMoveSeconds = seconds;
            SetCollisionState(mode, position);
        }

        public bool TryPop()
        {
            if (!_available || !bodyCollider.enabled) return false;
            _available = false;
            _respawnRemaining = respawnDelay;
            bodyCollider.enabled = false;
            body.detectCollisions = false;
            visual.SetActive(false);
            return true;
        }

        public void TickRespawn(float deltaTime, bool allowRespawn)
        {
            if (!allowRespawn) return;
            if (!_available)
            {
                _respawnRemaining -= deltaTime;
                if (_respawnRemaining > 0) return;
                _available = true;
                visual.SetActive(true);
                SetCollisionState(_lastMode, body.position);
                _settleRemaining = 0.16f;
            }
            if (_settleRemaining > 0)
            {
                _settleRemaining = Mathf.Max(0, _settleRemaining - deltaTime);
                visual.transform.localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(_settleRemaining / 0.16f * Mathf.PI));
            }
            else visual.transform.localScale = Vector3.one;
        }
    }
}
