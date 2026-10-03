using System;
using System.Collections.Generic;
using Application.Economy;
using UnityEngine;

namespace Game.Economy
{
    public class SalvagePickupPool : MonoBehaviour
    {
        [SerializeField] private int _defaultPoolSize = 25;
        [SerializeField] private int _maxPoolSize = 100;

        private readonly Queue<SalvagePickup> _available = new Queue<SalvagePickup>();
        private readonly HashSet<SalvagePickup> _availableSet = new HashSet<SalvagePickup>();
        private readonly List<SalvagePickup> _active = new List<SalvagePickup>();

        public int ActiveCount => _active.Count;
        public int AvailableCount => _available.Count;
        public int TotalCreatedCount => _active.Count + _available.Count;

        public event Action<SalvagePickup> OnPickupCollected;

        private bool _isInitialized;

        public void Initialize(int? prewarmCount = null)
        {
            if (_isInitialized) return;
            _isInitialized = true;
            Prewarm(prewarmCount ?? _defaultPoolSize);
        }

        private void Awake()
        {
            Initialize();
        }

        public void Prewarm(int count)
        {
            int toCreate = Math.Min(count, _maxPoolSize - TotalCreatedCount);
            for (int i = 0; i < toCreate; i++)
            {
                SalvagePickup pickup = CreatePickupInstance();
                pickup.ResetForPool();
                if (!_availableSet.Contains(pickup))
                {
                    _available.Enqueue(pickup);
                    _availableSet.Add(pickup);
                }
            }
        }

        public SalvagePickup Spawn(Vector3 position, EconomyComponentType type, int amount, Transform player = null)
        {
            if (!_isInitialized && TotalCreatedCount == 0)
            {
                Initialize();
            }

            SalvagePickup pickup;
            if (_available.Count > 0)
            {
                pickup = _available.Dequeue();
                _availableSet.Remove(pickup);
            }
            else if (TotalCreatedCount < _maxPoolSize)
            {
                pickup = CreatePickupInstance();
            }
            else
            {
                // Under pool pressure: coalesce amount onto existing active pickup of same type to preserve earned rewards
                for (int i = 0; i < _active.Count; i++)
                {
                    SalvagePickup existing = _active[i];
                    if (existing != null && existing.ComponentType == type && !existing.IsCollected)
                    {
                        existing.AddAmount(amount);
                        return existing;
                    }
                }

                // If no matching type active, recycle oldest active pickup and coalesce
                if (_active.Count > 0)
                {
                    pickup = _active[0];
                    _active.RemoveAt(0);
                    pickup.ResetForPool();
                }
                else
                {
                    pickup = CreatePickupInstance();
                }
            }

            pickup.transform.SetParent(transform);
            pickup.Initialize(type, amount, position);
            if (player != null)
            {
                pickup.SetTargetPlayer(player);
            }

            _active.Add(pickup);
            return pickup;
        }

        public void Release(SalvagePickup pickup)
        {
            if (pickup == null) return;

            if (!_active.Remove(pickup))
            {
                return; // Guard against duplicate release
            }

            if (_availableSet.Contains(pickup))
            {
                return;
            }

            pickup.ResetForPool();

            if (_available.Count < _maxPoolSize)
            {
                _available.Enqueue(pickup);
                _availableSet.Add(pickup);
            }
            else
            {
                SafeDestroy(pickup.gameObject);
            }
        }

        public void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                SalvagePickup pickup = _active[i];
                if (pickup != null)
                {
                    pickup.ResetForPool();
                    if (!_availableSet.Contains(pickup))
                    {
                        _available.Enqueue(pickup);
                        _availableSet.Add(pickup);
                    }
                }
            }
            _active.Clear();
        }

        private SalvagePickup CreatePickupInstance()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "SalvagePickup";
            go.transform.SetParent(transform);

            SalvagePickup pickup = go.AddComponent<SalvagePickup>();
            pickup.OnCollected += HandlePickupCollected;

            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            return pickup;
        }

        private void HandlePickupCollected(SalvagePickup pickup)
        {
            OnPickupCollected?.Invoke(pickup);
            Release(pickup);
        }

        private static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (!UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(obj);
                return;
            }
#endif
            UnityEngine.Object.Destroy(obj);
        }

        private void OnDestroy()
        {
            ClearAll();
            while (_available.Count > 0)
            {
                SalvagePickup p = _available.Dequeue();
                if (p != null && p.gameObject != null)
                {
                    SafeDestroy(p.gameObject);
                }
            }
            _availableSet.Clear();
        }
    }
}
