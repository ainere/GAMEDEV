using System.Collections.Generic;
using UnityEngine;
namespace LaserCorridor
{
    public static class PatternLibrary
    {
        public const int VariantCount=4;
        public static int NormalizeVariant(PatternFamily family,int variant)=>Mathf.Clamp(variant,0,VariantCount-1);
        public static DodgePose RequiredPose(PatternFamily family,int variant)=>RequiredPose(family);

        // Variant zero remains the original authored geometry. Advanced apertures use
        // capsule supports: diagonal lines chamfer the playable space instead of merely
        // touching the corners of an already open rectangle.
        public static Rect Opening(PatternFamily family,int variant,float challenge)
        {
            if(NormalizeVariant(family,variant)==0)return Opening(family);
            challenge=Mathf.Clamp01(challenge);
            float broad=1.45f,tight=1.08f;
            switch(family)
            {
                case PatternFamily.LowJump:case PatternFamily.CrouchBeam:broad=1.5f;tight=1.12f;break;
                case PatternFamily.VerticalCurtain:case PatternFamily.Comb:broad=1.35f;tight=1.02f;break;
                case PatternFamily.DiagonalSlash:case PatternFamily.Chevron:broad=1.55f;tight=1.18f;break;
                case PatternFamily.Circle:case PatternFamily.CircleGrid:broad=1.4f;tight=1.06f;break;
            }
            float width=Mathf.Lerp(broad,tight,challenge);
            if(variant==2)width+=.06f;else if(variant==3)width-=.03f;
            width=Mathf.Max(1,width);
            var pose=RequiredPose(family);
            float radius=pose==DodgePose.Crouch?Mathf.Lerp(.64f,.51f,challenge):Mathf.Lerp(.6f,.51f,challenge);
            float lower=pose==DodgePose.Jump?1.06f:.3f;
            float upper=pose==DodgePose.Jump?2.6f:pose==DodgePose.Crouch?.7f:1.5f;
            return new Rect(-width/2,lower-radius,width,upper-lower+radius*2);
        }
        public static float MaximumOffset(PatternFamily family,int variant,float challenge)
        {
            if(NormalizeVariant(family,variant)==0)return MaximumOffset(family);
            return Mathf.Min(1.35f,2-Opening(family,variant,challenge).width/2-.16f);
        }
        public static DodgePose RequiredPose(PatternFamily f)
        {
            switch(f){case PatternFamily.LowJump:case PatternFamily.Square:case PatternFamily.Circle:return DodgePose.Jump;
            case PatternFamily.CrouchBeam:case PatternFamily.Star:case PatternFamily.Chevron:case PatternFamily.Diamond:case PatternFamily.OffsetGrid:case PatternFamily.CircleGrid:case PatternFamily.Comb:return DodgePose.Crouch;default:return DodgePose.Stand;}
        }
        public static float MaximumOffset(PatternFamily f)
        {switch(f){case PatternFamily.Circle:case PatternFamily.Square:case PatternFamily.Star:return .3f;case PatternFamily.Diamond:case PatternFamily.DiagonalSlash:case PatternFamily.Chevron:return .5f;case PatternFamily.CircleGrid:return 1;default:return 1.15f;}}
        public static Rect Opening(PatternFamily f)
        {
            switch(f){case PatternFamily.LowJump:return new Rect(-2,.7f,4,3.3f);case PatternFamily.CrouchBeam:return new Rect(-2,0,4,1.25f);
            case PatternFamily.Square:case PatternFamily.Circle:return new Rect(-1.6f,.25f,3.2f,3.2f);
            case PatternFamily.Diamond:return new Rect(-1.35f,-.55f,2.7f,2.55f);case PatternFamily.CircleGrid:return new Rect(-.9f,-.25f,1.8f,1.8f);
            case PatternFamily.OffsetGrid:return new Rect(-.65f,0,1.3f,1.25f);case PatternFamily.Comb:return new Rect(-.7f,0,1.4f,1.25f);
            case PatternFamily.Chevron:case PatternFamily.Star:return new Rect(-.65f,0,1.3f,1.25f);default:return new Rect(-.8f,0,1.6f,2.35f);}
        }
        // Every beam is an entire straight line clipped to the corridor rectangle. Frames
        // are intersections of extended lines, never detached shapes or cut-off grid stubs.
        // Circle / CircleGrid retain their serialized IDs and now author octagonal apertures.
        public static List<BeamSegment> Create(PatternFamily family,float offset,bool mirror,float openingWidth=1.5f,Rect? configured=null,int variant=0,float challenge=0)
        {
            variant=NormalizeVariant(family,variant);
            if(variant>0)return CreateAdvanced(family,offset,mirror,openingWidth,configured,variant,challenge);
            var lines=new List<BeamSegment>();Rect opening=configured??Opening(family);
            float left=offset-openingWidth/2,right=offset+openingWidth/2;
            void Full(Vector2 point,Vector2 direction)
            {
                if(direction.sqrMagnitude<.000001f)return;
                direction.Normalize();var segment=new BeamSegment(point-direction*32,point+direction*32);
                if(Clip(segment,new Rect(-2,0,4,4.2f),out float a,out float b)&&b-a>.0001f)
                    lines.Add(new BeamSegment(Vector2.Lerp(segment.a,segment.b,a),Vector2.Lerp(segment.a,segment.b,b)));
            }
            void Through(float ax,float ay,float bx,float by)=>Full(new Vector2(ax,ay),new Vector2(bx-ax,by-ay));
            void Horizontal(float y)=>Full(new Vector2(0,y),Vector2.right);
            void Vertical(float x)=>Full(new Vector2(x,0),Vector2.up);
            void Octagon(float radius,float cy)
            {
                for(int side=0;side<8;side++){float angle=side*Mathf.PI/4;var normal=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));Full(new Vector2(offset,cy)+normal*radius,new Vector2(-normal.y,normal.x));}
            }
            void WindowGrid(float top)
            {
                Vertical(left);Vertical(right);Horizontal(top);
                for(float y=top+.5f;y<4.2f;y+=.5f)Horizontal(y);
                for(float x=-1.85f;x<2;x+=.5f)if(x<left-.1f||x>right+.1f)Vertical(x);
            }
            switch(family)
            {
                case PatternFamily.LowJump:Horizontal(.25f);break;
                case PatternFamily.CrouchBeam:Horizontal(1.4f);break;
                case PatternFamily.VerticalCurtain:for(float x=-2;x<=2;x+=.4f)if(x<=left||x>=right)Vertical(x);break;
                case PatternFamily.DiagonalSlash:Through(-2,0,offset,2.8f);Through(offset,2.8f,2,0);Horizontal(4);break;
                case PatternFamily.OffsetPairs:
                    foreach(int side in new[]{-1,1}){Through(offset+side*1.05f,0,offset+side*2.1f,4.2f);Through(offset+side*1.4f,0,offset+side*1.74f,4.2f);}break;
                case PatternFamily.Cross:Horizontal(2.7f);Vertical(offset-.8f);break;
                case PatternFamily.Star:
                    float cx=offset-.95f;Horizontal(2.7f);Vertical(cx);Full(new Vector2(cx,2.7f),new Vector2(1,.6f));Full(new Vector2(cx,2.7f),new Vector2(1,-.6f));break;
                case PatternFamily.Zigzag:
                    foreach(int side in new[]{-1,1}){Through(offset+side*1.1f,0,offset+side*1.9f,4.2f);Through(offset+side*1.8f,0,offset+side*1.1f,4.2f);}Horizontal(3.1f);break;
                case PatternFamily.Chevron:Through(-2,3.8f,offset,1.7f);Through(offset,1.7f,2,3.8f);Through(-2,4.2f,offset,3);Through(offset,3,2,4.2f);break;
                case PatternFamily.Square:Vertical(left);Vertical(right);Horizontal(.25f);Horizontal(3.45f);break;
                case PatternFamily.Diamond:
                    Through(offset,-.55f,offset+1.35f,.85f);Through(offset+1.35f,.85f,offset,2);Through(offset,2,offset-1.35f,.85f);Through(offset-1.35f,.85f,offset,-.55f);break;
                case PatternFamily.Circle:Octagon(openingWidth/2,1.85f);break;
                case PatternFamily.CornerGrid:case PatternFamily.OffsetGrid:WindowGrid(opening.yMax);break;
                case PatternFamily.CircleGrid:
                    float radius=Mathf.Max(.9f,openingWidth/2);Octagon(radius,.65f);
                    for(float y=.65f+radius+.5f;y<4.2f;y+=.5f)Horizontal(y);
                    for(float x=-1.85f;x<2;x+=.5f)if(Mathf.Abs(x-offset)>radius+.15f)Vertical(x);break;
                case PatternFamily.Comb:Horizontal(1.25f);for(float x=-2;x<=2;x+=.4f)if(x<=left||x>=right)Vertical(x);break;
            }
            if(mirror)for(int i=0;i<lines.Count;i++){var line=lines[i];line.a.x=-line.a.x;line.b.x=-line.b.x;lines[i]=line;}
            return lines;
        }
        static List<BeamSegment> CreateAdvanced(PatternFamily family,float offset,bool mirror,float openingWidth,Rect? configured,int variant,float challenge)
        {
            challenge=Mathf.Clamp01(challenge);
            Rect aperture=configured??Opening(family,variant,challenge);
            float width=Mathf.Min(openingWidth,aperture.width);
            if(float.IsNaN(width)||float.IsInfinity(width)||width<=0)return new List<BeamSegment>();
            var pose=RequiredPose(family,variant);
            float lower=pose==DodgePose.Jump?1.06f:.3f;
            float upper=pose==DodgePose.Jump?2.6f:pose==DodgePose.Crouch?.7f:1.5f;
            float cy=(lower+upper)/2,halfSpine=(upper-lower)/2;
            float verticalRadius=pose==DodgePose.Crouch?Mathf.Lerp(.64f,.51f,challenge):Mathf.Lerp(.6f,.51f,challenge);
            float halfWidth=width/2;
            var lines=new List<BeamSegment>();
            void Full(Vector2 point,Vector2 direction)
            {
                if(direction.sqrMagnitude<.000001f)return;
                direction.Normalize();var segment=new BeamSegment(point-direction*32,point+direction*32);
                if(!Clip(segment,new Rect(-2,0,4,4.2f),out float a,out float b)||b-a<.0001f)return;
                var clipped=new BeamSegment(Vector2.Lerp(segment.a,segment.b,a),Vector2.Lerp(segment.a,segment.b,b));
                foreach(var existing in lines)
                    if(((existing.a-clipped.a).sqrMagnitude<.000004f&&(existing.b-clipped.b).sqrMagnitude<.000004f)||
                        ((existing.b-clipped.a).sqrMagnitude<.000004f&&(existing.a-clipped.b).sqrMagnitude<.000004f))return;
                lines.Add(clipped);
            }
            // Support of a vertical spine expanded by an ellipse. Every normal has at
            // least .5m clearance at the certified lane, including the slowest jump.
            void Support(float nx,float ny,float extra=0)
            {
                var normal=new Vector2(nx,ny).normalized;
                float radial=Mathf.Sqrt(halfWidth*halfWidth*normal.x*normal.x+verticalRadius*verticalRadius*normal.y*normal.y);
                float distance=Mathf.Abs(normal.y)*halfSpine+radial+extra;
                Full(new Vector2(offset,cy)+normal*distance,new Vector2(-normal.y,normal.x));
            }
            void Sides(float lean=0){Support(-1,-lean);Support(1,lean);}
            void Roof(float lean=0,float extra=0)=>Support(lean,1,extra);
            void Rail(float lean=0,float extra=0)=>Support(lean,-1,extra);
            void AngularSides(float lean){Support(-1,lean);Support(-1,-lean);Support(1,lean);Support(1,-lean);}
            void Fan(int side,float steepness)
            {
                Support(side,0);Support(side,steepness);Support(side,-steepness);
            }
            void Curtain(float lean,float pitch,int side=0)
            {
                int count=Mathf.CeilToInt(4/pitch);
                for(int i=0;i<count;i++)
                {
                    float extra=i*pitch;
                    if(side<=0)Support(-1,-lean,extra);
                    if(side>=0)Support(1,lean,extra);
                }
            }
            void RoofGrid(float lean,float pitch)
            {for(float extra=0;extra<3.5f;extra+=pitch)Roof(lean,extra);}
            void Octagon(float rotation)
            {
                for(int side=0;side<8;side++)
                {
                    float angle=rotation+side*Mathf.PI/4;
                    Support(Mathf.Cos(angle),Mathf.Sin(angle));
                }
            }
            switch(family)
            {
                case PatternFamily.LowJump:
                    Rail(variant==2?.1f:variant==3?-.14f:0);
                    if(variant==1){Sides(.15f);Roof();}
                    else if(variant==2){AngularSides(.3f);Rail(.1f,.22f);}
                    else{Fan(-1,.55f);Support(1,-.22f);Roof(.12f);}
                    break;
                case PatternFamily.CrouchBeam:
                    if(variant==1){Roof(.16f);Sides();}
                    else if(variant==2){Roof(-.3f);Roof(.3f);Sides(.22f);}
                    else{Roof(-.12f);Roof(-.12f,.33f);Fan(1,.45f);Support(-1,-.2f);}
                    break;
                case PatternFamily.VerticalCurtain:
                    Curtain(variant==1?0:variant==2?.24f:-.32f,Mathf.Lerp(.45f,.32f,challenge));
                    if(variant==3){Fan(-1,.48f);Roof(.22f);}else Roof(variant==2?-.12f:0);
                    break;
                case PatternFamily.DiagonalSlash:
                    if(variant==1){AngularSides(.78f);Roof(.12f);}
                    else if(variant==2){Fan(-1,.55f);Support(1,-.8f);Support(1,.22f);Roof(-.16f);}
                    else{Support(-1,.28f);Support(-1,-1);Support(1,.9f);Support(1,-.36f);Roof(.2f);}
                    break;
                case PatternFamily.OffsetPairs:
                    if(variant==1){Sides(.36f);Support(-1,-.36f,.32f);Support(1,.36f,.32f);Roof(-.14f);}
                    else if(variant==2){AngularSides(.35f);Roof(.2f);}
                    else{Fan(-1,.66f);Support(1,.18f);Support(1,.18f,.25f);Roof(-.18f);}
                    break;
                case PatternFamily.Cross:
                    if(variant==1){Sides();Roof();Roof(0,.65f);}
                    else if(variant==2){Sides(.18f);Roof(-.22f);Roof(.22f);}
                    else{Support(-1,0);Support(1,-.5f);Roof(.12f);Support(-1,0,.3f);}
                    break;
                case PatternFamily.Star:
                    if(variant==1){Fan(-1,.68f);Fan(1,.68f);Roof();}
                    else if(variant==2){Fan(-1,.9f);Support(1,-.3f);Roof(.18f);Roof(-.32f,.3f);}
                    else{Fan(1,.42f);Support(-1,.68f);Support(-1,-.2f);Roof(-.16f);}
                    break;
                case PatternFamily.Zigzag:
                    if(variant==1){AngularSides(.34f);Roof();}
                    else if(variant==2){Sides(-.38f);Support(-1,.5f);Support(1,-.5f);Roof(.18f);}
                    else{Support(-1,.22f);Support(-1,-.72f);Support(1,.64f);Support(1,-.16f);Roof(-.2f);}
                    break;
                case PatternFamily.Chevron:
                    Roof(-.72f);Roof(variant==2?.32f:.72f);
                    if(variant==1){Sides(.12f);Roof(-.72f,.4f);Roof(.72f,.4f);}
                    else if(variant==2){Support(-1,-.42f);Support(1,.18f);Roof(-.12f);}
                    else{AngularSides(.4f);Roof(-.72f,.5f);Roof(.72f,.5f);Roof(.1f);}
                    break;
                case PatternFamily.Square:
                    Rail();Roof();
                    if(variant==1)Sides();
                    else if(variant==2){Sides(.18f);Rail(.12f);Roof(-.12f);}
                    else{Sides();Support(-1,-.58f);Support(1,.58f);Rail(0,.18f);}
                    break;
                case PatternFamily.Diamond:
                    if(variant==1){AngularSides(1);Roof(.08f);}
                    else if(variant==2){Support(-1,.55f);Support(-1,-1.15f);Support(1,1.15f);Support(1,-.55f);Roof(-.16f);}
                    else{AngularSides(.7f);Roof(.22f);Support(-1,.7f,.35f);Support(1,-.7f,.35f);}
                    break;
                case PatternFamily.Circle:
                    Octagon(variant==2?Mathf.PI/8:variant==3?Mathf.PI/12:0);
                    if(variant>1){Rail(variant==2?.12f:-.12f);Roof(variant==2?-.12f:.12f);}
                    break;
                case PatternFamily.CornerGrid:
                    Curtain(variant==2?.18f:0,variant==3?.38f:.5f);RoofGrid(variant==2?-.12f:0,.5f);
                    if(variant==1){Support(-1,.5f);Support(1,-.5f);}
                    else if(variant==3){Fan(-1,.5f);Roof(.24f);}
                    break;
                case PatternFamily.OffsetGrid:
                    Curtain(variant==1?.18f:variant==2?-.28f:0,.45f);RoofGrid(variant==1?-.12f:variant==2?.18f:-.2f,.45f);
                    if(variant==3){Support(-1,.62f);Support(1,-.35f);Roof(.12f);}
                    break;
                case PatternFamily.CircleGrid:
                    Octagon(variant==2?Mathf.PI/8:variant==3?-Mathf.PI/12:0);
                    RoofGrid(variant==1?0:variant==2?.16f:-.18f,.48f);
                    Curtain(variant==3?.18f:0,.5f,variant==2?-1:variant==3?1:0);
                    break;
                case PatternFamily.Comb:
                    Curtain(variant==1?0:variant==2?.28f:-.22f,Mathf.Lerp(.4f,.3f,challenge));
                    Roof(variant==2?-.18f:variant==3?.22f:0);
                    if(variant==3){Roof(-.2f,.32f);Support(-1,.5f);}
                    break;
            }
            if(mirror)for(int i=0;i<lines.Count;i++){var line=lines[i];line.a.x=-line.a.x;line.b.x=-line.b.x;lines[i]=line;}
            return lines;
        }
        static bool Clip(BeamSegment s,Rect r,out float lo,out float hi)
        {
            lo=0;hi=1;Vector2 d=s.b-s.a;
            for(int axis=0;axis<2;axis++)
            {
                float min=axis==0?r.xMin:r.yMin,max=axis==0?r.xMax:r.yMax;
                if(Mathf.Abs(d[axis])<.00001f){if(s.a[axis]<min||s.a[axis]>max)return false;continue;}
                float a=(min-s.a[axis])/d[axis],b=(max-s.a[axis])/d[axis];
                if(a>b){float t=a;a=b;b=t;}lo=Mathf.Max(lo,a);hi=Mathf.Min(hi,b);if(lo>hi)return false;
            }
            return true;
        }
    }
}
