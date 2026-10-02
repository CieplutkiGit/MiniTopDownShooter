using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Game
{
    public class HealthComponent : MonoBehaviour, IDamageable, IHealthReadable
    {
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private bool _debugLog = false;

        private Health _health;
        private readonly List<IDamageModifier> _damageModifiers = new List<IDamageModifier>();

        public event Action OnDead;
        public event Action<int, int> OnHealthChanged;

        public int Current => _health == null ? _maxHealth : _health.Current;
        public int Max => _health == null ? _maxHealth : _health.Max;
        public int CurrentHealth => Current;
        public int MaxHealth => Max;

        private void Awake()
        {
            EnsureHealthInitialized();
        }

        private void EnsureHealthInitialized()
        {
            if (_health == null)
            {
                CacheDamageModifiers();
                CreateHealth(_maxHealth);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeHealth();
        }

        public void SetMaxHealth(int maxHealth)
        {
            UnsubscribeHealth();
            CreateHealth(maxHealth);
            HandleHealthChanged(_health.Current, _health.Max);
        }

        public void TakeDamage(int damage)
        {
            TakeDamage(new DamageData(damage));
        }

        public void TakeDamage(DamageData data)
        {
            EnsureHealthInitialized();

            DamageData resolved = data;

            for (int i = 0; i < _damageModifiers.Count; i++)
            {
                resolved = _damageModifiers[i].ModifyDamage(resolved);
            }

            if (resolved.Damage <= 0)
            {
                return;
            }

            _health.TakeDamage(resolved);
        }

        public void ResetHealth()
        {
            EnsureHealthInitialized();
            _health.Reset();
        }

        public void RefreshDamageModifiers()
        {
            CacheDamageModifiers();
        }

        private void CreateHealth(int maxHealth)
        {
            _health = new Health(Mathf.Max(1, maxHealth));
            _health.OnDead += HandleDead;
            _health.OnHealthChanged += HandleHealthChanged;
        }

        private void UnsubscribeHealth()
        {
            if (_health == null)
            {
                return;
            }

            _health.OnDead -= HandleDead;
            _health.OnHealthChanged -= HandleHealthChanged;
        }

        private void CacheDamageModifiers()
        {
            _damageModifiers.Clear();
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IDamageModifier modifier)
                {
                    _damageModifiers.Add(modifier);
                }
            }
        }

        private void HandleDead()
        {
            OnDead?.Invoke();
        }

        private void HandleHealthChanged(int current, int max)
        {
            if (_debugLog)
            {
                Debug.Log($"{name} health: {current}/{max}");
            }

            OnHealthChanged?.Invoke(current, max);
        }
    }
}
