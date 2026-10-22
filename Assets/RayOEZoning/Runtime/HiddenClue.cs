using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning
{
    public sealed class HiddenClue : MonoBehaviour
    {
        public int clueIndex;
        public Transform anchor;
        public Renderer[] hiddenRenderers;
        public LayerMask obstructionMask;

        public bool IsVisible { get; private set; }
        public bool IsInSight { get; private set; }
        public float ReadProgress { get; private set; }

        private void Awake()
        {
            SetRenderersVisible(false);
        }

        public bool Evaluate(Camera camera, bool torchOn, float range, float coneDegrees, float dt, Transform lightOrigin = null)
        {
            Transform targetTransform = anchor != null ? anchor : transform;
            bool visible = false;

            if (camera != null && torchOn && range > 0f)
            {
                Transform emitter = lightOrigin ? lightOrigin : camera.transform;
                Vector3 fromCamera = targetTransform.position - emitter.position;
                float distance = fromCamera.magnitude;
                if (distance > 0.001f && distance <= range)
                {
                    float halfAngle = Mathf.Clamp(coneDegrees, 0f, 179.9f) * 0.5f;
                    float minimumDot = Mathf.Cos(halfAngle * Mathf.Deg2Rad);
                    if (Vector3.Dot(emitter.forward, fromCamera / distance) >= minimumDot &&
                        HasClearView(emitter.position, fromCamera / distance, distance))
                    {
                        visible = true;
                    }
                }
            }

            SetVisible(visible);
            IsInSight = false;
            float readingDistance = float.PositiveInfinity;
            if (visible)
            {
                Vector3 sight = targetTransform.position - camera.transform.position;
                readingDistance = sight.magnitude;
                Vector3 viewport = camera.WorldToViewportPoint(targetTransform.position);
                IsInSight = viewport.z > 0 && viewport.x >= 0 && viewport.x <= 1 &&
                    viewport.y >= 0 && viewport.y <= 1 && (readingDistance <= .001f ||
                    HasClearView(camera.transform.position, sight / readingDistance, readingDistance));
            }
            bool canRead = IsInSight && readingDistance <= 3f;
            if (!canRead)
            {
                ResetReading();
                return false;
            }

            float previous = ReadProgress;
            ReadProgress = Mathf.Min(0.8f, ReadProgress + Mathf.Max(0f, dt));
            return previous < 0.8f && ReadProgress >= 0.8f;
        }

        public void ResetReading()
        {
            ReadProgress = 0f;
        }

        private bool HasClearView(Vector3 origin, Vector3 direction, float distance)
        {
            return !Physics.Raycast(origin, direction, distance, obstructionMask,
                QueryTriggerInteraction.Ignore);
        }

        private void SetVisible(bool visible)
        {
            IsVisible = visible;
            SetRenderersVisible(visible);
        }

        private void SetRenderersVisible(bool visible)
        {
            if (hiddenRenderers == null)
                return;

            for (int i = 0; i < hiddenRenderers.Length; i++)
            {
                Renderer clueRenderer = hiddenRenderers[i];
                if (clueRenderer == null)
                    continue;

                clueRenderer.shadowCastingMode = ShadowCastingMode.Off;
                clueRenderer.receiveShadows = false;
                clueRenderer.enabled = visible;
            }
        }
    }
}
