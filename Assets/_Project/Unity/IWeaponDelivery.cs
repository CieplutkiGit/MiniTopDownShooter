using System;
using UnityEngine;

namespace Game
{
    public interface IWeaponDeliveryAdmission
    {
        bool TryReserveShot(Transform spawnPoint);
        void CancelReservedShot();
    }

    public interface IWeaponDelivery : IDisposable
    {
        void Deliver(
            Transform spawnPoint,
            Vector3 direction,
            int damage,
            DamageAffiliation sourceAffiliation,
            Transform sourceRoot,
            EffectPool effectPool,
            float spreadAngle = 0f);

        void ClearActiveProjectiles();
    }
}
