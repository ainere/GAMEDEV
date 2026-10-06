using UnityEngine;

namespace LaserCorridor
{
    public sealed class CorridorCamera : MonoBehaviour
    {
        public PlayerAvatar player;
        public float lookAhead=2.2f, smoothing=5;
        Vector3 mountedPosition;
        float shakeUntil;
        public void HitShake(){shakeUntil=Time.unscaledTime+.15f;}
        void Awake() {mountedPosition=transform.position;Snap();}
        public void Snap()
        {
            if(!player)return;shakeUntil=0;transform.position=mountedPosition;
            GetComponent<Camera>().fieldOfView=Mathf.Lerp(86,60,Mathf.InverseLerp(1,6,player.Feet.z));
            transform.rotation=Quaternion.LookRotation(Target()-mountedPosition);
        }
        Vector3 Target() => new Vector3(player.Feet.x*.22f,.85f+player.Feet.y*.65f,Mathf.Clamp(player.Feet.z+lookAhead,3.2f,12));
        void LateUpdate()
        {
            if(!player)return;transform.position=mountedPosition;
            GetComponent<Camera>().fieldOfView=Mathf.Lerp(86,60,Mathf.InverseLerp(1,6,player.Feet.z));
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(Target()-mountedPosition),1-Mathf.Exp(-smoothing*Time.unscaledDeltaTime));
            if(Time.unscaledTime<shakeUntil)transform.rotation*=Quaternion.Euler(Mathf.Sin(Time.unscaledTime*111)*.22f,Mathf.Cos(Time.unscaledTime*87)*.22f,0);
        }
    }
}
