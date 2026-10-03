using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Handles radial explosive damage, falloff, affiliations, and visual impact delivery
    /// (crater marks, radial debris, and staged destructible damage).
    /// </summary>
    public static class ExplosionDamage
    {
        public static void Detonate(
            Vector3 center,
            float radius,
            int maxDamage,
            DamageAffiliation sourceAffiliation,
            Transform sourceRoot,
            EffectPool effectPool = null,
            ParticleSystem explosionEffect = null,
            LayerMask mask = default,
            float minDamageRatio = 0.2f,
            float explosionForce = 8f)
        {
            if (mask.value == 0)
            {
                mask = ~0;
            }

            // Visual effect
            if (explosionEffect != null)
            {
                if (effectPool != null)
                {
                    effectPool.Play(explosionEffect, center);
                }
                else
                {
                    UnityEngine.Object.Instantiate(explosionEffect, center, Quaternion.identity);
                }
            }

            // Ground blast crater mark
            if (Physics.Raycast(center + Vector3.up * 0.5f, Vector3.down, out RaycastHit groundHit, radius * 1.5f))
            {
                Combat.CombatImpactPool.SpawnMark(groundHit.point, groundHit.normal, Vector3.down, groundHit.collider.transform, 2.0f);
            }

            Collider[] hits = Physics.OverlapSphere(center, radius, mask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return;

            CombatTeam sourceTeam = sourceAffiliation != null && sourceAffiliation.Team != CombatTeam.Neutral
                ? sourceAffiliation.Team
                : DamageAffiliation.ResolveTeam(sourceRoot);

            HashSet<int> damagedEntities = new HashSet<int>();

            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i];
                if (col == null || (sourceRoot != null && col.transform.root == sourceRoot))
                {
                    continue;
                }

                DamageAffiliation targetAffiliation = DamageAffiliation.Find(col);
                CombatTeam targetTeam = targetAffiliation != null && targetAffiliation.Team != CombatTeam.Neutral
                    ? targetAffiliation.Team
                    : DamageAffiliation.ResolveTeam(col);

                if (DamageAffiliation.ShouldIgnoreFriendlyCollision(sourceAffiliation, sourceTeam, targetAffiliation, targetTeam))
                {
                    continue;
                }

                // Check damageable
                if (!DamageAffiliation.TryGetDamageable(col, out IDamageable damageable, out Component owner))
                {
                    Combat.DestructibleProp prop = col.GetComponentInParent<Combat.DestructibleProp>();
                    if (prop != null && !prop.IsDestroyed)
                    {
                        damageable = prop;
                        owner = prop;
                    }
                }

                CombatTeam ownerTeam = targetAffiliation != null && targetAffiliation.Team != CombatTeam.Neutral
                    ? targetAffiliation.Team
                    : DamageAffiliation.ResolveTeam(owner);

                if (damageable != null && !DamageAffiliation.CanDamage(sourceAffiliation, sourceTeam, targetAffiliation, ownerTeam))
                {
                    continue;
                }

                int entityId = owner != null ? owner.GetInstanceID() : col.GetInstanceID();
                if (!damagedEntities.Add(entityId))
                {
                    continue;
                }

                // Calculate distance and falloff
                Vector3 targetPoint = col.ClosestPoint(center);
                float dist = Vector3.Distance(center, targetPoint);
                float t = Mathf.Clamp01(dist / Mathf.Max(0.01f, radius));
                float mult = Mathf.Lerp(1f, minDamageRatio, t);
                int resolvedDamage = Mathf.Max(1, Mathf.RoundToInt(maxDamage * mult));

                Vector3 dir = (targetPoint - center).normalized;
                if (dir.sqrMagnitude < 0.001f) dir = Vector3.up;

                Vector3 normal = -dir;
                if (Physics.Raycast(center, dir, out RaycastHit directHit, radius * 1.2f))
                {
                    if (directHit.collider == col)
                    {
                        normal = directHit.normal;
                        targetPoint = directHit.point;
                    }
                }

                HitContext hitCtx = new HitContext(targetPoint, normal, dir, sourceRoot);

                if (damageable != null)
                {
                    if (damageable is HealthComponent hc)
                    {
                        hc.TakeDamage(new DamageData(resolvedDamage), hitCtx);
                    }
                    else if (damageable is Combat.DestructibleProp dp)
                    {
                        dp.TakeDamage(new DamageData(resolvedDamage), hitCtx);
                    }
                    else
                    {
                        damageable.TakeDamage(new DamageData(resolvedDamage));
                        Combat.CombatImpactPool.SpawnMark(targetPoint, normal, dir, col.transform, 1.4f);
                    }
                }
                else
                {
                    // Non-damageable surface blast mark
                    Combat.CombatImpactPool.SpawnMark(targetPoint, normal, dir, col.transform, 1.4f);
                }

                // Radial blast debris
                Vector3 debrisDir = (dir + Vector3.up * 0.35f).normalized;
                Combat.CombatDebrisPool.Instance.SpawnChunk(
                    targetPoint + normal * 0.1f,
                    debrisDir * UnityEngine.Random.Range(4f, 8f),
                    UnityEngine.Random.insideUnitSphere * 360f,
                    1.1f,
                    0.2f);
            }
        }
    }
}
