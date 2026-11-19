using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShowcaseCharacters
{
    /// <summary>Joints of the posable figure. Root is the character's ground point; every other joint hangs from it.</summary>
    public enum Joint
    {
        Root, Hips, Spine, Chest, Neck, Head,
        UpperArmL, ForearmL, HandL,
        UpperArmR, ForearmR, HandR,
        ThighL, ShinL, FootL,
        ThighR, ShinR, FootR,
    }

    public enum Side { Left = -1, Right = 1 }

    /// <summary>A full-body pose: one local rotation per joint plus an offset and yaw for the root.</summary>
    public sealed class Pose
    {
        public static readonly int JointCount = Enum.GetValues(typeof(Joint)).Length;

        public readonly Quaternion[] Rotations = new Quaternion[JointCount];
        public Vector3 RootOffset;
        public float RootYaw;

        public Pose()
        {
            for (int i = 0; i < JointCount; i++)
                Rotations[i] = Quaternion.identity;
        }

        public Pose Clone()
        {
            var copy = new Pose { RootOffset = RootOffset, RootYaw = RootYaw };
            Array.Copy(Rotations, copy.Rotations, JointCount);
            return copy;
        }

        public static void Blend(Pose a, Pose b, float t, Pose result)
        {
            for (int i = 0; i < JointCount; i++)
                result.Rotations[i] = Quaternion.Slerp(a.Rotations[i], b.Rotations[i], t);
            result.RootOffset = Vector3.LerpUnclamped(a.RootOffset, b.RootOffset, t);
            result.RootYaw = Mathf.LerpUnclamped(a.RootYaw, b.RootYaw, t);
        }
    }

    /// <summary>
    /// Describes a pose in choreography terms (degrees). The figure faces +Z; its right side is +X.
    /// Arms: abduct raises sideways, flex raises forward, swing turns the raised arm toward the front, elbow bends the forearm forward.
    /// Legs: flex lifts the thigh forward, knee bends the shin back. Torso: lean tips forward, twist turns toward the figure's right.
    /// </summary>
    public sealed class PoseBuilder
    {
        readonly Pose pose = new Pose();

        public static PoseBuilder Start() => new PoseBuilder();

        public PoseBuilder Root(float yaw = 0f, float x = 0f, float y = 0f, float z = 0f)
        {
            pose.RootYaw = yaw;
            pose.RootOffset = new Vector3(x, y, z);
            return this;
        }

        public PoseBuilder Hips(float pitch = 0f, float yaw = 0f, float roll = 0f)
        {
            pose.Rotations[(int)Joint.Hips] = Quaternion.Euler(pitch, yaw, roll);
            return this;
        }

        public PoseBuilder Torso(float lean = 0f, float twist = 0f, float side = 0f)
        {
            Quaternion half = Quaternion.Euler(lean * 0.5f, twist * 0.5f, -side * 0.5f);
            pose.Rotations[(int)Joint.Spine] = half;
            pose.Rotations[(int)Joint.Chest] = half;
            return this;
        }

        public PoseBuilder Head(float pitch = 0f, float yaw = 0f, float roll = 0f)
        {
            pose.Rotations[(int)Joint.Neck] = Quaternion.Euler(pitch * 0.4f, yaw * 0.4f, roll * 0.4f);
            pose.Rotations[(int)Joint.Head] = Quaternion.Euler(pitch * 0.6f, yaw * 0.6f, roll * 0.6f);
            return this;
        }

        public PoseBuilder Arm(Side side, float abduct = 0f, float flex = 0f, float swing = 0f, float elbow = 0f, float wrist = 0f)
        {
            float s = (float)side;
            Quaternion upper = Quaternion.AngleAxis(-s * swing, Vector3.up)
                               * Quaternion.AngleAxis(-flex, Vector3.right)
                               * Quaternion.AngleAxis(s * abduct, Vector3.forward);
            bool right = side == Side.Right;
            pose.Rotations[(int)(right ? Joint.UpperArmR : Joint.UpperArmL)] = upper;
            pose.Rotations[(int)(right ? Joint.ForearmR : Joint.ForearmL)] = Quaternion.AngleAxis(-elbow, Vector3.right);
            pose.Rotations[(int)(right ? Joint.HandR : Joint.HandL)] = Quaternion.AngleAxis(-wrist, Vector3.right);
            return this;
        }

        public PoseBuilder Leg(Side side, float flex = 0f, float abduct = 0f, float knee = 0f, float ankle = 0f, float turn = 0f)
        {
            float s = (float)side;
            Quaternion thigh = Quaternion.AngleAxis(-s * turn, Vector3.up)
                               * Quaternion.AngleAxis(-flex, Vector3.right)
                               * Quaternion.AngleAxis(s * abduct, Vector3.forward);
            bool right = side == Side.Right;
            pose.Rotations[(int)(right ? Joint.ThighR : Joint.ThighL)] = thigh;
            pose.Rotations[(int)(right ? Joint.ShinR : Joint.ShinL)] = Quaternion.AngleAxis(knee, Vector3.right);
            pose.Rotations[(int)(right ? Joint.FootR : Joint.FootL)] = Quaternion.AngleAxis(-ankle, Vector3.right);
            return this;
        }

        /// <summary>Applies the same arm settings to both sides.</summary>
        public PoseBuilder Arms(float abduct = 0f, float flex = 0f, float swing = 0f, float elbow = 0f, float wrist = 0f) =>
            Arm(Side.Left, abduct, flex, swing, elbow, wrist).Arm(Side.Right, abduct, flex, swing, elbow, wrist);

        /// <summary>Applies the same leg settings to both sides.</summary>
        public PoseBuilder Legs(float flex = 0f, float abduct = 0f, float knee = 0f, float ankle = 0f, float turn = 0f) =>
            Leg(Side.Left, flex, abduct, knee, ankle, turn).Leg(Side.Right, flex, abduct, knee, ankle, turn);

        public Pose Build() => pose.Clone();
    }

    /// <summary>A timed list of poses. Each transition eases in and out.</summary>
    public sealed class PoseClip
    {
        readonly List<(float time, Pose pose)> keys = new List<(float, Pose)>();
        readonly Pose scratch = new Pose();

        public float Length => keys.Count == 0 ? 0f : keys[keys.Count - 1].time;

        /// <summary>Adds a key. Keys must be added in time order.</summary>
        public PoseClip Key(float time, PoseBuilder pose) => Key(time, pose.Build());

        public PoseClip Key(float time, Pose pose)
        {
            if (keys.Count > 0 && time < keys[keys.Count - 1].time)
                throw new ArgumentException($"Pose key at {time}s is earlier than the previous key.");
            keys.Add((time, pose));
            return this;
        }

        /// <summary>Holds the previous pose until the given time.</summary>
        public PoseClip Hold(float time) => Key(time, keys[keys.Count - 1].pose);

        public Pose Evaluate(float time)
        {
            if (keys.Count == 0)
                return scratch;
            if (time <= keys[0].time)
                return keys[0].pose;
            for (int i = 1; i < keys.Count; i++)
            {
                if (time > keys[i].time)
                    continue;
                (float t0, Pose a) = keys[i - 1];
                (float t1, Pose b) = keys[i];
                float u = t1 > t0 ? Mathf.Clamp01((time - t0) / (t1 - t0)) : 1f;
                u = u * u * (3f - 2f * u);
                Pose.Blend(a, b, u, scratch);
                return scratch;
            }
            return keys[keys.Count - 1].pose;
        }
    }
}
