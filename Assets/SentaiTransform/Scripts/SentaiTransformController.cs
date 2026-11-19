using ShowcaseCharacters;
using Pose = ShowcaseCharacters.Pose;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SentaiTransform
{
    /// <summary>
    /// Runs the team transformation: plays every effect group, drives each ranger's pose timeline, swaps the rangers
    /// into their suits under the flash and shakes the camera on the team blast.
    /// Press 1 or Space to transform, R to reset to civilian clothes, L to toggle auto-replay.
    /// </summary>
    public sealed class SentaiTransformController : MonoBehaviour
    {
        [Header("Effects (each root is played with its children)")]
        [SerializeField] ParticleSystem[] effectRoots = System.Array.Empty<ParticleSystem>();

        [Header("Rangers, camera left to right")]
        [SerializeField] PoseAnimator[] rangers = System.Array.Empty<PoseAnimator>();
        [SerializeField] CostumeSwap[] costumes = System.Array.Empty<CostumeSwap>();

        [Header("Camera: the rig follows the choreography's waypoints, the camera inside it shakes")]
        [SerializeField] Transform cameraRig;
        [SerializeField] Transform cameraTransform;
        [SerializeField, Min(0f)] float shakeDuration = 0.5f;
        [SerializeField, Min(0f)] float shakeAmplitude = 0.08f;

        [Header("Replay")]
        [SerializeField] bool autoReplay = true;

        float elapsed = -1f;
        bool suited;
        Vector3 cameraRestPosition;
        PoseClip[] clips;

        void Start()
        {
            if (cameraTransform != null)
                cameraRestPosition = cameraTransform.localPosition;
            clips = new PoseClip[rangers.Length];
            for (int i = 0; i < rangers.Length; i++)
                clips[i] = SentaiChoreography.ForRanger(i);
            Transform();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                    Transform();
                if (keyboard.rKey.wasPressedThisFrame)
                    ResetToCivilian();
                if (keyboard.lKey.wasPressedThisFrame)
                    autoReplay = !autoReplay;
            }

            if (elapsed < 0f)
                return;

            elapsed += Time.deltaTime;
            foreach (PoseAnimator ranger in rangers)
                ranger.Time = elapsed;
            if (!suited && elapsed >= SentaiChoreography.SuitSwapTime)
                Wear(true);

            UpdateCamera();
            UpdateShake();

            if (autoReplay && elapsed >= SentaiChoreography.Length)
                Transform();
        }

        /// <summary>Restarts the whole sequence from civilian clothes.</summary>
        public void Transform()
        {
            Wear(false);
            for (int i = 0; i < rangers.Length; i++)
                rangers[i].Play(clips[i]);
            foreach (ParticleSystem root in effectRoots)
            {
                root.Clear(true);
                root.Play(true);
            }
            elapsed = 0f;
        }

        public void ResetToCivilian()
        {
            foreach (ParticleSystem root in effectRoots)
                root.Clear(true);
            Wear(false);
            for (int i = 0; i < rangers.Length; i++)
                rangers[i].Play(clips[i]);
            elapsed = -1f;
            if (cameraTransform != null)
                cameraTransform.localPosition = cameraRestPosition;
        }

        void Wear(bool suit)
        {
            suited = suit;
            foreach (CostumeSwap costume in costumes)
                costume.Wear(suit);
        }

        void UpdateCamera()
        {
            if (cameraRig == null)
                return;
            var shots = SentaiChoreography.Shots;
            int i = 0;
            while (i < shots.Length - 2 && elapsed >= shots[i + 1].time)
                i++;
            float span = shots[i + 1].time - shots[i].time;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - shots[i].time) / span));
            Vector3 position = Vector3.Lerp(shots[i].position, shots[i + 1].position, u);
            Vector3 lookAt = Vector3.Lerp(shots[i].lookAt, shots[i + 1].lookAt, u);
            cameraRig.position = position;
            cameraRig.rotation = Quaternion.LookRotation(lookAt - position);
        }

        void UpdateShake()
        {
            if (cameraTransform == null)
                return;
            float t = elapsed - SentaiChoreography.BlastTime;
            if (t < 0f || t > shakeDuration)
            {
                cameraTransform.localPosition = cameraRestPosition;
                return;
            }
            float falloff = 1f - t / shakeDuration;
            float strength = shakeAmplitude * falloff * falloff;
            var offset = new Vector3(Mathf.PerlinNoise(t * 40f, 0.1f) - 0.5f, Mathf.PerlinNoise(0.7f, t * 40f) - 0.5f, 0f) * 2f * strength;
            cameraTransform.localPosition = cameraRestPosition + offset;
        }

        void OnGUI()
        {
            if (!Showcase.ShowcaseHud.Visible)
                return;
            GUI.Label(new Rect(16, 12, 640, 24),
                $"1 / Space: Transform     R: Reset     L: Auto-replay ({(autoReplay ? "on" : "off")})     Tab: Next scene     H: Hide help");
        }
    }
}
