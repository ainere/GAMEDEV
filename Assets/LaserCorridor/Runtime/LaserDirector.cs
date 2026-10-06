using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaserCorridor
{
    public sealed class LaserDirector : MonoBehaviour
    {
        public Material beamMaterial;
        public LaserPatternDefinition[] patterns;
        public LaserBurstDefinition[] authoredBursts;
        public AudioClip launchSound;
        public SafetyCertificate Reservation {get;private set;}
        public int WavesSpawned {get;private set;}
        public int Rejections {get;private set;}
        public int OrdinaryContacts {get;private set;}
        public int AdvancedWavesSpawned {get;private set;}
        public int FallbackEncounters {get;private set;}
        public IReadOnlyList<int> FamilySpawnCounts=>familySpawnCounts;
        public int VariantSpawnCount(PatternFamily family,int variant)=>variantSpawnCounts[(int)family,Mathf.Clamp(variant,0,PatternLibrary.VariantCount-1)];
        public int ActiveCount => active.Count;
        public int PendingCount => pending.Count;
        public int CurrentSeed {get;private set;}
        RunModel model;PlayerAvatar player;PanelDirector panels;SafetyValidator validator;
        System.Random random;
        public CorridorAudio audio;
        public Material haloMaterial;public Mesh nodeMesh;
        public Color beamCore=new Color(1.2f,2,3),beamHalo=new Color(.08f,.3f,.45f,.12f);
        int nextWaveId;
        float nextBurst;
        readonly List<WavePlan> pending=new List<WavePlan>();
        readonly List<BeamView> active=new List<BeamView>();
        readonly Stack<BeamView> pool=new Stack<BeamView>();
        MaterialPropertyBlock beamProperties;
        readonly int[] familySpawnCounts=new int[16];
        readonly int[,] variantSpawnCounts=new int[16,PatternLibrary.VariantCount];
        readonly Queue<int> recentPatterns=new Queue<int>();
        DodgePose lastPose=DodgePose.Stand;
        int lastPattern=-1;
        float lastTarget;
        bool hasLastTarget;

        sealed class BeamView
        {
            public GameObject root;
            public readonly List<LineRenderer> lines=new List<LineRenderer>(),halos=new List<LineRenderer>();
            public readonly List<Renderer> nodes=new List<Renderer>();
            public WavePlan plan;
            public float z;
            public bool touched;
        }
        public void Initialize(RunModel state,PlayerAvatar avatar,PanelDirector floor,SafetyValidator safety)
        {
            model=state;player=avatar;panels=floor;validator=safety;
            beamProperties=new MaterialPropertyBlock();

            ResetWaves();
        }
        public void ResetWaves()
        {
            int seed;
            int previousSeed=CurrentSeed==int.MinValue?int.MaxValue:Math.Abs(CurrentSeed);
            do {seed=Guid.NewGuid().GetHashCode()&int.MaxValue;} while(seed==previousSeed);
            ResetWaves(seed);
        }
        public void ResetWaves(int seed)
        {
            Clear();nextBurst=.4f;SetSeed(seed);WavesSpawned=Rejections=OrdinaryContacts=nextWaveId=0;
            AdvancedWavesSpawned=FallbackEncounters=0;Array.Clear(familySpawnCounts,0,familySpawnCounts.Length);Array.Clear(variantSpawnCounts,0,variantSpawnCounts.Length);
            recentPatterns.Clear();lastPose=DodgePose.Stand;lastPattern=-1;hasLastTarget=false;
        }
        public void SetSeed(int seed) {CurrentSeed=seed;random=new System.Random(seed);}
        public void Clear()
        {
            if(audio)audio.StopWaves();
            foreach(var view in active) Release(view);active.Clear();pending.Clear();Reservation=null;
        }
        public void QueueCertifiedEncounter(SafetyCertificate certificate)
        {
            Clear();Reservation=certificate;foreach(var wave in certificate.waves){wave.id=++nextWaveId;pending.Add(wave);}nextBurst=float.PositiveInfinity;
        }
        public void Step(float previousClock)
        {
            if(model.State!=RunState.Running) {Clear();return;}
            float clock=model.HazardClock;
            if(clock>=nextBurst && active.Count==0 && pending.Count==0) PlanBurst();
            for(int i=pending.Count-1;i>=0;i--)
            {
                var plan=pending[i]; if(clock<plan.launchTime) continue;
                var view=Get(plan);view.z=model.settings.emitterZ-plan.speed*(clock-plan.launchTime);active.Add(view);pending.RemoveAt(i);
                WavesSpawned++;familySpawnCounts[(int)plan.family]++;variantSpawnCounts[(int)plan.family,Mathf.Clamp(plan.variant,0,PatternLibrary.VariantCount-1)]++;
                if(plan.variant>0)AdvancedWavesSpawned++;if(audio)audio.Launch(plan);
            }
            for(int i=active.Count-1;i>=0;i--)
            {
                var view=active[i];float before=model.settings.emitterZ-view.plan.speed*Mathf.Max(0,previousClock-view.plan.launchTime);
                view.z=model.settings.emitterZ-view.plan.speed*(clock-view.plan.launchTime);
                if(!view.touched)
                {
                    foreach(var segment in view.plan.beams)
                        if(BeamMath.SweptContact(segment,before,view.z,player.PreviousFeet,player.Feet,player.Height,model.settings.playerRadius,model.settings.beamRadius,out var contact,out float hitTime))
                        { view.touched=true;OrdinaryContacts++;model.Damage(model.settings.laserDamage,"MOVING LASER",BeamMath.Region(contact,Vector3.Lerp(player.PreviousFeet,player.Feet,hitTime),player.Height));break; }
                }
                if(model.State!=RunState.Running){Clear();return;}
                Draw(view,view.z);if(audio)audio.MoveWave(view.plan,view.z);
                if(view.z < -1) {Release(view);active.RemoveAt(i);}
            }
            if(Reservation!=null && clock>=Reservation.reservationUntil) Reservation=null;
        }
        void PlanBurst()
        {
            float elapsed=model.Elapsed;
            var settings=model.settings;
            float speedDifficulty=Mathf.Clamp01(elapsed/60);
            float challenge=Mathf.Clamp01(elapsed/Mathf.Max(1,settings.patternRampDuration));
            bool advanced=elapsed>=settings.advancedPatternsAfter;
            int desiredCount=elapsed<settings.pairedBurstsAfter?1:elapsed<settings.tripleBurstsAfter?2:random.Next(2,4);
            var widths=new Dictionary<PatternFamily,float>();var definitions=new Dictionary<PatternFamily,LaserPatternDefinition>();
            if(patterns!=null)foreach(var definition in patterns)
                if(definition){widths[definition.family]=definition.family==PatternFamily.VerticalCurtain?Mathf.Lerp(1.6f,1.0f,challenge):definition.safeOpeningWidth;definitions[definition.family]=definition;}
            var eligible=new List<PatternFamily>();
            float unlocked=Mathf.Clamp01(elapsed/Mathf.Max(1,settings.patternUnlockDuration));
            for(int family=0;family<16;family++)
            {
                var f=(PatternFamily)family;
                if(definitions.TryGetValue(f,out var definition)){if(definition.difficulty<=unlocked)eligible.Add(f);}
                else if(patterns==null||patterns.Length==0)eligible.Add(f);
            }
            if(eligible.Count==0){eligible.Add(PatternFamily.LowJump);eligible.Add(PatternFamily.CrouchBeam);}
            var hazards=panels.GetHazards();SafetyCertificate certificate=null;
            int attempts=Mathf.Clamp(settings.patternPlanAttempts,4,24);
            for(int attempt=0;attempt<attempts;attempt++)
            {
                // Later retries shorten the burst and travel, retaining advanced geometry.
                // This avoids turning every rejected multi-wave design into the same easy rail.
                int count=attempt<attempts*2/3?desiredCount:1;
                var families=new PatternFamily[count];var speeds=new float[count];var variants=new int[count];
                // Start each encounter from the whole unlocked pool. Follow-up waves
                // still favour posture transitions, but no run has a fixed opener.
                DodgePose? desiredPose=null;
                bool authored=attempt==0&&advanced&&elapsed>=settings.pairedBurstsAfter&&random.Next(5)==0&&authoredBursts!=null;
                if(authored)
                {
                    var options=new List<LaserBurstDefinition>();
                    foreach(var burst in authoredBursts)
                        if(burst&&burst.patterns!=null&&burst.speeds!=null&&burst.patterns.Length>0&&burst.patterns.Length<=count&&burst.speeds.Length==burst.patterns.Length&&burst.earliestElapsed<=elapsed)options.Add(burst);
                    if(options.Count>0)
                    {
                        var chosen=options[random.Next(options.Count)];families=(PatternFamily[])chosen.patterns.Clone();speeds=(float[])chosen.speeds.Clone();variants=new int[families.Length];count=families.Length;
                    }
                    else authored=false;
                }
                for(int i=0;i<count;i++)
                {
                    if(!authored)
                    {
                        int previousFamily=i>0?(int)families[i-1]:lastPattern>=0?lastPattern/PatternLibrary.VariantCount:-1;
                        families[i]=ChooseFamily(eligible,desiredPose,previousFamily,elapsed,definitions);
                        float baseSpeed=Mathf.Lerp(settings.minimumWaveSpeed,settings.maximumWaveSpeed,speedDifficulty);
                        // Near-emitter players can still receive a different certified design
                        // at a slower fixed speed when the first aggressive candidate fails.
                        float retrySpeed=attempt>=attempts*3/4?settings.minimumWaveSpeed:baseSpeed;
                        speeds[i]=Mathf.Clamp(retrySpeed+((float)random.NextDouble()-.5f)*.4f,settings.minimumWaveSpeed,settings.maximumWaveSpeed);
                    }
                    variants[i]=advanced&&(!definitions.TryGetValue(families[i],out var definition)||definition.allowAdvancedVariants)?ChooseVariant(families[i]):0;
                    desiredPose=PatternLibrary.RequiredPose(families[i],variants[i])==DodgePose.Jump?DodgePose.Crouch:DodgePose.Jump;
                    if(i==1&&count==3&&random.Next(3)==0)desiredPose=DodgePose.Stand;
                }
                float limit=1.35f;bool mirror=random.Next(2)==0;
                for(int i=0;i<count;i++)
                {
                    limit=Mathf.Min(limit,PatternLibrary.MaximumOffset(families[i],variants[i],challenge));
                    if(definitions.TryGetValue(families[i],out var definition)&&!definition.allowMirror)mirror=false;
                }
                float effectiveTarget=ChooseTarget(limit,attempt,attempts,advanced);
                float target=mirror?-effectiveTarget:effectiveTarget;
                if(validator.TryPlan(player.Feet,hazards,families,speeds,target,mirror,model.HazardClock,out certificate,1.5f,widths,definitions,variants,challenge))break;
                Rejections++;
            }
            if(certificate==null)
            {
                // A choice of individually certified rails remains the final fallback.
                // Alternate posture and try both; never rely on an absorbed hit or pickup.
                var fallback=lastPose==DodgePose.Crouch?PatternFamily.LowJump:PatternFamily.CrouchBeam;
                float target=Mathf.Clamp(player.Feet.x,-1.5f,1.5f);
                for(int attempt=0;attempt<2&&certificate==null;attempt++)
                {
                    var family=attempt==0?fallback:fallback==PatternFamily.LowJump?PatternFamily.CrouchBeam:PatternFamily.LowJump;
                    if(!validator.TryPlan(player.Feet,hazards,new[]{family},new[]{settings.minimumWaveSpeed},target,false,model.HazardClock,out certificate))Rejections++;
                }
                if(certificate==null){nextBurst=model.HazardClock+.5f;return;}
                FallbackEncounters++;
            }
            foreach(var wave in certificate.waves)
            {
                recentPatterns.Enqueue((int)wave.family*PatternLibrary.VariantCount+wave.variant);
                while(recentPatterns.Count>Mathf.Max(2,settings.patternHistoryLength))recentPatterns.Dequeue();
                lastPose=wave.pose;lastPattern=(int)wave.family*PatternLibrary.VariantCount+wave.variant;
            }
            lastTarget=certificate.targetX;hasLastTarget=true;
            // Extra release spacing preserves the certified arrival order.
            float jitter=(float)random.NextDouble()*.35f;
            float delay=jitter;
            foreach(var wave in certificate.waves){wave.id=++nextWaveId;wave.launchTime+=delay;pending.Add(wave);delay+=(float)random.NextDouble()*.25f;}
            certificate.leadTime+=jitter;certificate.reservationUntil+=delay;
            Reservation=certificate;nextBurst=certificate.reservationUntil;
        }
        PatternFamily ChooseFamily(List<PatternFamily> eligible,DodgePose? desiredPose,int previous,float elapsed,Dictionary<PatternFamily,LaserPatternDefinition> definitions)
        {
            var options=new List<PatternFamily>();
            foreach(var family in eligible)
                if((!desiredPose.HasValue||PatternLibrary.RequiredPose(family)==desiredPose.Value)&&(int)family!=previous)options.Add(family);
            if(options.Count==0)foreach(var family in eligible)if((int)family!=previous)options.Add(family);
            if(options.Count==0)options.AddRange(eligible);
            float Weight(PatternFamily family)
            {
                float weight=definitions.TryGetValue(family,out var definition)?Mathf.Max(.1f,definition.selectionWeight):1;
                if(elapsed>=18&&(family==PatternFamily.LowJump||family==PatternFamily.CrouchBeam))weight*=.28f;
                foreach(int recent in recentPatterns)if(recent/PatternLibrary.VariantCount==(int)family)weight*=.3f;
                return weight;
            }
            float total=0;foreach(var option in options)total+=Weight(option);
            float choice=(float)random.NextDouble()*total;
            foreach(var option in options){choice-=Weight(option);if(choice<=0)return option;}
            return options[options.Count-1];
        }
        int ChooseVariant(PatternFamily family)
        {
            var weights=new float[PatternLibrary.VariantCount];float total=0;
            int latest=-1;
            foreach(int recent in recentPatterns)if(recent/PatternLibrary.VariantCount==(int)family)latest=recent%PatternLibrary.VariantCount;
            for(int variant=1;variant<PatternLibrary.VariantCount;variant++)
            {
                if(variant==latest&&PatternLibrary.VariantCount>2)continue;
                float weight=1;
                foreach(int recent in recentPatterns)if(recent==(int)family*PatternLibrary.VariantCount+variant)weight*=.2f;
                weights[variant]=weight;total+=weight;
            }
            float choice=(float)random.NextDouble()*total;
            int lastEligible=1;
            for(int variant=1;variant<PatternLibrary.VariantCount;variant++)
            {
                if(weights[variant]<=0)continue;
                lastEligible=variant;choice-=weights[variant];if(choice<=0)return variant;
            }
            return lastEligible;
        }
        float ChooseTarget(float limit,int attempt,int attempts,bool advanced)
        {
            float current=Mathf.Clamp(player.Feet.x,-limit,limit);
            if(attempt>=attempts*3/4)return current;
            float target=0;
            for(int sample=0;sample<8;sample++)
            {
                target=Mathf.Round(((float)random.NextDouble()*2-1)*limit*20)/20;
                float movement=Mathf.Abs(target-current);
                bool repeated=hasLastTarget&&Mathf.Abs(target-lastTarget)<.35f;
                if(!advanced||(movement>=model.settings.minimumLaneChange&&!repeated))break;
            }
            if(attempt>=attempts/2)target=Mathf.Lerp(current,target,.55f);
            return Mathf.Clamp(target,-limit,limit);
        }
        public static string Name(PatternFamily family)
        {
            switch(family) {case PatternFamily.LowJump:return "ANKLE SWEEP";case PatternFamily.CrouchBeam:return "HIGH SWEEP";case PatternFamily.CornerGrid:return "CORNER WINDOW";case PatternFamily.OffsetGrid:return "OFFSET WINDOW";case PatternFamily.Circle:return "OCTAGON";case PatternFamily.CircleGrid:return "OCTAGONAL APERTURE";default:return family.ToString().ToUpperInvariant();}
        }
        BeamView Get(WavePlan plan)
        {
            var view=pool.Count>0?pool.Pop():new BeamView {root=new GameObject("Laser wave")};view.root.transform.SetParent(transform,false);
            while(view.lines.Count<plan.beams.Count)
            {
                var go=new GameObject("Beam core");go.transform.SetParent(view.root.transform,false);var line=go.AddComponent<LineRenderer>();
                line.sharedMaterial=beamMaterial;line.positionCount=2;line.useWorldSpace=true;line.numCapVertices=3;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;view.lines.Add(line);
                var haloObject=new GameObject("Soft halo");haloObject.transform.SetParent(view.root.transform,false);var halo=haloObject.AddComponent<LineRenderer>();halo.sharedMaterial=haloMaterial?haloMaterial:beamMaterial;halo.positionCount=2;halo.useWorldSpace=true;halo.numCapVertices=3;halo.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;halo.receiveShadows=false;view.halos.Add(halo);
            }
            while(view.nodes.Count<plan.beams.Count*2){var go=new GameObject("Beam endpoint node",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(view.root.transform,false);go.GetComponent<MeshFilter>().sharedMesh=nodeMesh;var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=beamMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;view.nodes.Add(renderer);}
            view.plan=plan;view.touched=false;view.root.SetActive(true);return view;
        }
        void Draw(BeamView view,float z)
        {
            for(int i=0;i<view.lines.Count;i++)
            {
                var line=view.lines[i];var halo=view.halos[i];line.enabled=i<view.plan.beams.Count;halo.enabled=line.enabled;view.nodes[i*2].enabled=view.nodes[i*2+1].enabled=line.enabled;if(!line.enabled) continue;var s=view.plan.beams[i];
                line.SetPosition(0,new Vector3(s.a.x,s.a.y,z));line.SetPosition(1,new Vector3(s.b.x,s.b.y,z));
                line.startWidth=line.endWidth=model.settings.beamRadius*2;
                line.startColor=line.endColor=Color.white;
                beamProperties.Clear();beamProperties.SetColor("_BaseColor",new Color(1,1,1,1));beamProperties.SetColor("_EmissionColor",beamCore);beamProperties.SetFloat("_Halo",0);beamProperties.SetFloat("_BeamClock",model.HazardClock);beamProperties.SetFloat("_GridFlow",0);line.SetPropertyBlock(beamProperties);
                for(int end=0;end<2;end++){var node=view.nodes[i*2+end];var point=end==0?s.a:s.b;node.transform.position=new Vector3(point.x,point.y,z);node.transform.localScale=Vector3.one*model.settings.beamRadius*2;node.SetPropertyBlock(beamProperties);}
                if(halo.enabled){halo.SetPosition(0,new Vector3(s.a.x,s.a.y,z));halo.SetPosition(1,new Vector3(s.b.x,s.b.y,z));halo.startWidth=halo.endWidth=model.settings.beamRadius*5;beamProperties.SetColor("_BaseColor",beamHalo);beamProperties.SetColor("_EmissionColor",new Color(beamHalo.r,beamHalo.g,beamHalo.b,1));beamProperties.SetFloat("_Halo",1);halo.SetPropertyBlock(beamProperties);}
            }
        }
        void Release(BeamView view) {if(audio)audio.ReleaseWave(view.plan);view.root.SetActive(false);pool.Push(view);}

    }
}
