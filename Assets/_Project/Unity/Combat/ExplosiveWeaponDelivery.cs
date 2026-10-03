using System;
using Core;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Delivers radial explosive weapon impacts, feeding actual impact locations,
    /// marks, and damage to destructible objects and enemies while preserving affiliations.
    /// </summary>
    public class ExplosiveWeaponDelivery : IWeaponDelivery, IWeaponDeliveryAdmission
    {
        private float _radius;
        private LayerMask _mask;
        private float _minDamageRatio;
        private ParticleSystem _explosionEffect;
        private float _range;

        public float Radius => _radius;
        public float Range => _range;

        public ExplosiveWeaponDelivery(
            float radius = 4f,
            float range = 30f,
            LayerMask mask = default,
            ParticleSystem explosionEffect = null,
            float minDamageRatio = 0.25f)
        {
            _radius = Mathf.Max(0.5f, radius);
            _range = Mathf.Max(1f, range);
            _mask = mask.value == 0 ? (LayerMask)(~0) : mask;
            _explosionEffect = explosionEffect;
            _minDamageRatio = Mathf.Clamp01(minDamageRatio);
        }

        public void Deliver(
            Transform spawnPoint,
            Vector3 direction,
            int damage,
            DamageAffiliation sourceAffiliation,
            Transform sourceRoot,
            EffectPool effectPool,
            float spreadAngle = 0f)
        {
            if (spawnPoint == null) return;

            Vector3 fireDir = spreadAngle > 0.001f
                ? Quaternion.AngleAxis(UnityEngine.Random.Range(-spreadAngle, spreadAngle), Vector3.up) * direction
                : direction;

            Vector3 detonatePoint = spawnPoint.position + fireDir * _range;

            if (Physics.Raycast(spawnPoint.position, fireDir, out RaycastHit hit, _range, _mask, QueryTriggerInteraction.Ignore))
            {
                detonatePoint = hit.point;
            }

            ExplosionDamage.Detonate(
                detonatePoint,
                _radius,
                damage,
                sourceAffiliation,
                sourceRoot,
                effectPool,
                _explosionEffect,
                _mask,
                _minDamageRatio);
        }

        public bool TryReserveShot(Transform spawnPoint)
        {
            return true;
        }

        public void CancelReservedShot()
        {
        }

        public void ClearActiveProjectiles()
        {
        }

        public void Dispose()
        {
        }
    }
}
