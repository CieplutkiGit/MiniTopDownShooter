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
    public class GunCombatRuntimeTests
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

        private Gun CreateTestGun(WeaponDefinition definition = null)
        {
            var rootGo = new GameObject("PlayerRoot");
            _spawnedObjects.Add(rootGo);

            var rotation = rootGo.AddComponent<PlayerRotation>();

            var gunGo = new GameObject("Gun");
            gunGo.transform.SetParent(rootGo.transform);
            _spawnedObjects.Add(gunGo);

            var spawnPointGo = new GameObject("SpawnPoint");
            spawnPointGo.transform.SetParent(gunGo.transform);
            _spawnedObjects.Add(spawnPointGo);

            var gun = gunGo.AddComponent<Gun>();
            gun.SetSpawnPoint(spawnPointGo.transform);

            return gun;
        }

        private ResolvedWeaponStats CreateTestStats(
            float damage = 25f,
            float fireInterval = 0.15f,
            int magCapacity = 30,
            int maxReserve = 120,
            int startingReserve = 90,
            float reloadDuration = 1.8f,
            float baseSpread = 1.0f,
            float maxSpread = 10.0f,
            float recoilPerShot = 0.4f,
            float spreadRecovery = 12f,
            float range = 45f,
            float projSpeed = 35f,
            float projLifetime = 2.5f,
            int pelletCount = 1,
            WeaponFireMode fireMode = WeaponFireMode.Automatic,
            int burstCount = 3,
            float burstInterval = 0.08f,
            float aimTurnSpeed = 160f,
            WeaponDeliveryMode deliveryMode = WeaponDeliveryMode.Projectile,
            bool infiniteAmmo = false,
            bool autoReload = true,
            bool cancelReloadOnFire = true,
            float falloffStart = 0f,
            float falloffEnd = 0f,
            float minDamageRatio = 1f)
        {
            return new ResolvedWeaponStats(
                damage,
                fireInterval,
                magCapacity,
                maxReserve,
                startingReserve,
                reloadDuration,
                baseSpread,
                maxSpread,
                recoilPerShot,
                spreadRecovery,
                range,
                projSpeed,
                projLifetime,
                pelletCount,
                fireMode,
                burstCount,
                burstInterval,
                aimTurnSpeed,
                deliveryMode,
                infiniteAmmo,
                autoReload,
                cancelReloadOnFire,
                falloffStart,
                falloffEnd,
                minDamageRatio);
        }

        [Test]
        public void Gun_Implements_IWeaponBuildTarget()
        {
            Gun gun = CreateTestGun();
            Assert.IsInstanceOf<IWeaponBuildTarget>(gun);
        }

        [Test]
        public void Gun_WeaponId_ReturnsDefinitionOrFallback()
        {
            Gun gun = CreateTestGun();
            Assert.AreEqual(WeaponWorkshopIds.Rifle, gun.WeaponId);
        }

        [Test]
        public void TryApply_RejectsNullBuild()
        {
            Gun gun = CreateTestGun();
            ResolvedWeaponStats stats = CreateTestStats();

            ApplyResult result = gun.TryApply(null, stats);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("NullBuild", result.ErrorCode);
        }

        [Test]
        public void TryApply_RejectsNullStats()
        {
            Gun gun = CreateTestGun();
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle);

            ApplyResult result = gun.TryApply(build, null);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("NullStats", result.ErrorCode);
        }

        [Test]
        public void TryApply_RejectsMismatchedWeaponId()
        {
            Gun gun = CreateTestGun(); // Rifle by default
            var pistolBuild = new WeaponBuild(WeaponWorkshopIds.Pistol);
            ResolvedWeaponStats stats = CreateTestStats();

            ApplyResult result = gun.TryApply(pistolBuild, stats);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("WeaponMismatch", result.ErrorCode);
        }

        [Test]
        public void TryApply_Success_UpdatesCurrentBuildAndStats()
        {
            Gun gun = CreateTestGun();
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long" }
            });
            ResolvedWeaponStats stats = CreateTestStats(damage: 32f, aimTurnSpeed: 140f);

            ApplyResult result = gun.TryApply(build, stats);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreSame(build, gun.CurrentBuild);
            Assert.AreSame(stats, gun.CurrentStats);
            Assert.AreEqual(32, gun.Damage);
        }

        [Test]
        public void TryApply_DoesNotCallResetRuntimeState_PreservesAmmo()
        {
            Gun gun = CreateTestGun();
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle);
            ResolvedWeaponStats initialStats = CreateTestStats(magCapacity: 30, maxReserve: 120, startingReserve: 60);

            gun.TryApply(build, initialStats);
            Assert.AreEqual(30, gun.AmmoInMagazine);
            Assert.AreEqual(60, gun.ReserveAmmo);

            // Spend 8 rounds
            for (int i = 0; i < 8; i++)
            {
                gun.Runtime.TryFire(i * 0.2f);
            }
            Assert.AreEqual(22, gun.AmmoInMagazine);
            Assert.AreEqual(60, gun.ReserveAmmo);

            // Apply new extended stats (45 mag capacity)
            ResolvedWeaponStats extendedStats = CreateTestStats(magCapacity: 45, maxReserve: 150, startingReserve: 90);
            ApplyResult applyResult = gun.TryApply(build, extendedStats);
            Assert.IsTrue(applyResult.IsSuccess);

            // Ammo in magazine MUST be preserved at 22 rounds (no free ammo from ResetRuntimeState!)
            Assert.AreEqual(22, gun.AmmoInMagazine);
            Assert.AreEqual(60, gun.ReserveAmmo);
            Assert.AreEqual(45, gun.MagazineSize);
        }

        [Test]
        public void TryApply_CancelsActiveBurstAndReload()
        {
            Gun gun = CreateTestGun();
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle);
            ResolvedWeaponStats burstStats = CreateTestStats(fireMode: WeaponFireMode.Burst, reloadDuration: 2.0f);

            gun.TryApply(build, burstStats);
            gun.Runtime.StartBurst(1.0f);
            Assert.AreEqual(3, gun.Runtime.BurstShotsRemaining);

            gun.Runtime.TryFire(1.0f);
            gun.Runtime.StartReload(1.0f);
            Assert.IsTrue(gun.IsReloading);

            // Apply new stats mid-burst and mid-reload
            ResolvedWeaponStats autoStats = CreateTestStats(fireMode: WeaponFireMode.Automatic);
            gun.TryApply(build, autoStats);

            Assert.AreEqual(0, gun.Runtime.BurstShotsRemaining);
            Assert.IsFalse(gun.IsReloading);
        }

        [Test]
        public void TryApply_UpdatesDeliveryParameters_Projectile()
        {
            Gun gun = CreateTestGun();
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle);
            ResolvedWeaponStats stats = CreateTestStats(
                damage: 42f,
                pelletCount: 3,
                projSpeed: 55f,
                projLifetime: 4.2f);

            ApplyResult result = gun.TryApply(build, stats);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(42, gun.Damage);

            var projectileDelivery = gun.Delivery as ProjectileWeaponDelivery;
            Assert.IsNotNull(projectileDelivery);
            Assert.AreEqual(3, projectileDelivery.ProjectilesPerShot);
            Assert.AreEqual(55f, projectileDelivery.SpeedOverride);
            Assert.AreEqual(4.2f, projectileDelivery.LifetimeOverride);
        }

        [Test]
        public void TryApply_SwitchesDelivery_Hitscan()
        {
            Gun gun = CreateTestGun();
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle);
            ResolvedWeaponStats hitscanStats = CreateTestStats(
                damage: 50f,
                range: 75f,
                pelletCount: 2,
                deliveryMode: WeaponDeliveryMode.Hitscan,
                falloffStart: 20f,
                falloffEnd: 60f,
                minDamageRatio: 0.4f);

            ApplyResult result = gun.TryApply(build, hitscanStats);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(50, gun.Damage);
            Assert.AreEqual(Application.WeaponDeliveryMode.Hitscan, gun.DeliveryMode);

            var hitscanDelivery = gun.Delivery as HitscanWeaponDelivery;
            Assert.IsNotNull(hitscanDelivery);
            Assert.AreEqual(75f, hitscanDelivery.Range);
            Assert.AreEqual(2, hitscanDelivery.ProjectilesPerShot);
            Assert.IsNotNull(hitscanDelivery.FalloffEvaluator);

            // Test falloff curve evaluation
            Assert.AreEqual(1.0f, hitscanDelivery.FalloffEvaluator(10f), 0.001f);
            Assert.AreEqual(1.0f, hitscanDelivery.FalloffEvaluator(20f), 0.001f);
            Assert.AreEqual(0.7f, hitscanDelivery.FalloffEvaluator(40f), 0.001f);
            Assert.AreEqual(0.4f, hitscanDelivery.FalloffEvaluator(60f), 0.001f);
            Assert.AreEqual(0.4f, hitscanDelivery.FalloffEvaluator(80f), 0.001f);
        }

        [Test]
        public void TryApply_UpdatesPlayerRotationTurnSpeed()
        {
            Gun gun = CreateTestGun();
            var rotation = gun.GetComponentInParent<PlayerRotation>();
            Assert.AreEqual(720f, rotation.RotationSpeed);

            var build = new WeaponBuild(WeaponWorkshopIds.Rifle);
            ResolvedWeaponStats stats = CreateTestStats(aimTurnSpeed: 145f);

            gun.TryApply(build, stats);

            Assert.AreEqual(145f, rotation.RotationSpeed);
        }

        [Test]
        public void LegacyFallback_WithoutDefinitionOrCustomStats_RemainsFullyFunctional()
        {
            Gun gun = CreateTestGun();

            Assert.IsNull(gun.Definition);
            Assert.IsNull(gun.CurrentBuild);
            Assert.IsNull(gun.CurrentStats);
            Assert.AreEqual(10, gun.Damage);
            Assert.IsTrue(gun.InfiniteAmmo);
            Assert.AreEqual(Application.WeaponDeliveryMode.Projectile, gun.DeliveryMode);
            Assert.AreEqual(1, gun.AmmoInMagazine);

            // Can shoot without exceptions
            gun.Shoot(Vector3.forward);
            Assert.AreEqual(1, gun.AmmoInMagazine);
        }
    }
}
