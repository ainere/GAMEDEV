using UnityEngine;

namespace LaserCorridor
{
    public static class BeamMath
    {
        // Closest points on two finite segments, including degenerate and parallel segments.
        public static float SegmentDistanceSquared(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
            => SegmentDistanceSquared(p1,q1,p2,q2,out _);
        public static float SegmentDistanceSquared(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2,out Vector3 closestOnFirst)
        {
            Vector3 d1=q1-p1,d2=q2-p2,r=p1-p2;
            float a=Vector3.Dot(d1,d1),e=Vector3.Dot(d2,d2),f=Vector3.Dot(d2,r),s,t;
            if(a<1e-10f && e<1e-10f){closestOnFirst=p1;return (p1-p2).sqrMagnitude;}
            if(a<1e-10f) { s=0;t=Mathf.Clamp01(f/e); }
            else {
                float c=Vector3.Dot(d1,r);
                if(e<1e-10f) { t=0;s=Mathf.Clamp01(-c/a); }
                else {
                    float b=Vector3.Dot(d1,d2),denom=a*e-b*b;
                    s=denom>1e-10f?Mathf.Clamp01((b*f-c*e)/denom):0;
                    t=(b*s+f)/e;
                    if(t<0) { t=0;s=Mathf.Clamp01(-c/a); }
                    else if(t>1) { t=1;s=Mathf.Clamp01((b-c)/a); }
                }
            }
            closestOnFirst=p1+d1*s;return (closestOnFirst-p2-d2*t).sqrMagnitude;
        }
        public static bool CapsuleHit(BeamSegment beam,float z,Vector3 feet,float height,float radius,float beamRadius)
        {
            Vector3 a=new Vector3(beam.a.x,beam.a.y,z), b=new Vector3(beam.b.x,beam.b.y,z);
            float rr=radius+beamRadius;
            return SegmentDistanceSquared(a,b,feet+Vector3.up*radius,feet+Vector3.up*(height-radius))<=rr*rr;
        }
        public static bool SweptHit(BeamSegment beam,float previousZ,float currentZ,Vector3 previousFeet,Vector3 feet,float height,float radius,float beamRadius)
            => SweptContact(beam,previousZ,currentZ,previousFeet,feet,height,radius,beamRadius,out _,out _);
        public static bool SweptContact(BeamSegment beam,float previousZ,float currentZ,Vector3 previousFeet,Vector3 feet,float height,float radius,float beamRadius,out Vector3 contact,out float time)
        {
            // Relative translation of two convex capsules has a convex distance function.
            // Minimize it continuously rather than relying on trigger callbacks or frame samples.
            float Radius=radius+beamRadius+.001f;
            float lo=0,hi=1;
            float Distance(float t)
            {
                Vector3 f=Vector3.LerpUnclamped(previousFeet,feet,t);
                float z=Mathf.LerpUnclamped(previousZ,currentZ,t);
                return SegmentDistanceSquared(new Vector3(beam.a.x,beam.a.y,z),new Vector3(beam.b.x,beam.b.y,z),f+Vector3.up*radius,f+Vector3.up*(height-radius));
            }
            for(int i=0;i<18;i++) { float l=(lo*2+hi)/3,r=(lo+hi*2)/3; if(Distance(l)<Distance(r)) hi=r; else lo=l; }
            time=(lo+hi)*.5f;if(Distance(0)<Distance(time))time=0;if(Distance(1)<Distance(time))time=1;
            Vector3 atFeet=Vector3.LerpUnclamped(previousFeet,feet,time);float atZ=Mathf.LerpUnclamped(previousZ,currentZ,time);
            float distance=SegmentDistanceSquared(new Vector3(beam.a.x,beam.a.y,atZ),new Vector3(beam.b.x,beam.b.y,atZ),atFeet+Vector3.up*radius,atFeet+Vector3.up*(height-radius),out contact);
            return distance<=Radius*Radius;
        }
        public static HitRegion Region(Vector3 point,Vector3 feet,float height)
        {float fraction=(point.y-feet.y)/height;return fraction<.4f?HitRegion.Legs:fraction>=.8f?HitRegion.Head:HitRegion.Chest;}
        public static bool SegmentTouchesRect(Vector2 a,Vector2 b,Vector2 center,float half)
        { return SegmentTouchesRect(a,b,new Rect(center-Vector2.one*half,Vector2.one*half*2)); }
        public static bool SegmentTouchesRect(Vector2 a,Vector2 b,Rect rect)
        {
            Vector2 d=b-a,min=rect.min,max=rect.max;
            float enter=0,exit=1;
            for(int axis=0;axis<2;axis++) {
                if(Mathf.Abs(d[axis])<1e-7f) { if(a[axis]<min[axis] || a[axis]>max[axis]) return false; }
                else { float t1=(min[axis]-a[axis])/d[axis],t2=(max[axis]-a[axis])/d[axis]; if(t1>t2) { float swap=t1;t1=t2;t2=swap; } enter=Mathf.Max(enter,t1);exit=Mathf.Min(exit,t2); if(enter>exit) return false; }
            }
            return true;
        }
    }
}
