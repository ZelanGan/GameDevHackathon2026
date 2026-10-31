using NightTrolleybus.Core;
using UnityEngine;

namespace NightTrolleybus.Player
{
    /// <summary>Малює гравця: стоїть або йде за швидкістю. Розвертається дзеркально за поглядом: цілий стоячий водій не має крутитися навколо себе.</summary>
    public class PlayerSpriteView : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite idleFrame;
        [SerializeField] private Sprite[] walkFrames;
        [Tooltip("Кадри спиною: коли водій дивиться вгору екрана. Лише для виду зверху")]
        [SerializeField] private Sprite backIdleFrame;
        [SerializeField] private Sprite[] backWalkFrames;
        [Range(0f, 1f)]
        [SerializeField] private float backFacingThreshold = 0.5f;
        [SerializeField] private float framesPerSecond = 8f;
        [SerializeField] private float stepInterval = 0.34f;

        private float stepTimer;

        private void UpdateSteps(bool isWalking)
        {
            stepTimer -= Time.deltaTime;
            if (!isWalking || stepTimer > 0f)
                return;

            stepTimer = stepInterval;
            GameContext.Audio?.Play(SoundId.Step);
        }

        private void Update()
        {
            bool isWalking = body.linearVelocity.sqrMagnitude > 0.01f;
            UpdateSteps(isWalking);

            Vector2 facing = motor.FacingDirection;
            bool showsBack = motor.IsTopDown && backIdleFrame != null && facing.y > backFacingThreshold;
            Sprite[] frames = showsBack ? backWalkFrames : walkFrames;
            spriteRenderer.sprite = isWalking
                ? frames[(int)(Time.time * framesPerSecond) % frames.Length]
                : showsBack ? backIdleFrame : idleFrame;

            if (facing.x != 0f)
                spriteRenderer.flipX = facing.x < 0f;
        }
    }
}
