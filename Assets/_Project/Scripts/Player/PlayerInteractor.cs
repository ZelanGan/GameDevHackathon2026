using NightTrolleybus.Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Player
{
    /// <summary>Знаходить найближчий об'єкт взаємодії, показує підказку й виконує дію.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float radius = 1.2f;
        [SerializeField] private StringEventChannel promptChanged;

        private readonly Collider2D[] hits = new Collider2D[8];
        private ContactFilter2D filter;
        private InputAction interactAction;
        private string currentPrompt;

        private void Awake()
        {
            filter = ContactFilter2D.noFilter;
            interactAction = InputSystem.actions.FindAction("Player/Interact", true);
        }

        private void Update()
        {
            IInteractable nearest = FindNearest();
            SetPrompt(nearest != null ? $"E — {nearest.Prompt}" : string.Empty);

            if (nearest != null && interactAction.WasPressedThisFrame())
                nearest.Interact();
        }

        private IInteractable FindNearest()
        {
            IInteractable nearest = null;
            float bestDistance = float.MaxValue;
            int count = Physics2D.OverlapCircle(transform.position, radius, filter, hits);

            for (int i = 0; i < count; i++)
            {
                if (!hits[i].TryGetComponent(out IInteractable interactable))
                    continue;

                float distance = Vector2.Distance(transform.position, hits[i].transform.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = interactable;
                }
            }

            return nearest;
        }

        private void SetPrompt(string prompt)
        {
            if (prompt == currentPrompt)
                return;

            currentPrompt = prompt;
            promptChanged.Raise(prompt);
        }
    }
}
