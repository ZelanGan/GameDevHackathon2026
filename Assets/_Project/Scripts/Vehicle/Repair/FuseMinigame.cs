using NightTrolleybus.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>Заміна запобіжника: вибрати той, що відповідає потрібному струму.</summary>
    public class FuseMinigame : RepairMinigame
    {
        private static readonly int[] Amps = { 10, 25, 40 };

        private int required;
        private bool sparked;

        public FuseMinigame() => required = Amps[Random.Range(0, Amps.Length)];

        public override string Text =>
            Loc.F("Запобіжник: потрібен на {0} А\n1) {1} А   2) {2} А   3) {3} А", required, Amps[0], Amps[1], Amps[2]) +
            (sparked ? Loc.T("\nІскра! Не той запобіжник.") : string.Empty);

        public override bool Tick(Keyboard keyboard, float deltaTime)
        {
            int digit = ReadDigit(keyboard);
            if (digit == 0)
                return false;

            if (Amps[digit - 1] == required)
                return true;

            sparked = true;
            required = Amps[Random.Range(0, Amps.Length)];
            return false;
        }
    }
}
