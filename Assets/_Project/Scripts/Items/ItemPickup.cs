using UnityEngine;

namespace NightTrolleybus.Items
{
    /// <summary>Предмет на сцені. Гравець підбирає його дотиком, а за повного інвентаря предмет лишається.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemData item;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (item.Give())
                Destroy(gameObject);
        }
    }
}
