using System.Linq;
using NightTrolleybus.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>З'єднання проводів: натиснути номери в указаному порядку.</summary>
    public class WiresMinigame : RepairMinigame
    {
        private readonly int[] order = { 1, 2, 3 };
        private int connected;

        public WiresMinigame()
        {
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }

        public override string Text =>
            Loc.F("Проводи: з'єднайте по черзі\n{0}\nЗ'єднано: {1}/{2}", string.Join(" -> ", order.Select(wire => wire.ToString())), connected, order.Length);

        public override bool Tick(Keyboard keyboard, float deltaTime)
        {
            int digit = ReadDigit(keyboard);
            if (digit == 0)
                return false;

            connected = digit == order[connected] ? connected + 1 : 0;
            return connected == order.Length;
        }
    }
}
