using UnityEngine;
namespace LaserCorridor
{
    [CreateAssetMenu(menuName="Laser Corridor/Panel Effect")]
    public sealed class PanelEffectDefinition : ScriptableObject
    {
        public int iconCell;
        public PanelEffect effect;
        public string displayName;
        public Color color = Color.cyan;
        public bool harmful;
        public float duration;
        public int amount;
    }
}
