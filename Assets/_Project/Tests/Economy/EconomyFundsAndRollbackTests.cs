using Application;
using Application.Economy;
using Application.Weapons;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests.Economy
{
    [TestFixture]
    public class EconomyFundsAndRollbackTests
    {
        private UserProfileData _profile;
        private bool _simulateSaveFailure;
        private EconomyService _service;

        [SetUp]
        public void SetUp()
        {
            _profile = new UserProfileData
            {
                Coins = 50,
                Xp = 0,
                Level = 1
            };
            _profile.SetComponentCount(EconomyComponentExtensions.ScrapId, 5);
            _profile.SetComponentCount(EconomyComponentExtensions.AlloyId, 0);
            _profile.SetComponentCount(EconomyComponentExtensions.CoreId, 0);

            _simulateSaveFailure = false;
            _service = new EconomyService(
                () => _profile,
                p => !_simulateSaveFailure);
        }

        [Test]
        public void PurchaseWeapon_InsufficientCoins_FailsAndCoinsUnchanged()
        {
            // Rifle costs 100 coins, player has 50
            Assert.AreEqual(50, _service.Coins);
            Assert.IsFalse(_service.CanUnlockWeapon(WeaponWorkshopIds.Rifle, out string reason));
            Assert.IsTrue(reason.Contains("Coins"));

            var result = _service.TryUnlockWeapon(WeaponWorkshopIds.Rifle);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("RequirementsNotMet", result.ErrorCode);
            Assert.AreEqual(50, _service.Coins);
            Assert.IsFalse(_service.IsWeaponUnlocked(WeaponWorkshopIds.Rifle));
        }

        [Test]
        public void CraftPart_InsufficientComponents_FailsAndInventoryUnchanged()
        {
            _profile.UnlockWeapon(WeaponWorkshopIds.Rifle);

            // Extended barrel costs 8 Scrap + 2 Alloy; player has 5 Scrap + 0 Alloy
            Assert.IsFalse(_service.CanCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.extended", out string reason));
            Assert.IsTrue(reason.Contains("Need"));

            var result = _service.TryCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.extended");
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("InsufficientComponents", result.ErrorCode);
            Assert.AreEqual(5, _service.GetComponentCount("scrap"));
            Assert.IsFalse(_service.IsPartUnlocked(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.extended"));
        }

        [Test]
        public void PurchaseWeapon_SaveFailure_RollsBackCoinsAndWeaponRemainsLocked()
        {
            _profile.Coins = 150;
            _simulateSaveFailure = true; // Save will fail

            var result = _service.TryUnlockWeapon(WeaponWorkshopIds.Rifle);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("SaveFailed", result.ErrorCode);
            Assert.AreEqual(150, _service.Coins, "Coins must be restored when save fails");
            Assert.IsFalse(_service.IsWeaponUnlocked(WeaponWorkshopIds.Rifle), "Weapon must remain locked on save failure");
        }

        [Test]
        public void CraftPart_SaveFailure_RollsBackComponentsAndPartRemainsUnowned()
        {
            _profile.UnlockWeapon(WeaponWorkshopIds.Rifle);
            _profile.SetComponentCount("scrap", 20);
            _profile.SetComponentCount("alloy", 5);
            _simulateSaveFailure = true;

            var result = _service.TryCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.extended");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("SaveFailed", result.ErrorCode);
            Assert.AreEqual(20, _service.GetComponentCount("scrap"), "Scrap must be restored on save failure");
            Assert.AreEqual(5, _service.GetComponentCount("alloy"), "Alloy must be restored on save failure");
            Assert.IsFalse(_service.IsPartUnlocked(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.extended"));
        }

        [Test]
        public void SellComponents_SaveFailure_RollsBackInventoryAndCoins()
        {
            _profile.Coins = 50;
            _profile.SetComponentCount("scrap", 10);
            _simulateSaveFailure = true;

            var result = _service.TrySellComponents("scrap", 4);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("SaveFailed", result.ErrorCode);
            Assert.AreEqual(10, _service.GetComponentCount("scrap"), "Scrap must not be consumed on save failure");
            Assert.AreEqual(50, _service.Coins, "Coins must not be granted on save failure");
        }

        [Test]
        public void SellComponents_InsufficientQuantity_Fails()
        {
            _profile.SetComponentCount("core", 0);
            var result = _service.TrySellComponents("core", 1);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("InsufficientComponents", result.ErrorCode);
        }

        [Test]
        public void PurchaseWeapon_SaverThrowsException_RollsBackCoinsAndRemainsLocked()
        {
            _profile.Coins = 150;
            var failingService = new EconomyService(
                () => _profile,
                p => throw new System.IO.IOException("Simulated disk error"));

            var result = failingService.TryUnlockWeapon(WeaponWorkshopIds.Rifle);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("SaveFailed", result.ErrorCode);
            Assert.AreEqual(150, _profile.Coins, "Coins must be restored when saver throws exception");
            Assert.IsFalse(_profile.IsWeaponUnlocked(WeaponWorkshopIds.Rifle));
        }

        [Test]
        public void PurchaseWeapon_UnknownWeaponId_FailsWithoutSpendingCoins()
        {
            _profile.Coins = 500;
            var result = _service.TryUnlockWeapon("nonexistent.weapon.id");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("UnknownWeapon", result.ErrorCode);
            Assert.AreEqual(500, _profile.Coins);
        }

        [Test]
        public void CraftPart_DefaultStarterPart_FailsWithoutSpendingComponents()
        {
            _profile.UnlockWeapon(WeaponWorkshopIds.Pistol);
            _profile.SetComponentCount("scrap", 20);

            var result = _service.TryCraftPart(WeaponWorkshopIds.Pistol, "pistol.slot.barrel", "pistol.barrel.standard");

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("DefaultPart", result.ErrorCode);
            Assert.AreEqual(20, _profile.GetComponentCount("scrap"));
        }

        [Test]
        public void SuccessfulTransactions_MutateStateAndRaiseEvents()
        {
            _profile.Coins = 150;
            bool eventFired = false;
            _service.OnWeaponUnlocked += id => eventFired = (id == WeaponWorkshopIds.Rifle);

            var result = _service.TryUnlockWeapon(WeaponWorkshopIds.Rifle);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(eventFired);
            Assert.AreEqual(50, _service.Coins);
            Assert.IsTrue(_service.IsWeaponUnlocked(WeaponWorkshopIds.Rifle));
        }
    }
}
