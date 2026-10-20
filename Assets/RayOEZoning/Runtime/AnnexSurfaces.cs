using UnityEngine;

namespace RayOEZoning
{
    /// <summary>Retains original material/texture references while dimming the old luminous panels.</summary>
    [ExecuteAlways]
    public sealed class AnnexSurfaces : MonoBehaviour
    {
        public Renderer[] targets;
        void OnEnable() { Apply(); }
        public void Apply()
        {
            if (targets == null) return;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in targets)
            {
                if (!renderer) continue;
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i]; if (!material) continue;
                    renderer.GetPropertyBlock(block, i);
                    if (material.name.StartsWith("Backlit wall slats", System.StringComparison.Ordinal))
                    {
                        var wallColor = new Color(.11f, .18f, .20f, 1);
                        block.SetColor("_BaseColor", wallColor); block.SetColor("_Color", wallColor);
                    }
                    // The survival room's self-lit walls and plates flattened every light pool.
                    // Keep their textures/material assets while restoring a readable dark annex.
                    if (material.HasProperty("_EmissionColor"))
                    {
                        var color = material.GetColor("_EmissionColor");
                        float brightness = Mathf.Max(color.r, color.g, color.b);
                        float dim = brightness > .7f ? .010f : .060f;
                        block.SetColor("_EmissionColor", color * dim);
                    }
                    renderer.SetPropertyBlock(block, i); block.Clear();
                }
            }
        }
    }
}
