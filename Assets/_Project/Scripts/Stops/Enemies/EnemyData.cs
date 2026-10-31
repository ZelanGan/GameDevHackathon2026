using UnityEngine;

namespace NightTrolleybus.Stops.Enemies
{
    /// <summary>Параметри моба темряви.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Enemy", fileName = "Enemy")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private float detectionRange = 9f;
        [SerializeField] private float attackRange = 1.1f;
        [SerializeField] private float attackDamage = 10f;
        [SerializeField] private float attackInterval = 1.2f;
        [Tooltip("Страх Вогника 0..1, коли моб близько")]
        [Range(0f, 1f)]
        [SerializeField] private float fear = 0.25f;
        [SerializeField] private float fearRange = 6f;

        public float MaxHealth => maxHealth;
        public float Speed => speed;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackDamage => attackDamage;
        public float AttackInterval => attackInterval;
        public float Fear => fear;
        public float FearRange => fearRange;
    }
}
