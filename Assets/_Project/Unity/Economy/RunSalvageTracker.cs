using System;
using Application.Economy;
using UnityEngine;

namespace Game.Economy
{
    public class RunSalvageTracker : MonoBehaviour
    {
        private static RunSalvageTracker s_instance;
        public static RunSalvageTracker Instance => s_instance;

        private int _scrapCollected;
        private int _alloyCollected;
        private int _coreCollected;

        public int ScrapCollected => _scrapCollected;
        public int AlloyCollected => _alloyCollected;
        public int CoreCollected => _coreCollected;
        public int TotalComponentsCollected => _scrapCollected + _alloyCollected + _coreCollected;
        public string RunId { get; private set; }

        public event Action<EconomyComponentType, int> OnSalvageCollected;

        public void Initialize()
        {
            s_instance = this;
        }

        public void BeginRun(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId)) return;
            if (string.Equals(RunId, runId, StringComparison.Ordinal)) return;
            ResetTracker();
            RunId = runId;
        }

        private void Awake()
        {
            Initialize();
        }

        private SalvagePickupPool _boundPool;

        public static void ResetForTesting()
        {
            if (s_instance != null)
            {
                s_instance.UnbindPool();
                s_instance.EndRun();
            }
            s_instance = null;
        }

        public void BindPool(SalvagePickupPool pool)
        {
            if (_boundPool != null)
            {
                _boundPool.OnPickupCollected -= HandlePickupCollected;
            }
            _boundPool = pool;
            if (_boundPool != null)
            {
                _boundPool.OnPickupCollected -= HandlePickupCollected;
                _boundPool.OnPickupCollected += HandlePickupCollected;
            }
        }

        public void UnbindPool(SalvagePickupPool pool = null)
        {
            if (pool != null)
            {
                pool.OnPickupCollected -= HandlePickupCollected;
                if (_boundPool == pool) _boundPool = null;
            }
            else if (_boundPool != null)
            {
                _boundPool.OnPickupCollected -= HandlePickupCollected;
                _boundPool = null;
            }
        }

        public void RecordPickup(EconomyComponentType type, int amount)
        {
            if (amount <= 0) return;

            switch (type)
            {
                case EconomyComponentType.Scrap:
                    _scrapCollected += amount;
                    break;
                case EconomyComponentType.Alloy:
                    _alloyCollected += amount;
                    break;
                case EconomyComponentType.Core:
                    _coreCollected += amount;
                    break;
            }

            OnSalvageCollected?.Invoke(type, amount);
        }

        private void HandlePickupCollected(SalvagePickup pickup)
        {
            if (pickup != null)
            {
                RecordPickup(pickup.ComponentType, pickup.Amount);
            }
        }

        public void ResetTracker()
        {
            _scrapCollected = 0;
            _alloyCollected = 0;
            _coreCollected = 0;
        }

        public void EndRun()
        {
            ResetTracker();
            RunId = null;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                EndRun();
                s_instance = null;
            }
        }
    }
}
