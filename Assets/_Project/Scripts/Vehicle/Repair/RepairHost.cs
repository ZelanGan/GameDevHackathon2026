using NightTrolleybus.Core;
using NightTrolleybus.Core.Events;
using NightTrolleybus.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>Запускає міні-гру, передає її текст в інтерфейс і усуває несправність після успіху.</summary>
    public class RepairHost : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private StringEventChannel repairTextChanged;
        [Tooltip("Скільки енергії повертає успішний ремонт")]
        [SerializeField] private float powerOnRepair = 10f;

        private RepairMinigame minigame;
        private FaultType fault;

        public bool IsOpen => minigame != null;

        public void Open(FaultType faultToRepair)
        {
            if (IsOpen)
                return;

            fault = faultToRepair;
            minigame = Create(faultToRepair);
            playerMotor.SetLocked(true);
        }

        private static RepairMinigame Create(FaultType faultToRepair)
        {
            switch (faultToRepair)
            {
                case FaultType.PolesOff:
                    return new PolesMinigame();
                case FaultType.FuseBlown:
                    return new FuseMinigame();
                default:
                    return new WiresMinigame();
            }
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            if (minigame.Tick(keyboard, Time.deltaTime))
            {
                GameContext.Faults?.Clear(fault);
                GameContext.Power?.Restore(powerOnRepair);
                Close();
                return;
            }

            repairTextChanged.Raise($"{minigame.Text}\nEsc: відійти");
        }

        private void OnDestroy() => repairTextChanged.Raise(string.Empty);

        private void Close()
        {
            minigame = null;
            repairTextChanged.Raise(string.Empty);
            playerMotor.SetLocked(false);
        }
    }
}
