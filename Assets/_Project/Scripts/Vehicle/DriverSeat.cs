using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Місце водія: поки гравець сидить, клавіші керують швидкістю, а не ходьбою.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class DriverSeat : MonoBehaviour, IInteractable
    {
        [SerializeField] private BusDriveController bus;
        [SerializeField] private PlayerMotor2D playerMotor;

        public string Prompt => bus.IsDriving ? "Встати з місця водія" : "Сісти за кермо";

        public void Interact()
        {
            bool driving = !bus.IsDriving;
            bus.SetDriving(driving);
            playerMotor.SetLocked(driving);
        }
    }
}
