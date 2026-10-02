using System;
using UnityEngine;

namespace Game
{
    public interface IWeaponDelivery : IDisposable
    {
        void Deliver(
            Transform spawnPoint,
            Vector3 direction,
            int damage,
            DamageAffiliation sourceAffiliation,
            Transform sourceRoot,
            EffectPool effectPool);

        void ClearActiveProjectiles();
    }
}
