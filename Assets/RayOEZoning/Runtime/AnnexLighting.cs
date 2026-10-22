using UnityEngine;
using UnityEngine.Rendering;

namespace RayOEZoning
{
    /// <summary>Reapplies the after-hours ambient on player load: dark enough that the flashlight leads, never pitch black.</summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class AnnexLighting : MonoBehaviour
    {
        public static readonly Color Ambient = new Color(.085f, .097f, .118f);
        public static readonly Color Sky = new Color(.06f, .07f, .088f), Ground = new Color(.115f, .125f, .145f);

        /// <summary>Ambient probe with floor bounce reaching ceilings and upper walls.</summary>
        public static SphericalHarmonicsL2 Probe()
        {
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(Ambient * .82f);
            probe.AddDirectionalLight(Vector3.down, Ground - Ambient * .82f, .9f);
            probe.AddDirectionalLight(Vector3.up, Sky - Ambient * .82f, .9f);
            return probe;
        }

        void OnEnable()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Sky; RenderSettings.ambientEquatorColor = Ambient; RenderSettings.ambientGroundColor = Ground;
            RenderSettings.ambientLight = Ambient;
            RenderSettings.ambientIntensity = 1;
            RenderSettings.reflectionIntensity = .35f;
            // URP reads the spherical harmonics probe for unbaked surfaces. An empty
            // probe erases the authored flat ambient and leaves equipment silhouettes.
            RenderSettings.ambientProbe = Probe();
        }
    }
}
