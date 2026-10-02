using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Game
{
    public class HitscanWeaponDelivery : IWeaponDelivery
    {
        private readonly float _range;
        private readonly LayerMask _mask;
        private readonly int _maxPenetrations;
        private readonly int _projectilesPerShot;
        private readonly Func<float, float> _falloffEvaluator;
        private readonly ParticleSystem _impactEffect;

        public HitscanWeaponDelivery(
            float range,
            LayerMask mask,
            int maxPenetrations,
            int projectilesPerShot = 1,
            Func<float, float> falloffEvaluator = null,
            ParticleSystem impactEffect = null)
        {
            _range = Mathf.Max(0.1f, range);
            _mask = mask;
            _maxPenetrations = Mathf.Max(0, maxPenetrations);
            _projectilesPerShot = Mathf.Max(1, projectilesPerShot);
            _falloffEvaluator = falloffEvaluator ?? (d => 1f);
            _impactEffect = impactEffect;
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
            if (spawnPoint == null)
            {
                return;
            }

            CombatTeam sourceTeam = sourceAffiliation != null &&
                                    sourceAffiliation.Team != CombatTeam.Neutral
                ? sourceAffiliation.Team
                : DamageAffiliation.ResolveTeam(sourceRoot);

            for (int rayIndex = 0; rayIndex < _projectilesPerShot; rayIndex++)
            {
                Vector3 rayDir = spreadAngle > 0.001f
                    ? Quaternion.AngleAxis(UnityEngine.Random.Range(-spreadAngle, spreadAngle), Vector3.up) * direction
                    : direction;

                RaycastHit[] hits = Physics.RaycastAll(
                    spawnPoint.position,
                    rayDir,
                    _range,
                    _mask,
                    QueryTriggerInteraction.Ignore);

                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                int remainingPenetrations = _maxPenetrations;
                HashSet<int> damagedTargets = new HashSet<int>();

                for (int i = 0; i < hits.Length; i++)
                {
                    Collider collider = hits[i].collider;

                    if (collider == null ||
                        (sourceRoot != null && collider.transform.root == sourceRoot))
                    {
                        continue;
                    }

                    DamageAffiliation targetAffiliation = DamageAffiliation.Find(collider);
                    CombatTeam targetTeam = targetAffiliation != null &&
                                            targetAffiliation.Team != CombatTeam.Neutral
                        ? targetAffiliation.Team
                        : DamageAffiliation.ResolveTeam(collider);

                    if (DamageAffiliation.ShouldIgnoreFriendlyCollision(
                            sourceAffiliation,
                            sourceTeam,
                            targetAffiliation,
                            targetTeam))
                    {
                        continue;
                    }

                    if (!DamageAffiliation.TryGetDamageable(
                            collider,
                            out IDamageable damageable,
                            out Component owner))
                    {
                        SpawnImpact(hits[i].point, effectPool);
                        break;
                    }

                    targetTeam = targetAffiliation != null &&
                                 targetAffiliation.Team != CombatTeam.Neutral
                        ? targetAffiliation.Team
                        : DamageAffiliation.ResolveTeam(owner);

                    if (!DamageAffiliation.CanDamage(
                            sourceAffiliation,
                            sourceTeam,
                            targetAffiliation,
                            targetTeam))
                    {
                        break;
                    }

                    int targetId = owner != null ? owner.GetInstanceID() : collider.GetInstanceID();
                    if (!damagedTargets.Add(targetId))
                    {
                        continue;
                    }

                    float multiplier = _falloffEvaluator(hits[i].distance);
                    int resolvedDamage = Mathf.RoundToInt(damage * multiplier);

                    if (resolvedDamage > 0)
                    {
                        damageable.TakeDamage(new DamageData(resolvedDamage));
                    }

                    SpawnImpact(hits[i].point, effectPool);

                    if (remainingPenetrations <= 0)
                    {
                        break;
                    }

                    remainingPenetrations--;
                }
            }
        }

        public void ClearActiveProjectiles()
        {
            // Hitscan is instantaneous; no active projectiles to clear.
        }

        public void Dispose()
        {
        }

        private void SpawnImpact(Vector3 position, EffectPool effectPool)
        {
            if (_impactEffect == null)
            {
                return;
            }

            if (effectPool != null)
            {
                effectPool.Play(_impactEffect, position);
                return;
            }

            UnityEngine.Object.Instantiate(_impactEffect, position, Quaternion.identity);
        }
    }
}
