using ShowcaseCharacters;
using UnityEngine;
using Pose = ShowcaseCharacters.Pose;

namespace ElementalBending
{
    /// <summary>
    /// Timeline for the bending showcase: one bender moves through water, earth, fire and air (five seconds each),
    /// then bows while the four elements orbit them. All times are seconds from the start of the sequence.
    /// Positive torso twist turns the bender toward their right (+X); the pool is on their left (-X).
    /// </summary>
    public static class BendingChoreography
    {
        public const float Water = 0f;
        public const float Earth = 5f;
        public const float Fire = 10f;
        public const float Air = 15f;
        public const float Finale = 20f;
        public const float Length = 23.5f;

        public static readonly (float start, string title)[] Segments =
        {
            (Water, "WATER"), (Earth, "EARTH"), (Fire, "FIRE"), (Air, "AIR"), (Finale, ""),
        };

        /// <summary>Camera shots: time, camera position, look-at point. The camera eases from one shot to the next.</summary>
        public static readonly (float time, Vector3 position, Vector3 lookAt)[] Shots =
        {
            (Water, new Vector3(4.4f, 1.7f, 5.4f), new Vector3(-0.6f, 1.05f, 0.6f)),
            (Earth - 0.2f, new Vector3(6.4f, 1.1f, 3.2f), new Vector3(-0.2f, 1.0f, 2.8f)),
            (Fire - 0.2f, new Vector3(-2.4f, 1.3f, 5.4f), new Vector3(0.3f, 1.15f, 1.6f)),
            (Air - 0.2f, new Vector3(3.6f, 2.0f, 5.2f), new Vector3(0f, 1.2f, 0.8f)),
            (Finale - 0.2f, new Vector3(0.6f, 1.5f, 4.3f), new Vector3(0f, 1.2f, 0f)),
        };

        /// <summary>Moments that shake the camera: time and strength.</summary>
        public static readonly (float time, float strength)[] Impacts =
        {
            (Water + 2.85f, 0.03f), (Earth + 1.05f, 0.09f), (Earth + 2.25f, 0.05f),
            (Fire + 1.45f, 0.03f), (Fire + 3.15f, 0.07f), (Air + 2.3f, 0.06f),
        };

        static PoseBuilder Neutral() => PoseBuilder.Start().Arms(abduct: 8f, elbow: 12f).Legs(abduct: 4f);

        public static PoseClip Build()
        {
            var clip = new PoseClip();
            clip.Key(0f, Neutral());

            // WATER: draw water from the pool on the left, swirl it overhead, lash it forward as a whip.
            float t = Water;
            clip.Key(t + 0.4f, PoseBuilder.Start().Root(y: -0.04f).Legs(flex: 12f, abduct: 14f, knee: 22f).Arms(abduct: 20f, flex: 30f, elbow: 40f));
            clip.Key(t + 1.0f, PoseBuilder.Start().Root(y: -0.07f).Torso(lean: 25f, twist: -50f).Head(pitch: 15f, yaw: -20f)
                .Legs(flex: 15f, abduct: 16f, knee: 30f)
                .Arm(Side.Left, abduct: 40f, flex: 30f, elbow: 20f, wrist: 20f)
                .Arm(Side.Right, flex: 55f, swing: 60f, elbow: 20f, wrist: 20f));
            clip.Key(t + 1.8f, PoseBuilder.Start().Root(y: -0.02f).Torso(lean: -5f, twist: -20f).Head(pitch: -20f)
                .Legs(flex: 8f, abduct: 14f, knee: 12f)
                .Arms(abduct: 60f, flex: 120f, elbow: 30f));
            clip.Key(t + 2.35f, PoseBuilder.Start().Root(y: -0.06f).Torso(twist: 35f).Head(yaw: -25f)
                .Legs(flex: 15f, abduct: 18f, knee: 28f)
                .Arm(Side.Right, abduct: 80f, flex: -30f, swing: -40f, elbow: 30f)
                .Arm(Side.Left, flex: 70f, elbow: 60f));
            clip.Key(t + 2.75f, PoseBuilder.Start().Root(y: -0.08f).Torso(lean: 10f, twist: -25f)
                .Leg(Side.Left, flex: 30f, abduct: 10f, knee: 32f).Leg(Side.Right, flex: -10f, abduct: 14f, knee: 18f)
                .Arm(Side.Right, flex: 95f, swing: 10f, elbow: 0f)
                .Arm(Side.Left, abduct: 50f, flex: -20f, elbow: 20f));
            clip.Hold(t + 3.5f);
            clip.Key(t + 4.4f, PoseBuilder.Start().Root(y: -0.04f).Legs(flex: 12f, abduct: 14f, knee: 22f).Arms(abduct: 20f, flex: 30f, elbow: 40f));

            // EARTH: horse stance, knee raise and stomp, lift a boulder, punch it forward.
            t = Earth;
            PoseBuilder horse = PoseBuilder.Start().Root(y: -0.11f).Legs(flex: 22f, abduct: 22f, knee: 45f, ankle: -10f).Arms(abduct: 25f, flex: -10f, elbow: 100f);
            clip.Key(t + 0.3f, horse);
            clip.Key(t + 0.65f, PoseBuilder.Start().Torso(lean: -5f)
                .Leg(Side.Left, flex: 85f, abduct: 8f, knee: 95f).Leg(Side.Right, abduct: 6f, knee: 8f)
                .Arms(abduct: 70f, elbow: 60f));
            clip.Key(t + 1.0f, PoseBuilder.Start().Root(y: -0.17f).Torso(lean: 20f)
                .Legs(flex: 28f, abduct: 22f, knee: 52f, ankle: -12f)
                .Arms(abduct: 30f, flex: 20f, elbow: 10f));
            clip.Key(t + 1.6f, PoseBuilder.Start().Root(y: -0.11f).Legs(flex: 22f, abduct: 22f, knee: 45f, ankle: -10f)
                .Arms(abduct: 25f, flex: 70f, elbow: 70f, wrist: -20f).Head(pitch: -5f));
            clip.Key(t + 2.0f, PoseBuilder.Start().Root(y: -0.11f).Torso(twist: 30f).Legs(flex: 22f, abduct: 22f, knee: 45f, ankle: -10f)
                .Arm(Side.Right, abduct: 20f, flex: -40f, elbow: 120f)
                .Arm(Side.Left, flex: 70f, elbow: 30f));
            clip.Key(t + 2.2f, PoseBuilder.Start().Root(y: -0.12f).Torso(lean: 10f, twist: -30f)
                .Leg(Side.Left, flex: 35f, abduct: 18f, knee: 50f).Leg(Side.Right, flex: 5f, abduct: 22f, knee: 25f)
                .Arm(Side.Right, flex: 90f, elbow: 0f)
                .Arm(Side.Left, abduct: 30f, flex: -20f, elbow: 100f));
            clip.Hold(t + 3.0f);
            clip.Key(t + 4.4f, Neutral());

            // FIRE: fighting stance, three punches with fire jets, a spinning kick, then a two-handed blast.
            t = Fire;
            PoseBuilder guard = PoseBuilder.Start().Root(y: -0.05f).Torso(twist: 20f)
                .Leg(Side.Left, flex: 25f, abduct: 6f, knee: 22f).Leg(Side.Right, flex: -15f, abduct: 8f, knee: 15f)
                .Arm(Side.Left, flex: 60f, swing: 20f, elbow: 90f).Arm(Side.Right, flex: 40f, elbow: 115f);
            clip.Key(t + 0.3f, guard);
            clip.Key(t + 0.5f, PoseBuilder.Start().Root(y: -0.05f).Torso(twist: -20f)
                .Leg(Side.Left, flex: 25f, abduct: 6f, knee: 22f).Leg(Side.Right, flex: -15f, abduct: 8f, knee: 15f)
                .Arm(Side.Right, flex: 90f, swing: 15f, elbow: 0f).Arm(Side.Left, flex: 50f, elbow: 110f));
            clip.Hold(t + 0.64f);
            clip.Key(t + 0.78f, guard);
            clip.Key(t + 0.95f, PoseBuilder.Start().Root(y: -0.05f).Torso(twist: 25f)
                .Leg(Side.Left, flex: 25f, abduct: 6f, knee: 22f).Leg(Side.Right, flex: -15f, abduct: 8f, knee: 15f)
                .Arm(Side.Left, flex: 90f, swing: 15f, elbow: 0f).Arm(Side.Right, flex: 40f, elbow: 115f));
            clip.Hold(t + 1.09f);
            clip.Key(t + 1.22f, guard);
            clip.Key(t + 1.42f, PoseBuilder.Start().Root(y: -0.07f).Torso(lean: 10f, twist: -35f)
                .Leg(Side.Left, flex: 30f, abduct: 6f, knee: 28f).Leg(Side.Right, flex: -18f, abduct: 8f, knee: 12f)
                .Arm(Side.Right, flex: 92f, elbow: 0f).Arm(Side.Left, abduct: 20f, flex: -10f, elbow: 100f));
            clip.Hold(t + 1.57f);
            clip.Key(t + 1.85f, guard);
            clip.Key(t + 2.05f, PoseBuilder.Start().Root(yaw: 40f).Leg(Side.Right, flex: 70f, knee: 100f).Arms(abduct: 30f, elbow: 90f));
            clip.Key(t + 2.3f, PoseBuilder.Start().Root(yaw: 150f, y: 0.05f).Torso(side: -20f)
                .Leg(Side.Right, abduct: 80f, knee: 8f).Leg(Side.Left, abduct: -4f, knee: 10f).Arms(abduct: 55f, elbow: 30f));
            clip.Key(t + 2.55f, PoseBuilder.Start().Root(yaw: 270f, y: 0.05f).Torso(side: -20f)
                .Leg(Side.Right, abduct: 80f, knee: 8f).Leg(Side.Left, abduct: -4f, knee: 10f).Arms(abduct: 55f, elbow: 30f));
            clip.Key(t + 2.85f, PoseBuilder.Start().Root(yaw: 360f).Legs(flex: 20f, abduct: 16f, knee: 35f).Arms(abduct: 30f, flex: -20f, elbow: 100f));
            clip.Key(t + 3.15f, PoseBuilder.Start().Root(yaw: 360f, y: -0.12f).Torso(lean: 12f)
                .Legs(flex: 25f, abduct: 22f, knee: 48f, ankle: -10f).Arms(abduct: 12f, flex: 85f, elbow: 8f, wrist: -50f));
            clip.Hold(t + 3.9f);
            clip.Key(t + 4.6f, PoseBuilder.Start().Root(yaw: 360f).Arms(abduct: 8f, elbow: 12f).Legs(abduct: 4f));

            // AIR: gather air, spin up into a levitating vortex, land, thrust both palms forward.
            t = Air;
            clip.Key(t + 0.0f, PoseBuilder.Start().Arms(abduct: 8f, elbow: 12f).Legs(abduct: 4f));
            clip.Key(t + 0.4f, PoseBuilder.Start().Arms(abduct: 90f, flex: 40f, elbow: 30f).Legs(abduct: 10f, knee: 10f));
            clip.Key(t + 1.0f, PoseBuilder.Start().Root(yaw: 360f, y: 0.35f).Head(pitch: -10f)
                .Arms(abduct: 85f, flex: 20f, elbow: 10f).Legs(flex: 25f, knee: 40f, abduct: 6f));
            clip.Key(t + 1.6f, PoseBuilder.Start().Root(yaw: 720f, y: 0.3f).Head(pitch: -10f)
                .Arms(abduct: 85f, flex: 20f, elbow: 10f).Legs(flex: 25f, knee: 40f, abduct: 6f));
            clip.Key(t + 1.95f, PoseBuilder.Start().Root(yaw: 720f, y: -0.1f)
                .Legs(flex: 30f, abduct: 14f, knee: 52f, ankle: -12f).Arms(abduct: 30f, flex: -30f, elbow: 90f));
            clip.Key(t + 2.3f, PoseBuilder.Start().Root(yaw: 720f, y: -0.08f).Torso(lean: 15f)
                .Leg(Side.Left, flex: 40f, abduct: 8f, knee: 42f).Leg(Side.Right, flex: -20f, abduct: 10f, knee: 12f)
                .Arms(abduct: 12f, flex: 90f, elbow: 0f, wrist: -60f));
            clip.Hold(t + 3.3f);
            clip.Key(t + 4.3f, PoseBuilder.Start().Root(yaw: 720f).Arms(abduct: 8f, elbow: 12f).Legs(abduct: 4f));

            // FINALE: fist-to-palm bow while the four elements circle.
            t = Finale;
            clip.Key(t + 0.6f, PoseBuilder.Start().Root(yaw: 720f).Arms(flex: 55f, swing: 35f, elbow: 95f).Legs(abduct: 4f));
            clip.Key(t + 1.4f, PoseBuilder.Start().Root(yaw: 720f).Torso(lean: 28f).Head(pitch: 15f).Arms(flex: 55f, swing: 35f, elbow: 95f).Legs(abduct: 4f));
            clip.Hold(t + 2.2f);
            clip.Key(t + 3.0f, PoseBuilder.Start().Root(yaw: 720f).Arms(abduct: 8f, elbow: 12f).Legs(abduct: 4f));
            clip.Hold(Length);
            return clip;
        }

        public static Pose StartPose() => Neutral().Build();
    }
}
