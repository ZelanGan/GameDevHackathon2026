using System;
using NightTrolleybus.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace NightTrolleybus.UI
{
    /// <summary>Показує відео на весь екран і повідомляє, коли воно скінчилося або його пропустили.</summary>
    public class CutsceneView : MonoBehaviour
    {
        // Натискання, яким запустили гру, не має одразу пропустити відео.
        private const float SkipDelay = 0.3f;

        [SerializeField] private VideoClip clip;
        [SerializeField] private Sprite square;
        [SerializeField] private Font font;

        private GameObject root;
        private RawImage screen;
        private AspectRatioFitter fitter;
        private Text skipLabel;
        private VideoPlayer player;
        private RenderTexture texture;
        private Action finished;
        private float startedAt;

        private void Awake()
        {
            BuildFrame();
            root.SetActive(false);
        }

        public void Play(Action onFinished)
        {
            if (clip == null)
            {
                onFinished?.Invoke();
                return;
            }

            finished = onFinished;
            startedAt = Time.unscaledTime;

            texture = new RenderTexture((int)clip.width, (int)clip.height, 0);
            screen.texture = texture;
            screen.enabled = false;
            fitter.aspectRatio = (float)clip.width / clip.height;
            skipLabel.text = MenuText.Get("skip");
            root.SetActive(true);

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.clip = clip;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = texture;
            // Доки йде відео, час гри стоїть, тому плеєр рахує власний.
            player.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            for (ushort track = 0; track < clip.audioTrackCount; track++)
                player.SetDirectAudioVolume(track, GameSettings.Volume);

            player.prepareCompleted += OnPrepared;
            player.loopPointReached += OnEnded;
            player.errorReceived += OnError;
            player.Prepare();
        }

        private void Update()
        {
            if (finished == null || Time.unscaledTime - startedAt < SkipDelay)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame
                                     || keyboard.enterKey.wasPressedThisFrame))
                Finish();
        }

        private void OnPrepared(VideoPlayer source)
        {
            screen.enabled = true;
            source.Play();
        }

        private void OnEnded(VideoPlayer source) => Finish();

        private void OnError(VideoPlayer source, string message)
        {
            // Зламане відео не має заважати почати гру.
            Debug.LogWarning($"Катсцену не показано: {message}");
            Finish();
        }

        private void Finish()
        {
            if (finished == null)
                return;

            Action callback = finished;
            finished = null;

            Destroy(player);
            screen.texture = null;
            texture.Release();
            Destroy(texture);
            root.SetActive(false);
            callback();
        }

        private void BuildFrame()
        {
            var background = new GameObject("CutsceneRoot").AddComponent<Image>();
            background.transform.SetParent(transform, false);
            background.sprite = square;
            background.color = Color.black;
            Stretch(background.rectTransform);
            root = background.gameObject;

            // Чорні смуги лишаються, якщо пропорції екрана не збігаються з відео.
            screen = new GameObject("Screen").AddComponent<RawImage>();
            screen.transform.SetParent(root.transform, false);
            screen.raycastTarget = false;
            fitter = screen.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            skipLabel = new GameObject("SkipLabel").AddComponent<Text>();
            skipLabel.transform.SetParent(root.transform, false);
            skipLabel.font = font;
            skipLabel.fontSize = 24;
            skipLabel.alignment = TextAnchor.LowerRight;
            skipLabel.color = new Color(1f, 1f, 1f, 0.65f);
            skipLabel.raycastTarget = false;
            RectTransform labelRect = skipLabel.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(1f, 0f);
            labelRect.anchoredPosition = new Vector2(-32f, 24f);
            labelRect.sizeDelta = new Vector2(600f, 40f);
            skipLabel.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
