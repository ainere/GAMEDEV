using UnityEngine;
namespace LaserCorridor
{
    [CreateAssetMenu(menuName="Laser Corridor/Run Settings")]
    public sealed class RunSettings : ScriptableObject
    {
        public float duration = 90, corridorLength = 14, corridorWidth = 4, corridorHeight = 4.2f;
        public float walkSpeed = 4.5f, crouchSpeed = 3, jumpHeight = 1.1f, gravity = 18;
        public float standingHeight = 1.8f, crouchingHeight = 1, playerRadius = .3f;
        public float gateZ = 12, emitterZ = 13.35f, minimumZ = 1;
        public int maximumHealth = 100, laserDamage = 25, healAmount = 25, shockDamage = 15;
        public float damageGrace = .75f;
        public float shieldDuration = 10, laserSlowDuration = 5, movementSlowDuration = 4, slowMultiplier = .65f;
        public float panelCycle = 9, panelRamp = 2, panelActive = 6, effectCooldown = 24, pickupCooldown = 6;
        public float encounterLead = 1.75f, reactionAllowance = .35f, burstRecovery = .75f;
        public float[] waveSpeeds = { 3.5f, 4.5f, 5.5f, 7 };
        public float beamRadius = .045f, safetyMargin = .08f;
        public float tilePitch=.8f,tileSeam=.03f;
        public float minimumWaveSpeed=3.5f,maximumWaveSpeed=7;
        public float advancedPatternsAfter=6,patternUnlockDuration=32,patternRampDuration=70;
        public float pairedBurstsAfter=14,tripleBurstsAfter=42,minimumLaneChange=.55f;
        public int patternPlanAttempts=12,patternHistoryLength=6;
    }
}
