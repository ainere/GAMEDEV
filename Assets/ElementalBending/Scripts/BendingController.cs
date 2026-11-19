using ShowcaseCharacters;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ElementalBending
{
    /// <summary>
    /// Runs the bending showcase: plays the bender's pose timeline and every effect group, moves the camera between
    /// shots and shows the element title.
    /// Press 1 or Space to restart, L to toggle auto-replay.
    /// </summary>
    public sealed class BendingController : MonoBehaviour
    {
        [SerializeField] PoseAnimator bender;
        [SerializeField] ParticleSystem[] effectRoots = System.Array.Empty<ParticleSystem>();

        [Header("Camera")]
        [SerializeField] Transform cameraRig;
        [SerializeField] Transform cameraTransform;
        [SerializeField, Min(0.1f)] float shotBlendSeconds = 0.9f;

        [SerializeField] bool autoReplay = true;

        PoseClip clip;
        float elapsed;
        GUIStyle titleStyle;

        void Start()
        {
            clip = BendingChoreography.Build();
            Restart();
        }

        public void Restart()
        {
            elapsed = 0f;
            bender.Play(clip);
            foreach (ParticleSystem root in effectRoots)
            {
                root.Clear(true);
                root.Play(true);
            }
            UpdateCamera();
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
            if (elapsed >= BendingChoreography.Length)
            {
                if (autoReplay)
                    Restart();
                else
                    elapsed = BendingChoreography.Length;
            }

            bender.Time = elapsed;
            UpdateCamera();
        }

        int SegmentIndex()
        {
            int index = 0;
            for (int i = 0; i < BendingChoreography.Segments.Length; i++)
                if (elapsed >= BendingChoreography.Segments[i].start)
                    index = i;
            return index;
        }

        void UpdateCamera()
        {
            if (cameraRig == null)
                return;
            var shots = BendingChoreography.Shots;
            int current = 0;
            for (int i = 0; i < shots.Length; i++)
                if (elapsed >= shots[i].time)
                    current = i;
            int previous = Mathf.Max(0, current - 1);
            float u = Mathf.Clamp01((elapsed - shots[current].time) / shotBlendSeconds);
            u = u * u * (3f - 2f * u);
            if (current == 0)
                u = 1f;

            // Slow drift within each shot keeps the frame alive.
            Vector3 drift = new Vector3(Mathf.Sin(elapsed * 0.35f), 0f, Mathf.Cos(elapsed * 0.3f)) * 0.15f;
            Vector3 position = Vector3.Lerp(shots[previous].position, shots[current].position, u) + drift;
            Vector3 lookAt = Vector3.Lerp(shots[previous].lookAt, shots[current].lookAt, u);
            cameraRig.position = position;
            cameraRig.rotation = Quaternion.LookRotation(lookAt - position);

            if (cameraTransform == null)
                return;
            Vector3 shake = Vector3.zero;
            foreach ((float time, float strength) in BendingChoreography.Impacts)
            {
                float t = elapsed - time;
                if (t < 0f || t > 0.4f)
                    continue;
                float falloff = 1f - t / 0.4f;
                shake += new Vector3(Mathf.PerlinNoise(t * 40f, time) - 0.5f, Mathf.PerlinNoise(time, t * 40f) - 0.5f, 0f) * 2f * strength * falloff * falloff;
            }
            cameraTransform.localPosition = shake;
        }

        void OnGUI()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(Screen.height * 0.07f),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperLeft,
                };
                titleStyle.normal.textColor = new Color(1f, 0.97f, 0.9f, 0.9f);
            }

            var segment = BendingChoreography.Segments[SegmentIndex()];
            float age = elapsed - segment.start;
            if (!string.IsNullOrEmpty(segment.title) && age < 2.5f)
            {
                Color c = titleStyle.normal.textColor;
                c.a = Mathf.Clamp01(Mathf.Min(age * 4f, (2.5f - age) * 2f)) * 0.9f;
                titleStyle.normal.textColor = c;
                GUI.Label(new Rect(Screen.width * 0.05f, Screen.height * 0.08f, Screen.width, Screen.height * 0.15f), segment.title, titleStyle);
            }
            if (Showcase.ShowcaseHud.Visible)
                GUI.Label(new Rect(16, Screen.height - 32, 640, 24), $"1 / Space: Restart     L: Auto-replay ({(autoReplay ? "on" : "off")})     Tab: Next scene     H: Hide help");
        }
    }
}
