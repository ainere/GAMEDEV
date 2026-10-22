using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace RayOEZoning
{
    /// <summary>
    /// First-person body: the CC0 Quaternius mannequin blended between idle, walk, sprint and crouch clips from the
    /// actual stance and speed. The head is collapsed so it never blocks the view, and the right arm is solved with
    /// two-bone IK so the mannequin's own hand closes round the held flashlight's grip.
    /// </summary>
    [DefaultExecutionOrder(1350)]
    public sealed class PlayerBodyVisual : MonoBehaviour
    {
        public StealthGame game;
        public Animator animator;
        public Transform model;
        public AnimationClip idle, walk, sprint, crouchIdle, crouchWalk;
        public Transform[] hiddenBones;
        public Vector3 standingOffset = new Vector3(0, 0, -.08f);
        public Vector3 crouchedOffset = new Vector3(0, 0, -.16f);
        public Vector3 proneOffset = new Vector3(0, .18f, -1.55f);
        /// <summary>Extra step back while looking straight down, so the chest stays behind the camera.</summary>
        public float lookDownBack = .12f;

        [Header("Right arm")]
        public Transform upperArm, lowerArm, hand;
        /// <summary>Knuckle and finger-base bones that define the hand's frame: middle, index and pinky.</summary>
        public Transform middleBase, indexBase, pinkyBase;
        /// <summary>Tips of the middle finger and thumb, used to measure the closed hand and aim the thumb.</summary>
        public Transform middleTip, thumbTip;
        /// <summary>Grip point on the flashlight. Forward is the wanted knuckle direction, up the wanted thumb side.</summary>
        public Transform grip;
        /// <summary>The held flashlight root, moved to meet the hand if the arm cannot reach it.</summary>
        public Transform heldRoot;
        /// <summary>Where the barrel's axis passes through the closed hand: along the knuckles, out of the palm, toward the thumb.</summary>
        public Vector3 gripInHand = new Vector3(.072f, .032f, -.004f);
        /// <summary>Elbow hint in body space: out to the side and down.</summary>
        public Vector3 elbowHint = new Vector3(.75f, -.6f, -.1f);

        [Header("Fingers (index, middle, ring, pinky, thumb; three joints each)")]
        public Transform[] fingerJoints = new Transform[15];
        public Quaternion[] straightRotations = new Quaternion[15];
        public Vector3[] fingerCurl =
        {
            new Vector3(62, 58, 40), new Vector3(66, 60, 42), new Vector3(70, 62, 42), new Vector3(74, 64, 44)
        };
        public Vector3 thumbCurl = new Vector3(0, 25, 20);
        /// <summary>How far past the index finger's middle joint, in millimetres toward the palm side, the thumb tip aims.</summary>
        public float thumbOpposition = 14;

        /// <summary>Distance from shoulder to the wanted wrist position as a share of the arm's length.</summary>
        public float Reach { get; private set; }
        /// <summary>Radius of the curled middle finger's arc, measured round the grip.</summary>
        public float FistRadius { get; private set; }
        bool measured;

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
                if (playables != null && playables[2].IsValid()) playables[2].SetSpeed(playing ? 1 : 0);
            }
            PoseUpperBody();
        }

        /// <summary>Blends clip weights and places the model. Public so editor tools can preview a stance.</summary>
        public void ApplyPose(PlayerStance stance, bool sprinting, float dt)
        {
            if (dt <= 0)
            {
                crouch = stance == PlayerStance.Crouched ? 1 : 0;
                prone = stance == PlayerStance.Prone ? 1 : 0;
            }
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
            upright.z -= lookDownBack * Mathf.Clamp01(LookPitch() / 80f) * (1 - prone);
            model.localPosition = Vector3.Lerp(upright, proneOffset, prone);
            // The Quaternius rig faces -Z; turn it to face the view before tipping it forward for prone.
            model.localRotation = Quaternion.Euler(90 * prone, 0, 0) * Quaternion.Euler(0, 180, 0);
        }

        /// <summary>Degrees the view is pitched below the body's horizon (negative when looking up).</summary>
        public float LookPitch()
        {
            if (!game || !game.playerCamera) return 0;
            Transform view = game.playerCamera.transform, body = transform.parent ? transform.parent : transform;
            return Vector3.SignedAngle(body.forward, view.forward, view.right);
        }

        /// <summary>Hides the head, solves the arm onto the flashlight and closes the fingers. Runs after animation.</summary>
        public void PoseUpperBody()
        {
            if (hiddenBones != null)
                foreach (var bone in hiddenBones) if (bone) bone.localScale = Vector3.one * .0001f;
            if (!upperArm || !lowerArm || !hand || !grip || !middleBase || !indexBase || !pinkyBase) return;

            // Current frame of the hand and the frame the grip asks for.
            Frame(out Vector3 f0, out Vector3 t0);
            Vector3 f = grip.forward, t = grip.up, p = Vector3.Cross(f, t);
            Quaternion turn = Quaternion.LookRotation(f, t) * Quaternion.Inverse(Quaternion.LookRotation(f0, t0));
            Quaternion handRotation = turn * hand.rotation;
            Vector3 wrist = grip.position - (f * gripInHand.x + p * gripInHand.y + t * gripInHand.z);

            Vector3 reached = SolveArm(wrist);
            hand.rotation = handRotation;
            // If the arm falls short, bring the flashlight to the hand rather than leave it floating.
            if (heldRoot && (wrist - reached).sqrMagnitude > 1e-6f) heldRoot.position += reached - wrist;

            CloseFingers(f, p);
            if (!measured) MeasureFist(f, p, t);
        }

        /// <summary>
        /// Finds where the barrel should pass through the closed hand: the centre of the arc the curled middle finger
        /// makes, measured once in the hand's own frame (along the knuckles, out of the palm, toward the thumb).
        /// </summary>
        public void MeasureFist(Vector3 f, Vector3 p, Vector3 t)
        {
            if (!middleTip || !fingerJoints[5]) return;
            Vector2 Flat(Vector3 world) { Vector3 d = world - hand.position; return new Vector2(Vector3.Dot(d, f), Vector3.Dot(d, p)); }
            Vector2 a = Flat(middleBase.position), b = Flat(fingerJoints[5].position), c = Flat(middleTip.position);
            float d2 = 2 * (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
            if (Mathf.Abs(d2) < 1e-7f) return;
            float aa = a.sqrMagnitude, bb = b.sqrMagnitude, cc = c.sqrMagnitude;
            var centre = new Vector2((aa * (b.y - c.y) + bb * (c.y - a.y) + cc * (a.y - b.y)) / d2,
                (aa * (c.x - b.x) + bb * (a.x - c.x) + cc * (b.x - a.x)) / d2);
            FistRadius = Vector2.Distance(centre, a);
            float across = (Vector3.Dot(indexBase.position - hand.position, t) + Vector3.Dot(pinkyBase.position - hand.position, t)) * .5f;
            gripInHand = new Vector3(centre.x, centre.y, across);
            measured = true;
        }

        void Frame(out Vector3 f, out Vector3 t)
        {
            f = (middleBase.position - hand.position).normalized;
            Vector3 across = indexBase.position - pinkyBase.position;
            t = (across - f * Vector3.Dot(across, f)).normalized;
        }

        /// <summary>Two-bone IK. Returns where the wrist actually ended up.</summary>
        Vector3 SolveArm(Vector3 target)
        {
            Vector3 shoulder = upperArm.position;
            float a = Vector3.Distance(upperArm.position, lowerArm.position), b = Vector3.Distance(lowerArm.position, hand.position);
            Vector3 toTarget = target - shoulder;
            float distance = toTarget.magnitude;
            Reach = distance / Mathf.Max(.001f, a + b);
            float d = Mathf.Clamp(distance, Mathf.Abs(a - b) + .001f, a + b - .001f);
            Vector3 direction = distance > .0001f ? toTarget / distance : transform.forward;
            Vector3 hint = transform.TransformDirection(elbowHint);
            Vector3 bend = hint - direction * Vector3.Dot(hint, direction);
            bend = bend.sqrMagnitude > 1e-6f ? bend.normalized : Vector3.Cross(direction, transform.right).normalized;
            float cosine = Mathf.Clamp((a * a + d * d - b * b) / (2 * a * d), -1, 1);
            Vector3 elbow = shoulder + direction * (a * cosine) + bend * (a * Mathf.Sqrt(1 - cosine * cosine));
            upperArm.rotation = Quaternion.FromToRotation(lowerArm.position - shoulder, elbow - shoulder) * upperArm.rotation;
            Vector3 wrist = shoulder + direction * d;
            lowerArm.rotation = Quaternion.FromToRotation(hand.position - lowerArm.position, wrist - lowerArm.position) * lowerArm.rotation;
            return hand.position;
        }

        /// <summary>Resets the fingers straight, then curls each joint toward the palm and wraps the thumb under.</summary>
        void CloseFingers(Vector3 f, Vector3 p)
        {
            for (int i = 0; i < fingerJoints.Length; i++) if (fingerJoints[i]) fingerJoints[i].localRotation = straightRotations[i];
            Vector3 axis = Vector3.Cross(f, p).normalized;
            if (Vector3.Dot(Quaternion.AngleAxis(90, axis) * f, p) < 0) axis = -axis;
            for (int finger = 0; finger < 4; finger++)
                for (int joint = 0; joint < 3; joint++)
                {
                    var bone = fingerJoints[finger * 3 + joint];
                    if (bone) bone.rotation = Quaternion.AngleAxis(fingerCurl[finger][joint], axis) * bone.rotation;
                }
            var thumb = fingerJoints[12];
            if (!thumb) return;
            // Bend the thumb's joints a little toward the palm, then aim it across the curled index finger.
            for (int joint = 1; joint < 3; joint++)
            {
                var bone = fingerJoints[12 + joint];
                if (!bone) continue;
                // The rig's bones run along their local Y axis.
                Vector3 dir = bone.up;
                Vector3 curlAxis = Vector3.Cross(dir, p);
                if (curlAxis.sqrMagnitude < 1e-6f) continue;
                curlAxis.Normalize();
                if (Vector3.Dot(Quaternion.AngleAxis(90, curlAxis) * dir, p) < 0) curlAxis = -curlAxis;
                bone.rotation = Quaternion.AngleAxis(thumbCurl[joint], curlAxis) * bone.rotation;
            }
            var index = fingerJoints[1];
            if (thumbTip && index)
            {
                Vector3 target = index.position + p * thumbOpposition * .001f;
                thumb.rotation = Quaternion.FromToRotation(thumbTip.position - thumb.position, target - thumb.position) * thumb.rotation;
            }
        }
    }
}
