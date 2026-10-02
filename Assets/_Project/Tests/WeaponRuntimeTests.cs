using Application;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests
{
    public class WeaponRuntimeTests
    {
        [Test]
        public void TryFire_EnforcesFireIntervalCooldown()
        {
            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                FireInterval = 0.2f,
                InfiniteAmmo = true
            };
            WeaponRuntime weapon = new WeaponRuntime(config);

            // First shot succeeds at t=1.0
            Assert.IsTrue(weapon.TryFire(1.0f));
            Assert.AreEqual(1.0f, weapon.LastShootTime);

            // Shot before 0.2s elapsed fails
            Assert.IsFalse(weapon.TryFire(1.1f));

            // Shot at or after 0.2s elapsed succeeds
            Assert.IsTrue(weapon.TryFire(1.2f));
            Assert.AreEqual(1.2f, weapon.LastShootTime);
        }

        [Test]
        public void BurstFiring_SequencesShotsCorrectly()
        {
            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                FireMode = WeaponFireMode.Burst,
                FireInterval = 0.5f,
                BurstCount = 3,
                BurstInterval = 0.1f,
                InfiniteAmmo = true
            };
            WeaponRuntime weapon = new WeaponRuntime(config);

            Assert.IsTrue(weapon.StartBurst(1.0f));
            Assert.AreEqual(3, weapon.BurstShotsRemaining);

            // Tick burst shot 1
            Assert.IsTrue(weapon.TickBurst(1.0f));
            Assert.AreEqual(2, weapon.BurstShotsRemaining);

            // Shot 2 too early
            Assert.IsFalse(weapon.TickBurst(1.05f));
            Assert.AreEqual(2, weapon.BurstShotsRemaining);

            // Shot 2 at t=1.1
            Assert.IsTrue(weapon.TickBurst(1.1f));
            Assert.AreEqual(1, weapon.BurstShotsRemaining);

            // Shot 3 at t=1.2
            Assert.IsTrue(weapon.TickBurst(1.2f));
            Assert.AreEqual(0, weapon.BurstShotsRemaining);

            // No further burst shots remaining
            Assert.IsFalse(weapon.TickBurst(1.3f));
        }

        [Test]
        public void Reload_CompletesAfterDuration()
        {
            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                MagazineSize = 10,
                StartingReserveAmmo = 20,
                MaxReserveAmmo = 50,
                InfiniteAmmo = false,
                ReloadDuration = 1.0f
            };
            WeaponRuntime weapon = new WeaponRuntime(config);

            // Fire 5 rounds
            for (int i = 0; i < 5; i++)
            {
                weapon.TryFire(i * 0.1f, ignoreFireInterval: true);
            }
            Assert.AreEqual(5, weapon.Ammo.InMagazine);

            bool reloadStarted = false;
            bool reloadCompleted = false;
            weapon.ReloadStarted += () => reloadStarted = true;
            weapon.ReloadCompleted += () => reloadCompleted = true;

            Assert.IsTrue(weapon.StartReload(10.0f));
            Assert.IsTrue(weapon.IsReloading);
            Assert.IsTrue(reloadStarted);

            // Not yet completed at 10.5s
            Assert.IsFalse(weapon.TickReload(10.5f));
            Assert.IsTrue(weapon.IsReloading);
            Assert.IsFalse(reloadCompleted);

            // Completed at 11.0s
            Assert.IsTrue(weapon.TickReload(11.0f));
            Assert.IsFalse(weapon.IsReloading);
            Assert.IsTrue(reloadCompleted);
            Assert.AreEqual(10, weapon.Ammo.InMagazine);
            Assert.AreEqual(15, weapon.Ammo.ReserveAmmo);
        }

        [Test]
        public void CancelReload_AbortsReloadInProgress()
        {
            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                MagazineSize = 6,
                StartingReserveAmmo = 12,
                InfiniteAmmo = false,
                ReloadDuration = 2.0f
            };
            WeaponRuntime weapon = new WeaponRuntime(config);

            weapon.TryFire(0f, ignoreFireInterval: true);
            Assert.AreEqual(5, weapon.Ammo.InMagazine);

            bool canceled = false;
            weapon.ReloadCanceled += () => canceled = true;

            weapon.StartReload(1.0f);
            Assert.IsTrue(weapon.IsReloading);

            weapon.CancelReload();
            Assert.IsFalse(weapon.IsReloading);
            Assert.IsTrue(canceled);

            // Tick after duration should do nothing
            weapon.TickReload(4.0f);
            Assert.AreEqual(5, weapon.Ammo.InMagazine);
        }

        [Test]
        public void Spread_IncreasesOnFireAndRecoversOverTime()
        {
            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                BaseSpreadAngle = 2f,
                MaxSpreadAngle = 10f,
                SpreadPerShot = 3f,
                SpreadRecoveryPerSecond = 5f,
                InfiniteAmmo = true
            };
            WeaponRuntime weapon = new WeaponRuntime(config);

            Assert.AreEqual(2f, weapon.CurrentSpreadAngle);
            Assert.AreEqual(0f, weapon.DynamicSpread);

            // Fire 1 shot -> +3 spread
            weapon.TryFire(0f);
            Assert.AreEqual(3f, weapon.DynamicSpread);
            Assert.AreEqual(5f, weapon.CurrentSpreadAngle);

            // Fire 2nd shot -> +3 spread = 6
            weapon.TryFire(1f);
            Assert.AreEqual(6f, weapon.DynamicSpread);

            // Fire 3rd shot -> +3 would be 9, but max dynamic spread is (10 - 2) = 8
            weapon.TryFire(2f);
            Assert.AreEqual(8f, weapon.DynamicSpread);
            Assert.AreEqual(10f, weapon.CurrentSpreadAngle);

            // Recover spread for 1 second (recovers 5 degrees)
            weapon.RecoverSpread(1.0f);
            Assert.AreEqual(3f, weapon.DynamicSpread);

            // Recover spread for another 1 second (clamps to 0)
            weapon.RecoverSpread(1.0f);
            Assert.AreEqual(0f, weapon.DynamicSpread);
            Assert.AreEqual(2f, weapon.CurrentSpreadAngle);
        }

        [Test]
        public void Reset_RestoresInitialAmmoAndState()
        {
            WeaponRuntimeConfig config = new WeaponRuntimeConfig
            {
                MagazineSize = 8,
                StartingReserveAmmo = 24,
                InfiniteAmmo = false
            };
            WeaponRuntime weapon = new WeaponRuntime(config);

            for (int i = 0; i < 4; i++)
            {
                weapon.TryFire(i * 0.1f, ignoreFireInterval: true);
            }
            Assert.AreEqual(4, weapon.Ammo.InMagazine);

            weapon.Reset();

            Assert.AreEqual(8, weapon.Ammo.InMagazine);
            Assert.AreEqual(24, weapon.Ammo.ReserveAmmo);
            Assert.IsFalse(weapon.IsReloading);
            Assert.AreEqual(0f, weapon.DynamicSpread);
        }
    }
}
