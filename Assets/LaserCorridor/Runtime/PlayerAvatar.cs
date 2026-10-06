using UnityEngine;

namespace LaserCorridor
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerAvatar : MonoBehaviour
    {
        public Animator animator;
        public Transform visual;
        public float visualYawOffset;
        public Vector3 FacingDirection=>visual?visual.rotation*Quaternion.Euler(0,-visualYawOffset,0)*Vector3.forward:Vector3.forward;
        public Transform[] proceduralLimbs;
        CharacterController controller;
        RunSettings settings;
        float verticalVelocity, gait;
        public bool Grounded { get; private set; } = true;
        public bool PreviouslyGrounded {get;private set;}=true;
        public bool Crouched { get; private set; }
        public float Height => controller.height;
        public Vector3 Feet => transform.position;
        public Vector3 PreviousFeet {get;private set;}
        public Vector2 Motion { get; private set; }
        public bool IsDead {get;private set;}
        public float DeathTime {get;private set;}
        public HitRegion LastHitRegion {get;private set;}
        public float HurtTime {get;private set;}
        public int HurtCount {get;private set;}
        int upperLayer=-1,legLayer=-1;

        public void Initialize(RunSettings s)
        {
            settings=s;controller=GetComponent<CharacterController>();
            controller.radius=s.playerRadius;controller.height=s.standingHeight;
            controller.center=Vector3.up*s.standingHeight/2;controller.skinWidth=.015f;
            controller.stepOffset=.12f;controller.minMoveDistance=0;controller.slopeLimit=45;
            if(animator){animator.applyRootMotion=false;upperLayer=animator.GetLayerIndex("Upper body reactions");legLayer=animator.GetLayerIndex("Leg reactions");}
            ResetPosition();
        }
        public void ResetPosition()
        {
            IsDead=false;DeathTime=HurtTime=0;HurtCount=0;LastHitRegion=HitRegion.Chest;PreviouslyGrounded=true;
            controller.enabled=false;transform.position=new Vector3(0,.015f,settings.minimumZ);
            controller.height=settings.standingHeight;controller.center=Vector3.up*settings.standingHeight/2;controller.enabled=true;
            verticalVelocity=-2; Grounded=true;Crouched=false;Motion=Vector2.zero;PreviousFeet=Feet;
            if(visual) {visual.localScale=Vector3.one;visual.localRotation=Quaternion.Euler(0,visualYawOffset,0);}
            if(animator){animator.Rebind();animator.Update(0);animator.SetFloat("Speed",0);animator.SetBool("Crouch",false);animator.SetBool("Airborne",false);}
            SetReactionWeight(0);
        }
        public void Step(Vector2 input,bool jump,bool crouch,float multiplier,float dt)
        {
            if(IsDead)return;
            PreviousFeet=Feet;PreviouslyGrounded=Grounded;AdvanceHurt(dt);
            Crouched=crouch;controller.height=crouch?settings.crouchingHeight:settings.standingHeight;
            controller.center=Vector3.up*controller.height/2;
            if(Grounded && verticalVelocity<0) verticalVelocity=-2;
            if(jump && Grounded) {verticalVelocity=Mathf.Sqrt(2*settings.gravity*settings.jumpHeight);Grounded=false;}
            float verticalDisplacement=verticalVelocity*dt-.5f*settings.gravity*dt*dt;
            verticalVelocity-=settings.gravity*dt;
            Motion=Vector2.ClampMagnitude(input,1)*(crouch?settings.crouchSpeed:settings.walkSpeed)*multiplier;
            float sideLimit=settings.corridorWidth/2-settings.playerRadius-.03f;
            float nextX=Mathf.Clamp(Feet.x+Motion.x*dt,-sideLimit,sideLimit);
            float nextZ=Mathf.Max(settings.minimumZ,Feet.z+Motion.y*dt);
            controller.Move(new Vector3(nextX-Feet.x,verticalDisplacement,nextZ-Feet.z));
            Grounded=controller.isGrounded || (Feet.y<.02f && verticalVelocity<=0);
            if(visual)
            {
                if(input.sqrMagnitude>.01f) visual.rotation=Quaternion.Slerp(visual.rotation,Quaternion.LookRotation(new Vector3(input.x,0,input.y))*Quaternion.Euler(0,visualYawOffset,0),1-Mathf.Exp(-12*dt));
                if(!animator) visual.localScale=new Vector3(1,crouch?.56f:1,1);
            }
            gait+=Motion.magnitude*dt*3;
            if(animator)
            {
                animator.SetFloat("Speed",Motion.magnitude);animator.SetBool("Crouch",crouch);animator.SetBool("Airborne",!Grounded);
            }
            else if(proceduralLimbs!=null)
                for(int i=0;i<proceduralLimbs.Length;i++) if(proceduralLimbs[i]) proceduralLimbs[i].localRotation=Quaternion.Euler(Mathf.Sin(gait+(i%2)*Mathf.PI)*Mathf.Min(Motion.magnitude*8,32),0,0);
        }
        public void Teleport(Vector3 feet)
        {
            controller.enabled=false;transform.position=feet;controller.enabled=true;PreviousFeet=feet;Grounded=PreviouslyGrounded=feet.y<.02f;verticalVelocity=Grounded?-2:0;
        }
        public void PlayHurt(HitRegion region)
        {
            if(IsDead)return;LastHitRegion=region;HurtTime=.55f;HurtCount++;SetReactionWeight(0);
            int layer=region==HitRegion.Legs?legLayer:upperLayer;
            if(animator&&layer>=0){animator.SetLayerWeight(layer,.8f);animator.Play(region==HitRegion.Head?"Head hit":region==HitRegion.Legs?"Leg hit":"Chest hit",layer,0);}
        }
        void AdvanceHurt(float dt)
        {
            if(HurtTime<=0)return;HurtTime=Mathf.Max(0,HurtTime-dt);
            if(animator){int layer=LastHitRegion==HitRegion.Legs?legLayer:upperLayer;if(layer>=0)animator.SetLayerWeight(layer,.8f*Mathf.Clamp01(HurtTime/.15f));}
        }
        void SetReactionWeight(float weight)
        {if(animator){if(upperLayer>=0)animator.SetLayerWeight(upperLayer,weight);if(legLayer>=0)animator.SetLayerWeight(legLayer,weight);}}
        public void BeginDeath()
        {
            IsDead=true;DeathTime=HurtTime=0;Motion=Vector2.zero;SetReactionWeight(0);
            if(animator){animator.SetFloat("Speed",0);animator.SetBool("Airborne",false);animator.CrossFadeInFixedTime("Death",.08f);animator.Update(0);}
        }
        public void AdvanceDeath(float dt,bool simulateAnimation=false)
        {
            if(!IsDead)return;DeathTime+=dt;
            if(!Grounded){verticalVelocity-=settings.gravity*dt;controller.Move(Vector3.up*verticalVelocity*dt);Grounded=controller.isGrounded;}
            if(animator){if(simulateAnimation)animator.Update(dt);}
            else if(visual)visual.localRotation=Quaternion.Euler(0,visualYawOffset,Mathf.SmoothStep(0,85,Mathf.Clamp01(DeathTime/.8f)));
        }
    }
}
