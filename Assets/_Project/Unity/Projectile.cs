using System;
using Core;
using UnityEngine;

namespace Game
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 20f;
        [SerializeField] private float _lifetime = 3f;

        private Vector3 _direction;
        private Action<Projectile> _returnToPool;
        private int _damage;
        private float _spawnTime;
        private bool _isReturning;

        public void Initialize(Vector3 direction, Action<Projectile> returnToPool, int damage)
        {
            _direction = direction;
            _returnToPool = returnToPool;
            _damage = damage;
            _spawnTime = Time.time;
            _isReturning = false;
        }

        private void Update()
        {
            transform.position += _direction * _speed * Time.deltaTime;

            if (Time.time - _spawnTime >= _lifetime)
                Return();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                DamageData damage = new DamageData(_damage);
                damageable.TakeDamage(damage);
            }

            Return();
        }

        private void Return()
        {
            if (_isReturning)
            {
                return;
            }

            _isReturning = true;
            _returnToPool(this);
        }
    }
}
