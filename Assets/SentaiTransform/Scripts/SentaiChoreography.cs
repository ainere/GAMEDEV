using ShowcaseCharacters;
using Pose = ShowcaseCharacters.Pose;

namespace SentaiTransform
{
    /// <summary>
    /// Pose timeline for the five rangers. Everyone shares the call, the overhead thrust and the suit-up stance,
    /// then each ranger strikes their own pose as the team blast goes off.
    /// Index order (camera left to right): 0 Pink, 1 Blue, 2 Red, 3 Yellow, 4 Green.
    /// </summary>
    public static class SentaiChoreography
    {
        public const float SuitSwapTime = 2.45f;
        public const float BlastTime = 2.8f;
        public const float Length = 8f;

        /// <summary>
        /// Camera waypoints (time, position, look-at). The camera eases between consecutive waypoints: a slow push-in
        /// through the charge and spiral, then a punch-out to a low hero angle as the team blast lands.
        /// </summary>
        public static readonly (float time, UnityEngine.Vector3 position, UnityEngine.Vector3 lookAt)[] Shots =
        {
            (0f, new UnityEngine.Vector3(0f, 1.3f, 7.6f), new UnityEngine.Vector3(0f, 1.4f, -1f)),
            (1.9f, new UnityEngine.Vector3(0f, 1.35f, 6.5f), new UnityEngine.Vector3(0f, 1.35f, -0.6f)),
            (2.55f, new UnityEngine.Vector3(0f, 1.35f, 6.4f), new UnityEngine.Vector3(0f, 1.35f, -0.6f)),
            (2.95f, new UnityEngine.Vector3(0f, 0.75f, 8.3f), new UnityEngine.Vector3(0f, 1.75f, -1.2f)),
            (6.8f, new UnityEngine.Vector3(0f, 0.8f, 8.0f), new UnityEngine.Vector3(0f, 1.7f, -1.2f)),
            (7.8f, new UnityEngine.Vector3(0f, 1.3f, 7.6f), new UnityEngine.Vector3(0f, 1.4f, -1f)),
        };

        static PoseBuilder Idle() => PoseBuilder.Start().Arms(abduct: 8f, elbow: 10f).Legs(abduct: 3f);

        /// <summary>The A-pose held during the suit-up; also used to bake the panel emitter's mesh.</summary>
        public static PoseBuilder SuitUpPose() => PoseBuilder.Start().Arms(abduct: 28f, elbow: 12f).Legs(abduct: 6f);

        static PoseBuilder Call() => PoseBuilder.Start()
            .Arm(Side.Right, abduct: 25f, flex: 75f, elbow: 115f)
            .Arm(Side.Left, abduct: 35f, flex: -25f, elbow: 40f)
            .Legs(abduct: 9f).Torso(twist: 10f).Head(pitch: -5f);

        static PoseBuilder Thrust() => PoseBuilder.Start()
            .Arm(Side.Right, abduct: 10f, flex: 170f, elbow: 5f)
            .Arm(Side.Left, abduct: 30f, flex: -10f, elbow: 85f)
            .Legs(abduct: 9f).Torso(lean: -6f).Head(pitch: -20f);

        static PoseBuilder Crouch() => PoseBuilder.Start().Root(y: -0.09f)
            .Arms(abduct: 15f, flex: 30f, elbow: 95f)
            .Legs(flex: 28f, abduct: 8f, knee: 50f, ankle: -20f)
            .Torso(lean: 15f).Head(pitch: 8f);

        /// <summary>
        /// Each ranger's pose, built to differ in height, facing and shape so the line-up reads as five characters.
        /// The outer rangers turn in toward the leader (Pink and Blue stand at +X, so they turn with negative yaw).
        /// <paramref name="settle"/> is a slightly shifted version held later, so the pose breathes instead of freezing.
        /// </summary>
        static PoseBuilder Signature(int index, bool settle = false)
        {
            float s = settle ? 1f : 0f;
            switch (index)
            {
                case 0: // Pink: on one leg with the other knee lifted, arms framing a heart overhead
                    return PoseBuilder.Start().Root(yaw: -22f, y: 0.01f)
                        .Arms(abduct: 150f, flex: 15f, elbow: 75f + 5f * s)
                        .Leg(Side.Left, abduct: 4f, knee: 4f)
                        .Leg(Side.Right, flex: 70f, abduct: 10f, knee: 95f, ankle: 20f)
                        .Torso(side: 9f, twist: -8f).Head(roll: 12f - 4f * s, yaw: 10f + 8f * s, pitch: -5f);
                case 1: // Blue: deep side lunge with both arms in one long diagonal line
                    return PoseBuilder.Start().Root(yaw: -28f, x: 0.15f, y: -0.18f)
                        .Arm(Side.Right, abduct: 140f, elbow: 0f)
                        .Arm(Side.Left, abduct: 40f, flex: -5f, elbow: 0f)
                        .Leg(Side.Left, flex: 45f, abduct: 18f, knee: 80f, ankle: -15f)
                        .Leg(Side.Right, abduct: 38f, knee: 4f)
                        .Torso(side: 22f - 3f * s, lean: 6f).Head(yaw: -25f + 6f * s, pitch: -12f);
                case 2: // Red (leader): fist driven straight at the camera, other arm drawn back, low wide stance
                    return PoseBuilder.Start().Root(y: -0.12f)
                        .Arm(Side.Right, flex: 88f, swing: 4f, elbow: 0f)
                        .Arm(Side.Left, abduct: 55f, flex: -35f, elbow: 25f)
                        .Legs(flex: 22f, abduct: 24f, knee: 45f, ankle: -12f)
                        .Torso(lean: 12f + 2f * s, twist: -18f).Head(pitch: -6f);
                case 3: // Yellow: down on one knee, fist punched up to the sky
                    return PoseBuilder.Start().Root(yaw: 20f, y: -0.43f)
                        .Arm(Side.Right, abduct: 20f, flex: 165f, elbow: 0f)
                        .Arm(Side.Left, abduct: 15f, flex: 55f, elbow: 70f)
                        .Leg(Side.Left, flex: 88f, abduct: 6f, knee: 90f, ankle: 0f)
                        .Leg(Side.Right, flex: -12f, abduct: 6f, knee: 102f, ankle: 25f)
                        .Torso(lean: -4f, twist: 6f).Head(pitch: -24f + 6f * s);
                default: // Green: crane stance, side kick held high, arms spread like wings
                    return PoseBuilder.Start().Root(yaw: 26f, y: 0.02f)
                        .Arm(Side.Right, abduct: 95f, flex: 10f, elbow: 10f + 10f * s)
                        .Arm(Side.Left, abduct: 75f, flex: 40f, elbow: 70f)
                        .Leg(Side.Left, abduct: -3f, knee: 6f)
                        .Leg(Side.Right, abduct: 70f - 6f * s, flex: 10f, knee: 12f)
                        .Torso(side: -18f, lean: -4f).Head(yaw: 22f, pitch: -4f);
            }
        }

        public static PoseClip ForRanger(int index)
        {
            float stagger = (index == 2 ? 0f : 0.04f * System.Math.Abs(index - 2));
            return new PoseClip()
                .Key(0f, Idle())
                .Key(0.25f + stagger, Call())
                .Hold(0.55f)
                .Key(0.8f, Thrust())
                .Hold(1.25f)
                .Key(1.5f, SuitUpPose())
                .Hold(2.45f)
                .Key(2.6f, Crouch())
                .Key(2.85f + stagger, Signature(index))
                .Hold(4.2f)
                .Key(5.2f, Signature(index, settle: true))
                .Hold(6.6f)
                .Key(7.4f, Idle())
                .Hold(Length);
        }
    }
}
