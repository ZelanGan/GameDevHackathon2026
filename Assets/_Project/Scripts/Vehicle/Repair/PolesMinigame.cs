using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Vehicle.Repair
{
    /// <summary>Підключення штанг: тричі поспіль натиснути пробіл, коли штанга над дротом.</summary>
    public class PolesMinigame : RepairMinigame
    {
        public const int RequiredHits = 3;

        private const float MarkerSpeed = 0.8f;

        /// <summary>Половина ширини влучної зони в частках шкали.</summary>
        public const float WireZone = 0.12f;

        private float time;

        public int Hits { get; private set; }

        /// <summary>Положення штанги на шкалі 0..1, дріт посередині.</summary>
        public float Marker => Mathf.PingPong(time * MarkerSpeed, 1f);

        public override bool Tick(Keyboard keyboard, float deltaTime)
        {
            time += deltaTime;
            bool pressed = ConsumePress() != 0 || keyboard.spaceKey.wasPressedThisFrame;
            if (!pressed)
                return false;

            if (Mathf.Abs(Marker - 0.5f) <= WireZone)
            {
                Hits++;
            }
            else
            {
                Hits = 0;
                Mistakes++;
            }

            return Hits >= RequiredHits;
        }
    }
}
