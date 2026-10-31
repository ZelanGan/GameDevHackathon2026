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
            $"Запобіжник: потрібен на {required} А\n1) {Amps[0]} А   2) {Amps[1]} А   3) {Amps[2]} А" +
            (sparked ? "\nІскра! Не той запобіжник." : string.Empty);

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
