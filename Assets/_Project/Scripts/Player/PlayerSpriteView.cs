using UnityEngine;

namespace NightTrolleybus.Player
{
    /// <summary>Малює гравця: стоїть або йде залежно від швидкості, дивиться в напрямку руху.</summary>
    public class PlayerSpriteView : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite idleFrame;
        [SerializeField] private Sprite[] walkFrames;
        [SerializeField] private float framesPerSecond = 8f;

        private void Update()
        {
            bool isWalking = Mathf.Abs(body.linearVelocity.x) > 0.1f;
            spriteRenderer.sprite = isWalking
                ? walkFrames[(int)(Time.time * framesPerSecond) % walkFrames.Length]
                : idleFrame;
            spriteRenderer.flipX = motor.Facing < 0f;
        }
    }
}
