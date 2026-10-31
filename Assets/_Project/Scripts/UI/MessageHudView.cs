using NightTrolleybus.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace NightTrolleybus.UI
{
    /// <summary>Повідомлення й підказка взаємодії.</summary>
    public class MessageHudView : MonoBehaviour
    {
        [SerializeField] private StringEventChannel promptChanged;
        [SerializeField] private StringEventChannel noticeRaised;
        [SerializeField] private Text promptLabel;
        [SerializeField] private Text noticeLabel;
        [SerializeField] private StringEventChannel objectiveChanged;
        [SerializeField] private Text objectiveLabel;
        [SerializeField] private float noticeDuration = 4f;

        private float noticeTimer;

        private void OnEnable()
        {
            promptChanged.Raised += OnPromptChanged;
            noticeRaised.Raised += OnNoticeRaised;
            objectiveChanged.Raised += OnObjectiveChanged;
        }

        private void OnDisable()
        {
            promptChanged.Raised -= OnPromptChanged;
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

        private void OnPromptChanged(string prompt) => promptLabel.text = prompt;

        private void OnNoticeRaised(string notice)
        {
            noticeLabel.text = notice;
            noticeTimer = noticeDuration;
        }
    }
}
