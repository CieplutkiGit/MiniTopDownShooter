using System.Collections.Generic;
using Application;
using Application.Weapons;
using Game;
using NUnit.Framework;
using UnityEngine;
using WeaponFireMode = Application.WeaponFireMode;
using WeaponDeliveryMode = Application.WeaponDeliveryMode;

namespace MiniTopDownShooter.Tests.Workshop.A3
{
    [TestFixture]
    public class WeaponCombatAdapterTests
    {
        private List<GameObject> _spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            _spawnedObjects = new List<GameObject>();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_spawnedObjects != null)
            {
                for (int i = 0; i < _spawnedObjects.Count; i++)
                {
                    if (_spawnedObjects[i] != null)
                    {
                        Object.DestroyImmediate(_spawnedObjects[i]);
                    }
                }
                _spawnedObjects.Clear();
            }
        }

        [Test]
        public void WeaponCombatAdapter_Implements_IWeaponBuildTarget()
        {
            var go = new GameObject("AdapterGo");
            _spawnedObjects.Add(go);
            var adapter = go.AddComponent<WeaponCombatAdapter>();

            Assert.IsInstanceOf<IWeaponBuildTarget>(adapter);
        }

        [Test]
        public void WeaponCombatAdapter_RejectsIfGunMissing()
        {
            var go = new GameObject("AdapterGo");
            _spawnedObjects.Add(go);
            var adapter = go.AddComponent<WeaponCombatAdapter>();

            var build = new WeaponBuild(WeaponWorkshopIds.Rifle);
            var stats = new ResolvedWeaponStats(
                damage: 20f,
                fireInterval: 0.1f,
                magazineCapacity: 30,
                maxReserveAmmo: 120,
                startingReserveAmmo: 60,
                reloadDuration: 1.5f,
                baseSpreadAngle: 1f,
                maxSpreadAngle: 10f,
                recoilPerShot: 0.5f,
                spreadRecoveryRate: 10f,
                range: 40f,
                projectileSpeed: 30f,
                projectileLifetime: 2f,
                pelletCount: 1,
                fireMode: WeaponFireMode.Automatic,
                burstCount: 3,
                burstInterval: 0.08f,
                aimTurnSpeed: 180f,
                deliveryMode: WeaponDeliveryMode.Projectile,
                infiniteAmmo: false,
                autoReloadOnEmpty: true,
                cancelReloadOnFire: true);

            ApplyResult result = adapter.TryApply(build, stats);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("MissingGun", result.ErrorCode);
        }

        [Test]
        public void WeaponCombatAdapter_Delegates_TryApply_To_Gun_AndUpdatesRotation()
        {
            var rootGo = new GameObject("PlayerRoot");
            _spawnedObjects.Add(rootGo);
            var playerRotation = rootGo.AddComponent<PlayerRotation>();

            var gunGo = new GameObject("Gun");
            gunGo.transform.SetParent(rootGo.transform);
            _spawnedObjects.Add(gunGo);
            var gun = gunGo.AddComponent<Gun>();

            var adapterGo = new GameObject("Adapter");
            adapterGo.transform.SetParent(gunGo.transform);
            _spawnedObjects.Add(adapterGo);
            var adapter = adapterGo.AddComponent<WeaponCombatAdapter>();
            adapter.Bind(gun, playerRotation);

            Assert.AreEqual(WeaponWorkshopIds.Rifle, adapter.WeaponId);

            var build = new WeaponBuild(WeaponWorkshopIds.Rifle);
            var stats = new ResolvedWeaponStats(
                damage: 35f,
                fireInterval: 0.12f,
                magazineCapacity: 45,
                maxReserveAmmo: 180,
                startingReserveAmmo: 90,
                reloadDuration: 1.7f,
                baseSpreadAngle: 1.5f,
                maxSpreadAngle: 12f,
                recoilPerShot: 0.6f,
                spreadRecoveryRate: 11f,
                range: 55f,
                projectileSpeed: 38f,
                projectileLifetime: 2.8f,
                pelletCount: 1,
                fireMode: WeaponFireMode.Automatic,
                burstCount: 3,
                burstInterval: 0.08f,
                aimTurnSpeed: 135f,
                deliveryMode: WeaponDeliveryMode.Projectile,
                infiniteAmmo: false,
                autoReloadOnEmpty: true,
                cancelReloadOnFire: true);

            ApplyResult result = adapter.TryApply(build, stats);
            Assert.IsTrue(result.IsSuccess);

            Assert.AreSame(build, adapter.CurrentBuild);
            Assert.AreSame(stats, adapter.CurrentStats);
            Assert.AreSame(build, gun.CurrentBuild);
            Assert.AreSame(stats, gun.CurrentStats);
            Assert.AreEqual(35, gun.Damage);
            Assert.AreEqual(135f, playerRotation.RotationSpeed);
        }

        [Test]
        public void WeaponCombatAdapter_SetMuzzleAnchor_UpdatesSpawnPoint()
        {
            var gunGo = new GameObject("Gun");
            _spawnedObjects.Add(gunGo);
            var gun = gunGo.AddComponent<Gun>();

            var muzzleGo = new GameObject("MuzzleAnchor");
            muzzleGo.transform.SetParent(gunGo.transform);
            _spawnedObjects.Add(muzzleGo);

            var adapterGo = new GameObject("Adapter");
            adapterGo.transform.SetParent(gunGo.transform);
            _spawnedObjects.Add(adapterGo);
            var adapter = adapterGo.AddComponent<WeaponCombatAdapter>();
            adapter.Bind(gun);

            adapter.SetMuzzleAnchor(muzzleGo.transform);

            Assert.AreSame(muzzleGo.transform, adapter.MuzzleAnchor);
            Assert.AreSame(muzzleGo.transform, gun.SpawnPoint);
        }
    }
}
