using NightTrolleybus.Core.Events;
using NightTrolleybus.Flame;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Бульбашка з реплікою над Вогником. Якщо Вогника в кадрі немає, стоїть унизу екрана.</summary>
    public class FlameBubbleView : MonoBehaviour
    {
        [SerializeField] private StringEventChannel bubbleRaised;
        [SerializeField] private RectTransform bubble;
        [SerializeField] private Text label;
        [SerializeField] private float duration = 5f;
        [SerializeField] private Vector2 screenOffset = new Vector2(0f, 60f);

        private float timer;

        private void Awake() => bubble.gameObject.SetActive(false);

        private void OnEnable() => bubbleRaised.Raised += OnBubbleRaised;

        private void OnDisable() => bubbleRaised.Raised -= OnBubbleRaised;

        private void OnBubbleRaised(string text)
        {
            label.text = text;
            timer = duration;
            bubble.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (timer <= 0f)
                return;

            // Час не масштабуємо, щоб бульбашка зникала й на мапі, де час стоїть.
            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                bubble.gameObject.SetActive(false);
                return;
            }

            bubble.position = FindScreenPosition();
        }

        private Vector2 FindScreenPosition()
        {
            FlameView flame = FindAnyObjectByType<FlameView>();
            Camera camera = Camera.main;
            if (flame == null || camera == null)
                return new Vector2(Screen.width / 2f, Screen.height * 0.18f);

            Vector2 position = (Vector2)camera.WorldToScreenPoint(flame.transform.position) + screenOffset;
            Vector2 halfSize = bubble.rect.size * bubble.lossyScale / 2f;
            position.x = Mathf.Clamp(position.x, halfSize.x, Screen.width - halfSize.x);
            position.y = Mathf.Clamp(position.y, halfSize.y, Screen.height - halfSize.y);
            return position;
        }
    }
}
