using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>З'єднання проводів: натиснути номери в указаному порядку.</summary>
    public class WiresMinigame : RepairMinigame
    {
        private readonly int[] order = { 1, 2, 3 };

        public WiresMinigame()
        {
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }

        public int WireCount => order.Length;
        public int Connected { get; private set; }

        /// <summary>Номер проводу (1..3), який стоїть на цьому місці в черзі.</summary>
        public int WireAt(int position) => order[position];

        public bool IsConnected(int wire)
        {
            for (int i = 0; i < Connected; i++)
            {
                if (order[i] == wire)
                    return true;
            }

            return false;
        }

        public override bool Tick(Keyboard keyboard, float deltaTime)
        {
            int choice = ReadChoice(keyboard);
            if (choice == 0)
                return false;

            if (choice == order[Connected])
            {
                Connected++;
            }
            else
            {
                Connected = 0;
                Mistakes++;
            }

            return Connected == order.Length;
        }
    }
}
