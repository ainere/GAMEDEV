using UnityEngine;

namespace Knockdown
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public sealed class KnockdownCameraFraming : MonoBehaviour
    {
        [SerializeField, Min(0.01f), Tooltip("Degrees of rotation per mouse pixel.")]
        private float mouseSensitivity = 0.10f;
        [SerializeField, Tooltip("Minimum and maximum horizontal look angles in degrees.")]
        private Vector2 yawLimits = new Vector2(-28f, 28f);
        [SerializeField, Tooltip("Minimum and maximum vertical look angles in degrees.")]
        private Vector2 pitchLimits = new Vector2(-16f, 16f);
        private Camera _camera;
        private Quaternion _restRotation;
        private Vector2 _lookAngles;

        public float MouseSensitivity => mouseSensitivity;
        public Vector2 YawLimits => yawLimits;
        public Vector2 PitchLimits => pitchLimits;
        public Vector2 LookAngles => _lookAngles;

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            _restRotation = transform.localRotation;
            _lookAngles = Vector2.zero;
            RefreshFraming();
        }

        public void ApplyMouseLook(Vector2 mouseDelta)
        {
            if (!IsFinite(mouseDelta.x) || !IsFinite(mouseDelta.y)) return;
            _lookAngles.x = Mathf.Clamp(_lookAngles.x + mouseDelta.x * mouseSensitivity, yawLimits.x, yawLimits.y);
            _lookAngles.y = Mathf.Clamp(_lookAngles.y - mouseDelta.y * mouseSensitivity, pitchLimits.x, pitchLimits.y);
            transform.localRotation = _restRotation * Quaternion.Euler(_lookAngles.y, _lookAngles.x, 0f);
            RefreshFraming();
        }

        public void ResetLook()
        {
            _lookAngles = Vector2.zero;
            transform.localRotation = _restRotation;
            RefreshFraming();
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private void LateUpdate() => RefreshFraming();

        private void RefreshFraming()
        {
            if (_camera == null) return;
            // Fit the closest counter edge while retaining the booth's fixed viewpoint.
            const float widthWithMargin = 13.06f;
            const float nearestBoothDistance = 8.35f;
            var horizontalFit = 2f * Mathf.Atan(widthWithMargin / (2f * nearestBoothDistance * Mathf.Max(0.5f, _camera.aspect))) * Mathf.Rad2Deg;
            _camera.fieldOfView = Mathf.Max(45f, horizontalFit);
        }
    }
}
