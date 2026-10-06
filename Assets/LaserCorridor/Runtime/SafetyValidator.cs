using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaserCorridor
{
    // A constructive certificate: travel to the reserved lane after the first visible launch, then hold z
    // and execute isolated posture changes. Time gaps cover the whole playable corridor.
    public sealed class SafetyValidator
    {
        readonly RunSettings s;
        public SafetyValidator(RunSettings settings) { s=settings; }

        public bool TryPlan(Vector3 player,IReadOnlyList<FloorHazard> hazards,PatternFamily[] families,float[] speeds,
            float targetX,bool mirror,float clock,out SafetyCertificate certificate,float openingWidth=1.5f,IReadOnlyDictionary<PatternFamily,float> openingWidths=null,IReadOnlyDictionary<PatternFamily,LaserPatternDefinition> definitions=null,int[] variants=null,float challenge=0)
        {
            certificate=null;
            if(families==null||speeds==null||families.Length==0||speeds.Length!=families.Length||variants!=null&&variants.Length!=families.Length) return false;
            if(float.IsNaN(targetX)||float.IsInfinity(targetX)||float.IsNaN(challenge)||float.IsInfinity(challenge))return false;
            challenge=Mathf.Clamp01(challenge);
            for(int i=0;i<families.Length;i++)
            {
                if(float.IsNaN(speeds[i])||float.IsInfinity(speeds[i])||speeds[i]<s.minimumWaveSpeed||speeds[i]>s.maximumWaveSpeed)return false;
                if(variants!=null&&(variants[i]<0||variants[i]>=PatternLibrary.VariantCount))return false;
            }
            float effectiveTarget=mirror?-targetX:targetX;
            if(Mathf.Abs(effectiveTarget)>s.corridorWidth/2-s.playerRadius-s.safetyMargin) return false;
            var route=FindRoute(new Vector2(player.x,player.z),new Vector2(effectiveTarget,player.z),hazards);
            if(route==null) return false;
            var result=new SafetyCertificate {targetX=effectiveTarget,playerZ=player.z,route=route};
            for(int i=1;i<route.Count;i++) result.pathLength+=Vector2.Distance(route[i-1],route[i]);
            // Never rely on a speed pickup or on the absence of a movement penalty.
            // Leave setup time between encounters; no visible or audible preview is emitted.
            result.leadTime=Mathf.Max(s.encounterLead+.025f,result.pathLength/(s.crouchSpeed*s.slowMultiplier)+s.reactionAllowance+.25f);
            // The wave itself is the first information available to the player. Ensure the
            // route can be travelled AFTER launch plus reaction time, including capsule depth.
            // A floor detour can temporarily move toward the emitter. Use its nearest point
            // so the first wave cannot catch the player before they finish reaching the lane.
            float nearestZ=player.z;foreach(var point in route)nearestZ=Mathf.Max(nearestZ,point.y);
            float firstContact=(s.emitterZ-nearestZ-s.playerRadius-s.beamRadius)/speeds[0];
            int firstVariant=variants==null?0:variants[0];
            var firstPose=firstVariant>0?PatternLibrary.RequiredPose(families[0],firstVariant):definitions!=null&&definitions.TryGetValue(families[0],out var firstDefinition)&&firstDefinition?firstDefinition.pose:PatternLibrary.RequiredPose(families[0]);
            float posture=firstPose==DodgePose.Jump?Mathf.Sqrt(2*s.jumpHeight/s.gravity):.025f;
            if(firstContact < s.reactionAllowance+posture+result.pathLength/(s.crouchSpeed*s.slowMultiplier))return false;
            float launch=clock+result.leadTime;
            for(int i=0;i<families.Length;i++)
            {
                if(i>0)
                {
                    float maxDistance=s.emitterZ-s.minimumZ+s.playerRadius;
                    launch+=Mathf.Max(0,maxDistance/speeds[i-1]-maxDistance/speeds[i])+Mathf.Max(1.4f,s.encounterLead+.1f);
                }
                LaserPatternDefinition definition=null;if(definitions!=null)definitions.TryGetValue(families[i],out definition);
                int variant=variants==null?0:variants[i];
                var opening=variant>0?PatternLibrary.Opening(families[i],variant,challenge):definition?definition.opening:PatternLibrary.Opening(families[i]);
                float width=opening.width;
                if(variant>0)
                {
                    if(definition)width=Mathf.Min(width,definition.safeOpeningWidth);
                    if(openingWidths!=null&&openingWidths.TryGetValue(families[i],out var cap))width=Mathf.Min(width,cap);
                    else if(!Mathf.Approximately(openingWidth,1.5f))width=Mathf.Min(width,openingWidth);
                }
                else width=openingWidths!=null&&openingWidths.TryGetValue(families[i],out var configured)?configured:Mathf.Approximately(openingWidth,1.5f)?definition?definition.safeOpeningWidth:opening.width:openingWidth;
                if(float.IsNaN(width)||float.IsInfinity(width)||width<=0)return false;
                opening.width=width;opening.x=-width/2;
                var wave=new WavePlan {family=families[i],variant=variant,challenge=challenge,speed=speeds[i],launchTime=launch,pose=variant>0?PatternLibrary.RequiredPose(families[i],variant):definition?definition.pose:PatternLibrary.RequiredPose(families[i]),opening=new Rect(effectiveTarget-width/2,opening.y,width,opening.height),beams=PatternLibrary.Create(families[i],targetX,mirror,width,opening,variant,challenge)};
                if(!Clearance(wave,effectiveTarget)) return false;
                result.waves.Add(wave);
            }
            float last=clock;
            foreach(var wave in result.waves) last=Mathf.Max(last,wave.launchTime+(s.emitterZ+1)/wave.speed);
            result.reservationUntil=last+s.burstRecovery;
            certificate=result; return true;
        }

        public bool Clearance(WavePlan wave,float x)
        {
            float feet=0,height=wave.pose==DodgePose.Crouch?s.crouchingHeight:s.standingHeight;
            if(wave.pose==DodgePose.Jump)
            {
                // Bound the complete jump throughout longitudinal overlap, including clock slowdown.
                float halfWindow=(s.playerRadius+s.beamRadius)/(wave.speed*s.slowMultiplier)+.035f;
                feet=s.jumpHeight-.5f*s.gravity*halfWindow*halfWindow;
                if(feet<=0) return false;
                height+=s.jumpHeight-feet;
            }
            float radius=s.playerRadius+s.beamRadius+s.safetyMargin+.025f;
            Vector3 lower=new Vector3(x,feet+s.playerRadius,0),upper=new Vector3(x,feet+height-s.playerRadius,0);
            foreach(var line in wave.beams)
            {
                var a=new Vector3(line.a.x,line.a.y,0); var b=new Vector3(line.b.x,line.b.y,0);
                if(BeamMath.SegmentDistanceSquared(a,b,lower,upper)<radius*radius) return false;
            }
            return wave.beams.Count>0;
        }

        public bool CanActivateTrap(FloorHazard trap,Vector3 player,SafetyCertificate reservation)
        {
            float extent=trap.halfSize+s.playerRadius+s.safetyMargin;
            Vector2 p=new Vector2(player.x,player.z);
            if(Mathf.Abs(p.x-trap.center.x)<=extent && Mathf.Abs(p.y-trap.center.y)<=extent) return false;
            if(reservation==null) return true;
            Rect rect=new Rect(trap.center-Vector2.one*extent,Vector2.one*extent*2);
            foreach(var point in reservation.route) if(rect.Contains(point)) return false;
            for(int i=1;i<reservation.route.Count;i++)
                if(BeamMath.SegmentTouchesRect(reservation.route[i-1],reservation.route[i],rect)) return false;
            return true;
        }

        public List<Vector2> FindRoute(Vector2 start,Vector2 goal,IReadOnlyList<FloorHazard> hazards)
        {
            bool Clear(Vector2 a,Vector2 b)
            {
                foreach(var hazard in hazards)
                {
                    float extent=hazard.halfSize+s.playerRadius+s.safetyMargin;
                    var rect=new Rect(hazard.center-Vector2.one*extent,Vector2.one*extent*2);
                    // The character may already be in a consumed trap; the director excludes it.
                    if(BeamMath.SegmentTouchesRect(a,b,rect)) return false;
                }
                return true;
            }
            if(Clear(start,goal)) return new List<Vector2>{start,goal};
            // Visibility graph around inflated trap corners. Edges provide continuous clearance.
            var nodes=new List<Vector2>{start,goal};
            foreach(var h in hazards)
            {
                float e=h.halfSize+s.playerRadius+s.safetyMargin+.04f;
                for(int x=-1;x<=1;x+=2) for(int y=-1;y<=1;y+=2)
                {
                    Vector2 p=h.center+new Vector2(x*e,y*e);
                    if(Mathf.Abs(p.x)<s.corridorWidth/2-s.playerRadius-.02f && p.y>=s.minimumZ && p.y<s.gateZ-.5f) nodes.Add(p);
                }
            }
            int count=nodes.Count; var cost=new float[count];var prev=new int[count];var done=new bool[count];
            for(int i=0;i<count;i++) {cost[i]=float.PositiveInfinity;prev[i]=-1;} cost[0]=0;
            for(int step=0;step<count;step++)
            {
                int current=-1; float best=float.PositiveInfinity;
                for(int i=0;i<count;i++) if(!done[i] && cost[i]<best) {current=i;best=cost[i];}
                if(current<0) return null; if(current==1) break; done[current]=true;
                for(int i=0;i<count;i++) if(!done[i] && Clear(nodes[current],nodes[i]))
                { float next=best+Vector2.Distance(nodes[current],nodes[i]); if(next<cost[i]) {cost[i]=next;prev[i]=current;} }
            }
            if(float.IsInfinity(cost[1])) return null;
            var path=new List<Vector2>();for(int i=1;i>=0;i=prev[i]) path.Add(nodes[i]);path.Reverse();return path;
        }
    }
}
