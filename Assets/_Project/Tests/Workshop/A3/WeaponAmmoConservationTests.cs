using Application;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests.Workshop.A3
{
    [TestFixture]
    public class WeaponAmmoConservationTests
    {
        [Test]
        public void AdaptCapacity_LargerMagazine_DoesNotCreateFreeAmmo_PreservesInMagazine()
        {
            // Initial magazine 30, reserve 60, max reserve 120.
            var ammo = new WeaponAmmoState(30, 60, 120, false);
            Assert.AreEqual(30, ammo.InMagazine);
            Assert.AreEqual(60, ammo.ReserveAmmo);

            // Spend 10 rounds -> 20 in magazine.
            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(ammo.TryConsumeRound());
            }
            Assert.AreEqual(20, ammo.InMagazine);
            Assert.AreEqual(60, ammo.ReserveAmmo);

            // Adapt capacity to larger magazine (45), max reserve (165).
            ammo.AdaptCapacity(45, 165);

            // InMagazine is strictly preserved (20), no free rounds granted!
            Assert.AreEqual(20, ammo.InMagazine);
            Assert.AreEqual(60, ammo.ReserveAmmo);
            Assert.AreEqual(45, ammo.MagazineSize);
            Assert.AreEqual(165, ammo.MaxReserveAmmo);

            // Can now reload to fill the new capacity from reserve
            Assert.IsTrue(ammo.CanReload);
            int loaded = ammo.Reload();
            Assert.AreEqual(25, loaded); // 20 + 25 = 45
            Assert.AreEqual(45, ammo.InMagazine);
            Assert.AreEqual(35, ammo.ReserveAmmo); // 60 - 25 = 35
        }

        [Test]
        public void AdaptCapacity_SmallerMagazine_MovesExcessToReserve()
        {
            // Initial magazine 30, reserve 15, max reserve 100.
            var ammo = new WeaponAmmoState(30, 15, 100, false);
            Assert.AreEqual(30, ammo.InMagazine);
            Assert.AreEqual(15, ammo.ReserveAmmo);

            // Adapt capacity to smaller magazine (20).
            // Excess 10 rounds should return to reserve (15 + 10 = 25).
            ammo.AdaptCapacity(20, 100);

            Assert.AreEqual(20, ammo.InMagazine);
            Assert.AreEqual(25, ammo.ReserveAmmo);
            Assert.AreEqual(20, ammo.MagazineSize);

            // Total ammunition count (45) is strictly conserved
            Assert.AreEqual(45, ammo.InMagazine + ammo.ReserveAmmo);
        }

        [Test]
        public void AdaptCapacity_SmallerMagazine_ClampsExcessToMaxReserveAmmo()
        {
            // Initial magazine 30, reserve 95, max reserve 100.
            var ammo = new WeaponAmmoState(30, 95, 100, false);

            // Shrink to 20 rounds, but max reserve remains 100.
            // Excess is 10 rounds -> reserve would be 105, which must clamp to 100.
            ammo.AdaptCapacity(20, 100);

            Assert.AreEqual(20, ammo.InMagazine);
            Assert.AreEqual(100, ammo.ReserveAmmo);
        }

        [Test]
        public void AdaptCapacity_ReserveExceedingNewMax_ClampsToNewMax()
        {
            // Initial magazine 30, reserve 80, max reserve 100.
            var ammo = new WeaponAmmoState(30, 80, 100, false);

            // Player switches to low-profile setup with max reserve of 40.
            ammo.AdaptCapacity(30, 40);

            Assert.AreEqual(30, ammo.InMagazine);
            Assert.AreEqual(40, ammo.ReserveAmmo);
            Assert.AreEqual(40, ammo.MaxReserveAmmo);
        }

        [Test]
        public void AdaptCapacity_ZeroOrNegativeValues_ClampedSafely()
        {
            var ammo = new WeaponAmmoState(10, 20, 50, false);

            ammo.AdaptCapacity(-5, -10);

            // Magazine size clamped >= 1, max reserve clamped >= 0
            Assert.AreEqual(1, ammo.MagazineSize);
            Assert.AreEqual(0, ammo.MaxReserveAmmo);
            Assert.AreEqual(1, ammo.InMagazine);
            Assert.AreEqual(0, ammo.ReserveAmmo);
        }

        [Test]
        public void AdaptCapacity_InfiniteAmmo_MaintainsZeroReserve()
        {
            var ammo = new WeaponAmmoState(10, 0, 0, true);
            Assert.AreEqual(10, ammo.InMagazine);
            Assert.AreEqual(0, ammo.ReserveAmmo);

            ammo.AdaptCapacity(30, 100);

            Assert.AreEqual(10, ammo.InMagazine);
            Assert.AreEqual(0, ammo.ReserveAmmo);
            Assert.AreEqual(30, ammo.MagazineSize);
            Assert.IsFalse(ammo.CanReload);
        }
    }
}
