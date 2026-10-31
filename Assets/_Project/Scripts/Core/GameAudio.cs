using System;
using System.Collections.Generic;
using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Core
{
    public enum SoundId
    {
        UiClick,
        DeviceSwitch,
        DoorOpen,
        DoorClose,
        DoorKnock,
        FlameFed,
        Flash,
        Reveal,
        Lose,
        Victory,
        Lock,
        Spark,
        Thump,
        LineFlicker,
        RepairDone,
        PowerUp,
        Pickup,
        Swing,
        Hit,
        Hurt,
        Step,
        EnemyAppear
    }

    /// <summary>Один звук гри: кліп і його гучність.</summary>
    [Serializable]
    public struct SoundEntry
    {
        public SoundId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    /// <summary>Програє короткі звуки на запит і плавно веде фонові петлі за станом гри.</summary>
    public class GameAudio : MonoBehaviour
    {
        private const float MinRepeatInterval = 0.05f;
        private const float FadeSpeed = 1.5f;
        private const float MusicFadeSpeed = 0.2f;
        private const float CalmMusicVolume = 0.4f;
        private const float RideMusicVolume = 0.3f;
        private const float StopMusicVolume = 0.34f;
        private const float PausedMusicFactor = 0.45f;

        [SerializeField] private SoundEntry[] sounds;

        [Header("Фонові петлі")]
        [SerializeField] private AudioClip engineLoop;
        [SerializeField] private AudioClip windLoop;
        [SerializeField] private AudioClip darknessLoop;

        [Header("Музика")]
        [Tooltip("Меню, мапа й події")]
        [SerializeField] private AudioClip calmMusic;
        [SerializeField] private AudioClip rideMusic;
        [SerializeField] private AudioClip stopMusic;

        [Header("Канали подій")]
        [SerializeField] private FloatEventChannel busSpeedChanged;
        [SerializeField] private FloatEventChannel darknessChanged;
        [SerializeField] private StringEventChannel gameStateChanged;

        private readonly Dictionary<SoundId, SoundEntry> byId = new Dictionary<SoundId, SoundEntry>();
        private readonly Dictionary<SoundId, float> lastPlayed = new Dictionary<SoundId, float>();
        private AudioSource effects;
        private AudioSource engine;
        private AudioSource wind;
        private AudioSource darkness;
        private AudioSource calm;
        private AudioSource ride;
        private AudioSource stop;
        private string state = string.Empty;
        private float busSpeed;
        private float darknessLevel;
        private bool isPaused;
        private float calmLevel;
        private float rideLevel;
        private float stopLevel;

        public void SetPaused(bool paused) => isPaused = paused;

        private void Awake()
        {
            foreach (SoundEntry sound in sounds)
                byId[sound.id] = sound;

            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.ignoreListenerPause = true;
            engine = CreateLoop(engineLoop);
            wind = CreateLoop(windLoop);
            darkness = CreateLoop(darknessLoop);
            calm = CreateLoop(calmMusic);
            ride = CreateLoop(rideMusic);
            stop = CreateLoop(stopMusic);

            GameContext.Register(this);
        }

        private void OnDestroy() => GameContext.Unregister(this);

        private void OnEnable()
        {
            busSpeedChanged.Raised += OnBusSpeedChanged;
            darknessChanged.Raised += OnDarknessChanged;
            gameStateChanged.Raised += OnGameStateChanged;
        }

        private void OnDisable()
        {
            busSpeedChanged.Raised -= OnBusSpeedChanged;
            darknessChanged.Raised -= OnDarknessChanged;
            gameStateChanged.Raised -= OnGameStateChanged;
        }

        public void Play(SoundId id)
        {
            if (!byId.TryGetValue(id, out SoundEntry sound) || sound.clip == null)
                return;

            // Той самий звук кілька разів за мить лише гучно клацає.
            if (lastPlayed.TryGetValue(id, out float last) && Time.unscaledTime - last < MinRepeatInterval)
                return;

            lastPlayed[id] = Time.unscaledTime;
            effects.PlayOneShot(sound.clip, sound.volume);
        }

        private AudioSource CreateLoop(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            source.playOnAwake = false;
            source.Play();
            return source;
        }

        private void OnBusSpeedChanged(float normalized) => busSpeed = normalized;

        private void OnDarknessChanged(float normalized) => darknessLevel = normalized;

        private void OnGameStateChanged(string newState)
        {
            state = newState;
            if (state != "Ride")
                busSpeed = 0f;
            if (state != "Stop")
                darknessLevel = 0f;
        }

        private void Update()
        {
            // Час стоїть у меню, на мапі й на паузі: там дорога й темрява мовчать.
            bool isRunning = Time.timeScale > 0f;
            bool inRide = isRunning && state == "Ride";
            bool inStop = isRunning && state == "Stop";

            Fade(engine, inRide ? 0.05f + busSpeed * 0.28f : 0f);
            engine.pitch = 0.7f + busSpeed * 0.6f;
            Fade(wind, inStop ? 0.14f : inRide ? 0.05f + busSpeed * 0.07f : 0f);
            Fade(darkness, inStop ? darknessLevel * 0.45f : 0f);

            // Музика йде за станом, а не за часом, бо на мапі час стоїть, а трек грає.
            float factor = isPaused ? PausedMusicFactor : 1f;
            bool isCalm = state == "Menu" || state == "Map" || state == "Event";
            float musicVolume = GameSettings.MusicVolume;
            FadeMusic(calm, ref calmLevel, isCalm ? CalmMusicVolume * factor : 0f, musicVolume);
            FadeMusic(ride, ref rideLevel, state == "Ride" ? RideMusicVolume * factor : 0f, musicVolume);
            FadeMusic(stop, ref stopLevel, state == "Stop" ? StopMusicVolume * factor : 0f, musicVolume);
        }

        private static void Fade(AudioSource source, float target) =>
            source.volume = Mathf.MoveTowards(source.volume, target, FadeSpeed * Time.unscaledDeltaTime);

        // Перехід між треками плавний, а повзунок у налаштуваннях має діяти одразу, тому рівень і гучність окремо.
        private static void FadeMusic(AudioSource source, ref float level, float target, float musicVolume)
        {
            level = Mathf.MoveTowards(level, target, MusicFadeSpeed * Time.unscaledDeltaTime);
            source.volume = level * musicVolume;
        }
    }
}
