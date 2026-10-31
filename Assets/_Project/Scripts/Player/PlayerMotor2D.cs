using UnityEngine;
using UnityEngine.InputSystem;

namespace NightTrolleybus.Player
{
    /// <summary>Рух гравця через Rigidbody2D: хода та, поза салоном, стрибок.</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private float speed = 4f;
        [SerializeField] private bool canJump = true;
        [SerializeField] private float jumpSpeed = 8f;

        private Rigidbody2D body;
        private InputAction moveAction;
        private InputAction jumpAction;
        private ContactFilter2D groundFilter;
        private float input;
        private bool locked;
        private bool warnedNoKeyboard;

        /// <summary>Напрямок погляду: 1 вправо, -1 вліво.</summary>
        public float Facing { get; private set; } = 1f;
        public bool IsLocked => locked;
        public bool IsGrounded => body.IsTouching(groundFilter);

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            moveAction = InputSystem.actions.FindAction("Player/Move", true);
            jumpAction = InputSystem.actions.FindAction("Player/Jump", true);
            // Опорою вважається лише поверхня, нормаль якої дивиться вгору.
            groundFilter = new ContactFilter2D { useNormalAngle = true, minNormalAngle = 45f, maxNormalAngle = 135f };
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

            input = locked ? 0f : moveAction.ReadValue<Vector2>().x;
            if (input != 0f)
                Facing = Mathf.Sign(input);

            if (!locked && canJump && jumpAction.WasPressedThisFrame() && IsGrounded)
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
        }

        private void FixedUpdate() => body.linearVelocity = new Vector2(input * speed, body.linearVelocity.y);
    }
}
