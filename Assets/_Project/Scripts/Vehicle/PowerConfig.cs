using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Налаштування енергії тролейбуса. Під час гри цей асет не змінюється.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Config/Power", fileName = "PowerConfig")]
    public class PowerConfig : ScriptableObject
    {
        [SerializeField] private float maxPower = 100f;
        [SerializeField] private float startPower = 80f;

        public float MaxPower => maxPower;
        public float StartPower => startPower;
    }
}
