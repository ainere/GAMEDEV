using UnityEngine;
using UnityEngine.InputSystem;

namespace LaserCorridor
{
    public sealed class RunController : MonoBehaviour
    {
        public RunSettings settings;
        public PlayerAvatar player;
        public LaserDirector lasers;
        public PanelDirector panels;
        public CorridorCamera securityCamera;
        public CorridorHud hud;
        public GameObject exitGrid;
        public CorridorAudio audio;
        public AudioClip bonusSound,damageSound,shutdownSound;
        public RunModel Model {get;private set;}
        public SafetyValidator Validator {get;private set;}
        public bool Automated {get;set;}
        public int SimulationSteps {get;private set;}
        float deadTime;


        void Awake()
        {
            settings=Instantiate(settings);
            foreach(var effect in panels.definitions)
            {
                switch(effect.effect)
                {
                    case PanelEffect.Healing:settings.healAmount=effect.amount;break;
                    case PanelEffect.Shock:settings.shockDamage=effect.amount;break;
                    case PanelEffect.Shield:settings.shieldDuration=effect.duration;break;
                    case PanelEffect.LaserSlow:settings.laserSlowDuration=effect.duration;break;
                    case PanelEffect.MovementSlow:settings.movementSlowDuration=effect.duration;break;
                }
            }
            Model=new RunModel(settings);Validator=new SafetyValidator(settings);
            player.Initialize(settings);panels.Initialize(Model,player,Validator,lasers);lasers.Initialize(Model,player,panels,Validator);
            if(!audio)audio=gameObject.AddComponent<CorridorAudio>();audio.Initialize(this);hud.Initialize(this);
            Model.StateChanged+=OnState;Model.Feedback+=OnFeedback;Model.Hurt+=player.PlayHurt;OnState(Model.State);
        }
        void Update()
        {
            if(Automated) return;
            var keys=Keyboard.current;
            if(keys!=null)
            {
                if(keys.rKey.wasPressedThisFrame) Restart();
                if(keys.escapeKey.wasPressedThisFrame) Model.TogglePause();
                if(Model.State==RunState.Ready && (keys.spaceKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame)) {Model.Start();return;}
            }
            if(Model.State==RunState.Dead)
            {
                AdvanceDeathTime(Time.unscaledDeltaTime);return;
            }
            if(Model.Paused || (Model.State!=RunState.Running && Model.State!=RunState.ExitOpen)) return;
            Vector2 input=keys==null?Vector2.zero:new Vector2((keys.dKey.isPressed?1:0)-(keys.aKey.isPressed?1:0),(keys.wKey.isPressed?1:0)-(keys.sKey.isPressed?1:0));
            Advance(Mathf.Min(Time.deltaTime,.5f),input,keys!=null&&keys.spaceKey.wasPressedThisFrame,keys!=null&&(keys.leftCtrlKey.isPressed||keys.rightCtrlKey.isPressed));
        }
        public void Advance(float duration,Vector2 input,bool jump,bool crouch)
        {
            if(Model.Paused) return;
            // Bound both gravity curvature and geometry movement. Swept checks still catch crossings
            // between these steps, including a complete beam traversal during a low-FPS frame.
            while(duration>.000001f && (Model.State==RunState.Running || Model.State==RunState.ExitOpen))
            {
                float dt=Mathf.Min(duration,1f/120);duration-=dt;float previousClock=Model.HazardClock;
                Model.Tick(dt);player.Step(input,jump,crouch,Model.MovementMultiplier,dt);jump=false;
                if(Model.State==RunState.Running)
                {
                    panels.Step(dt);lasers.Step(previousClock);
                    // CharacterController keeps a skin outside the physical grid collider.
                    if(Model.State==RunState.Running && player.Feet.z+settings.playerRadius+.02f>=settings.gateZ-.05f) Model.TouchExitGrid();
                }
                else {lasers.Clear();if(player.Feet.z>settings.gateZ+.8f) Model.ReachExit();}
                audio.Step(dt);SimulationSteps++;
            }
        }
        void LateUpdate() {if(player.animator)player.animator.speed=Model.Paused?0:1;panels.RefreshVisuals();hud.Refresh();}
        public void StartRun() {Model.Start();}
        public void AdvanceDeathTime(float dt)
        {if(Model.State!=RunState.Dead)return;player.AdvanceDeath(dt,Automated);deadTime+=dt;if(deadTime>=2)Restart();}
        public void Restart()
        {
            audio.ResetRun();Model.Reset();player.ResetPosition();lasers.ResetWaves();panels.ResetPanels();deadTime=0;SimulationSteps=0;securityCamera.Snap();
        }
        void OnState(RunState state)
        {
            exitGrid.SetActive(state!=RunState.ExitOpen && state!=RunState.Won);
            if(state==RunState.Dead){deadTime=0;lasers.Clear();player.BeginDeath();}
        }
        void OnFeedback(string kind)
        {
            audio.QueueFeedback(kind);
        }
    }
}
