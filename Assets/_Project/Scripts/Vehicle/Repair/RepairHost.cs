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

        /// <summary>Кадр, у якому закрилася міні-гра: той самий Esc не має ще й ставити паузу.</summary>
        public int LastClosedFrame { get; private set; } = -1;

        public void Open(FaultType faultToRepair)
        {
            if (IsOpen)
                return;

            fault = faultToRepair;
            minigame = Create(faultToRepair);
            GameContext.Audio?.Play(SoundId.Lock);
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
                GameContext.Audio?.Play(SoundId.RepairDone);
                Close();
                return;
            }

            repairTextChanged.Raise($"{minigame.Text}\nEsc: відійти");
        }

        private void OnDestroy() => repairTextChanged.Raise(string.Empty);

        private void Close()
        {
            minigame = null;
            LastClosedFrame = Time.frameCount;
            repairTextChanged.Raise(string.Empty);
            playerMotor.SetLocked(false);
        }
    }
}
