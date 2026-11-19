using ShowcaseCharacters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FinalBoss
{
    /// <summary>
    /// Runs the throne-room sequence. One value, the aura intensity, drives every looping aura layer's emission rate and
    /// the aura light; each layer has a threshold, so layers join in as the power builds. At the climax the candles turn
    /// crimson and flare, and the camera gets a brief FOV, Bloom and chromatic aberration kick.
    /// Press 1 or Space to restart, L to toggle auto-replay.
    /// </summary>
    public sealed class BossController : MonoBehaviour
    {
        [SerializeField] PoseAnimator boss;

        [Header("One-shot and timed effects, played at the start")]
        [SerializeField] ParticleSystem[] effectRoots = System.Array.Empty<ParticleSystem>();

        [Header("Looping aura layers driven by intensity")]
        [SerializeField] ParticleSystem[] auraSystems = System.Array.Empty<ParticleSystem>();
        [SerializeField] float[] auraThresholds = System.Array.Empty<float>();

        [Header("Lights and glow")]
        [SerializeField] Light auraLight;
        [SerializeField, Min(0f)] float auraLightMax = 9f;
        [SerializeField] Renderer eyes;
        [SerializeField] Color eyeGlow = new Color(5f, 0.3f, 0.8f);

        [Header("Candles that react to the climax")]
        [SerializeField] ParticleSystem[] candleFlames = System.Array.Empty<ParticleSystem>();
        [SerializeField] Light[] candleLights = System.Array.Empty<Light>();
        [SerializeField] Color candleCalm = new Color(1f, 0.7f, 0.35f);
        [SerializeField] Color candleSurge = new Color(1f, 0.15f, 0.45f);

        [Header("Camera")]
        [SerializeField] Volume volume;
        [SerializeField] Transform cameraRig;
        [SerializeField] Transform cameraTransform;
        [SerializeField] Vector3 cameraStart = new Vector3(0f, 0.4f, 5.2f);
        [SerializeField] Vector3 cameraEnd = new Vector3(0f, 0.35f, 3.7f);
        [SerializeField] Vector3 lookAt = new Vector3(0f, 2.05f, 0f);
        [SerializeField, Min(0f)] float fovKick = 7f;

        [SerializeField] bool autoReplay = true;

        PoseClip clip;
        float elapsed;
        float[] baseRates;
        Material eyeMaterial;
        Camera viewCamera;
        float baseFov;
        Bloom bloom;
        ChromaticAberration chromatic;
        float baseBloom;
        Color candleLightCalm;
        bool candlesSurging;

        void Start()
        {
            clip = BossChoreography.Build();
            baseRates = new float[auraSystems.Length];
            for (int i = 0; i < auraSystems.Length; i++)
                baseRates[i] = auraSystems[i].emission.rateOverTimeMultiplier;
            if (eyes != null)
                eyeMaterial = eyes.material;
            if (cameraTransform != null)
            {
                viewCamera = cameraTransform.GetComponent<Camera>();
                baseFov = viewCamera.fieldOfView;
            }
            if (volume != null)
            {
                // volume.profile makes a runtime copy, so the kick never edits the saved asset.
                volume.profile.TryGet(out bloom);
                volume.profile.TryGet(out chromatic);
                if (bloom != null)
                    baseBloom = bloom.intensity.value;
            }
            if (candleLights.Length > 0)
                candleLightCalm = candleLights[0].color;
            Restart();
        }

        public void Restart()
        {
            elapsed = 0f;
            boss.Play(clip);
            foreach (ParticleSystem root in effectRoots)
            {
                root.Clear(true);
                root.Play(true);
            }
            foreach (ParticleSystem aura in auraSystems)
            {
                aura.Clear(false);
                aura.Play(false);
            }
            SetCandles(false);
            Apply();
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                    Restart();
                if (keyboard.lKey.wasPressedThisFrame)
                    autoReplay = !autoReplay;
            }

            elapsed += Time.deltaTime;
            if (elapsed >= BossChoreography.Length && autoReplay)
                Restart();
            boss.Time = elapsed;
            Apply();
        }

        void Apply()
        {
            float intensity = BossChoreography.AuraIntensity(elapsed);
            for (int i = 0; i < auraSystems.Length; i++)
            {
                float threshold = i < auraThresholds.Length ? auraThresholds[i] : 0f;
                float layer = Mathf.Clamp01((intensity - threshold) / Mathf.Max(0.01f, 1f - threshold));
                var emission = auraSystems[i].emission;
                emission.rateOverTimeMultiplier = baseRates[i] * layer;
            }

            if (auraLight != null)
            {
                float flicker = 0.85f + 0.3f * Mathf.PerlinNoise(elapsed * 6f, 0.3f);
                auraLight.intensity = auraLightMax * intensity * flicker;
            }
            if (eyeMaterial != null)
                eyeMaterial.SetColor("_EmissionColor", eyeGlow * BossChoreography.EyeGlow(elapsed));

            if (!candlesSurging && elapsed >= BossChoreography.Climax)
                SetCandles(true);

            UpdateCamera(intensity);
        }

        void SetCandles(bool surge)
        {
            candlesSurging = surge;
            foreach (ParticleSystem flame in candleFlames)
            {
                var main = flame.main;
                main.startColor = surge ? candleSurge : candleCalm;
                main.startSizeMultiplier = surge ? 1.3f : 1f;
            }
            foreach (Light candleLight in candleLights)
            {
                candleLight.color = surge ? candleSurge : candleLightCalm;
                candleLight.intensity = surge ? 3f : 1.6f;
            }
        }

        void UpdateCamera(float intensity)
        {
            if (cameraRig == null)
                return;
            float u = Mathf.SmoothStep(0f, 1f, elapsed / BossChoreography.Length);
            Vector3 position = Vector3.Lerp(cameraStart, cameraEnd, u);
            cameraRig.position = position;
            cameraRig.rotation = Quaternion.LookRotation(lookAt - position);

            // The kick: strongest right at the climax, fading over 0.7 s.
            float sinceClimax = elapsed - BossChoreography.Climax;
            float kick = sinceClimax >= 0f && sinceClimax < 0.7f ? Mathf.Pow(1f - sinceClimax / 0.7f, 2f) : 0f;
            if (viewCamera != null)
                viewCamera.fieldOfView = baseFov + fovKick * kick;
            if (bloom != null)
                bloom.intensity.value = baseBloom * (1f + 1.2f * kick);
            if (chromatic != null)
                chromatic.intensity.value = 0.9f * kick + 0.1f * intensity;

            if (cameraTransform == null)
                return;
            float shake = 0.012f * intensity + 0.14f * kick;
            cameraTransform.localPosition = new Vector3(
                Mathf.PerlinNoise(elapsed * 30f, 0.2f) - 0.5f,
                Mathf.PerlinNoise(0.8f, elapsed * 30f) - 0.5f, 0f) * 2f * shake;
        }

        void OnGUI()
        {
            if (!Showcase.ShowcaseHud.Visible)
                return;
            GUI.Label(new Rect(16, Screen.height - 32, 640, 24), $"1 / Space: Restart     L: Auto-replay ({(autoReplay ? "on" : "off")})     Tab: Next scene     H: Hide help");
        }
    }
}
