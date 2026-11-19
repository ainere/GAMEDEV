using ShowcaseCharacters;
using UnityEngine;
using Pose = ShowcaseCharacters.Pose;

namespace FinalBoss
{
    /// <summary>
    /// Timeline for the boss on the throne. The boss wakes, lifts their head, grips the armrests, leans in, and
    /// at the climax throws their arms wide as the aura maxes out. Aura strength follows <see cref="AuraIntensity"/>.
    /// </summary>
    public static class BossChoreography
    {
        public const float EyesIgnite = 2.6f;
        public const float AuraStart = 3.2f;
        public const float Climax = 7.0f;
        public const float Length = 15f;

        /// <summary>Root drop that sits the figure's hips on the seat.</summary>
        const float SeatDrop = -0.5f;

        static PoseBuilder Seated() => PoseBuilder.Start().Root(y: SeatDrop)
            .Legs(flex: 88f, abduct: 13f, knee: 92f, ankle: 8f);

        static PoseBuilder Resting(PoseBuilder pose) => pose.Arms(abduct: 24f, flex: 8f, elbow: 82f, wrist: 10f);

        /// <summary>The pose baked into the aura's emitter mesh.</summary>
        public static PoseBuilder RegalPose() => Resting(Seated()).Head(pitch: 8f);

        public static PoseClip Build()
        {
            return new PoseClip()
                .Key(0f, Resting(Seated()).Torso(lean: 18f).Head(pitch: 40f))
                .Hold(1.9f)
                .Key(3.0f, Resting(Seated()).Torso(lean: 6f).Head(pitch: 10f))
                .Key(4.2f, Seated().Arms(abduct: 24f, flex: 8f, elbow: 86f, wrist: -15f).Torso(lean: -4f).Head(pitch: 12f))
                .Key(5.4f, Seated().Torso(lean: 14f).Head(pitch: 16f, yaw: -6f)
                    .Arm(Side.Left, abduct: 24f, flex: 8f, elbow: 84f, wrist: -15f)
                    .Arm(Side.Right, abduct: 22f, flex: 55f, elbow: 70f))
                .Hold(6.1f)
                .Key(6.75f, Seated().Torso(lean: 22f).Head(pitch: 28f).Arms(abduct: 18f, flex: 30f, elbow: 110f))
                .Key(Climax, Seated().Torso(lean: -12f).Head(pitch: -18f).Arms(abduct: 72f, flex: 22f, elbow: 25f, wrist: -20f))
                .Hold(8.2f)
                .Key(9.6f, RegalPose())
                .Hold(Length);
        }

        /// <summary>0 before the aura starts, building to about half before the climax, then full.</summary>
        public static float AuraIntensity(float time)
        {
            if (time < AuraStart)
                return 0f;
            if (time < 6f)
                return Mathf.SmoothStep(0f, 0.45f, (time - AuraStart) / (6f - AuraStart));
            if (time < Climax - 0.1f)
                return Mathf.Lerp(0.45f, 0.55f, (time - 6f) / (Climax - 0.1f - 6f));
            return Mathf.SmoothStep(0.55f, 1f, (time - (Climax - 0.1f)) / 0.15f);
        }

        public static float EyeGlow(float time) => Mathf.SmoothStep(0f, 1f, (time - EyesIgnite + 0.1f) / 0.4f);

        public static Pose StartPose() => Build().Evaluate(0f).Clone();
    }
}
