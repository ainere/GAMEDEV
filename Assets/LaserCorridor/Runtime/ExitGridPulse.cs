using UnityEngine;
namespace LaserCorridor
{
    public sealed class ExitGridPulse : MonoBehaviour
    {
        public LineRenderer[] beams,halos;
        public Light spill;
        public Color coreColor=new Color(1.2f,2,3),haloColor=new Color(.08f,.3f,.45f,.12f);
        RunController run;MaterialPropertyBlock block;
        void Start(){run=FindAnyObjectByType<RunController>();}
        void Update(){Refresh(run&&run.Model!=null?run.Model.Elapsed:0);}
        public void Refresh(float clock)
        {
            if(block==null)block=new MaterialPropertyBlock();float pulse=.7f+.3f*Mathf.Sin(clock*3.2f);
            block.Clear();block.SetColor("_BaseColor",Color.white);block.SetColor("_EmissionColor",coreColor*pulse);block.SetFloat("_Halo",0);block.SetFloat("_BeamClock",clock);block.SetFloat("_GridFlow",1);
            foreach(var beam in beams)beam.SetPropertyBlock(block);
            block.SetColor("_BaseColor",haloColor);block.SetColor("_EmissionColor",new Color(haloColor.r,haloColor.g,haloColor.b,1)*pulse);block.SetFloat("_Halo",1);
            foreach(var halo in halos)halo.SetPropertyBlock(block);
            if(spill){spill.color=new Color(.25f,.6f,1);spill.intensity=.6f*pulse;}
        }
    }
}
