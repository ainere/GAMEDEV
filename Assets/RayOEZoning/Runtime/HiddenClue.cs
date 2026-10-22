using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning
{
    // Hidden writing revealed by the held flashlight. This component decides whether the writing can be lit at all
    // (light on, within range, near the cone, clear line from the lamp); the RevealInk shader then shows only the
    // strokes that actually fall inside the cone.
    public sealed class HiddenClue : MonoBehaviour
    {
        public int clueIndex;
        public Transform anchor;
        public Renderer[] hiddenRenderers;
        public LayerMask obstructionMask;
        // Half the diagonal of the writing, so a lit edge still counts while the centre is outside the cone.
        public float radius = .2f;

        public bool IsVisible { get; private set; }

        private void Awake()
        {
            SetRenderersVisible(false);
        }

        public bool Evaluate(bool torchOn, float range, float coneDegrees, Transform emitter)
        {
            Transform target = anchor != null ? anchor : transform;
            bool visible = false;
            if (emitter != null && torchOn && range > 0f)
            {
                Vector3 toTarget = target.position - emitter.position;
                float distance = toTarget.magnitude;
                if (distance > 0.001f && distance <= range + radius)
                {
                    float halfAngle = Mathf.Clamp(coneDegrees, 0f, 179.9f) * 0.5f;
                    float spread = Mathf.Atan2(radius, distance) * Mathf.Rad2Deg;
                    visible = Vector3.Angle(emitter.forward, toTarget) <= halfAngle + spread &&
                        !Physics.Raycast(emitter.position, toTarget / distance, distance, obstructionMask, QueryTriggerInteraction.Ignore);
                }
            }

            IsVisible = visible;
            SetRenderersVisible(visible);
            return visible;
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

    // Feeds the held flashlight's cone to the RevealInk shader.
    public static class RevealInk
    {
        static readonly int Position = Shader.PropertyToID("_RevealLightPosition");
        static readonly int Direction = Shader.PropertyToID("_RevealLightDirection");
        static readonly int Cone = Shader.PropertyToID("_RevealLightCone");

        public static void Push(Light light, bool on)
        {
            if (!light)
            {
                Shader.SetGlobalVector(Position, Vector4.zero);
                return;
            }
            Transform t = light.transform;
            Vector3 p = t.position, f = t.forward;
            float outer = light.spotAngle * .5f * Mathf.Deg2Rad;
            Shader.SetGlobalVector(Position, new Vector4(p.x, p.y, p.z, on && light.enabled ? 1 : 0));
            Shader.SetGlobalVector(Direction, new Vector4(f.x, f.y, f.z, light.range));
            // Full strength across the bright part of the pool, fading out at its edge.
            Shader.SetGlobalVector(Cone, new Vector4(Mathf.Cos(outer), Mathf.Cos(outer * .78f), 0, 0));
        }
    }
}
