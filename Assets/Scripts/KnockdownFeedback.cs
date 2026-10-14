using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Knockdown
{
    public sealed class KnockdownFeedback : MonoBehaviour
    {
        [SerializeField] private ParticleSystem popPrefab;
        [SerializeField] private KnockdownPopLabel labelPrefab;
        [SerializeField] private AudioClip popClip;
        [SerializeField] private AudioClip throwClip;
        [SerializeField] private AudioClip bellClip;
        [SerializeField] private AudioClip[] missClips;
        [SerializeField] private AudioSource[] voices;
        private int _nextVoice;
        private readonly List<GameObject> _effects = new List<GameObject>();

        public void Configure(ParticleSystem particles, KnockdownPopLabel label, AudioClip pop, AudioClip launch,
            AudioClip bell, AudioClip[] misses, AudioSource[] sources)
        {
            popPrefab = particles; labelPrefab = label; popClip = pop; throwClip = launch;
            bellClip = bell; missClips = misses; voices = sources;
        }

        private void Play(AudioClip clip, float volume, float pitch)
        {
            if (clip == null || voices == null || voices.Length == 0) return;
            var voice = voices[_nextVoice++ % voices.Length];
            if (voice == null) return;
            voice.clip = clip; voice.volume = volume; voice.pitch = pitch; voice.Play();
        }

        public void Throw() => Play(throwClip, 0.19f, Random.Range(0.95f, 1.06f));
        public void StartBell() => Play(bellClip, 0.28f, 1.12f);
        public void EndBell() => Play(bellClip, 0.35f, 0.86f);
        public void Miss(Vector3 position)
        {
            if (missClips != null && missClips.Length > 0)
                Play(missClips[Random.Range(0, missClips.Length)], 0.15f, Random.Range(0.95f, 1.05f));
        }

        public void Pop(Vector3 position, Camera camera)
        {
            Play(popClip, 0.45f, Random.Range(0.94f, 1.08f));
            var particles = Instantiate(popPrefab, position, Quaternion.identity);
            particles.Play();
            _effects.Add(particles.gameObject);
            Destroy(particles.gameObject, 0.8f);
            var label = Instantiate(labelPrefab, position + new Vector3(0, 0.12f, -0.55f), camera.transform.rotation);
            _effects.Add(label.gameObject);
            _effects.RemoveAll(item => item == null);
        }

        public void SetPaused(bool paused)
        {
            if (voices == null) return;
            foreach (var voice in voices)
            {
                if (voice == null) continue;
                if (paused) voice.Pause();
                else voice.UnPause();
            }
        }

        public void ClearEffects()
        {
            foreach (var effect in _effects) if (effect != null) Destroy(effect);
            _effects.Clear();
            if (voices == null) return;
            foreach (var voice in voices) if (voice != null) voice.Stop();
        }
    }

}
