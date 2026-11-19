using UnityEngine;

namespace ShowcaseCharacters
{
    /// <summary>Holds the joint transforms of a posable figure and applies poses to them.</summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        [SerializeField] Transform[] joints = new Transform[Pose.JointCount];
        [SerializeField] Quaternion[] restRotations = new Quaternion[Pose.JointCount];
        [SerializeField] Vector3 rootRestPosition;

        public Transform GetJoint(Joint joint) => joints[(int)joint];

        /// <summary>Called by the editor builder after it creates the joints.</summary>
        public void Configure(Transform[] jointTransforms)
        {
            joints = jointTransforms;
            restRotations = new Quaternion[joints.Length];
            for (int i = 0; i < joints.Length; i++)
                restRotations[i] = joints[i].localRotation;
            rootRestPosition = joints[(int)Joint.Root].localPosition;
        }

        public void ApplyPose(Pose pose)
        {
            for (int i = 1; i < joints.Length; i++)
                joints[i].localRotation = restRotations[i] * pose.Rotations[i];

            Transform root = joints[(int)Joint.Root];
            root.localPosition = rootRestPosition + pose.RootOffset;
            root.localRotation = restRotations[0] * Quaternion.AngleAxis(pose.RootYaw, Vector3.up) * pose.Rotations[0];
        }
    }
}
