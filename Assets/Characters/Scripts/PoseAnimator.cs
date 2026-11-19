using UnityEngine;

namespace ShowcaseCharacters
{
    /// <summary>
    /// Plays a <see cref="PoseClip"/> on a <see cref="CharacterRig"/>, with a slight breathing motion layered on top
    /// so held poses do not look frozen. Scene controllers set the clip and drive the time.
    /// </summary>
    [RequireComponent(typeof(CharacterRig))]
    public sealed class PoseAnimator : MonoBehaviour
    {
        [SerializeField, Min(0f)] float breathingDegrees = 1.5f;
        [SerializeField, Min(0f)] float breathingRate = 1.6f;

        CharacterRig rig;
        PoseClip clip;
        float phaseOffset;

        public float Time { get; set; }

        void Awake()
        {
            rig = GetComponent<CharacterRig>();
            phaseOffset = Random.value * 10f;
        }

        public void Play(PoseClip newClip, float startTime = 0f)
        {
            clip = newClip;
            Time = startTime;
            Apply();
        }

        void LateUpdate()
        {
            if (clip == null)
                return;
            Apply();
        }

        void Apply()
        {
            if (rig == null)
                rig = GetComponent<CharacterRig>();
            Pose pose = clip.Evaluate(Time);
            rig.ApplyPose(pose);

            float breath = Mathf.Sin((UnityEngine.Time.time + phaseOffset) * breathingRate) * breathingDegrees;
            Transform chest = rig.GetJoint(Joint.Chest);
            chest.localRotation *= Quaternion.Euler(-breath, 0f, 0f);
        }
    }
}
