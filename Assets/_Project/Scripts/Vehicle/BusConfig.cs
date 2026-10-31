using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Налаштування руху тролейбуса. Під час гри цей асет не змінюється.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Config/Bus", fileName = "BusConfig")]
    public class BusConfig : ScriptableObject
    {
        [Tooltip("Як швидко змінюється положення важеля за секунду")]
        [SerializeField] private float throttleChangePerSecond = 0.5f;
        [Tooltip("Як швидко швидкість наздоганяє важіль за секунду")]
        [SerializeField] private float accelerationPerSecond = 0.25f;
        [Tooltip("Витрата енергії за секунду на повній швидкості")]
        [SerializeField] private float powerPerSecondAtFullSpeed = 0.6f;
        [Tooltip("Найбільша швидкість 0..1 на аварійному ході, коли енергії немає")]
        [SerializeField] private float emergencySpeed = 0.35f;
        [Tooltip("Скільки сили за секунду Вогник віддає, щоб тягнути тролейбус без енергії")]
        [SerializeField] private float emergencyFlamePerSecond = 0.3f;

        public float EmergencySpeed => emergencySpeed;
        public float EmergencyFlamePerSecond => emergencyFlamePerSecond;
        public float ThrottleChangePerSecond => throttleChangePerSecond;
        public float AccelerationPerSecond => accelerationPerSecond;
        public float PowerPerSecondAtFullSpeed => powerPerSecondAtFullSpeed;
    }
}
