using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>Заміна запобіжника: вибрати той, що відповідає потрібному струму.</summary>
    public class FuseMinigame : RepairMinigame
    {
        private static readonly int[] Amps = { 10, 25, 40 };

        public FuseMinigame() => Required = Amps[Random.Range(0, Amps.Length)];

        public int Required { get; private set; }
        public int ChoiceCount => Amps.Length;

        public int AmpsOf(int index) => Amps[index];

        public override bool Tick(Keyboard keyboard, float deltaTime)
        {
            int choice = ReadChoice(keyboard);
            if (choice == 0)
                return false;

            if (Amps[choice - 1] == Required)
                return true;

            Mistakes++;
            Required = Amps[Random.Range(0, Amps.Length)];
            return false;
        }
    }
}
