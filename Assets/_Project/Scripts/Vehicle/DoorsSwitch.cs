using NightTrolleybus.Core;
using NightTrolleybus.Player;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Перемикач дверей і вікон у салоні. Стан дверей живе в сцені Boot.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class DoorsSwitch : MonoBehaviour, IInteractable
    {
        private DoorsWindows doors;

        public string Prompt => doors.IsOpen ? "Зачинити двері" : "Відчинити двері";

        private void Awake() => doors = FindAnyObjectByType<DoorsWindows>();

        public void Interact()
        {
            doors.Toggle();
            GameContext.Audio?.Play(doors.IsOpen ? SoundId.DoorOpen : SoundId.DoorClose);
        }
    }
}
