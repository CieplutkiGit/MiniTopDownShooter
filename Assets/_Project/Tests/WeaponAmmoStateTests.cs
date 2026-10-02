using Cieplutki.MiniTopDownShooter.Application;
using NUnit.Framework;

namespace Cieplutki.MiniTopDownShooter.Tests
{
    public class WeaponAmmoStateTests
    {
        [Test]
        public void ConsumeRound_DecrementsMagazine()
        {
            WeaponAmmoState ammo = new WeaponAmmoState(6, 12, 24, false);

            Assert.IsTrue(ammo.TryConsumeRound());
            Assert.AreEqual(5, ammo.InMagazine);
            Assert.AreEqual(12, ammo.ReserveAmmo);
        }

        [Test]
        public void Reload_FillsMagazineFromReserve()
        {
            WeaponAmmoState ammo = new WeaponAmmoState(6, 4, 24, false);

            ammo.TryConsumeRound();
            ammo.TryConsumeRound();
            ammo.TryConsumeRound();

            int loaded = ammo.Reload();

            Assert.AreEqual(3, loaded);
            Assert.AreEqual(6, ammo.InMagazine);
            Assert.AreEqual(1, ammo.ReserveAmmo);
        }

        [Test]
        public void AddReserve_ClampsAtMaximum()
        {
            WeaponAmmoState ammo = new WeaponAmmoState(6, 20, 24, false);

            int added = ammo.AddReserve(10);

            Assert.AreEqual(4, added);
            Assert.AreEqual(24, ammo.ReserveAmmo);
        }

        [Test]
        public void InfiniteAmmo_NeverConsumesOrReloads()
        {
            WeaponAmmoState ammo = new WeaponAmmoState(1, 0, 0, true);

            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(ammo.TryConsumeRound());
            }

            Assert.AreEqual(1, ammo.InMagazine);
            Assert.IsFalse(ammo.CanReload);
            Assert.AreEqual(0, ammo.Reload());
        }
    }
}
