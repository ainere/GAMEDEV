using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
namespace LaserCorridor
{
    public sealed class CorridorAudio:MonoBehaviour
    {
        public AudioMixer mixer;
        public AudioMixerGroup ambienceGroup,hazardGroup,feedbackGroup,uiGroup;
        public AudioClip roomTone,gridHum,flight,launch,laserHit,shock,sticky,shieldBreak,reveal,exitOpen,doorOpen,death;
        public bool SilentAutomation{get;private set;}
        public float MasterVolume{get;private set;}=.35f;
        public readonly List<string> Timeline=new List<string>();
        public readonly Dictionary<int,int> Launches=new Dictionary<int,int>();
        public int DeathCues{get;private set;}
        public AudioCueLimiter Limiter{get;}=new AudioCueLimiter();
        sealed class Voice{public AudioSource source;public string key;public float end;public int priority;public bool loop;}
        readonly Voice[] voices=new Voice[8];
        readonly Dictionary<WavePlan,Voice> flights=new Dictionary<WavePlan,Voice>();
        AudioSource room,grid;AudioClip healChime,shieldChime,slowChime,tickTone,winTone,crackle;
        RunController run;bool initialized,paused;string pending;float duckUntil,deathAt=-1,walked;int lastSecond=-1;bool grounded=true;
        float Now=>run.Model.Elapsed;
        public void Initialize(RunController owner)
        {
            if(initialized)return;initialized=true;run=owner;SilentAutomation=Array.IndexOf(Environment.GetCommandLineArgs(),"--qa")>=0;
            for(int i=0;i<voices.Length;i++)voices[i]=new Voice{source=Source("Limited cue voice "+i,feedbackGroup)};
            room=Source("Quiet room tone",ambienceGroup);room.loop=true;room.clip=roomTone;room.volume=.035f;room.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency=800;
            grid=Source("Local exit-grid hum",ambienceGroup);grid.loop=true;grid.clip=gridHum;grid.volume=.025f;grid.spatialBlend=1;grid.transform.position=new Vector3(0,1.7f,run.settings.gateZ);
            healChime=Tone("Heal chime",new[]{520f,650f,780f},.32f);shieldChime=Tone("Shield chime",new[]{650f,980f},.3f);slowChime=Tone("Slow chime",new[]{850f,640f,480f},.35f);tickTone=Tone("Countdown tick",new[]{420f},.08f);winTone=Tone("Resolve",new[]{520f,650f,780f,1040f},.6f);
            crackle=Noise();SetMasterVolume(SilentAutomation?.35f:PlayerPrefs.GetFloat("Corridor.MasterVolume",.35f));
        }
        AudioSource Source(string name,AudioMixerGroup group)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var source=go.AddComponent<AudioSource>();source.playOnAwake=false;source.outputAudioMixerGroup=group;source.minDistance=1;source.maxDistance=20;source.rolloffMode=AudioRolloffMode.Logarithmic;return source;
        }
        public void SetMasterVolume(float value)
        {MasterVolume=Mathf.Clamp01(value);if(mixer)mixer.SetFloat("MasterDb",MasterVolume>.001f?20*Mathf.Log10(MasterVolume):-80);if(!SilentAutomation)PlayerPrefs.SetFloat("Corridor.MasterVolume",MasterVolume);}
        public void ResetRun()
        {StopAll();Timeline.Clear();Launches.Clear();DeathCues=0;lastSecond=-1;walked=0;grounded=true;deathAt=-1;paused=false;}
        public void QueueFeedback(string kind)
        {if(kind=="death"||pending!="death")pending=kind;}
        Voice Cue(string key,AudioClip clip,float volume,int priority,AudioMixerGroup group,float interval=.15f,int maximum=1,float pitch=1,Vector3? position=null,float duration=-1,bool loop=false)
        {
            if(!initialized||!clip||run.Model.Paused)return null;float length=duration>0?duration:clip.length/pitch;
            if(!Limiter.TryStart(key,Now,length,interval,maximum))return null;
            Voice slot=null;foreach(var v in voices)if(v.key==null||(!v.loop&&v.end<=Now)){slot=v;break;}
            if(slot==null){foreach(var v in voices)if(v.priority<priority&&(slot==null||v.priority<slot.priority))slot=v;}
            if(slot==null){Limiter.Stop(key);return null;}
            slot.source.Stop();if(slot.key!=null&&slot.key!=key)Limiter.Stop(slot.key);slot.key=key;slot.end=Now+length;slot.priority=priority;slot.loop=loop;slot.source.clip=clip;slot.source.loop=loop;slot.source.pitch=pitch;slot.source.volume=volume;slot.source.outputAudioMixerGroup=group;slot.source.spatialBlend=position.HasValue?1:0;if(position.HasValue)slot.source.transform.position=position.Value;
            if(!SilentAutomation)slot.source.Play();Timeline.Add(Now.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+" / "+key);
            if(group==feedbackGroup)duckUntil=Now+.4f;return slot;
        }
        public void Launch(WavePlan wave)
        {
            Cue("launch",launch,.055f,70,hazardGroup,.15f,1,1,new Vector3(0,1.8f,run.settings.emitterZ));Launches.TryGetValue(wave.id,out int count);Launches[wave.id]=count+1;
            var v=Cue("flight:"+wave.id,flight,.018f,20,hazardGroup,0,1,Mathf.Lerp(.75f,1.1f,(wave.speed-3.5f)/3.5f),new Vector3(0,1.5f,run.settings.emitterZ),1000,true);if(v!=null)flights[wave]=v;
        }
        public void MoveWave(WavePlan wave,float z){if(flights.TryGetValue(wave,out var v)&&v.key=="flight:"+wave.id)v.source.transform.position=new Vector3(0,1.5f,z);}
        public void ReleaseWave(WavePlan wave){if(flights.TryGetValue(wave,out var v)){if(v.key=="flight:"+wave.id)Stop(v);flights.Remove(wave);}}
        void Stop(Voice voice){voice.source.Stop();Limiter.Stop(voice.key??"");voice.key=null;voice.loop=false;}
        public void StopWaves(){if(!initialized)return;foreach(var v in voices)if(v.key=="launch"||(v.key!=null&&v.key.StartsWith("flight:")))Stop(v);flights.Clear();}
        public void StopAll(){if(!initialized)return;foreach(var v in voices)Stop(v);room.Stop();grid.Stop();flights.Clear();Limiter.Clear();pending=null;}
        public void PauseAll(){if(paused||!initialized)return;paused=true;foreach(var v in voices)v.source.Pause();grid.Pause();room.volume=.0088f;}
        public void ResumeAll(){if(!paused)return;paused=false;foreach(var v in voices)if(!SilentAutomation)v.source.UnPause();if(!SilentAutomation)grid.UnPause();room.volume=.035f;}
        public void Step(float dt)
        {
            if(!initialized)return;var m=run.Model;
            if(m.Paused){PauseAll();return;}if(paused)ResumeAll();
            foreach(var v in voices)if(v.key!=null&&!v.loop&&v.end<=Now)Stop(v);
            if(pending!=null)
            {
                string kind=pending;pending=null;
                if(kind=="death"){StopAll();deathAt=Time.unscaledTime;DeathCues++;Cue("death",death,.09f,100,feedbackGroup,0,1,1,null,.3f);}
                else if(kind=="open"){StopAll();Cue("exit open",exitOpen,.07f,80,feedbackGroup);Cue("vault unlock",doorOpen,.04f,75,hazardGroup,0,1,1,new Vector3(0,1.8f,13.8f));}
                else if(kind=="win"){StopAll();Cue("win",winTone,.09f,90,feedbackGroup);}
                else if(kind=="damage")Cue("laser hit",laserHit,.09f,80,feedbackGroup,.2f);
                else if(kind=="shock")Cue("shock",crackle,.07f,80,feedbackGroup,.2f);
                else if(kind=="trap")Cue("sticky trap",sticky,.065f,70,feedbackGroup);
                else if(kind=="shield")Cue("shield break",shieldBreak,.08f,80,feedbackGroup);
                else if(kind.StartsWith("pickup:")){var clip=kind.EndsWith("Healing")?healChime:kind.EndsWith("Shield")?shieldChime:slowChime;Cue(kind,clip,.075f,70,feedbackGroup);}
            }
            if(m.State==RunState.Dead){if(deathAt>=0&&Time.unscaledTime-deathAt>.3f)StopAll();return;}
            if(m.State!=RunState.Running&&m.State!=RunState.ExitOpen)return;
            if(!SilentAutomation&&!room.isPlaying){room.volume=.035f;room.Play();}if(m.State==RunState.Running&&!SilentAutomation&&!grid.isPlaying)grid.Play();
            if(mixer)mixer.SetFloat("AmbienceDb",Now<duckUntil?-4:0);
            int second=Mathf.CeilToInt(m.Remaining);if(second<=10&&second>0&&second!=lastSecond){lastSecond=second;Cue("countdown",tickTone,.035f,50,uiGroup,.7f);}
            bool onGround=run.player.Grounded;if(grounded&&!onGround)Cue("jump",tickTone,.022f,15,feedbackGroup);if(!grounded&&onGround)Cue("land",shock,.025f,20,feedbackGroup,.2f,1,.8f);grounded=onGround;
            if(onGround){walked+=run.player.Motion.magnitude*dt;if(walked>.7f){walked=0;Cue("footstep",shock,.012f,10,feedbackGroup,.12f,1,.88f+.12f*Mathf.Sin(Now*13),run.player.Feet);}}
        }
        void Start(){if(initialized)SetMasterVolume(MasterVolume);}
        void Update(){if(initialized)Step(0);}
        static AudioClip Tone(string name,float[] notes,float duration)
        {
            const int rate=24000;var data=new float[(int)(duration*rate)];float segment=duration/notes.Length;
            for(int i=0;i<data.Length;i++){float t=i/(float)rate;int note=Mathf.Min(notes.Length-1,(int)(t/segment));float phase=t-note*segment;float envelope=Mathf.Clamp01(phase/.008f)*Mathf.Clamp01((segment*.72f-phase)/.025f);data[i]=Mathf.Sin(t*notes[note]*Mathf.PI*2)*envelope*.28f;}
            var clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        static AudioClip Noise(){const int count=2400;var data=new float[count];var rng=new System.Random(771);for(int i=0;i<count;i++)data[i]=((float)rng.NextDouble()*2-1)*Mathf.Pow(1-i/(float)count,2)*.2f;var clip=AudioClip.Create("Local electric crackle",count,1,24000,false);clip.SetData(data,0);return clip;}
    }
}
