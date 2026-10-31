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
        [SerializeField] private float powerPerSecondAtFullSpeed = 0.7f;

        public float ThrottleChangePerSecond => throttleChangePerSecond;
        public float AccelerationPerSecond => accelerationPerSecond;
        public float PowerPerSecondAtFullSpeed => powerPerSecondAtFullSpeed;
    }
}
