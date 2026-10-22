using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace RayOEZoning
{
    /// <summary>
    /// First-person body: the CC0 Quaternius mannequin blended between idle, walk, sprint and crouch clips from the
    /// actual stance and controller speed. Head and arms are collapsed so only the torso, legs and feet show.
    /// </summary>
    [DefaultExecutionOrder(1250)]
    public sealed class PlayerBodyVisual : MonoBehaviour
    {
        public StealthGame game;
        public Animator animator;
        public Transform model;
        public AnimationClip idle, walk, sprint, crouchIdle, crouchWalk;
        public Transform[] hiddenBones;
        public Vector3 standingOffset = new Vector3(0, 0, -.26f);
        public Vector3 crouchedOffset = new Vector3(0, 0, -.36f);
        public Vector3 proneOffset = new Vector3(0, .18f, -1.62f);

        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable[] playables;
        readonly float[] weights = new float[5];
        float speed, prone, crouch;

        void OnEnable()
        {
            if (!animator || graph.IsValid()) return;
            var clips = new[] { idle, walk, sprint, crouchIdle, crouchWalk };
            graph = PlayableGraph.Create("Investigator body");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            mixer = AnimationMixerPlayable.Create(graph, clips.Length);
            playables = new AnimationClipPlayable[clips.Length];
            for (int i = 0; i < clips.Length; i++)
            {
                if (!clips[i]) continue;
                playables[i] = AnimationClipPlayable.Create(graph, clips[i]);
                graph.Connect(playables[i], 0, mixer, i);
            }
            weights[0] = 1;
            AnimationPlayableOutput.Create(graph, "Body", animator).SetSourcePlayable(mixer);
            graph.Play();
        }

        void OnDisable()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        void LateUpdate()
        {
            if (!game) return;
            bool playing = game.Mode == GameMode.Playing;
            PlayerStance stance = game.stance ? game.stance.CurrentStance : PlayerStance.Standing;
            bool sprinting = game.stance && game.stance.IsSprinting;
            float target = playing && game.stance ? game.stance.NormalizedMoveSpeed : 0;
            float dt = Time.deltaTime;
            speed = Mathf.MoveTowards(speed, target, dt * 6);
            crouch = Mathf.MoveTowards(crouch, stance == PlayerStance.Crouched ? 1 : 0, dt * 5);
            prone = Mathf.MoveTowards(prone, stance == PlayerStance.Prone ? 1 : 0, dt * 4);
            ApplyPose(stance, sprinting, dt);
            if (graph.IsValid())
            {
                for (int i = 0; i < weights.Length; i++) mixer.SetInputWeight(i, weights[i]);
                // Keep footfalls roughly matched to ground speed.
                if (playables != null && playables[1].IsValid()) playables[1].SetSpeed(Mathf.Lerp(.6f, 1.1f, speed));
                if (playables != null && playables[4].IsValid()) playables[4].SetSpeed(Mathf.Lerp(.5f, 1f, speed));
                if (playables != null && playables[2].IsValid()) playables[2].SetSpeed(game.Mode == GameMode.Playing ? 1 : 0);
            }
            if (hiddenBones != null)
                foreach (var bone in hiddenBones) if (bone) bone.localScale = Vector3.one * .0001f;
        }

        /// <summary>Blends clip weights and the lying pose. Public so editor tools can preview a stance.</summary>
        public void ApplyPose(PlayerStance stance, bool sprinting, float dt)
        {
            float moving = Mathf.Clamp01(speed);
            float standing = 1 - crouch;
            float[] goal = {
                standing * (1 - moving),
                standing * moving * (sprinting ? 0 : 1),
                standing * moving * (sprinting ? 1 : 0),
                crouch * (1 - moving),
                crouch * moving
            };
            float total = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = dt <= 0 ? goal[i] : Mathf.MoveTowards(weights[i], goal[i], dt * 6);
                total += weights[i];
            }
            if (total > .0001f) for (int i = 0; i < weights.Length; i++) weights[i] /= total;
            if (!model) return;
            Vector3 upright = Vector3.Lerp(standingOffset, crouchedOffset, crouch);
            model.localPosition = Vector3.Lerp(upright, proneOffset, prone);
            model.localRotation = Quaternion.Euler(90 * prone, 0, 0);
        }
    }
}
