using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Vehicle;
using UnityEngine;

namespace NightTrolleybus.Route
{
    /// <summary>Перегін між вузлами: рахує шлях, визначає прибуття й підказує гравцю наступну дію.</summary>
    public class TripController : MonoBehaviour
    {
        [SerializeField] private BusDriveController bus;
        [Tooltip("Скільки секунд триває перегін на повній швидкості")]
        [SerializeField] private float secondsAtFullSpeed = 35f;
        [Tooltip("Перший перегін партії: коротший, щоб гравець швидше дістався до першої події чи зупинки")]
        [SerializeField] private float firstTripSeconds = 10f;
        [Tooltip("Пройдений шлях у діапазоні 0..1")]
        [SerializeField] private FloatEventChannel tripChanged;
        [SerializeField] private VoidEventChannel arrivedAtNode;
        [SerializeField] private StringEventChannel objectiveChanged;

        private RouteNode target;
        private float progress;
        private float tripSeconds;
        private string currentObjective;

        public bool IsArrived { get; private set; }

        /// <summary>Вийти можна, коли тролейбус дійшов до зупинки й стоїть.</summary>
        public bool CanExit => IsArrived && bus.Speed <= 0f;

        private void Start()
        {
            RouteManager route = FindAnyObjectByType<RouteManager>();
            target = route.Target;
            tripSeconds = route.Current == null ? firstTripSeconds : secondsAtFullSpeed;
            tripChanged.Raise(0f);
        }

        private void Update()
        {
            if (!IsArrived)
            {
                progress = Mathf.Clamp01(progress + bus.Speed * Time.deltaTime / tripSeconds);
                tripChanged.Raise(progress);

                if (progress >= 1f)
                {
                    IsArrived = true;
                    if (target.Type.StopScene() == null)
                    {
                        // Далі гра показує подію або мапу, і підказка перегону вже не потрібна.
                        enabled = false;
                        arrivedAtNode.Raise();
                        return;
                    }
                }
            }

            SetObjective(BuildObjective());
        }

        public void Exit() => arrivedAtNode.Raise();

        private string BuildObjective()
        {
            string destination = target.Type.DisplayName();

            if (CanExit)
                return $"Зупинка: {destination}. Підійдіть до зелених дверей посеред салону й натисніть E";
            if (IsArrived)
                return bus.IsDriving
                    ? $"Ви на місці: {destination}. Тримайте S, щоб зупинитися"
                    : $"Ви на місці: {destination}. Сядьте за кермо й зупиніть тролейбус клавішею S";

            if (GameContext.Faults != null && GameContext.Faults.IsActive(FaultType.PolesOff))
                return "Штанги злетіли, тролейбус не їде. Підійдіть до червоної станції й натисніть E";
            if (GameContext.Power != null && GameContext.Power.Current <= 0f)
                return "Немає енергії! Застосуйте акумулятор (Q) або їдьте за кермом на W: аварійний хід повільний і палить Вогника";

            string kind = target.Type.StopScene() != null ? "зупинка" : "без зупинки";
            if (bus.IsDriving)
                return bus.Throttle > 0f ? $"Їдемо до: {destination} ({kind})" : $"Тримайте W, щоб їхати до: {destination} ({kind})";

            return bus.Speed > 0f || bus.Throttle > 0f
                ? $"Їдемо до: {destination} ({kind})"
                : $"Підійдіть до місця водія праворуч і натисніть E. Далі: {destination} ({kind})";
        }

        private void SetObjective(string objective)
        {
            if (objective == currentObjective)
                return;

            currentObjective = objective;
            objectiveChanged.Raise(objective);
        }
    }
}
