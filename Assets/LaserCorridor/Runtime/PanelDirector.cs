using System;
using System.Collections.Generic;
using UnityEngine;

namespace LaserCorridor
{
    public sealed class PanelDirector : MonoBehaviour
    {
        public FloorSocket[] sockets;
        public PanelEffectDefinition[] definitions;
        RunModel model;PlayerAvatar player;SafetyValidator validator;LaserDirector lasers;
        System.Random random;int cycle=-1,lastHelpful=-1;bool activated;
        public int Cycle => cycle;
        public string Phase {get;private set;}="STANDBY";
        public int HelpfulPickups {get;private set;}
        public int HarmfulContacts {get;private set;}
        public int HelpfulActivations {get;private set;}
        public void Initialize(RunModel state,PlayerAvatar avatar,SafetyValidator safety,LaserDirector hazard)
        {model=state;player=avatar;validator=safety;lasers=hazard;ResetPanels();}
        public void ResetPanels()
        {
            cycle=-1;lastHelpful=-1;activated=false;random=new System.Random(Environment.TickCount^73819);
            Phase="STANDBY";HelpfulPickups=HarmfulContacts=HelpfulActivations=0;
            foreach(var socket in sockets) {socket.effect=PanelEffect.None;socket.consumed=socket.skipped=false;socket.SetNeutral();}
            RefreshVisuals();
        }
        public void SetSeed(int seed) {random=new System.Random(seed);}
        public List<FloorHazard> GetHazards()
        {
            var result=new List<FloorHazard>();
            if(model.State!=RunState.Running) return result;
            float age=model.Elapsed%model.settings.panelCycle;
            if(age>=model.settings.panelRamp+model.settings.panelActive) return result;
            foreach(var socket in sockets)
                if(Harmful(socket.effect) && !socket.consumed && !socket.skipped) result.Add(new FloorHazard(socket.Point,socket.halfSize));
            return result;
        }
        public void Step(float dt)
        {
            if(model.State!=RunState.Running) return;
            int next=Mathf.FloorToInt(model.Elapsed/model.settings.panelCycle);
            if(next!=cycle) SelectCycle(next);
            float age=model.Elapsed-cycle*model.settings.panelCycle;
            bool active=age>=model.settings.panelRamp && age<model.settings.panelRamp+model.settings.panelActive;
            if(active && !activated)
            {
                activated=true;
                foreach(var socket in sockets)
                    if(Harmful(socket.effect) && !validator.CanActivateTrap(new FloorHazard(socket.Point,socket.halfSize),player.Feet,lasers.Reservation)) socket.skipped=true;
            }
            // Helpful illumination is usable immediately; only traps need an arming delay.
            if(age<model.settings.panelRamp+model.settings.panelActive && player.Grounded)
                foreach(var socket in sockets)
                    if(socket.effect!=PanelEffect.None && (!Harmful(socket.effect)||active) && !socket.consumed && !socket.skipped && socket.GroundContact(player,model.settings.playerRadius*.7f))
                    {
                        socket.consumed=model.ApplyPanel(socket.effect);
                        if(socket.consumed){if(Harmful(socket.effect))HarmfulContacts++;else HelpfulPickups++;}
                    }
            Phase=age<model.settings.panelRamp?"SOCKETS ARMING":active?"SOCKETS LIVE":"SOCKETS COOLING";
        }
        void SelectCycle(int index)
        {
            cycle=index;activated=false;
            foreach(var socket in sockets) {socket.effect=PanelEffect.None;socket.consumed=socket.skipped=false;}
            var benefits=new List<PanelEffect>();
            for(int e=1;e<=3;e++) if(model.CanOffer((PanelEffect)e)) benefits.Add((PanelEffect)e);
            if(benefits.Count>0)
            {
                var available=new List<int>();var ahead=new List<int>();var feet=new Vector2(player.Feet.x,player.Feet.z);
                for(int i=0;i<sockets.Length;i++)
                {
                    if(Vector2.Distance(sockets[i].Point,feet)<1.6f)continue;
                    if(lastHelpful>=0&&Neighbour(sockets[i].Point,sockets[lastHelpful].Point))continue;
                    available.Add(i);if(sockets[i].Point.y>=feet.y-model.settings.tilePitch)ahead.Add(i);
                }
                if(available.Count==0)for(int i=0;i<sockets.Length;i++)if(i!=lastHelpful)available.Add(i);
                var choices=ahead.Count>0?ahead:available;int helpful=choices[random.Next(choices.Count)];
                var effect=model.Health<=50 && model.CanOffer(PanelEffect.Healing)?PanelEffect.Healing:benefits[random.Next(benefits.Count)];
                sockets[helpful].effect=effect;lastHelpful=helpful;HelpfulActivations++;
            }
            int traps=index==0?0:model.Elapsed<27?1:2;
            var candidates=new List<int>();for(int i=0;i<sockets.Length;i++) if(sockets[i].effect==PanelEffect.None&&(lastHelpful<0||!Neighbour(sockets[i].Point,sockets[lastHelpful].Point))) candidates.Add(i);
            for(int t=0;t<traps && candidates.Count>0;t++)
            {
                int at=random.Next(candidates.Count),chosen=candidates[at];candidates.RemoveAt(at);var socket=sockets[chosen];
                if(!validator.CanActivateTrap(new FloorHazard(socket.Point,socket.halfSize),player.Feet,lasers.Reservation)) {t--;continue;}
                socket.effect=random.Next(2)==0?PanelEffect.MovementSlow:PanelEffect.Shock;
            }
        }
        public void RefreshVisuals()
        {
            bool running=model.State==RunState.Running;
            if(model.State==RunState.ExitOpen||model.State==RunState.Dead||model.State==RunState.Won){Phase="SOCKETS OFFLINE";}
            float age=running?model.Elapsed%model.settings.panelCycle:0;
            foreach(var socket in sockets)
            {
                bool available=socket.effect!=PanelEffect.Healing||model.Health<model.settings.maximumHealth;
                if(!Harmful(socket.effect)&&socket.effect!=PanelEffect.None)available&=model.CanOffer(socket.effect)&&model.Elapsed>=model.PickupReadyAt;
                bool on=running && available && age<model.settings.panelRamp+model.settings.panelActive && !socket.consumed && !socket.skipped && socket.effect!=PanelEffect.None;
                if(on&&age<model.settings.panelRamp)socket.SetReveal(socket.effect,Harmful(socket.effect),age/model.settings.panelRamp);
                else if(on)socket.SetActive(socket.effect,Harmful(socket.effect),model.Elapsed);
                else if(running&&socket.consumed&&age<model.settings.panelRamp+model.settings.panelActive)socket.SetSpent(model.Elapsed);
                else socket.SetNeutral();
            }
        }
        PanelEffectDefinition Definition(PanelEffect effect) {if(definitions!=null)foreach(var d in definitions)if(d.effect==effect)return d;return null;}
        bool Neighbour(Vector2 a,Vector2 b) => Mathf.Abs(a.x-b.x)<=model.settings.tilePitch+.01f&&Mathf.Abs(a.y-b.y)<=model.settings.tilePitch+.01f;
        public static bool Harmful(PanelEffect effect) => effect==PanelEffect.MovementSlow || effect==PanelEffect.Shock;
    }
}
