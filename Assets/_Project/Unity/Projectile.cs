using System;
using Core;
using UnityEngine;

namespace Game
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed = 20f;
        [SerializeField] private float _lifetime = 3f;
        [SerializeField] private ParticleSystem _impactEffect;

        private Vector3 _direction;
        private Action<Projectile> _returnToPool;
        private EffectPool _effectPool;
        private int _damage;
        private float _spawnTime;
        private float _runtimeSpeed;
        private float _runtimeLifetime;
        private bool _isReturning;

        public void Initialize(
            Vector3 direction,
            Action<Projectile> returnToPool,
            int damage,
            EffectPool effectPool,
            float speedOverride = -1f,
            float lifetimeOverride = -1f)
        {
            _direction = direction.normalized;
            _returnToPool = returnToPool;
            _effectPool = effectPool;
            _damage = damage;
            _runtimeSpeed = speedOverride > 0f ? speedOverride : _speed;
            _runtimeLifetime = lifetimeOverride > 0f ? lifetimeOverride : _lifetime;
            _spawnTime = Time.time;
            _isReturning = false;
        }

        private void Update()
        {
            transform.position += _direction * _runtimeSpeed * Time.deltaTime;

            if (Time.time - _spawnTime >= _runtimeLifetime)
            {
                Return();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                DamageData damage = new DamageData(_damage);
                damageable.TakeDamage(damage);
            }
            else
            {
                SpawnImpact();
            }

            Return();
        }

        private void SpawnImpact()
        {
            if (_impactEffect == null)
            {
                return;
            }

            if (_effectPool != null)
            {
                _effectPool.Play(_impactEffect, transform.position);
                return;
            }

            Instantiate(_impactEffect, transform.position, Quaternion.identity);
        }

        private void Return()
        {
            if (_isReturning)
            {
                return;
            }

            _isReturning = true;

            if (_returnToPool != null)
            {
                _returnToPool(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
