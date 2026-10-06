using UnityEngine;
namespace LaserCorridor
{
    [CreateAssetMenu(menuName="Laser Corridor/Laser Burst")]
    public sealed class LaserBurstDefinition : ScriptableObject
    {
        public string displayName;
        public PatternFamily[] patterns;
        public float[] speeds;
        public float earliestElapsed;
    }
}
