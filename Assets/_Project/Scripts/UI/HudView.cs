using NightTrolleybus.Core.Events;
using NightTrolleybus.Core;
using NightTrolleybus.Flame;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Шкали, смуга шляху й спідометр. Сам будує свої елементи й лише слухає події.</summary>
    public class HudView : MonoBehaviour
    {
        private const float RowHeight = 44f;
        private const float BarWidth = 330f;
        private const float BarHeight = 34f;
        private const float IconSize = 38f;
        private const float TrackWidth = 760f;
        private const float DialSize = 170f;
        private const float NeedleMaxAngle = 120f;
        private const float MaxSpeedKmh = 60f;
        private const float TrendHoldSeconds = 0.4f;
        private const int HeartCount = 5;
        private const float HeartSize = 44f;
        private const float HeartSpacing = 6f;

        private static readonly Color HeatColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color PowerColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color FearColor = new Color(0.62f, 0.3f, 0.85f);
        private static readonly Color EmptyHeartColor = new Color(0.16f, 0.13f, 0.14f);
        private static readonly Color TripColor = new Color(0.95f, 0.62f, 0.25f);

        [SerializeField] private FlameConfig config;

        [Header("Канали подій")]
        [SerializeField] private FloatEventChannel flameChanged;
        [SerializeField] private FlameStateEventChannel flameStateChanged;
        [SerializeField] private FloatEventChannel temperatureChanged;
        [SerializeField] private FloatEventChannel powerChanged;
        [SerializeField] private FloatEventChannel fearChanged;
        [SerializeField] private FloatEventChannel healthChanged;
        [SerializeField] private FloatEventChannel speedChanged;
        [SerializeField] private FloatEventChannel tripChanged;

        [Header("Куди класти")]
        [SerializeField] private RectTransform gaugesRoot;
        [SerializeField] private RectTransform ridePanel;
        [SerializeField] private RectTransform stopPanel;

        [Header("Малюнки")]
        [SerializeField] private Sprite frame;
        [SerializeField] private Sprite square;
        [SerializeField] private Sprite flameIcon;
        [SerializeField] private Sprite heatIcon;
        [SerializeField] private Sprite powerIcon;
        [SerializeField] private Sprite fearIcon;
        [SerializeField] private Sprite healthIcon;
        [SerializeField] private Sprite trendArrow;
        [SerializeField] private Sprite trolleyIcon;
        [SerializeField] private Sprite stopIcon;
        [SerializeField] private Sprite dial;
        [SerializeField] private Font font;

        private Gauge flame;
        private Gauge heat;
        private Gauge power;
        private Gauge fear;
        private readonly Image[] hearts = new Image[HeartCount];
        private Image tripFill;
        private RectTransform trolley;
        private RectTransform needle;
        private Text speedLabel;
        private Image fearTrend;
        private float lastFear;
        private float trendTimer;

        private void Awake()
        {
            flame = CreateGauge(gaugesRoot, 0, "Вогник", flameIcon, Color.white);
            heat = CreateGauge(gaugesRoot, 1, "Тепло", heatIcon, HeatColor);
            power = CreateGauge(gaugesRoot, 2, "Енергія", powerIcon, PowerColor);
            fear = CreateGauge(gaugesRoot, 3, "Страх", fearIcon, FearColor);
            fear.Set(0f);
            CreateHearts();

            fearTrend = CreateImage("FearTrend", fear.Frame, trendArrow, Color.white);
            Place(fearTrend.rectTransform, new Vector2(1f, 0.5f), new Vector2(22f, 22f), new Vector2(20f, 0f));
            fearTrend.enabled = false;

            CreateTrip();
            CreateSpeedometer();
        }

        private void OnEnable()
        {
            flameChanged.Raised += OnFlameChanged;
            flameStateChanged.Raised += OnStateChanged;
            temperatureChanged.Raised += OnTemperatureChanged;
            powerChanged.Raised += OnPowerChanged;
            fearChanged.Raised += OnFearChanged;
            healthChanged.Raised += OnHealthChanged;
            speedChanged.Raised += OnSpeedChanged;
            tripChanged.Raised += OnTripChanged;
        }

        private void OnDisable()
        {
            flameChanged.Raised -= OnFlameChanged;
            flameStateChanged.Raised -= OnStateChanged;
            temperatureChanged.Raised -= OnTemperatureChanged;
            powerChanged.Raised -= OnPowerChanged;
            fearChanged.Raised -= OnFearChanged;
            healthChanged.Raised -= OnHealthChanged;
            speedChanged.Raised -= OnSpeedChanged;
            tripChanged.Raised -= OnTripChanged;
        }

        private void Update()
        {
            // Страх змінюється лише час від часу, тому стрілка гасне, коли подій більше немає.
            if (trendTimer <= 0f)
                return;

            trendTimer -= Time.unscaledDeltaTime;
            if (trendTimer <= 0f)
                fearTrend.enabled = false;
        }

        private void OnFlameChanged(float normalized) => flame.Set(normalized);

        private void OnStateChanged(FlameState state) => flame.Fill.color = config.GetSettings(state).color;

        private void OnTemperatureChanged(float normalized) => heat.Set(normalized);

        private void OnPowerChanged(float normalized) => power.Set(normalized);

        // Удар знімає частину серця, тому останнє заповнене лише наполовину чи менше.
        private void OnHealthChanged(float normalized)
        {
            for (int i = 0; i < HeartCount; i++)
                hearts[i].fillAmount = Mathf.Clamp01(normalized * HeartCount - i);
        }

        private void OnFearChanged(float normalized)
        {
            fear.Set(normalized);

            // Стрілка вгору: страх росте, вниз: спадає.
            bool isRising = normalized > lastFear;
            fearTrend.rectTransform.localRotation = Quaternion.Euler(0f, 0f, isRising ? 0f : 180f);
            fearTrend.color = isRising ? new Color(1f, 0.45f, 0.4f) : new Color(0.5f, 0.9f, 0.6f);
            fearTrend.enabled = true;
            trendTimer = TrendHoldSeconds;
            lastFear = normalized;
        }

        private void OnSpeedChanged(float normalized)
        {
            needle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(NeedleMaxAngle, -NeedleMaxAngle, normalized));
            speedLabel.text = Loc.F("{0} км/год", Mathf.RoundToInt(normalized * MaxSpeedKmh));
        }

        private void OnTripChanged(float normalized)
        {
            tripFill.fillAmount = normalized;
            trolley.anchoredPosition = new Vector2(normalized * TrackWidth, trolley.anchoredPosition.y);
        }

        private Gauge CreateGauge(RectTransform parent, int row, string title, Sprite icon, Color color)
        {
            float y = -24f - row * RowHeight;
            Image iconImage = CreateImage(title + "Icon", parent, icon, Color.white);
            PlaceTopLeft(iconImage.rectTransform, new Vector2(24f, y), new Vector2(IconSize, IconSize));

            Image background = CreateImage(title + "Bar", parent, frame, Color.white);
            background.type = Image.Type.Sliced;
            PlaceTopLeft(background.rectTransform, new Vector2(24f + IconSize + 8f, y - 2f), new Vector2(BarWidth, BarHeight));

            Image fill = CreateImage("Fill", background.transform, square, color);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            Stretch(fill.rectTransform, 5f);

            Text label = CreateText("Label", background.transform, 20, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform, 5f);
            label.rectTransform.offsetMin = new Vector2(12f, 0f);
            return new Gauge(title, fill, label, background.rectTransform);
        }

        private void CreateHearts()
        {
            for (int i = 0; i < HeartCount; i++)
            {
                float x = -24f - (HeartCount - 1 - i) * (HeartSize + HeartSpacing);
                Image empty = CreateImage("HeartEmpty", stopPanel, healthIcon, EmptyHeartColor);
                PlaceTopRight(empty.rectTransform, new Vector2(x, -24f), new Vector2(HeartSize, HeartSize));

                Image heart = CreateImage("Heart", empty.transform, healthIcon, Color.white);
                heart.type = Image.Type.Filled;
                heart.fillMethod = Image.FillMethod.Horizontal;
                heart.fillOrigin = (int)Image.OriginHorizontal.Left;
                Stretch(heart.rectTransform, 0f);
                hearts[i] = heart;
            }
        }

        private void CreateTrip()
        {
            Image track = CreateImage("TripTrack", ridePanel, frame, Color.white);
            track.type = Image.Type.Sliced;
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = trackRect.anchorMax = new Vector2(0.5f, 1f);
            trackRect.pivot = new Vector2(0.5f, 1f);
            trackRect.sizeDelta = new Vector2(TrackWidth, 26f);
            trackRect.anchoredPosition = new Vector2(0f, -30f);

            tripFill = CreateImage("Fill", track.transform, square, TripColor);
            tripFill.type = Image.Type.Filled;
            tripFill.fillMethod = Image.FillMethod.Horizontal;
            tripFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            tripFill.fillAmount = 0f;
            Stretch(tripFill.rectTransform, 5f);

            Image start = CreateImage("From", track.transform, stopIcon, new Color(0.7f, 0.7f, 0.7f));
            Place(start.rectTransform, new Vector2(0f, 0.5f), new Vector2(28f, 28f), new Vector2(-18f, 0f));
            Image finish = CreateImage("To", track.transform, stopIcon, Color.white);
            Place(finish.rectTransform, new Vector2(1f, 0.5f), new Vector2(28f, 28f), new Vector2(18f, 0f));

            Image trolleyImage = CreateImage("Trolley", track.transform, trolleyIcon, Color.white);
            trolley = trolleyImage.rectTransform;
            trolley.anchorMin = trolley.anchorMax = new Vector2(0f, 0.5f);
            trolley.pivot = new Vector2(0.5f, 0.5f);
            trolley.sizeDelta = new Vector2(60f, 30f);
            trolley.anchoredPosition = new Vector2(0f, 6f);
        }

        private void CreateSpeedometer()
        {
            Image dialImage = CreateImage("Speedometer", ridePanel, dial, Color.white);
            RectTransform dialRect = dialImage.rectTransform;
            dialRect.anchorMin = dialRect.anchorMax = dialRect.pivot = new Vector2(1f, 0f);
            dialRect.sizeDelta = new Vector2(DialSize, DialSize);
            dialRect.anchoredPosition = new Vector2(-28f, 28f);

            Image needleImage = CreateImage("Needle", dialRect, square, new Color(1f, 0.35f, 0.25f));
            needle = needleImage.rectTransform;
            needle.anchorMin = needle.anchorMax = new Vector2(0.5f, 0.5f);
            needle.pivot = new Vector2(0.5f, 0f);
            needle.sizeDelta = new Vector2(5f, DialSize * 0.38f);
            needle.anchoredPosition = Vector2.zero;
            needle.localRotation = Quaternion.Euler(0f, 0f, NeedleMaxAngle);

            speedLabel = CreateText("SpeedLabel", dialRect, 22, TextAnchor.MiddleCenter);
            RectTransform labelRect = speedLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 34f);
            labelRect.anchoredPosition = new Vector2(0f, 8f);
            speedLabel.text = Loc.F("{0} км/год", 0);
        }

        private Text CreateText(string objectName, Transform parent, int size, TextAnchor alignment)
        {
            var label = new GameObject(objectName).AddComponent<Text>();
            label.transform.SetParent(parent, false);
            label.font = font;
            label.fontSize = size;
            label.fontStyle = FontStyle.Bold;
            label.alignment = alignment;
            label.color = new Color(1f, 0.93f, 0.8f);
            label.raycastTarget = false;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return label;
        }

        private static Image CreateImage(string objectName, Transform parent, Sprite sprite, Color color)
        {
            var image = new GameObject(objectName).AddComponent<Image>();
            image.transform.SetParent(parent, false);
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void PlaceTopRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>Одна шкала: заповнення й підпис із назвою та відсотками.</summary>
        private class Gauge
        {
            private readonly string title;
            private readonly Text label;

            public Gauge(string title, Image fill, Text label, RectTransform frame)
            {
                this.title = title;
                this.label = label;
                Fill = fill;
                Frame = frame;
            }

            public Image Fill { get; }
            public RectTransform Frame { get; }

            public void Set(float normalized)
            {
                Fill.fillAmount = normalized;
                label.text = $"{Loc.T(title)} {Mathf.RoundToInt(normalized * 100f)}%";
            }
        }
    }
}
