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

        [SerializeField] private float _collisionRadius = 0.15f;
        [SerializeField] private LayerMask _collisionMask = ~0;

        private Vector3 _direction;
        private Action<Projectile> _returnToPool;
        private EffectPool _effectPool;
        private int _damage;
        private float _spawnTime;
        private float _runtimeSpeed;
        private float _runtimeLifetime;
        private bool _isReturning;
        private int _defaultLayer;
        private Transform _sourceRoot;
        private DamageAffiliation _sourceAffiliation;
        private CombatTeam _sourceTeam;

        private void Awake()
        {
            _defaultLayer = gameObject.layer;
        }

        public void Initialize(
            Vector3 direction,
            Action<Projectile> returnToPool,
            int damage,
            EffectPool effectPool,
            float speedOverride = -1f,
            float lifetimeOverride = -1f,
            DamageAffiliation sourceAffiliation = null,
            Transform sourceRoot = null)
        {
            _direction = direction.normalized;
            _returnToPool = returnToPool;
            _effectPool = effectPool;
            _damage = damage;
            _runtimeSpeed = speedOverride > 0f ? speedOverride : _speed;
            _runtimeLifetime = lifetimeOverride > 0f ? lifetimeOverride : _lifetime;
            _spawnTime = Time.time;
            _isReturning = false;
            _sourceRoot = sourceRoot != null ? sourceRoot.root : null;
            _sourceAffiliation = sourceAffiliation;

            if (_sourceAffiliation == null && _sourceRoot != null)
            {
                _sourceAffiliation = DamageAffiliation.Find(_sourceRoot);
            }

            _sourceTeam = _sourceAffiliation != null &&
                          _sourceAffiliation.Team != CombatTeam.Neutral
                ? _sourceAffiliation.Team
                : DamageAffiliation.ResolveTeam(_sourceRoot);

            gameObject.layer = ResolveProjectileLayer(_sourceTeam, _defaultLayer);
        }

        private void OnDisable()
        {
            if (_defaultLayer >= 0)
            {
                gameObject.layer = _defaultLayer;
            }
        }

        private void Update()
        {
            float moveDistance = _runtimeSpeed * Time.deltaTime;
            Vector3 startPosition = transform.position;

            RaycastHit[] hits = Physics.SphereCastAll(
                startPosition,
                _collisionRadius,
                _direction,
                moveDistance,
                _collisionMask,
                QueryTriggerInteraction.Ignore);

            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                for (int i = 0; i < hits.Length; i++)
                {
                    if (ProcessHit(hits[i].collider, hits[i].point))
                    {
                        return;
                    }
                }
            }

            transform.position += _direction * moveDistance;

            if (Time.time - _spawnTime >= _runtimeLifetime)
            {
                Return();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            ProcessHit(other, transform.position);
        }

        private bool ProcessHit(Collider other, Vector3 hitPosition)
        {
            if (other == null || _isReturning)
            {
                return false;
            }

            if (_sourceRoot != null && other.transform.root == _sourceRoot)
            {
                return false;
            }

            DamageAffiliation targetAffiliation = DamageAffiliation.Find(other);
            CombatTeam targetTeam =
                targetAffiliation != null &&
                targetAffiliation.Team != CombatTeam.Neutral
                    ? targetAffiliation.Team
                    : DamageAffiliation.ResolveTeam(other);

            if (DamageAffiliation.ShouldIgnoreFriendlyCollision(
                    _sourceAffiliation,
                    _sourceTeam,
                    targetAffiliation,
                    targetTeam))
            {
                return false;
            }

            Vector3 normal = -_direction;
            Ray probeRay = new Ray(hitPosition - _direction * 0.2f, _direction);
            if (other.Raycast(probeRay, out RaycastHit hitInfo, 0.5f))
            {
                normal = hitInfo.normal;
                hitPosition = hitInfo.point;
            }

            if (!DamageAffiliation.TryGetDamageable(
                    other,
                    out IDamageable damageable,
                    out Component owner))
            {
                Combat.DestructibleProp prop = other.GetComponentInParent<Combat.DestructibleProp>();
                if (prop != null && !prop.IsDestroyed)
                {
                    damageable = prop;
                    owner = prop;
                }
            }

            if (damageable != null)
            {
                CombatTeam ownerTeam =
                    targetAffiliation != null &&
                    targetAffiliation.Team != CombatTeam.Neutral
                        ? targetAffiliation.Team
                        : DamageAffiliation.ResolveTeam(owner);

                if (DamageAffiliation.CanDamage(
                        _sourceAffiliation,
                        _sourceTeam,
                        targetAffiliation,
                        ownerTeam))
                {
                    HitContext hitCtx = new HitContext(hitPosition, normal, _direction, _sourceRoot);
                    if (damageable is HealthComponent hc)
                    {
                        hc.TakeDamage(new DamageData(_damage), hitCtx);
                    }
                    else if (damageable is Combat.DestructibleProp dp)
                    {
                        dp.TakeDamage(new DamageData(_damage), hitCtx);
                    }
                    else
                    {
                        damageable.TakeDamage(new DamageData(_damage));
                        Combat.CombatImpactPool.SpawnMark(hitPosition, normal, _direction, other.transform);
                    }
                }

                SpawnImpact(hitPosition);
                Return();
                return true;
            }

            Combat.CombatImpactPool.SpawnMark(hitPosition, normal, _direction, other.transform);
            SpawnImpact(hitPosition);
            Return();
            return true;
        }

        public void ForceReturnToPool()
        {
            Return();
        }

        private static int ResolveProjectileLayer(
            CombatTeam sourceTeam,
            int defaultLayer)
        {
            if (sourceTeam == CombatTeam.Player)
            {
                int playerProjectileLayer = LayerMask.NameToLayer("PlayerProjectile");
                return playerProjectileLayer >= 0
                    ? playerProjectileLayer
                    : defaultLayer;
            }

            if (sourceTeam == CombatTeam.Enemy)
            {
                int enemyProjectileLayer = LayerMask.NameToLayer("EnemyProjectile");

                if (enemyProjectileLayer >= 0)
                {
                    return enemyProjectileLayer;
                }

                return LayerMask.NameToLayer("Default");
            }

            return defaultLayer;
        }

        private void SpawnImpact(Vector3? position = null)
        {
            Vector3 impactPos = position ?? transform.position;

            if (_impactEffect == null)
            {
                return;
            }

            if (_effectPool != null)
            {
                _effectPool.Play(_impactEffect, impactPos);
                return;
            }

            Instantiate(_impactEffect, impactPos, Quaternion.identity);
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
