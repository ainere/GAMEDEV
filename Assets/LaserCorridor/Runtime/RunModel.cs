using System;
using UnityEngine;

namespace LaserCorridor
{
    // All gameplay clocks and state transitions live here; graphics never own game rules.
    public sealed class RunModel
    {
        public readonly RunSettings settings;
        public RunState State { get; private set; }
        public bool Paused { get; private set; }
        public int Health { get; private set; }
        public float Remaining { get; private set; }
        public float Elapsed { get; private set; }
        public float HazardClock { get; private set; }
        public float GraceUntil { get; private set; }
        public float ShieldUntil { get; private set; }
        public float LaserSlowUntil { get; private set; }
        public float MovementSlowUntil { get; private set; }
        public float PickupReadyAt { get; private set; }
        public readonly float[] EffectReadyAt = new float[6];
        public string DeathReason { get; private set; } = "";
        public string Notice { get; private set; } = "";
        public float NoticeUntil { get; private set; }
        public bool Shielded => State == RunState.Running && ShieldUntil > Elapsed;
        public bool LaserSlowed => State == RunState.Running && LaserSlowUntil > Elapsed;
        public bool MovementSlowed => State == RunState.Running && MovementSlowUntil > Elapsed;
        public float MovementMultiplier => MovementSlowed ? settings.slowMultiplier : 1;
        public event Action<RunState> StateChanged;
        public event Action<string> Feedback;
        public event Action<HitRegion> Hurt;

        public RunModel(RunSettings configuration) { settings=configuration; Reset(); }
        public void Reset()
        {
            Health=settings.maximumHealth; Remaining=settings.duration; Elapsed=HazardClock=0;
            GraceUntil=ShieldUntil=LaserSlowUntil=MovementSlowUntil=PickupReadyAt=0;
            Array.Clear(EffectReadyAt,0,EffectReadyAt.Length);
            DeathReason=Notice=""; NoticeUntil=0; Paused=false; ChangeState(RunState.Ready);
        }
        public void Start() { if(State==RunState.Ready) { ChangeState(RunState.Running); Say("SURVIVE "+settings.duration.ToString("0")+" SECONDS"); } }
        public void TogglePause() { if(State==RunState.Running || State==RunState.ExitOpen) Paused=!Paused; }
        public void Tick(float dt)
        {
            if(Paused || State!=RunState.Running || dt<=0) return;
            // Integrate a slowdown expiration exactly, even when a frame straddles it.
            float slowed = Mathf.Clamp(LaserSlowUntil-Elapsed,0,dt);
            HazardClock += slowed * settings.slowMultiplier + dt-slowed;
            Elapsed += dt; Remaining=Mathf.Max(0,Remaining-dt);
            if(Remaining<=0) OpenExit();
        }
        public bool Damage(int amount,string reason,HitRegion region=HitRegion.Chest)
        {
            if(State!=RunState.Running || Paused || Elapsed<GraceUntil) return false;
            GraceUntil=Elapsed+settings.damageGrace;
            if(Shielded) { ShieldUntil=0; Say("SHIELD ABSORBED HIT"); Feedback?.Invoke("shield"); return true; }
            Health=Mathf.Max(0,Health-amount); Feedback?.Invoke(reason.Contains("SHOCK")?"shock":"damage"); Say(reason+" · -"+amount+" HP");
            if(Health==0) Kill(reason);
            else Hurt?.Invoke(region);
            return true;
        }
        public void TouchExitGrid() { if(State==RunState.Running && !Paused) Kill("EXIT LASERS — SHIELDS CANNOT BLOCK THEM"); }
        public void Kill(string reason)
        {
            if(State!=RunState.Running) return;
            Health=0; ShieldUntil=LaserSlowUntil=MovementSlowUntil=0; DeathReason=reason;
            Feedback?.Invoke("death"); ChangeState(RunState.Dead);
        }
        public void ReachExit() { if(State==RunState.ExitOpen && !Paused) { ChangeState(RunState.Won); Feedback?.Invoke("win"); } }
        public bool CanOffer(PanelEffect effect) => effect>=PanelEffect.Healing && effect<=PanelEffect.LaserSlow && Elapsed>=EffectReadyAt[(int)effect];
        public bool ApplyPanel(PanelEffect effect)
        {
            if(State!=RunState.Running || Paused) return false;
            switch(effect)
            {
                case PanelEffect.Healing:
                case PanelEffect.Shield:
                case PanelEffect.LaserSlow:
                    if(!CanOffer(effect) || Elapsed<PickupReadyAt) return false;
                    if(effect==PanelEffect.Healing && Health>=settings.maximumHealth) return false;
                    if(effect==PanelEffect.Healing) { Health=Mathf.Min(settings.maximumHealth,Health+settings.healAmount); Say("HEALING · +"+settings.healAmount+" HP"); }
                    else if(effect==PanelEffect.Shield) { ShieldUntil=Elapsed+settings.shieldDuration; Say("SHIELD · BLOCKS ONE HIT FOR "+settings.shieldDuration.ToString("0")+" SECONDS"); }
                    else { LaserSlowUntil=Elapsed+settings.laserSlowDuration; Say("LASERS SLOWED · "+settings.laserSlowDuration.ToString("0")+" SECONDS"); }
                    EffectReadyAt[(int)effect]=Elapsed+settings.effectCooldown; PickupReadyAt=Elapsed+settings.pickupCooldown;
                    Feedback?.Invoke("pickup:"+effect); return true;
                case PanelEffect.MovementSlow:
                    MovementSlowUntil=Elapsed+settings.movementSlowDuration; Say("MOVEMENT SLOWED · "+settings.movementSlowDuration.ToString("0")+" SECONDS"); Feedback?.Invoke("trap"); return true;
                case PanelEffect.Shock:
                    bool hadShield=Shielded;
                    if(!Damage(settings.shockDamage,"SHOCK PLATE",HitRegion.Legs))
                    { Say("SHOCK PLATE · BLOCKED BY DAMAGE PROTECTION"); Feedback?.Invoke("trap"); }
                    else if(hadShield) Say("SHOCK PLATE · SHIELD ABSORBED HIT");
                    return true;
                default: return false;
            }
        }
        void OpenExit()
        {
            Remaining=0; ShieldUntil=LaserSlowUntil=MovementSlowUntil=0;
            Say("LASERS OFF · REACH THE EXIT"); ChangeState(RunState.ExitOpen); Feedback?.Invoke("open");
        }
        void ChangeState(RunState state) { State=state; StateChanged?.Invoke(state); }
        void Say(string message) { Notice=message; NoticeUntil=Elapsed+3; }
    }
}
