using UnityEngine;

namespace Game
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Mini Top Down Shooter/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Projectile")]
        [SerializeField] private Projectile _projectilePrefab;

        [Header("Firing")]
        [Min(1)]
        [SerializeField] private int _damage = 10;

        [Min(0.01f)]
        [SerializeField] private float _fireInterval = 0.2f;

        [Min(1)]
        [SerializeField] private int _projectilesPerShot = 1;

        [Range(0f, 45f)]
        [SerializeField] private float _spreadAngle;

        [Header("Pooling")]
        [Min(1)]
        [SerializeField] private int _defaultPoolSize = 10;

        [Min(1)]
        [SerializeField] private int _maxPoolSize = 20;

        public Projectile ProjectilePrefab => _projectilePrefab;
        public int Damage => _damage;
        public float FireInterval => _fireInterval;
        public int ProjectilesPerShot => _projectilesPerShot;
        public float SpreadAngle => _spreadAngle;
        public int DefaultPoolSize => _defaultPoolSize;
        public int MaxPoolSize => _maxPoolSize;

        private void OnValidate()
        {
            _damage = Mathf.Max(1, _damage);
            _fireInterval = Mathf.Max(0.01f, _fireInterval);
            _projectilesPerShot = Mathf.Max(1, _projectilesPerShot);
            _defaultPoolSize = Mathf.Max(1, _defaultPoolSize);
            _maxPoolSize = Mathf.Max(_defaultPoolSize, _maxPoolSize);
        }
    }
}
