using NightTrolleybus.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Повідомлення й поточна ціль.</summary>
    public class MessageHudView : MonoBehaviour
    {
        [SerializeField] private StringEventChannel noticeRaised;
        [SerializeField] private Text noticeLabel;
        [SerializeField] private StringEventChannel objectiveChanged;
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private float noticeDuration = 4f;

        private float noticeTimer;

        private void OnEnable()
        {
            noticeRaised.Raised += OnNoticeRaised;
            objectiveChanged.Raised += OnObjectiveChanged;
        }

        private void OnDisable()
        {
            noticeRaised.Raised -= OnNoticeRaised;
            objectiveChanged.Raised -= OnObjectiveChanged;
        }

        private void OnObjectiveChanged(string objective) => objectiveLabel.text = objective;

        private void Update()
        {
            if (noticeTimer <= 0f)
                return;

            // Час не масштабуємо, щоб повідомлення зникало й на мапі, де час стоїть.
            noticeTimer -= Time.unscaledDeltaTime;
            if (noticeTimer <= 0f)
                noticeLabel.text = string.Empty;
        }

        private void OnNoticeRaised(string notice)
        {
            noticeLabel.text = notice;
            noticeTimer = noticeDuration;
        }
    }
}
