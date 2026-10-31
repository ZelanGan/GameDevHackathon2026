using UnityEngine;

namespace NightTrolleybus.Core
{
    /// <summary>Просто перебирає кадри спрайта по колу. Зсув за об'єктом, щоб однакові об'єкти не рухались синхронно.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteFrameLoop : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private float framesPerSecond = 6f;

        private SpriteRenderer spriteRenderer;
        private float offset;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            offset = Random.value * frames.Length;
        }

        private void Update()
        {
            int frame = (int)(Time.time * framesPerSecond + offset) % frames.Length;
            spriteRenderer.sprite = frames[frame];
        }
    }
}
