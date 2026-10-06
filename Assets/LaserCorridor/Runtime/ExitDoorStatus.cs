using UnityEngine;
namespace LaserCorridor
{
    public sealed class ExitDoorStatus:MonoBehaviour
    {
        public Renderer statusRenderer;
        RunController run;MaterialPropertyBlock block;
        void Start(){run=FindAnyObjectByType<RunController>();block=new MaterialPropertyBlock();}
        void LateUpdate(){if(!run)return;bool open=run.Model.State==RunState.ExitOpen||run.Model.State==RunState.Won;var color=open?new Color(.2f,1,.4f):new Color(1,.02f,.04f);block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color*2);statusRenderer.SetPropertyBlock(block);}
    }
}
