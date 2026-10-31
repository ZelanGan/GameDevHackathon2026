using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Core
{
    /// <summary>Вмикає дії введення перед запуском сцени, бо самі вони лишаються вимкненими.</summary>
    public static class GameInput
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnableActions()
        {
            InputSystem.actions.Enable();
            Application.quitting -= DisableActions;
            Application.quitting += DisableActions;
        }

        private static void DisableActions() => InputSystem.actions.Disable();
    }
}
