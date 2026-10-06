using System;
using UnityEngine;
namespace LaserCorridor
{
    public sealed class FloorSocket : MonoBehaviour
    {
        public Transform plate;
        public Renderer[] seams;
        public Light tileLight;
        public PanelEffectDefinition[] definitions;
        public float halfSize=.37f;
        public bool eligible;
        [NonSerialized] public PanelEffect effect;
        [NonSerialized] public bool consumed,skipped;
        MaterialPropertyBlock block;
        float spentAt=-1;
        public Vector2 Point => new Vector2(transform.position.x,transform.position.z);
        public bool Contains(PlayerAvatar player,float radius=0) => Touches(new Vector2(player.Feet.x,player.Feet.z),new Vector2(player.Feet.x,player.Feet.z),radius);
        public bool GroundContact(PlayerAvatar player,float radius)
        {
            if(!player.Grounded)return false;
            // Landing cannot collect a plate crossed earlier while airborne.
            Vector2 end=new Vector2(player.Feet.x,player.Feet.z);
            Vector2 start=player.PreviouslyGrounded?new Vector2(player.PreviousFeet.x,player.PreviousFeet.z):end;
            return Touches(start,end,radius);
        }
        public bool Touches(Vector2 start,Vector2 end,float radius)
        {
            var rect=new Rect(Point-Vector2.one*halfSize,Vector2.one*halfSize*2);
            if(BeamMath.SegmentTouchesRect(start,end,rect))return true;
            var corners=new[]{rect.min,new Vector2(rect.xMin,rect.yMax),rect.max,new Vector2(rect.xMax,rect.yMin)};
            for(int edge=0;edge<4;edge++)
            {
                Vector2 a=corners[edge],b=corners[(edge+1)%4];
                if(BeamMath.SegmentDistanceSquared(new Vector3(start.x,0,start.y),new Vector3(end.x,0,end.y),new Vector3(a.x,0,a.y),new Vector3(b.x,0,b.y))<=radius*radius)return true;
            }
            return false;
        }
        public void SetNeutral()
        {spentAt=-1;foreach(var seam in seams)seam.gameObject.SetActive(false);tileLight.enabled=false;}
        Color LightColor(PanelEffect value)
        {foreach(var definition in definitions)if(definition.effect==value)return definition.color;return Color.cyan;}
        void Glow(PanelEffect value,float strength)
        {
            if(block==null)block=new MaterialPropertyBlock();Color color=LightColor(value);
            block.Clear();block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",color*strength*2.5f);
            foreach(var seam in seams){seam.gameObject.SetActive(strength>.005f);seam.SetPropertyBlock(block);}
            tileLight.enabled=strength>.005f;tileLight.color=color;tileLight.intensity=strength*.25f;
        }
        static float Pulse(bool harmful,float time) => harmful?.22f+.78f*Mathf.Abs(Mathf.Sin(time*13)*Mathf.Sin(time*7)):1;
        public void SetReveal(PanelEffect value,bool harmful,float t01)
        {spentAt=-1;Glow(value,Mathf.Lerp(.18f,.6f,Mathf.Clamp01(t01))*Pulse(harmful,t01*2));}
        public void SetActive(PanelEffect value,bool harmful,float time)
        {spentAt=-1;Glow(value,Pulse(harmful,time));}
        public void SetSpent(float time)
        {if(spentAt<0)spentAt=time;Glow(effect,1-Mathf.Clamp01((time-spentAt)/.3f));}
    }
}
