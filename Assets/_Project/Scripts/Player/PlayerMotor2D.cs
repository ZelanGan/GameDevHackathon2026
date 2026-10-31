using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Player
{
    /// <summary>Рух гравця через Rigidbody2D: у салоні вид збоку, на зупинках вид зверху.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private float speed = 4f;
        [Tooltip("Вид зверху: рух у всіх напрямках без гравітації, погляд за курсором")]
        [SerializeField] private bool topDown;

        private Rigidbody2D body;
        private InputAction moveAction;
        private Vector2 input;
        private bool locked;
        private bool warnedNoKeyboard;

        /// <summary>Куди дивиться гравець. Завжди одиничний вектор.</summary>
        public Vector2 FacingDirection { get; private set; } = Vector2.right;
        public bool IsTopDown => topDown;
        public bool IsLocked => locked;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            moveAction = InputSystem.actions.FindAction("Player/Move", true);
            if (topDown)
                body.gravityScale = 0f;
        }

        /// <summary>Блокує рух, наприклад коли гравець сидить за кермом.</summary>
        public void SetLocked(bool value) => locked = value;

        private void Update()
        {
            // Без цього попередження зникнення пристроїв у редакторі виглядає як помилка в русі.
            if (Keyboard.current == null && !warnedNoKeyboard && Application.isFocused)
            {
                warnedNoKeyboard = true;
                Debug.LogWarning("[Рух] Input System не бачить клавіатуру. Спробуйте перезапустити редактор.");
            }

            input = locked ? Vector2.zero : moveAction.ReadValue<Vector2>();
            if (!topDown)
                input.y = 0f;

            UpdateFacing();
        }

        private void UpdateFacing()
        {
            if (topDown && Mouse.current != null && Camera.main != null)
            {
                Vector2 cursor = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                Vector2 toCursor = cursor - (Vector2)transform.position;
                if (toCursor.sqrMagnitude > 0.01f)
                    FacingDirection = toCursor.normalized;
            }
            else if (input != Vector2.zero)
            {
                FacingDirection = input.normalized;
            }
        }

        private void FixedUpdate()
        {
            // Діагональ не має бути швидшою за рух по прямій.
            if (topDown)
                body.linearVelocity = Vector2.ClampMagnitude(input, 1f) * speed;
            else
                body.linearVelocity = new Vector2(input.x * speed, body.linearVelocity.y);
        }
    }
}
