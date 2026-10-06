using System.Collections.Generic;
namespace LaserCorridor
{
    // Uses gameplay time so pause and deterministic QA have the same voice limits.
    public sealed class AudioCueLimiter
    {
        readonly Dictionary<string,List<float>> ends=new Dictionary<string,List<float>>();
        readonly Dictionary<string,float> last=new Dictionary<string,float>();
        public bool TryStart(string cue,float now,float duration,float interval,int maximum)
        {
            if(!ends.TryGetValue(cue,out var voices)){voices=new List<float>();ends[cue]=voices;}
            voices.RemoveAll(end=>end<=now);
            if(voices.Count>=maximum||(last.TryGetValue(cue,out float previous)&&now-previous<interval-.0001f))return false;
            voices.Add(now+duration);last[cue]=now;return true;
        }
        public int Active(string cue,float now){if(!ends.TryGetValue(cue,out var voices))return 0;voices.RemoveAll(end=>end<=now);return voices.Count;}
        public void Stop(string cue){ends.Remove(cue);}
        public void Clear(){ends.Clear();last.Clear();}
    }
}
