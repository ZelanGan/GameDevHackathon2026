using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Двері й вікна салону: відчинені прискорюють охолодження.</summary>
    public class DoorsWindows : MonoBehaviour
    {
        [SerializeField] private BoolEventChannel doorsOpenChanged;

        public bool IsOpen { get; private set; }

        private void Start() => doorsOpenChanged.Raise(IsOpen);

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool value)
        {
            IsOpen = value;
            doorsOpenChanged.Raise(IsOpen);
        }
    }
}
