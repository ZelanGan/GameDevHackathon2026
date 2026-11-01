using NightTrolleybus.Core;
using NightTrolleybus.Player;
using NightTrolleybus.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>Запускає міні-гру, показує її у вікні ремонту й усуває несправність після успіху.</summary>
    public class RepairHost : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D playerMotor;
        [SerializeField] private RepairView view;
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
            view.Show(minigame, Title(faultToRepair), Leave);
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

        private static string Title(FaultType faultToRepair)
        {
            switch (faultToRepair)
            {
                case FaultType.PolesOff:
                    return Loc.T("Підключити штанги");
                case FaultType.FuseBlown:
                    return Loc.T("Замінити запобіжник");
                default:
                    return Loc.T("З'єднати проводи");
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
                Leave();
                return;
            }

            if (minigame.Tick(keyboard, Time.deltaTime))
            {
                GameContext.Faults?.Clear(fault);
                GameContext.Power?.Restore(powerOnRepair);
                GameContext.Audio?.Play(SoundId.RepairDone);
                view.ShowDone();
                Close();
            }
        }

        private void Leave()
        {
            view.Hide();
            Close();
        }

        private void Close()
        {
            minigame = null;
            LastClosedFrame = Time.frameCount;
            playerMotor.SetLocked(false);
        }
    }
}
