using System.Linq;
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
            $"Проводи: з'єднайте по черзі\n{string.Join(" -> ", order.Select(wire => wire.ToString()))}\nЗ'єднано: {connected}/{order.Length}";

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
