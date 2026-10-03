using System;
using Application;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests.Workshop.A3
{
    [TestFixture]
    public class WeaponRuntimeApplyConfigTests
    {
        [Test]
        public void ApplyConfig_ThrowsOnNull()
        {
            var runtime = new WeaponRuntime(new WeaponRuntimeConfig());
            Assert.Throws<ArgumentNullException>(() => runtime.ApplyConfig(null));
        }

        [Test]
        public void ApplyConfig_UpdatesAllConfigParameters()
        {
            var initialConfig = new WeaponRuntimeConfig
            {
                FireMode = WeaponFireMode.Automatic,
                FireInterval = 0.2f,
                BurstCount = 3,
                BurstInterval = 0.08f,
                BaseSpreadAngle = 1f,
                MaxSpreadAngle = 15f,
                SpreadPerShot = 0.5f,
                SpreadRecoveryPerSecond = 10f,
                ReloadDuration = 1.5f,
                MagazineSize = 20,
                MaxReserveAmmo = 100,
                StartingReserveAmmo = 60,
                InfiniteAmmo = false
            };

            var runtime = new WeaponRuntime(initialConfig);
            Assert.AreEqual(20, runtime.Ammo.InMagazine);
            Assert.AreEqual(60, runtime.Ammo.ReserveAmmo);

            var newConfig = new WeaponRuntimeConfig
            {
                FireMode = WeaponFireMode.Burst,
                FireInterval = 0.35f,
                BurstCount = 4,
                BurstInterval = 0.06f,
                BaseSpreadAngle = 0.5f,
                MaxSpreadAngle = 8f,
                SpreadPerShot = 0.3f,
                SpreadRecoveryPerSecond = 14f,
                ReloadDuration = 2.0f,
                MagazineSize = 30,
                MaxReserveAmmo = 150,
                StartingReserveAmmo = 90,
                InfiniteAmmo = false
            };

            runtime.ApplyConfig(newConfig);

            Assert.AreSame(newConfig, runtime.Config);
            Assert.AreEqual(WeaponFireMode.Burst, runtime.Config.FireMode);
            Assert.AreEqual(0.35f, runtime.Config.FireInterval);
            Assert.AreEqual(4, runtime.Config.BurstCount);
            Assert.AreEqual(0.06f, runtime.Config.BurstInterval);
            Assert.AreEqual(0.5f, runtime.Config.BaseSpreadAngle);
            Assert.AreEqual(8f, runtime.Config.MaxSpreadAngle);
            Assert.AreEqual(0.3f, runtime.Config.SpreadPerShot);
            Assert.AreEqual(14f, runtime.Config.SpreadRecoveryPerSecond);
            Assert.AreEqual(2.0f, runtime.Config.ReloadDuration);
            Assert.AreEqual(30, runtime.Config.MagazineSize);
            Assert.AreEqual(150, runtime.Config.MaxReserveAmmo);
        }

        [Test]
        public void ApplyConfig_CancelsActiveBurst()
        {
            var config = new WeaponRuntimeConfig
            {
                FireMode = WeaponFireMode.Burst,
                FireInterval = 0.5f,
                BurstCount = 3,
                BurstInterval = 0.1f,
                MagazineSize = 10,
                InfiniteAmmo = true
            };

            var runtime = new WeaponRuntime(config);
            Assert.IsTrue(runtime.StartBurst(1.0f));
            Assert.AreEqual(3, runtime.BurstShotsRemaining);

            // Apply new config mid-burst
            var newConfig = new WeaponRuntimeConfig
            {
                FireMode = WeaponFireMode.Burst,
                FireInterval = 0.4f,
                BurstCount = 5,
                BurstInterval = 0.08f,
                MagazineSize = 15,
                InfiniteAmmo = true
            };

            runtime.ApplyConfig(newConfig);

            // Active burst must be canceled
            Assert.AreEqual(0, runtime.BurstShotsRemaining);
        }

        [Test]
        public void ApplyConfig_CancelsActiveReload_AndFiresReloadCanceled()
        {
            var config = new WeaponRuntimeConfig
            {
                MagazineSize = 10,
                StartingReserveAmmo = 20,
                MaxReserveAmmo = 50,
                ReloadDuration = 2.0f,
                InfiniteAmmo = false
            };

            var runtime = new WeaponRuntime(config);
            runtime.TryFire(0f);
            Assert.IsTrue(runtime.StartReload(1.0f));
            Assert.IsTrue(runtime.IsReloading);

            bool reloadCanceledFired = false;
            runtime.ReloadCanceled += () => reloadCanceledFired = true;

            var newConfig = new WeaponRuntimeConfig
            {
                MagazineSize = 15,
                StartingReserveAmmo = 30,
                MaxReserveAmmo = 60,
                ReloadDuration = 1.0f,
                InfiniteAmmo = false
            };

            runtime.ApplyConfig(newConfig);

            Assert.IsFalse(runtime.IsReloading);
            Assert.IsTrue(reloadCanceledFired);
        }

        [Test]
        public void ApplyConfig_FiresAmmoChanged_AndConservesAmmo()
        {
            var config = new WeaponRuntimeConfig
            {
                MagazineSize = 30,
                StartingReserveAmmo = 60,
                MaxReserveAmmo = 120,
                InfiniteAmmo = false
            };

            var runtime = new WeaponRuntime(config);

            // Spend 10 rounds
            for (int i = 0; i < 10; i++)
            {
                runtime.TryFire(i * 0.5f);
            }
            Assert.AreEqual(20, runtime.Ammo.InMagazine);
            Assert.AreEqual(60, runtime.Ammo.ReserveAmmo);

            int observedMag = -1;
            int observedReserve = -1;
            runtime.AmmoChanged += (mag, res) =>
            {
                observedMag = mag;
                observedReserve = res;
            };

            // Switch to magazine size 15 (shrinking magazine -> excess 5 goes to reserve)
            var newConfig = new WeaponRuntimeConfig
            {
                MagazineSize = 15,
                MaxReserveAmmo = 120,
                InfiniteAmmo = false
            };

            runtime.ApplyConfig(newConfig);

            Assert.AreEqual(15, runtime.Ammo.InMagazine);
            Assert.AreEqual(65, runtime.Ammo.ReserveAmmo);
            Assert.AreEqual(15, observedMag);
            Assert.AreEqual(65, observedReserve);
        }

        [Test]
        public void ApplyConfig_ClampsDynamicSpreadToNewMaxSpread()
        {
            var config = new WeaponRuntimeConfig
            {
                BaseSpreadAngle = 0f,
                MaxSpreadAngle = 30f,
                SpreadPerShot = 10f,
                InfiniteAmmo = true
            };

            var runtime = new WeaponRuntime(config);
            runtime.TryFire(0f);
            runtime.TryFire(1f);
            // Dynamic spread is now 20f
            Assert.AreEqual(20f, runtime.DynamicSpread);

            // Apply new config with a tighter max spread of 10f
            var newConfig = new WeaponRuntimeConfig
            {
                BaseSpreadAngle = 0f,
                MaxSpreadAngle = 10f,
                SpreadPerShot = 2f,
                InfiniteAmmo = true
            };

            runtime.ApplyConfig(newConfig);

            Assert.LessOrEqual(runtime.DynamicSpread, 10f);
            Assert.LessOrEqual(runtime.CurrentSpreadAngle, 10f);
        }
    }
}
