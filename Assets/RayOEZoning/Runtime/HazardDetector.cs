using UnityEngine;

namespace RayOEZoning
{
    public enum DetectionShape
    {
        Sphere,
        Box,
        Capsule
    }

    public sealed class HazardDetector : MonoBehaviour
    {
        public DetectionShape shape;
        public Vector3 home;
        public float lateralAmplitude = 1.15f;
        public float period = 8f;
        public float phase;
        public float radius = 1.55f;
        public Vector3 halfExtents = new Vector3(1.3f, 1.2f, 2f);
        public Vector3 capsulePointA = Vector3.back * 2f;
        public Vector3 capsulePointB = Vector3.forward * 2f;
        public LayerMask playerMask;
        public LayerMask obstructionMask;
        public Transform player;
        public Transform rayOrigin;

        public bool IsDetecting { get; private set; }
        public bool HasPlayerOverlap { get; private set; }
        public Vector3 LastRayOrigin { get; private set; }
        public Vector3 LastRayTarget { get; private set; }
        public bool LastRayBlocked { get; private set; }
        public float LastSampleTime { get; private set; }

        public void SetPatrolTime(float elapsed)
        {
            float cycle = period > 0.0001f ? elapsed * (Mathf.PI * 2f / period) : 0f;
            transform.position = home + Vector3.right * (lateralAmplitude * Mathf.Sin(cycle + phase));
        }

        public bool Evaluate()
        {
            LastSampleTime = Time.time;
            IsDetecting = false;
            LastRayBlocked = false;

            HasPlayerOverlap = player != null && OverlapsPlayer();
            if (!HasPlayerOverlap)
                return false;

            Vector3[] targets =
            {
                player.position + Vector3.up * 1.55f,
                player.position + Vector3.up * 0.9f
            };

            bool anyBlocked = false;
            for (int i = 0; i < targets.Length; i++)
            {
                Vector3 target = targets[i];
                Vector3 origin = FindRayOrigin();
                Vector3 delta = target - origin;
                float distance = delta.magnitude;
                LastRayOrigin = origin;
                LastRayTarget = target;

                if (distance <= 0.001f)
                {
                    IsDetecting = true;
                    LastRayBlocked = false;
                    return true;
                }

                if (Physics.Raycast(origin, delta / distance, out _, distance, obstructionMask,
                        QueryTriggerInteraction.Ignore))
                {
                    anyBlocked = true;
                    continue;
                }

                IsDetecting = true;
                LastRayBlocked = false;
                return true;
            }

            LastRayBlocked = anyBlocked;
            return false;
        }

        private bool OverlapsPlayer()
        {
            switch (shape)
            {
                case DetectionShape.Sphere:
                    return Physics.CheckSphere(transform.position, Mathf.Max(0f, radius), playerMask,
                        QueryTriggerInteraction.Ignore);
                case DetectionShape.Box:
                    return Physics.CheckBox(transform.position, Abs(halfExtents), transform.rotation, playerMask,
                        QueryTriggerInteraction.Ignore);
                case DetectionShape.Capsule:
                    return Physics.CheckCapsule(transform.TransformPoint(capsulePointA),
                        transform.TransformPoint(capsulePointB), Mathf.Max(0f, radius), playerMask,
                        QueryTriggerInteraction.Ignore);
                default:
                    return false;
            }
        }

        private Vector3 FindRayOrigin()
        {
            // Use the physical sensor/lens as the ray origin. Detection volumes can extend past the
            // player; starting from their boundary would let a ray begin on the far side of cover.
            return rayOrigin ? rayOrigin.position : transform.position;
        }

        private static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsDetecting ? Color.red : Color.yellow;
            switch (shape)
            {
                case DetectionShape.Sphere:
                    Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, radius));
                    break;
                case DetectionShape.Box:
                    Matrix4x4 previous = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, Abs(halfExtents) * 2f);
                    Gizmos.matrix = previous;
                    break;
                case DetectionShape.Capsule:
                {
                    Vector3 pointA = transform.TransformPoint(capsulePointA);
                    Vector3 pointB = transform.TransformPoint(capsulePointB);
                    Gizmos.DrawWireSphere(pointA, Mathf.Max(0f, radius));
                    Gizmos.DrawWireSphere(pointB, Mathf.Max(0f, radius));
                    Gizmos.DrawLine(pointA + Vector3.up * radius, pointB + Vector3.up * radius);
                    Gizmos.DrawLine(pointA - Vector3.up * radius, pointB - Vector3.up * radius);
                    Gizmos.DrawLine(pointA + Vector3.right * radius, pointB + Vector3.right * radius);
                    Gizmos.DrawLine(pointA - Vector3.right * radius, pointB - Vector3.right * radius);
                    break;
                }
            }

            if (LastRayTarget != Vector3.zero)
            {
                Gizmos.color = LastRayBlocked ? Color.red : Color.green;
                Gizmos.DrawLine(LastRayOrigin, LastRayTarget);
            }
        }
    }
}
