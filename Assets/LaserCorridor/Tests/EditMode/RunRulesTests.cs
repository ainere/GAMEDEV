using NUnit.Framework;
using UnityEngine;

namespace LaserCorridor.Tests
{
    public class RunRulesTests
    {
        RunSettings settings;RunModel model;
        [SetUp] public void Setup(){settings=ScriptableObject.CreateInstance<RunSettings>();model=new RunModel(settings);model.Start();}
        [TearDown] public void Cleanup(){Object.DestroyImmediate(settings);}
        [Test] public void OrdinaryDamageGraceShieldAndExpiry()
        {
            Assert.That(model.Damage(25,"laser"),Is.True);Assert.That(model.Health,Is.EqualTo(75));
            Assert.That(model.Damage(25,"laser"),Is.False);model.Tick(.751f);
            Assert.That(model.ApplyPanel(PanelEffect.Shield),Is.True);model.Damage(25,"laser");Assert.That(model.Health,Is.EqualTo(75));Assert.That(model.Shielded,Is.False);
            model.Tick(25);model.ApplyPanel(PanelEffect.Shield);model.Tick(10.01f);Assert.That(model.Shielded,Is.False);
        }
        [Test] public void ExitGridBypassesShieldAndGrace()
        {
            model.ApplyPanel(PanelEffect.Shield);model.Damage(25,"protected hit");model.TouchExitGrid();
            Assert.That(model.State,Is.EqualTo(RunState.Dead));Assert.That(model.Health,Is.Zero);Assert.That(model.DeathReason,Does.Contain("EXIT LASERS"));
            model.Reset();model.Start();model.ApplyPanel(PanelEffect.Shield);model.TouchExitGrid();Assert.That(model.State,Is.EqualTo(RunState.Dead));
        }
        [Test] public void SurvivalAlwaysRequiresNinetyGameplaySeconds()
        {
            model.Tick(60);Assert.That(model.State,Is.EqualTo(RunState.Running));Assert.That(model.Remaining,Is.EqualTo(30));
            model.Tick(29.9f);Assert.That(model.State,Is.EqualTo(RunState.Running));model.Tick(.11f);Assert.That(model.State,Is.EqualTo(RunState.ExitOpen));
        }
        [Test] public void PauseFreezesBothClocksAndInteraction()
        {
            model.ApplyPanel(PanelEffect.LaserSlow);model.TogglePause();model.Tick(4);model.Damage(25,"laser");
            Assert.That(model.Elapsed,Is.Zero);Assert.That(model.HazardClock,Is.Zero);Assert.That(model.Health,Is.EqualTo(100));
            model.TogglePause();model.Tick(6);Assert.That(model.HazardClock,Is.EqualTo(4.25f).Within(.001));Assert.That(model.Remaining,Is.EqualTo(84));
        }
        [Test] public void ShutdownClearsHarmfulEffectsAndRequiresExitCrossing()
        {
            model.ApplyPanel(PanelEffect.MovementSlow);model.Tick(90);Assert.That(model.State,Is.EqualTo(RunState.ExitOpen));
            Assert.That(model.MovementSlowed,Is.False);model.TouchExitGrid();Assert.That(model.State,Is.EqualTo(RunState.ExitOpen));
            model.ReachExit();Assert.That(model.State,Is.EqualTo(RunState.Won));
        }
        [Test] public void HealingAndCooldownsDoNotConsumeFullHealthPickup()
        {
            Assert.That(model.ApplyPanel(PanelEffect.Healing),Is.False);Assert.That(model.PickupReadyAt,Is.Zero);
            model.Damage(25,"laser");Assert.That(model.ApplyPanel(PanelEffect.Healing),Is.True);Assert.That(model.Health,Is.EqualTo(100));
            Assert.That(model.ApplyPanel(PanelEffect.LaserSlow),Is.False);model.Tick(6);Assert.That(model.ApplyPanel(PanelEffect.LaserSlow),Is.True);
            Assert.That(model.CanOffer(PanelEffect.Healing),Is.False);model.Tick(18);Assert.That(model.CanOffer(PanelEffect.Healing),Is.True);
        }
        [Test] public void SlowRefreshDoesNotStackAndResetClearsEverything()
        {
            model.ApplyPanel(PanelEffect.MovementSlow);model.Tick(2);model.ApplyPanel(PanelEffect.MovementSlow);
            Assert.That(model.MovementMultiplier,Is.EqualTo(.65f));Assert.That(model.MovementSlowUntil,Is.EqualTo(6));
            model.ApplyPanel(PanelEffect.Shield);model.Kill("test");model.Reset();
            Assert.That(model.State,Is.EqualTo(RunState.Ready));Assert.That(model.Health,Is.EqualTo(100));Assert.That(model.Remaining,Is.EqualTo(90));
            Assert.That(model.HazardClock,Is.Zero);Assert.That(model.Elapsed,Is.Zero);
            Assert.That(model.EffectReadyAt,Is.All.Zero);Assert.That(model.DeathReason,Is.Empty);Assert.That(model.Paused,Is.False);
        }
        [TestCase(30)][TestCase(60)][TestCase(120)][TestCase(4)]
        public void SweptCollisionCatchesRelativeMotionAndTunneling(int fps)
        {
            var beam=new BeamSegment(new Vector2(-2,.9f),new Vector2(2,.9f));
            Vector3 feet=new Vector3(0,0,6);float z=8;bool hit=false;
            for(float time=0;time<1;time+=1f/fps)
            {
                float next=z-10f/fps;Vector3 moved=feet+Vector3.back*(4.5f/fps);
                hit|=BeamMath.SweptHit(beam,z,next,feet,moved,1.8f,.3f,.045f);z=next;feet=moved;
            }
            Assert.That(hit,Is.True);
            Assert.That(BeamMath.SweptHit(beam,7,5,new Vector3(0,0,6),new Vector3(0,0,6),.6f,.3f,.045f),Is.False);
        }
        [TestCase(.25f,HitRegion.Legs)][TestCase(1.1f,HitRegion.Chest)][TestCase(1.7f,HitRegion.Head)]
        public void SweptContactsReportActualHitHeight(float y,HitRegion expected)
        {
            Vector3 feet=new Vector3(0,0,6);var beam=new BeamSegment(new Vector2(-2,y),new Vector2(2,y));
            Assert.That(BeamMath.SweptContact(beam,7,5,feet,feet,1.8f,.3f,.045f,out var contact,out float time),Is.True);
            Assert.That(time,Is.InRange(0,1));Assert.That(BeamMath.Region(contact,feet,1.8f),Is.EqualTo(expected));
        }
        [Test] public void HurtFeedbackOnlyFiresOnSurvivingUnprotectedDamage()
        {
            int reactions=0;HitRegion last=HitRegion.Chest;model.Hurt+=region=>{reactions++;last=region;};
            model.ApplyPanel(PanelEffect.Shield);model.Damage(25,"laser",HitRegion.Head);Assert.That(reactions,Is.Zero);
            model.Tick(.8f);model.Damage(25,"laser",HitRegion.Head);Assert.That(reactions,Is.EqualTo(1));Assert.That(last,Is.EqualTo(HitRegion.Head));
            model.Damage(25,"laser",HitRegion.Legs);Assert.That(reactions,Is.EqualTo(1));
            model.Tick(.8f);model.ApplyPanel(PanelEffect.Shock);Assert.That(reactions,Is.EqualTo(2));Assert.That(last,Is.EqualTo(HitRegion.Legs));
            model.Tick(.8f);model.Damage(100,"lethal");Assert.That(reactions,Is.EqualTo(2));Assert.That(model.State,Is.EqualTo(RunState.Dead));
        }
        [Test] public void ShockPanelDuringDamageGraceIsConsumedButProtected()
        {
            int hurtEvents=0;model.Hurt+=_=>hurtEvents++;
            Assert.That(model.Damage(10,"laser"),Is.True);
            int health=model.Health;float graceUntil=model.GraceUntil;float previousNoticeUntil=model.NoticeUntil;
            model.Tick(.2f);

            Assert.That(model.ApplyPanel(PanelEffect.Shock),Is.True);
            Assert.That(model.Health,Is.EqualTo(health));
            Assert.That(model.GraceUntil,Is.EqualTo(graceUntil).Within(.001f));
            Assert.That(hurtEvents,Is.EqualTo(1));
            Assert.That(model.Notice,Does.Contain("SHOCK PLATE").And.Contain("DAMAGE PROTECTION"));
            Assert.That(model.NoticeUntil,Is.EqualTo(model.Elapsed+3).Within(.001f));
            Assert.That(model.NoticeUntil,Is.GreaterThan(previousNoticeUntil));
        }
        [Test] public void ShieldedShockPanelConsumesShieldWithoutDamageOrHurt()
        {
            int hurtEvents=0;model.Hurt+=_=>hurtEvents++;
            Assert.That(model.ApplyPanel(PanelEffect.Shield),Is.True);
            model.Tick(.2f);int health=model.Health;float previousNoticeUntil=model.NoticeUntil;
            Assert.That(model.Shielded,Is.True);

            Assert.That(model.ApplyPanel(PanelEffect.Shock),Is.True);
            Assert.That(model.Health,Is.EqualTo(health));
            Assert.That(model.Shielded,Is.False);Assert.That(model.ShieldUntil,Is.Zero);
            Assert.That(hurtEvents,Is.Zero);
            Assert.That(model.Notice,Does.Contain("SHOCK PLATE").And.Contain("SHIELD ABSORBED"));
            Assert.That(model.NoticeUntil,Is.EqualTo(model.Elapsed+3).Within(.001f));
            Assert.That(model.NoticeUntil,Is.GreaterThan(previousNoticeUntil));
        }
    }
}
