using UnityEngine;
namespace LaserCorridor
{
    [CreateAssetMenu(menuName="Laser Corridor/Laser Pattern")]
    public sealed class LaserPatternDefinition : ScriptableObject
    {
        public PatternFamily family;
        public string displayName;
        public bool allowMirror = true;
        public bool allowAdvancedVariants = true;
        [Range(.1f,3)] public float selectionWeight = 1;
        public float safeOpeningWidth = 1.5f;
        public DodgePose pose;
        public Rect opening;
        [Range(0,1)] public float difficulty;
    }
}
