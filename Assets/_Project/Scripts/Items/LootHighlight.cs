using UnityEngine;

namespace NightTrolleybus.Items
{
    /// <summary>Лут ледь пульсує й сяє, щоб його не можна було сплутати зі сміттям на підлозі. Колір не чіпає: ним керує приховування предметів.</summary>
    public class LootHighlight : MonoBehaviour
    {
        [SerializeField] private Transform glow;
        [SerializeField] private float pulseSpeed = 3.5f;
        [SerializeField] private float itemPulse = 0.1f;
        [SerializeField] private float glowPulse = 0.25f;

        private Vector3 itemScale;
        private Vector3 glowScale;
        private float phase;

        private void Awake()
        {
            itemScale = transform.localScale;
            glowScale = glow.localScale;
            // Предмети не мають пульсувати в унісон.
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            float wave = Mathf.Sin(Time.time * pulseSpeed + phase);
            transform.localScale = itemScale * (1f + wave * itemPulse);
            glow.localScale = glowScale * (1f + wave * glowPulse);
        }
    }
}
