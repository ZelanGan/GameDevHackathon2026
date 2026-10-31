using NightTrolleybus.Core;
using NightTrolleybus.Items;
using UnityEngine;

namespace NightTrolleybus.Passengers
{
    /// <summary>Опис пасажира. Нейтральні можуть дати предмет, тіньові лякають Вогника й крадуть тепло.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Passenger", fileName = "Passenger")]
    public class PassengerData : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private Color color = Color.gray;
        [Tooltip("Предмет, який пасажир дає гравцю. Може бути порожнім")]
        [SerializeField] private ItemData gift;
        [Range(0f, 1f)]
        [SerializeField] private float fear;
        [Tooltip("Скільки тепла за секунду краде пасажир")]
        [SerializeField] private float heatStealPerSecond;

        public string DisplayName => Loc.T(displayName);
        public Color Color => color;
        public ItemData Gift => gift;
        public float Fear => fear;
        public float HeatStealPerSecond => heatStealPerSecond;
    }
}
