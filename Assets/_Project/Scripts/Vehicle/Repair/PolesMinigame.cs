using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>Підключення штанг: тричі поспіль натиснути пробіл, коли штанга над дротом.</summary>
    public class PolesMinigame : RepairMinigame
    {
        private const int RequiredHits = 3;
        private const int BarLength = 21;
        private const float MarkerSpeed = 0.8f;
        private const float WireZone = 0.12f;

        private float time;
        private int hits;

        private float Marker => Mathf.PingPong(time * MarkerSpeed, 1f);

        public override string Text
        {
            get
            {
                char[] bar = new string('-', BarLength).ToCharArray();
                bar[BarLength / 2] = '#';
                bar[Mathf.RoundToInt(Marker * (BarLength - 1))] = '|';
                return $"Штанги: Пробіл, коли | на дроті #\n[{new string(bar)}]\nПідключено: {hits}/{RequiredHits}";
            }
        }

        public override bool Tick(Keyboard keyboard, float deltaTime)
        {
            time += deltaTime;
            if (!keyboard.spaceKey.wasPressedThisFrame)
                return false;

            hits = Mathf.Abs(Marker - 0.5f) <= WireZone ? hits + 1 : 0;
            return hits >= RequiredHits;
        }
    }
}
