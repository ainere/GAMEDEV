using System.Collections.Generic;
using UnityEngine;

namespace Knockdown
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class KnockdownTarget : MonoBehaviour
    {
        private enum Pose { Standing, Falling, Down, Rising }

        [SerializeField] private Vector3 slotPosition;
        [SerializeField] private Transform hinge;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Collider[] faceColliders;
        [SerializeField, Min(0.01f)] private float cutoutHalfWidth = 0.55f;
        [SerializeField, Min(0.01f)] private float cutoutHeight = 1.08f;
        [Header("Conveyor")]
        [SerializeField] private float laneMinX = -5.6f;
        [SerializeField] private float laneMaxX = 5.6f;
        [SerializeField, Range(0, 3)] private int laneColumn;
        [SerializeField] private int laneDirection = 1;
        [SerializeField, Min(0.1f)] private float horizontalSpeed = 1.4f;
        [SerializeField, Min(0f), Tooltip("Inner edge of the opaque side covers.")]
        private float collisionHalfWidth = 4.4f;
        [SerializeField, Min(0f)] private float concealmentMargin = 0.025f;
        [Header("Hinge")]
        [SerializeField, Range(80f, 90f)] private float downAngle = 88f;
        [SerializeField, Min(0.01f)] private float foldSeconds = 0.16f;
        [SerializeField, Min(0f)] private float respawnDelay = 0.75f;
        [SerializeField, Min(0.01f)] private float raiseSeconds = 0.24f;
        private Pose _pose;
        private float _poseSeconds;
        private float _foldAngle;
        private GameMode _lastMode;
        private int _lastLaneCycle;
        private float _lastMoveSeconds;
        private bool _teleportPending;
        private RigidbodyInterpolation _restoreInterpolation;

        public bool IsAvailable => _pose == Pose.Standing;
        public bool IsDown => _pose != Pose.Standing;
        public Vector3 SlotPosition => slotPosition;
        public Transform Hinge => hinge;
        public float FoldAngle => _foldAngle;
        public Vector3 FaceCenter => hinge.TransformPoint(new Vector3(0f, cutoutHeight * 0.5f, -0.025f));
        public Collider BodyCollider => faceColliders != null && faceColliders.Length > 0 ? faceColliders[0] : null;
        public IReadOnlyList<Collider> FaceColliders => faceColliders;
        public float LaneMinX => laneMinX;
        public float LaneMaxX => laneMaxX;
        public float HorizontalSpeed => horizontalSpeed;
        public int LaneDirection => laneDirection < 0 ? -1 : 1;
        public float MovingCycleSeconds => LaneLength / Mathf.Max(0.1f, horizontalSpeed);
        public float CollisionHalfWidth => collisionHalfWidth;
        public float CutoutHalfWidth => cutoutHalfWidth;
        public float FullyConcealedX => collisionHalfWidth + cutoutHalfWidth + concealmentMargin;
        private float LaneLength => Mathf.Max(0.1f, laneMaxX - laneMinX);
        private float LaneStartOffset => (Mathf.Clamp(laneColumn, 0, 3) + 0.5f) * LaneLength / 4f;

        public void Initialize(KnockdownGameplayController controller) { }

        public void Configure(Vector3 slot, Transform hingeRoot, Rigidbody rigidbody,
            Collider[] colliders, float halfWidth, float height = 1.08f)
        {
            slotPosition = slot;
            hinge = hingeRoot;
            body = rigidbody;
            faceColliders = colliders;
            cutoutHalfWidth = Mathf.Max(0.27f, halfWidth);
            cutoutHeight = height;
        }

        public void ConfigureLane(int row, int column)
        {
            laneColumn = Mathf.Clamp(column, 0, 3);
            laneDirection = row % 2 == 0 ? 1 : -1;
            laneMinX = -5.6f;
            laneMaxX = 5.6f;
            horizontalSpeed = 1.4f;
            collisionHalfWidth = 4.4f;
        }

        public Vector3 PositionAt(GameMode mode, float seconds)
        {
            if (mode == GameMode.Static) return slotPosition;
            var x = laneMinX + Mathf.Repeat(LaneStartOffset + LaneDirection * horizontalSpeed * seconds, LaneLength);
            return new Vector3(x, slotPosition.y, slotPosition.z);
        }

        public bool IsHiddenAt(GameMode mode, float seconds) =>
            mode == GameMode.Moving && IsFullyConcealed(PositionAt(mode, seconds));

        private bool IsFullyConcealed(Vector3 position) => Mathf.Abs(position.x) >= FullyConcealedX;

        private int LaneCycleAt(float seconds) =>
            Mathf.FloorToInt((LaneStartOffset + LaneDirection * horizontalSpeed * seconds) / LaneLength);

        private void SetCollisionState()
        {
            var enabled = IsAvailable && !_teleportPending &&
                (_lastMode == GameMode.Static || !IsFullyConcealed(body.position));
            foreach (var collider in faceColliders) if (collider != null) collider.enabled = enabled;
            body.detectCollisions = enabled;
        }

        private void SetFold(float angle)
        {
            _foldAngle = angle;
            // Positive X rotation carries the plate away from the player, onto its tray.
            hinge.localRotation = Quaternion.Euler(angle, 0f, 0f);
        }

        private void Teleport(Vector3 position)
        {
            foreach (var collider in faceColliders) if (collider != null) collider.enabled = false;
            body.detectCollisions = false;
            if (!_teleportPending) _restoreInterpolation = body.interpolation;
            _teleportPending = true;
            body.interpolation = RigidbodyInterpolation.None;
            body.position = position;
            body.rotation = Quaternion.identity;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            body.PublishTransform();
        }

        public void ResetTarget(GameMode mode)
        {
            _pose = Pose.Standing;
            _poseSeconds = 0f;
            SetFold(0f);
            hinge.gameObject.SetActive(true);
            hinge.localScale = Vector3.one;
            _lastMode = mode;
            _lastMoveSeconds = 0f;
            _lastLaneCycle = LaneCycleAt(0f);
            Teleport(PositionAt(mode, 0f));
            SetCollisionState();
        }

        // Called by the controller in FixedUpdate so collision follows the conveyor.
        public void Move(GameMode mode, float seconds)
        {
            var position = PositionAt(mode, seconds);
            var cycle = LaneCycleAt(seconds);
            var wrapping = mode != _lastMode || seconds < _lastMoveSeconds ||
                (mode == GameMode.Moving && cycle != _lastLaneCycle);
            if (wrapping) Teleport(position);
            else
            {
                body.MovePosition(position);
                if (_teleportPending)
                {
                    // Leave one physics step between teleporting and restoring interpolation.
                    _teleportPending = false;
                    body.interpolation = _restoreInterpolation;
                }
            }
            _lastMode = mode;
            _lastLaneCycle = cycle;
            _lastMoveSeconds = seconds;
            if (_pose == Pose.Rising && mode == GameMode.Moving && !IsFullyConcealed(position))
            {
                // A skipped frame must never expose a target partway through its hidden reset.
                _pose = Pose.Down;
                _poseSeconds = 0f;
                SetFold(downAngle);
            }
            SetCollisionState();
        }

        public bool TryKnockDown()
        {
            if (!IsAvailable || !body.detectCollisions) return false;
            _pose = Pose.Falling;
            _poseSeconds = 0f;
            SetCollisionState();
            return true;
        }

        public void TickRespawn(float deltaTime, bool allowStandUp)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            var remaining = deltaTime;
            // Carry frame overshoot into the next pose so timing stays consistent.
            for (var transition = 0; transition < 4 && remaining > 0f; transition++)
            {
                if (_pose == Pose.Standing) break;
                if (_pose == Pose.Down && _lastMode == GameMode.Moving)
                {
                    if (!allowStandUp || !IsFullyConcealed(body.position)) break;
                    _pose = Pose.Rising;
                    _poseSeconds = 0f;
                }
                if (_pose == Pose.Rising && !allowStandUp) break;
                if (_pose == Pose.Rising && _lastMode == GameMode.Moving && !IsFullyConcealed(body.position)) break;

                var duration = _pose == Pose.Falling ? foldSeconds : _pose == Pose.Down ? respawnDelay : raiseSeconds;
                var step = Mathf.Min(remaining, Mathf.Max(0f, duration - _poseSeconds));
                _poseSeconds += step;
                remaining -= step;
                var progress = Mathf.Clamp01(_poseSeconds / Mathf.Max(0.001f, duration));
                if (_pose == Pose.Falling) SetFold(downAngle * (1f - Mathf.Pow(1f - progress, 3f)));
                else if (_pose == Pose.Rising) SetFold(downAngle * (1f - progress * progress * (3f - 2f * progress)));
                if (_poseSeconds < duration) break;
                if (_pose == Pose.Down && !allowStandUp) break;
                _poseSeconds = 0f;
                if (_pose == Pose.Falling) { _pose = Pose.Down; SetFold(downAngle); }
                else if (_pose == Pose.Down) _pose = Pose.Rising;
                else { _pose = Pose.Standing; SetFold(0f); }
            }
            SetCollisionState();
        }
    }
}
