using UnityEngine;
namespace LaserCorridor
{
    public sealed class LightPanelFlicker:MonoBehaviour
    {
        public Color baseEmission;
        Renderer surface;MaterialPropertyBlock block;
        void Awake(){surface=GetComponent<Renderer>();block=new MaterialPropertyBlock();}
        void Update(){surface.GetPropertyBlock(block);float t=Time.unscaledTime;float dip=.975f-.025f*Mathf.Sin(t*.8f)-.03f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*.57f)),8);block.SetColor("_EmissionColor",baseEmission*dip);surface.SetPropertyBlock(block);}
    }
}
