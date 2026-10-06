using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LaserCorridor.Tests
{
    public class ScheduleTests
    {
        static bool Boundary(Vector2 p)=>Mathf.Abs(Mathf.Abs(p.x)-2)<.0001f||Mathf.Abs(p.y)<.0001f||Mathf.Abs(p.y-4.2f)<.0001f;
        static string PointKey(Vector2 p)=>Mathf.RoundToInt(p.x*10000)+","+Mathf.RoundToInt(p.y*10000);
        static string GeometrySignature(IReadOnlyList<BeamSegment> lines)
        {
            var segments=new List<string>();
            foreach(var line in lines)
            {
                string a=PointKey(line.a),b=PointKey(line.b);
                if(string.CompareOrdinal(a,b)>0){string swap=a;a=b;b=swap;}
                segments.Add(a+"-"+b);
            }
            segments.Sort(StringComparer.Ordinal);return string.Join("|",segments);
        }
        static string GeometrySignature(PatternFamily family,int variant,float challenge,float offset=0,bool mirror=false)
        {
            var opening=PatternLibrary.Opening(family,variant,challenge);
            return GeometrySignature(PatternLibrary.Create(family,offset,mirror,opening.width,opening,variant,challenge));
        }
        static float MaximumRouteZ(SafetyCertificate certificate)
        {float z=certificate.playerZ;foreach(var point in certificate.route)z=Mathf.Max(z,point.y);return z;}
        static string Number(float value)=>value.ToString("R",CultureInfo.InvariantCulture);
        static string MirrorObservation(SafetyCertificate certificate,WavePlan wave)
        {
            string actual=GeometrySignature(wave.beams);
            var unmirrored=PatternLibrary.Create(wave.family,certificate.targetX,false,wave.opening.width,wave.opening,wave.variant,wave.challenge);
            var mirrored=PatternLibrary.Create(wave.family,-certificate.targetX,true,wave.opening.width,wave.opening,wave.variant,wave.challenge);
            bool matchesUnmirrored=actual==GeometrySignature(unmirrored),matchesMirrored=actual==GeometrySignature(mirrored);
            if(matchesUnmirrored&&matchesMirrored)return "symmetric";
            if(matchesUnmirrored)return "false";
            if(matchesMirrored)return "true";
            return "unmatched";
        }
        static string ReservationSignature(SafetyCertificate certificate)
        {
            var parts=new List<string>{"target="+Number(certificate.targetX),"playerZ="+Number(certificate.playerZ)};
            foreach(var wave in certificate.waves)
                parts.Add(((int)wave.family)+":"+wave.variant+":"+Number(wave.challenge)+":"+MirrorObservation(certificate,wave)+":"+Number(wave.speed)+":"+Number(wave.launchTime)+":"+GeometrySignature(wave.beams));
            return string.Join("|",parts);
        }
        static void AssertCertified(SafetyCertificate certificate,SafetyValidator validator)
        {
            Assert.That(certificate,Is.Not.Null);
            Assert.That(certificate.waves,Is.Not.Empty);
            Assert.That(certificate.targetX,Is.InRange(-1.71f,1.71f));
            foreach(var wave in certificate.waves)
            {
                Assert.That(wave.beams,Is.Not.Empty);
                Assert.That(MirrorObservation(certificate,wave),Is.Not.EqualTo("unmatched"),"selected mirror orientation must match the reserved beam geometry");
                Assert.That(validator.Clearance(wave,certificate.targetX),Is.True,"every live-selected wave must retain its safety clearance");
            }
        }

        sealed class LiveLaserFixture : IDisposable
        {
            public readonly RunSettings settings;
            public readonly RunModel model;
            public readonly PlayerAvatar player;
            public readonly PanelDirector panels;
            public readonly LaserDirector lasers;
            public readonly SafetyValidator validator;
            readonly GameObject playerRoot,panelRoot,laserRoot;
            readonly MethodInfo planBurst=typeof(LaserDirector).GetMethod("PlanBurst",BindingFlags.Instance|BindingFlags.NonPublic);

            public LiveLaserFixture()
            {
                var savedSettings=AssetDatabase.LoadAssetAtPath<RunSettings>("Assets/LaserCorridor/Data/RunSettings.asset");
                Assert.That(savedSettings,Is.Not.Null,"live selector fixture requires the saved run settings asset");
                var definitions=new LaserPatternDefinition[Enum.GetValues(typeof(PatternFamily)).Length];
                foreach(PatternFamily family in Enum.GetValues(typeof(PatternFamily)))
                {
                    string path="Assets/LaserCorridor/Data/Patterns/"+family+".asset";
                    var definition=AssetDatabase.LoadAssetAtPath<LaserPatternDefinition>(path);
                    Assert.That(definition,Is.Not.Null,"live selector fixture requires the saved pattern asset at "+path);
                    Assert.That(definition.family,Is.EqualTo(family),"asset filename and serialized family must agree");
                    definitions[(int)family]=definition;
                }
                settings=UnityEngine.Object.Instantiate(savedSettings);
                model=new RunModel(settings);validator=new SafetyValidator(settings);

                playerRoot=new GameObject("Schedule test player");
                player=playerRoot.AddComponent<PlayerAvatar>();player.Initialize(settings);

                panelRoot=new GameObject("Schedule test panels");
                panels=panelRoot.AddComponent<PanelDirector>();panels.sockets=Array.Empty<FloorSocket>();

                laserRoot=new GameObject("Schedule test lasers");
                lasers=laserRoot.AddComponent<LaserDirector>();
                lasers.patterns=definitions;
                // Authored combinations have a separate selection path; these tests isolate
                // the randomized procedural selector configured by the saved pattern assets.
                lasers.authoredBursts=Array.Empty<LaserBurstDefinition>();
                lasers.Initialize(model,player,panels,validator);
                panels.Initialize(model,player,validator,lasers);
            }

            public void Begin(int? seed,float elapsed)
            {
                model.Reset();player.ResetPosition();panels.ResetPanels();
                if(seed.HasValue)lasers.ResetWaves(seed.Value);else lasers.ResetWaves();
                model.Start();model.Tick(Mathf.Max(.401f,elapsed));
                lasers.Step(Mathf.Max(0,model.HazardClock-.001f));
            }

            public void PlanAnotherBurst()
            {
                Assert.That(planBurst,Is.Not.Null,"live selection test requires the runtime PlanBurst method");
                lasers.Clear();
                planBurst.Invoke(lasers,null);
            }

            public void Dispose()
            {
                if(lasers)lasers.Clear();
                UnityEngine.Object.DestroyImmediate(laserRoot);
                UnityEngine.Object.DestroyImmediate(panelRoot);
                UnityEngine.Object.DestroyImmediate(playerRoot);
                if(settings)UnityEngine.Object.DestroyImmediate(settings);
            }
        }

        [Test] public void TenThousandSeededContinuousBurstCertificates()
        {
            var settings=ScriptableObject.CreateInstance<RunSettings>();var validator=new SafetyValidator(settings);
            int accepted=0,rejected=0;var covered=new HashSet<PatternFamily>();var combos=new HashSet<string>();
            var variantCoverage=new HashSet<string>();var mirroredVariantCoverage=new HashSet<string>();var challengeBands=new HashSet<int>();
            var tileCenters=TileGrid.EligibleCenters(settings);
            for(int seed=0;seed<10000;seed++)
            {
                var rng=new System.Random(seed);int count=1+rng.Next(3);var families=new PatternFamily[count];var speeds=new float[count];var variants=new int[count];
                float challenge=(float)rng.NextDouble(),limit=1.15f;
                for(int i=0;i<count;i++)
                {
                    families[i]=(PatternFamily)rng.Next(16);variants[i]=rng.Next(PatternLibrary.VariantCount);
                    speeds[i]=seed%2==0?settings.waveSpeeds[rng.Next(4)]:Mathf.Lerp(settings.minimumWaveSpeed,settings.maximumWaveSpeed,(float)rng.NextDouble());
                    limit=Mathf.Min(limit,PatternLibrary.MaximumOffset(families[i],variants[i],challenge));
                }
                float target=Mathf.Clamp(Mathf.Round(((float)rng.NextDouble()*2-1)*limit*10)/10,-limit,limit);
                Vector3 start=new Vector3((float)rng.NextDouble()*3-1.5f,0,1+(float)rng.NextDouble()*10);
                var traps=new List<FloorHazard>();
                for(int t=0;t<rng.Next(3);t++) {var h=new FloorHazard(tileCenters[rng.Next(tileCenters.Count)],.37f);if(Mathf.Abs(start.x-h.center.x)>.85f || Mathf.Abs(start.z-h.center.y)>.85f)traps.Add(h);}
                bool mirror=rng.Next(2)==0;
                if(!validator.TryPlan(start,traps,families,speeds,target,mirror,0,out var c,1.5f,null,null,variants,challenge)) {rejected++;continue;}
                float contact=(settings.emitterZ-MaximumRouteZ(c)-settings.playerRadius-settings.beamRadius)/c.waves[0].speed;
                float posture=c.waves[0].pose==DodgePose.Jump?Mathf.Sqrt(2*settings.jumpHeight/settings.gravity):.025f;
                Assert.That(c.pathLength/(settings.crouchSpeed*settings.slowMultiplier)+settings.reactionAllowance+posture,Is.LessThanOrEqualTo(contact+.001f),"route can be followed after the first visible wave / seed "+seed);
                accepted++;Assert.That(c.leadTime,Is.GreaterThanOrEqualTo(1.75f));
                Assert.That(c.pathLength/(3*.65f)+.35f,Is.LessThanOrEqualTo(c.leadTime));
                challengeBands.Add(Mathf.Clamp(Mathf.FloorToInt(challenge*10),0,9));
                foreach(var h in traps)
                    for(int p=1;p<c.route.Count;p++) Assert.That(BeamMath.SegmentTouchesRect(c.route[p-1],c.route[p],h.center,h.halfSize+.3f+.08f),Is.False,"reserved path intersects trap, seed "+seed);
                // Runtime release jitter is nonnegative and increases with each wave. Apply the
                // same bounded delays here so the sampled contact intervals cover that schedule.
                float releaseDelay=(float)rng.NextDouble()*.35f,delay=releaseDelay;
                for(int i=0;i<c.waves.Count;i++){c.waves[i].launchTime+=delay;delay+=(float)rng.NextDouble()*.25f;}
                // Verify actual capsule geometry independently at fine time samples, including a
                // slowdown active for the complete flight, jumping and changing posture.
                for(int i=0;i<c.waves.Count;i++)
                {
                    var wave=c.waves[i];covered.Add(wave.family);variantCoverage.Add(((int)wave.family)+":"+wave.variant);
                    mirroredVariantCoverage.Add(((int)wave.family)+":"+wave.variant+":"+mirror);
                    Assert.That(wave.variant,Is.EqualTo(variants[i]),"variant metadata / seed "+seed);
                    Assert.That(wave.challenge,Is.EqualTo(challenge).Within(.0001f),"challenge metadata / seed "+seed);
                    float arrival=wave.launchTime+(settings.emitterZ-c.playerZ)/wave.speed;
                    if(i>0)
                    {
                        var before=c.waves[i-1];
                        float jumpFlight=2*Mathf.Sqrt(2*settings.jumpHeight/settings.gravity);
                        float requiredTransition=before.pose==DodgePose.Jump?jumpFlight+.05f:wave.pose==DodgePose.Jump?Mathf.Sqrt(2*settings.jumpHeight/settings.gravity):.05f;
                        float farZ=settings.minimumZ,nearZ=settings.gateZ-settings.playerRadius-.07f;
                        foreach(float z in new[]{farZ,nearZ})foreach(float slowdown in new[]{1f,.65f})
                        {
                            float previousCenter=before.launchTime+(settings.emitterZ-z)/before.speed;
                            float nextCenter=wave.launchTime+(settings.emitterZ-z)/wave.speed;
                            float capsuleHalf=(settings.playerRadius+settings.beamRadius)/before.speed+(settings.playerRadius+settings.beamRadius)/wave.speed;
                            float clearGap=(nextCenter-previousCenter-capsuleHalf)/slowdown;
                            Assert.That(clearGap,Is.GreaterThanOrEqualTo(requiredTransition),"capsule interval / posture transition gap / seed "+seed+" / z "+z);
                        }
                        Assert.That(wave.launchTime-before.launchTime,Is.GreaterThanOrEqualTo(settings.encounterLead+.099f),"posture transition launch gap / seed "+seed);
                        if(seed%2==0)combos.Add(before.speed+"/"+wave.speed);
                    }
                    foreach(float slowdown in new[]{1f,.65f})
                    {
                        float hitAt=arrival/slowdown;
                        for(float t=hitAt-.4f;t<=hitAt+.4f;t+=.02f)
                        {
                            float feet=wave.pose==DodgePose.Jump?Mathf.Max(0,settings.jumpHeight-.5f*settings.gravity*(t-hitAt)*(t-hitAt)):0;
                            float height=wave.pose==DodgePose.Crouch?settings.crouchingHeight:settings.standingHeight;
                            float z=settings.emitterZ-(t*slowdown-wave.launchTime)*wave.speed;
                            foreach(var segment in wave.beams) if(BeamMath.CapsuleHit(segment,z,new Vector3(c.targetX,feet,c.playerZ),height,.3f,.045f)) Assert.Fail("geometry / seed "+seed+" / "+wave.family);
                        }
                    }
                }
                // A future trap occupying the lane must be rejected by the shared validator.
                Assert.That(validator.CanActivateTrap(new FloorHazard(new Vector2(c.targetX,c.playerZ),.43f),new Vector3(-1.6f,0,1),c),Is.False);
            }
            Assert.That(accepted,Is.GreaterThan(1000));Assert.That(covered.Count,Is.EqualTo(16));Assert.That(combos.Count,Is.EqualTo(16));
            Assert.That(variantCoverage.Count,Is.EqualTo(64),"all 16 families x 4 variants receive accepted seeded coverage");
            Assert.That(mirroredVariantCoverage.Count,Is.EqualTo(128),"each family/variant succeeds in mirrored and unmirrored seeded plans");
            Assert.That(challengeBands.Count,Is.EqualTo(10),"accepted seeded plans cover challenge from 0 to 1");
            Directory.CreateDirectory("Artifacts");File.WriteAllText("Artifacts/ScheduleReport.txt","Seeds: 10000\nAccepted: "+accepted+"\nRejected safely: "+rejected+"\nFamilies: "+covered.Count+"\nFamily/variant pairs: "+variantCoverage.Count+" / 64\nMirrored family/variant pairs: "+mirroredVariantCoverage.Count+" / 128\nChallenge bands: "+challengeBands.Count+" / 10\nSpeed pairs: "+combos.Count+"\nIncludes release jitter, 35% movement and hazard slowdowns, floor reservations, posture transitions and boundary-to-boundary straight geometry.\n");
            UnityEngine.Object.DestroyImmediate(settings);
        }
        [Test] public void UnsafeOpeningAndImpossibleFloorRouteAreRejected()
        {
            var s=ScriptableObject.CreateInstance<RunSettings>();var v=new SafetyValidator(s);
            Assert.That(v.TryPlan(new Vector3(0,0,6),new List<FloorHazard>(),new[]{PatternFamily.OffsetGrid},new[]{7f},0,false,0,out _,.4f),Is.False);
            Assert.That(v.TryPlan(new Vector3(0,0,6),new List<FloorHazard>(),new[]{PatternFamily.CrouchBeam,PatternFamily.OffsetGrid},new[]{3.5f,7f},0,false,0,out _,1.5f,new Dictionary<PatternFamily,float>{{PatternFamily.OffsetGrid,.4f}}),Is.False);
            Assert.That(v.TryPlan(new Vector3(0,0,6),new[]{new FloorHazard(new Vector2(0,6),2)},new[]{PatternFamily.CrouchBeam},new[]{3.5f},0,false,0,out _),Is.False);
            UnityEngine.Object.DestroyImmediate(s);
        }
        [Test] public void EveryFamilyVariantCertifiesMirroredAtBothLateralLimitsAcrossChallenge()
        {
            var settings=ScriptableObject.CreateInstance<RunSettings>();var validator=new SafetyValidator(settings);
            var noHazards=new List<FloorHazard>();
            foreach(PatternFamily family in Enum.GetValues(typeof(PatternFamily)))
            foreach(float challenge in new[]{0f,1f})
            {
                var variantSignatures=new HashSet<string>();
                for(int candidate=0;candidate<PatternLibrary.VariantCount;candidate++)variantSignatures.Add(GeometrySignature(family,candidate,challenge));
                Assert.That(variantSignatures.Count,Is.EqualTo(PatternLibrary.VariantCount),family+" variant layouts must have distinct segment geometry at challenge "+challenge);
            }
            foreach(PatternFamily family in Enum.GetValues(typeof(PatternFamily)))
            foreach(int variant in new[]{0,1,2,3})
            foreach(float challenge in new[]{0f,1f})
            {
                if(variant==0)Assert.That(GeometrySignature(family,variant,0),Is.EqualTo(GeometrySignature(family,variant,1)),family+" legacy geometry remains unchanged by challenge");
                else Assert.That(GeometrySignature(family,variant,0),Is.Not.EqualTo(GeometrySignature(family,variant,1)),family+" advanced geometry responds to challenge");

                float limit=PatternLibrary.MaximumOffset(family,variant,challenge);
                foreach(float side in new[]{-1f,1f})foreach(bool mirror in new[]{false,true})
                {
                    float target=side*limit;float lane=mirror?-target:target;
                    var families=new[]{family};var speeds=new[]{settings.maximumWaveSpeed};var variants=new[]{variant};
                    Assert.That(validator.TryPlan(new Vector3(lane,0,6),noHazards,families,speeds,target,mirror,0,out var certificate,1.5f,null,null,variants,challenge),Is.True,
                        family+" variant "+variant+" / challenge "+challenge+" / target "+target+" / mirror "+mirror+" must certify");
                    Assert.That(certificate.targetX,Is.EqualTo(lane).Within(.0001f));
                    var wave=certificate.waves[0];Assert.That(wave.variant,Is.EqualTo(variant));Assert.That(wave.challenge,Is.EqualTo(challenge).Within(.0001f));
                    Assert.That(wave.pose,Is.EqualTo(PatternLibrary.RequiredPose(family,variant)),"authored variant posture is retained");
                    Assert.That(validator.Clearance(wave,certificate.targetX),Is.True,"certificate uses safe variant geometry");
                    Assert.That(wave.beams,Is.Not.Empty);
                    foreach(var line in wave.beams)Assert.That(Boundary(line.a)&&Boundary(line.b),Is.True,family+" variant "+variant+" has a floating endpoint");
                }
            }
            UnityEngine.Object.DestroyImmediate(settings);
        }
        [Test] public void AdvancedOpeningWidthHonorsDefinitionAndExplicitCaps()
        {
            var settings=ScriptableObject.CreateInstance<RunSettings>();var validator=new SafetyValidator(settings);
            var definition=ScriptableObject.CreateInstance<LaserPatternDefinition>();definition.family=PatternFamily.Cross;definition.safeOpeningWidth=1.25f;
            // An advanced variant owns its pose and aperture geometry; the asset only caps its width.
            definition.pose=DodgePose.Crouch;
            var definitions=new Dictionary<PatternFamily,LaserPatternDefinition>{{PatternFamily.Cross,definition}};
            var family=new[]{PatternFamily.Cross};var speed=new[]{5f};var variants=new[]{1};var start=new Vector3(0,0,6);
            Assert.That(validator.TryPlan(start,new List<FloorHazard>(),family,speed,0,false,0,out var capped,1.5f,null,definitions,variants,0),Is.True);
            Assert.That(capped.waves[0].opening.width,Is.EqualTo(1.25f).Within(.0001f),"asset safeOpeningWidth caps an advanced library aperture");
            Assert.That(capped.waves[0].pose,Is.EqualTo(DodgePose.Stand),"variant pose is not replaced by the legacy asset pose");
            Assert.That(GeometrySignature(capped.waves[0].beams),Is.EqualTo(GeometrySignature(PatternLibrary.Create(PatternFamily.Cross,0,false,1.25f,capped.waves[0].opening,1,0))),"certified segments use the capped geometry");

            var explicitCaps=new Dictionary<PatternFamily,float>{{PatternFamily.Cross,1.1f}};
            Assert.That(validator.TryPlan(start,new List<FloorHazard>(),family,speed,0,false,0,out var explicitlyCapped,1.5f,explicitCaps,definitions,variants,0),Is.True);
            Assert.That(explicitlyCapped.waves[0].opening.width,Is.EqualTo(1.1f).Within(.0001f),"explicit width cap further narrows the advanced aperture");
            Assert.That(explicitlyCapped.waves[0].opening.width,Is.LessThanOrEqualTo(definition.safeOpeningWidth));

            definition.safeOpeningWidth=.4f;explicitCaps[PatternFamily.Cross]=.4f;
            Assert.That(validator.TryPlan(start,new List<FloorHazard>(),family,speed,0,false,0,out var rejected,.4f,explicitCaps,definitions,variants,0),Is.False,"an unsafe supplied width cannot be widened to a library default");
            Assert.That(rejected,Is.Null);
            UnityEngine.Object.DestroyImmediate(definition);UnityEngine.Object.DestroyImmediate(settings);
        }
        [Test] public void InvalidVariantMetadataIsRejectedAndFiniteChallengeIsClamped()
        {
            var settings=ScriptableObject.CreateInstance<RunSettings>();var validator=new SafetyValidator(settings);
            var families=new[]{PatternFamily.Cross,PatternFamily.CrouchBeam};var speeds=new[]{5f,5f};var start=new Vector3(0,0,6);var noHazards=new List<FloorHazard>();
            foreach(var invalid in new[]{new[]{1},new[]{-1,0},new[]{0,PatternLibrary.VariantCount}})
            {
                SafetyCertificate certificate=null;
                Assert.That(validator.TryPlan(start,noHazards,families,speeds,0,false,0,out certificate,1.5f,null,null,invalid,0),Is.False,"invalid variant array is rejected");
                Assert.That(certificate,Is.Null);
            }
            foreach(float invalidChallenge in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity})
            {
                SafetyCertificate certificate=null;
                Assert.That(validator.TryPlan(start,noHazards,new[]{PatternFamily.Cross},new[]{5f},0,false,0,out certificate,1.5f,null,null,new[]{1},invalidChallenge),Is.False,"nonfinite challenge is rejected");
                Assert.That(certificate,Is.Null);
            }
            Assert.That(validator.TryPlan(start,noHazards,new[]{PatternFamily.Cross},new[]{5f},0,false,0,out var clamped,1.5f,null,null,new[]{1},2),Is.True);
            Assert.That(clamped.waves[0].challenge,Is.EqualTo(1).Within(.0001f),"finite challenge is clamped to the supported range");
            UnityEngine.Object.DestroyImmediate(settings);
        }
        [Test] public void EveryPatternLineTerminatesAtCorridorBoundaries()
        {
            foreach(PatternFamily family in Enum.GetValues(typeof(PatternFamily)))foreach(int variant in new[]{0,1,2,3})foreach(float challenge in new[]{0f,1f})foreach(bool mirror in new[]{false,true})foreach(float fraction in new[]{-1f,-.5f,0,.5f,1})
            {
                var opening=PatternLibrary.Opening(family,variant,challenge);
                float offset=PatternLibrary.MaximumOffset(family,variant,challenge)*fraction;
                var lines=PatternLibrary.Create(family,offset,mirror,opening.width,opening,variant,challenge);
                Assert.That(lines,Is.Not.Empty);
                if(variant==0)Assert.That(GeometrySignature(lines),Is.EqualTo(GeometrySignature(PatternLibrary.Create(family,offset,mirror,opening.width))),family+" variant zero preserves the legacy Create overload");
                foreach(var line in lines)
                {
                    Assert.That(Boundary(line.a)&&Boundary(line.b),Is.True,family+" has a floating end");
                    foreach(var p in new[]{line.a,line.b}){Assert.That(p.x,Is.InRange(-2.0001f,2.0001f));Assert.That(p.y,Is.InRange(-.0001f,4.2001f));}
                }
            }
            Assert.That(PatternLibrary.Create(PatternFamily.Circle,0,false,3.2f).Count,Is.EqualTo(8),"eight full straight sides replace the sampled curve");
        }

        [Test] public void LiveOpeningSelectionVariesAcrossDeterministicSeeds()
        {
            using(var fixture=new LiveLaserFixture())
            {
                var counts=new Dictionary<PatternFamily,int>();int fallbackCount=0;
                for(int seed=0;seed<64;seed++)
                {
                    fixture.Begin(seed,.401f);
                    Assert.That(fixture.lasers.CurrentSeed,Is.EqualTo(seed));
                    var certificate=fixture.lasers.Reservation;AssertCertified(certificate,fixture.validator);
                    Assert.That(certificate.waves,Has.Count.EqualTo(1),"opening encounters remain a single wave");
                    var family=certificate.waves[0].family;
                    Assert.That((int)family,Is.LessThan(4),"before the unlock timer only the four opening patterns can be selected");
                    Assert.That(certificate.waves[0].variant,Is.Zero,"advanced variants remain locked during the opening");
                    if(!counts.ContainsKey(family))counts[family]=0;counts[family]++;
                    fallbackCount+=fixture.lasers.FallbackEncounters;
                }
                var report=new List<string>();foreach(var family in new[]{PatternFamily.LowJump,PatternFamily.CrouchBeam,PatternFamily.VerticalCurtain,PatternFamily.DiagonalSlash})report.Add(family+":"+(counts.TryGetValue(family,out var count)?count:0));
                TestContext.WriteLine("Live opening selection over 64 seeds: "+string.Join(", ",report)+"; fallbacks="+fallbackCount);
                Assert.That(counts.Count,Is.GreaterThanOrEqualTo(3),"the live opener should reach at least three of its four eligible families");
                Assert.That(fallbackCount,Is.Zero,"all opening picks should certify directly without the legacy fallback");
            }
        }

        [Test] public void SeededLivePlansReplayExactlyAndFreshResetProducesNewReplayableSeed()
        {
            using(var fixture=new LiveLaserFixture())
            {
                foreach(int seed in new[]{0,1,17,9281,int.MaxValue})
                {
                    fixture.Begin(seed,.401f);var first=fixture.lasers.Reservation;AssertCertified(first,fixture.validator);
                    string firstSignature=ReservationSignature(first);
                    fixture.Begin(seed,.401f);var replay=fixture.lasers.Reservation;AssertCertified(replay,fixture.validator);
                    Assert.That(ReservationSignature(replay),Is.EqualTo(firstSignature),"family, variant, target, mirror geometry, speed, release time and safety certificate replay for seed "+seed);
                }

                fixture.lasers.SetSeed(7341);
                Assert.That(fixture.lasers.CurrentSeed,Is.EqualTo(7341),"the existing SetSeed entry point remains supported");

                fixture.Begin(null,.401f);int firstFreshSeed=fixture.lasers.CurrentSeed;
                var firstFreshCertificate=fixture.lasers.Reservation;AssertCertified(firstFreshCertificate,fixture.validator);
                string firstFreshPlan=ReservationSignature(firstFreshCertificate);
                fixture.Begin(null,.401f);int secondFreshSeed=fixture.lasers.CurrentSeed;
                var secondFreshCertificate=fixture.lasers.Reservation;AssertCertified(secondFreshCertificate,fixture.validator);
                string secondFreshPlan=ReservationSignature(secondFreshCertificate);
                Assert.That(secondFreshSeed,Is.Not.EqualTo(firstFreshSeed),"consecutive parameterless resets must not reuse the seed");
                fixture.Begin(firstFreshSeed,.401f);
                Assert.That(ReservationSignature(fixture.lasers.Reservation),Is.EqualTo(firstFreshPlan),"the first fresh seed must reproduce its captured live reservation");
                fixture.Begin(secondFreshSeed,.401f);
                Assert.That(ReservationSignature(fixture.lasers.Reservation),Is.EqualTo(secondFreshPlan),"the second fresh seed must reproduce its captured live reservation");
            }
        }

        [Test] public void FullyUnlockedLivePoolVariesPatternsAndAvoidsRecentRepeats()
        {
            using(var fixture=new LiveLaserFixture())
            {
                var families=new HashSet<PatternFamily>();var variants=new HashSet<int>();var familyVariants=new HashSet<string>();var geometries=new HashSet<string>();
                int fallbackCount=0;
                for(int seed=0;seed<64;seed++)
                {
                    fixture.Begin(seed,38.1f);
                    fallbackCount+=fixture.lasers.FallbackEncounters;
                    var recent=new List<KeyValuePair<PatternFamily,int>>();
                    for(int encounter=0;encounter<4;encounter++)
                    {
                        var certificate=fixture.lasers.Reservation;AssertCertified(certificate,fixture.validator);
                        Assert.That(certificate.waves,Is.Not.Empty);
                        foreach(var wave in certificate.waves)
                        {
                            Assert.That(wave.variant,Is.InRange(1,PatternLibrary.VariantCount-1),"advanced variants are eligible after the unlock timer");
                            if(recent.Count>0)Assert.That(wave.family,Is.Not.EqualTo(recent[recent.Count-1].Key),"the next certified wave avoids the immediately previous family while alternatives exist");
                            for(int i=recent.Count-1;i>=0;i--)
                            {
                                if(recent[i].Key==wave.family)
                                {
                                    Assert.That(wave.variant,Is.Not.EqualTo(recent[i].Value),"a returning family avoids its most recently certified variant");
                                    break;
                                }
                            }
                            families.Add(wave.family);variants.Add(wave.variant);familyVariants.Add(wave.family+":"+wave.variant);
                            geometries.Add(ReservationSignature(new SafetyCertificate{targetX=certificate.targetX,playerZ=certificate.playerZ,waves=new List<WavePlan>{wave}}));
                            recent.Add(new KeyValuePair<PatternFamily,int>(wave.family,wave.variant));
                            if(recent.Count>fixture.settings.patternHistoryLength)recent.RemoveAt(0);
                        }
                        if(encounter<3)
                        {
                            int previousFallbacks=fixture.lasers.FallbackEncounters;
                            fixture.PlanAnotherBurst();
                            fallbackCount+=fixture.lasers.FallbackEncounters-previousFallbacks;
                        }
                    }
                }
                var familyReport=new List<string>();foreach(var family in families)familyReport.Add(family.ToString());familyReport.Sort(StringComparer.Ordinal);
                TestContext.WriteLine("Live fully unlocked selection: "+families.Count+" families ["+string.Join(", ",familyReport)+"], "+variants.Count+" variants, "+familyVariants.Count+" family/variant pairs, "+geometries.Count+" reservation geometries; fallbacks="+fallbackCount);
                Assert.That(families.Count,Is.GreaterThanOrEqualTo(10),"the fully unlocked live selector should exercise a broad family pool");
                Assert.That(variants.Count,Is.GreaterThanOrEqualTo(2),"advanced variants should vary across seeded plans");
                Assert.That(familyVariants.Count,Is.GreaterThanOrEqualTo(24),"selection should vary the family/variant combination, not only the target lane");
                Assert.That(geometries.Count,Is.GreaterThan(64),"different seeds and consecutive encounters should produce different actual beam plans");
                Assert.That(fallbackCount,Is.Zero,"all sampled unlocked choices should remain directly certifiable");
            }
        }
    }
}
