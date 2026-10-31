using NightTrolleybus.Core;
using UnityEngine;

namespace NightTrolleybus.Player
{
    /// <summary>Малює гравця: стоїть або йде за швидкістю. Збоку розвертається дзеркально, зверху обертається за поглядом.</summary>
    public class PlayerSpriteView : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite idleFrame;
        [SerializeField] private Sprite[] walkFrames;
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
            spriteRenderer.sprite = isWalking
                ? walkFrames[(int)(Time.time * framesPerSecond) % walkFrames.Length]
                : idleFrame;

            Vector2 facing = motor.FacingDirection;
            if (motor.IsTopDown)
                spriteRenderer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg);
            else if (facing.x != 0f)
                spriteRenderer.flipX = facing.x < 0f;
        }
    }
}
