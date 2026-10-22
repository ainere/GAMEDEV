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
        public bool useViewCone;
        public float viewAngle = 70f;
        public float viewRange = 9f;
        public Transform panPivot;
        public Vector3 panBaseEuler;
        public float panAmplitude;
        public float panPeriod = 8f;
        public float panPhase;

        public bool IsDetecting { get; private set; }
        public bool HasPlayerOverlap { get; private set; }
        public bool HasSightSample { get; private set; }
        public Vector3 LastRayOrigin { get; private set; }
        public Vector3 LastRayTarget { get; private set; }
        public bool LastRayBlocked { get; private set; }
        public float LastSampleTime { get; private set; }

        public Vector3 SightOrigin => rayOrigin ? rayOrigin.position : transform.position;
        public Vector3 SightForward => rayOrigin ? rayOrigin.forward : transform.forward;
        public float VisionRange => Mathf.Max(0f, viewRange);
        public float VisionAngle => Mathf.Clamp(viewAngle, 0f, 179.9f);
        public Quaternion QueryRotation => useViewCone && rayOrigin ? rayOrigin.rotation : transform.rotation;
        public Vector3 QueryCenter => useViewCone
            ? SightOrigin + (shape == DetectionShape.Sphere ? Vector3.zero : SightForward * (VisionRange * .5f))
            : transform.position;
        public float QueryRadius => useViewCone
            ? (shape == DetectionShape.Sphere ? VisionRange : Mathf.Max(.001f, VisionRange * Mathf.Sin(VisionAngle * .5f * Mathf.Deg2Rad)))
            : Mathf.Max(0f, radius);
        public Vector3 QueryHalfExtents => useViewCone
            ? new Vector3(VisionRange * Mathf.Sin(VisionAngle * .5f * Mathf.Deg2Rad),
                VisionRange * Mathf.Sin(VisionAngle * .5f * Mathf.Deg2Rad), VisionRange * .5f)
            : Abs(halfExtents);
        public Vector3 QueryPointA => useViewCone ? SightOrigin : transform.TransformPoint(capsulePointA);
        public Vector3 QueryPointB => useViewCone ? SightOrigin + SightForward * VisionRange : transform.TransformPoint(capsulePointB);

        public void SetPatrolTime(float elapsed)
        {
            float cycle = period > 0.0001f ? elapsed * (Mathf.PI * 2f / period) : 0f;
            transform.position = home + Vector3.right * (lateralAmplitude * Mathf.Sin(cycle + phase));
            if (panPivot)
            {
                float panCycle = panPeriod > .0001f ? elapsed * (Mathf.PI * 2f / panPeriod) : 0f;
                float yaw = panAmplitude * Mathf.Sin(panCycle + panPhase);
                panPivot.localRotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(panBaseEuler);
            }
        }

        public bool Evaluate()
        {
            LastSampleTime = Time.time;
            IsDetecting = false;
            LastRayBlocked = false;
            HasSightSample = false;

            HasPlayerOverlap = player != null && (!useViewCone || (VisionRange > .001f && VisionAngle > 0f)) && OverlapsPlayer();
            if (!HasPlayerOverlap)
                return false;

            GetPlayerSamples(out Vector3 head, out Vector3 torso, out Vector3 feet);
            Vector3[] targets = { head, torso };
            // A viewing camera covers exactly its lit floor pool: the player must stand inside the cone's
            // footprint (the spotlight shares its angle and range), then be visible to a sight ray.
            bool lit = !useViewCone || IsWithinView(feet);

            bool anyBlocked = false;
            for (int i = 0; i < targets.Length; i++)
            {
                Vector3 target = targets[i];
                if (!lit || (!useViewCone && !IsWithinView(target)))
                    continue;
                Vector3 origin = SightOrigin;
                Vector3 delta = target - origin;
                float distance = delta.magnitude;
                LastRayOrigin = origin;
                LastRayTarget = target;
                HasSightSample = true;

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

        private void GetPlayerSamples(out Vector3 head, out Vector3 torso, out Vector3 feet)
        {
            CharacterController body = player.GetComponent<CharacterController>();
            if (body == null)
            {
                head = player.position + Vector3.up * 1.55f;
                torso = player.position + Vector3.up * 0.9f;
                feet = player.position + Vector3.up * 0.05f;
                return;
            }
            feet = player.TransformPoint(new Vector3(body.center.x, body.center.y - body.height * 0.5f + 0.05f, body.center.z));

            float bottom = body.center.y - body.height * 0.5f;
            float top = body.center.y + body.height * 0.5f;
            float headOffset = Mathf.Min(Mathf.Max(body.radius * 0.5f, 0.08f), body.height * 0.5f);
            float headY = Mathf.Max(bottom, top - headOffset);
            torso = player.TransformPoint(new Vector3(body.center.x, body.center.y, body.center.z));
            head = player.TransformPoint(new Vector3(body.center.x, headY, body.center.z));
        }

        private bool OverlapsPlayer()
        {
            switch (shape)
            {
                case DetectionShape.Sphere:
                    return Physics.CheckSphere(QueryCenter, QueryRadius, playerMask,
                        QueryTriggerInteraction.Ignore);
                case DetectionShape.Box:
                    return Physics.CheckBox(QueryCenter, QueryHalfExtents, QueryRotation, playerMask,
                        QueryTriggerInteraction.Ignore);
                case DetectionShape.Capsule:
                    return Physics.CheckCapsule(QueryPointA, QueryPointB, QueryRadius, playerMask,
                        QueryTriggerInteraction.Ignore);
                default:
                    return false;
            }
        }

        public bool IsWithinView(Vector3 target)
        {
            if (!useViewCone)
                return true;
            if (VisionRange <= .001f || VisionAngle <= 0f)
                return false;
            Vector3 delta = target - SightOrigin;
            float distance = delta.magnitude;
            if (distance > VisionRange)
                return false;
            return distance <= .001f || Vector3.Dot(SightForward, delta / distance) >=
                Mathf.Cos(VisionAngle * .5f * Mathf.Deg2Rad);
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
                    Gizmos.DrawWireSphere(QueryCenter, QueryRadius);
                    break;
                case DetectionShape.Box:
                    Matrix4x4 previous = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(QueryCenter, QueryRotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, QueryHalfExtents * 2f);
                    Gizmos.matrix = previous;
                    break;
                case DetectionShape.Capsule:
                {
                    Vector3 pointA = QueryPointA;
                    Vector3 pointB = QueryPointB;
                    float r = QueryRadius;
                    Gizmos.DrawWireSphere(pointA, r);
                    Gizmos.DrawWireSphere(pointB, r);
                    Gizmos.DrawLine(pointA + Vector3.up * r, pointB + Vector3.up * r);
                    Gizmos.DrawLine(pointA - Vector3.up * r, pointB - Vector3.up * r);
                    Gizmos.DrawLine(pointA + Vector3.right * r, pointB + Vector3.right * r);
                    Gizmos.DrawLine(pointA - Vector3.right * r, pointB - Vector3.right * r);
                    break;
                }
            }

            if (HasSightSample)
            {
                Gizmos.color = LastRayBlocked ? Color.red : Color.green;
                Gizmos.DrawLine(LastRayOrigin, LastRayTarget);
            }
        }
    }
}
