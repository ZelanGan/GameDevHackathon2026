using System;
using UnityEngine;

namespace Candlewick
{
    /// <summary>All sound effects, synthesised into AudioClips at startup.</summary>
    public class Sfx : MonoBehaviour
    {
        const int Rate = 44100;
        AudioSource source, flameSource;
        AudioClip flare, dim, pickup, extinguish, hit, kill, hurt, light, deny, drone, moan, jump;
        float flameTarget;
        readonly System.Random rng = new System.Random(7);

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;

            float rumble = 0f;
            AudioClip roar = Clip(2f, t => { rumble += (Noise() - rumble) * 0.09f; return rumble * (2.6f + 0.5f * Sine(t, 9f)); });
            flameSource = gameObject.AddComponent<AudioSource>();
            flameSource.playOnAwake = false;
            flameSource.clip = roar;
            flameSource.loop = true;
            flameSource.volume = 0f;
            flare = Clip(0.6f, t => (Noise() * 0.5f + Saw(t, 70f + t * 240f) * 0.5f) * Mathf.Sin(Mathf.PI * t / 0.6f));
            dim = Clip(0.3f, t => Sine(t, Mathf.Lerp(360f, 110f, t / 0.3f)) * 0.6f * Fade(t, 0.3f));
            pickup = Clip(0.16f, t => Sine(t, t < 0.07f ? 780f : 1170f) * 0.6f * Fade(t, 0.16f));
            extinguish = Clip(1.1f, t => Noise() * 0.8f * Mathf.Pow(Fade(t, 1.1f), 3f));
            hit = Clip(0.05f, t => Noise() * 0.5f * Fade(t, 0.05f));
            kill = Clip(0.2f, t => (Noise() * 0.5f + Sine(t, Mathf.Lerp(240f, 60f, t / 0.2f))) * 0.6f * Fade(t, 0.2f));
            hurt = Clip(0.22f, t => Saw(t, Mathf.Lerp(190f, 70f, t / 0.22f)) * 0.7f * Fade(t, 0.22f));
            light = Clip(1.2f, t => (Sine(t, 392f) + Sine(t, 587f) * 0.7f + Sine(t, 784f) * 0.4f) * 0.35f * Mathf.Min(t * 12f, 1f) * Fade(t, 1.2f));
            deny = Clip(0.18f, t => Square(t, 110f) * 0.4f * Fade(t, 0.18f));
            jump = Clip(0.14f, t => Sine(t, Mathf.Lerp(260f, 620f, t / 0.14f)) * 0.5f * Fade(t, 0.14f));
            moan = Clip(1.6f, t => (Sine(t, 240f - t * 60f + 8f * Sine(t, 5f)) + 0.4f * Sine(t, 362f - t * 90f)) * 0.45f * Mathf.Sin(Mathf.PI * t / 1.6f));
            drone = Clip(8f, t => (Sine(t, 55f) + Sine(t, 82.5f) * 0.5f) * (0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * t / 8f)) * 0.5f);

            var ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = drone;
            ambience.loop = true;
            ambience.volume = 0.16f;
            ambience.Play();
        }

        void Update()
        {
            flameSource.volume = Mathf.MoveTowards(flameSource.volume, flameTarget, Time.unscaledDeltaTime * 3f);
            if (flameSource.volume > 0f && !flameSource.isPlaying) flameSource.Play();
            else if (flameSource.volume <= 0f && flameSource.isPlaying) flameSource.Stop();
        }

        /// <summary>Target loudness of the looping flamethrower roar; 0 fades it out.</summary>
        public void SetFlame(float volume) => flameTarget = volume;

        public void Flare() => source.PlayOneShot(flare, 0.55f);
        public void Dim() => source.PlayOneShot(dim, 0.5f);
        public void Pickup() => source.PlayOneShot(pickup, 0.45f);
        public void Extinguish() => source.PlayOneShot(extinguish, 0.8f);
        public void Hit() => source.PlayOneShot(hit, 0.25f);
        public void Kill() => source.PlayOneShot(kill, 0.5f);
        public void Hurt() => source.PlayOneShot(hurt, 0.65f);
        public void Light() => source.PlayOneShot(light, 0.7f);
        public void Deny() => source.PlayOneShot(deny, 0.5f);
        public void Moan() => source.PlayOneShot(moan, 0.6f);
        public void Jump() => source.PlayOneShot(jump, 0.35f);

        AudioClip Clip(float seconds, Func<float, float> wave)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f);
            var clip = AudioClip.Create("sfx", n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        float Noise() => (float)rng.NextDouble() * 2f - 1f;
        static float Sine(float t, float f) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float Square(float t, float f) => Mathf.Sign(Sine(t, f));
        static float Saw(float t, float f) => 2f * (t * f - Mathf.Floor(t * f + 0.5f));
        static float Fade(float t, float length) => Mathf.Clamp01(1f - t / length);
    }
}
