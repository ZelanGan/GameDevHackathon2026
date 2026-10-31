using NightTrolleybus.Core.Events;
using UnityEngine;

namespace NightTrolleybus.Vehicle
{
    /// <summary>Шар міста за вікном. Сегменти їдуть уліво й повертаються в кінець черги.</summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private FloatEventChannel speedChanged;
        [Tooltip("Швидкість шару в юнітах за секунду на повній швидкості тролейбуса")]
        [SerializeField] private float scrollAtFullSpeed = 5f;
        [SerializeField] private float segmentWidth = 8f;

        private float speed;
        private float layerWidth;

        private void Awake() => layerWidth = transform.childCount * segmentWidth;

        private void OnEnable() => speedChanged.Raised += OnSpeedChanged;

        private void OnDisable() => speedChanged.Raised -= OnSpeedChanged;

        private void OnSpeedChanged(float normalized) => speed = normalized;

        private void Update()
        {
            float offset = speed * scrollAtFullSpeed * Time.deltaTime;
            if (offset <= 0f)
                return;

            foreach (Transform segment in transform)
            {
                Vector3 position = segment.localPosition;
                position.x -= offset;
                if (position.x < -layerWidth / 2f)
                    position.x += layerWidth;
                segment.localPosition = position;
            }
        }
    }
}
