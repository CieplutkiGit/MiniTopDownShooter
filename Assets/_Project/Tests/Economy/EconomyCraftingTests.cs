using System.Collections.Generic;
using Application;
using Application.Economy;
using Application.Weapons;
using Application.Workshop;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests.Economy
{
    [TestFixture]
    public class EconomyCraftingTests
    {
        private UserProfileData _profile;
        private EconomyService _service;
        private FakeCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _profile = new UserProfileData
            {
                Coins = 100,
                Xp = 200,
                Level = 2
            };
            _profile.ValidateAndMigrate();
            _profile.SetComponentCount("scrap", 20);
            _profile.SetComponentCount("alloy", 5);
            _profile.SetComponentCount("core", 1);

            _service = new EconomyService(() => _profile, p => true);
            _catalog = new FakeCatalog();
        }

        [Test]
        public void DefaultParts_AreConsideredOwnedByDefault()
        {
            Assert.IsTrue(_service.IsPartUnlocked(WeaponWorkshopIds.Pistol, "pistol.slot.barrel", "pistol.barrel.standard"));
            Assert.IsTrue(_service.IsPartUnlocked(WeaponWorkshopIds.Rifle, "rifle.slot.magazine", "rifle.magazine.standard"));
        }

        [Test]
        public void UpgradePart_InitiallyUnowned_CanBeCraftedWithComponents()
        {
            const string partId = "rifle.barrel.extended";
            _profile.UnlockWeapon(WeaponWorkshopIds.Rifle);

            Assert.IsFalse(_service.IsPartUnlocked(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", partId));
            Assert.IsTrue(_service.CanCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", partId, out _));

            // Craft part
            var result = _service.TryCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", partId);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(_service.IsPartUnlocked(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", partId));

            // Components were deducted: Extended barrel costs 8 Scrap + 2 Alloy
            Assert.AreEqual(12, _service.GetComponentCount("scrap"));
            Assert.AreEqual(3, _service.GetComponentCount("alloy"));
            Assert.AreEqual(1, _service.GetComponentCount("core"));
        }

        [Test]
        public void CraftedPart_CanBeAppliedInWorkshopSession()
        {
            _profile.UnlockWeapon(WeaponWorkshopIds.Rifle);
            _service.TryCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.extended");

            var catalog = Game.Workshop.WeaponBuildApplier.DefaultCatalog ?? _catalog;
            catalog.TryGetPlatform(WeaponWorkshopIds.Rifle, out var platform);
            var build = platform.CreateDefaultBuild().WithSelection("rifle.slot.barrel", "rifle.barrel.extended");

            var resolver = new WeaponBuildResolver();
            var store = new FakeBuildStore();

            var session = new WorkshopSession(
                WeaponWorkshopIds.Rifle,
                build,
                catalog,
                resolver,
                store,
                economyPolicy: _service);

            ApplyResult applyResult = session.Apply();
            Assert.IsTrue(applyResult.IsSuccess, "Crafted and owned part must apply successfully in WorkshopSession");
        }

        [Test]
        public void SellComponents_DeductsComponentsAndGrantsCoins()
        {
            int startCoins = _service.Coins;
            int startScrap = _service.GetComponentCount("scrap");

            // Sell 4 Scrap (5 coins each = 20 coins)
            var result = _service.TrySellComponents("scrap", 4);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(startScrap - 4, _service.GetComponentCount("scrap"));
            Assert.AreEqual(startCoins + 20, _service.Coins);
        }

        [Test]
        public void TryCraftPart_RejectsInvalidPart_UnknownSlot_UnknownPlatform_AndDefaultParts()
        {
            _profile.UnlockWeapon(WeaponWorkshopIds.Rifle);

            // Unknown platform
            var unknownPlatform = _service.TryCraftPart("laser_blaster", "rifle.slot.barrel", "rifle.barrel.extended");
            Assert.IsFalse(unknownPlatform.IsSuccess);
            Assert.AreEqual("UnknownWeapon", unknownPlatform.ErrorCode);

            // Unknown / mismatched slot
            var mismatchedSlot = _service.TryCraftPart(WeaponWorkshopIds.Rifle, "shotgun.slot.barrel", "rifle.barrel.extended");
            Assert.IsFalse(mismatchedSlot.IsSuccess);
            Assert.AreEqual("InvalidPart", mismatchedSlot.ErrorCode);

            // Phantom / unknown part ID
            var phantomPart = _service.TryCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.nonexistent");
            Assert.IsFalse(phantomPart.IsSuccess);
            Assert.AreEqual("InvalidPart", phantomPart.ErrorCode);

            // Default starter part
            var defaultPart = _service.TryCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.standard");
            Assert.IsFalse(defaultPart.IsSuccess);
            Assert.AreEqual("DefaultPart", defaultPart.ErrorCode);

            // CanCraftPart also returns false and gives helpful reason
            Assert.IsFalse(_service.CanCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.nonexistent", out string reason));
            Assert.IsNotEmpty(reason);

            Assert.IsFalse(_service.CanCraftPart(WeaponWorkshopIds.Rifle, "rifle.slot.barrel", "rifle.barrel.standard", out string defaultReason));
            Assert.IsNotEmpty(defaultReason);
        }

        private class FakeBuildStore : IWeaponBuildStore
        {
            public WeaponBuild Saved;
            public BuildLoadResult Load(string id) => Saved != null ? BuildLoadResult.Success(Saved) : BuildLoadResult.Failure("None");
            public SaveResult Save(WeaponBuild build) { Saved = build; return SaveResult.Success(); }
        }

        private class FakeCatalog : IWeaponCatalog
        {
            private readonly Dictionary<string, WeaponPlatformSpec> _platforms = new Dictionary<string, WeaponPlatformSpec>(System.StringComparer.OrdinalIgnoreCase);

            public FakeCatalog()
            {
                var rifleSlots = new[] { "rifle.slot.barrel", "rifle.slot.magazine", "rifle.slot.grip", "rifle.slot.stock" };
                var rifleDefaults = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                {
                    { "rifle.slot.barrel", "rifle.barrel.standard" },
                    { "rifle.slot.magazine", "rifle.magazine.standard" },
                    { "rifle.slot.grip", "rifle.grip.standard" },
                    { "rifle.slot.stock", "rifle.stock.standard" }
                };
                _platforms[WeaponWorkshopIds.Rifle] = new WeaponPlatformSpec(WeaponWorkshopIds.Rifle, "Rifle", rifleSlots, rifleDefaults, 10, 0.2f, 10, 10, 20, 1f, 0, 0, 0, 1, 10, 10, 1, 1, WeaponFireMode.Automatic, 1, 1, 10, WeaponDeliveryMode.Projectile);

                var pistolSlots = new[] { "pistol.slot.barrel", "pistol.slot.magazine", "pistol.slot.grip", "pistol.slot.slide" };
                var pistolDefaults = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
                {
                    { "pistol.slot.barrel", "pistol.barrel.standard" },
                    { "pistol.slot.magazine", "pistol.magazine.standard" },
                    { "pistol.slot.grip", "pistol.grip.standard" },
                    { "pistol.slot.slide", "pistol.slide.standard" }
                };
                _platforms[WeaponWorkshopIds.Pistol] = new WeaponPlatformSpec(WeaponWorkshopIds.Pistol, "Pistol", pistolSlots, pistolDefaults, 10, 0.2f, 10, 10, 20, 1f, 0, 0, 0, 1, 10, 10, 1, 1, WeaponFireMode.SemiAutomatic, 1, 1, 10, WeaponDeliveryMode.Projectile);
            }

            public IReadOnlyList<string> GetAllWeaponIds() => WeaponWorkshopIds.AllWeaponIds;
            public WeaponPartSpec GetPart(string partId)
            {
                string slotName = partId != null && partId.Contains(".barrel.") ? "barrel"
                    : partId != null && partId.Contains(".magazine.") ? "magazine"
                    : partId != null && partId.Contains(".grip.") ? "grip"
                    : partId != null && partId.Contains(".stock.") ? "stock"
                    : partId != null && partId.Contains(".slide.") ? "slide" : "slot";
                string platform = partId != null && partId.StartsWith("pistol.") ? "pistol" : "rifle";
                return new WeaponPartSpec(partId, platform + ".slot." + slotName, partId);
            }
            public IReadOnlyList<WeaponPartSpec> GetPartsForSlot(string weaponId, string slotId) => new List<WeaponPartSpec>();
            public WeaponPlatformSpec GetPlatform(string weaponId)
            {
                if (_platforms.TryGetValue(weaponId, out var spec)) return spec;
                return new WeaponPlatformSpec(weaponId, weaponId, new[] { "slot" }, null, 10, 0.2f, 10, 10, 20, 1f, 0, 0, 0, 1, 10, 10, 1, 1, WeaponFireMode.Automatic, 1, 1, 10, WeaponDeliveryMode.Projectile);
            }
            public bool TryGetPart(string partId, out WeaponPartSpec part) { part = GetPart(partId); return true; }
            public bool TryGetPlatform(string weaponId, out WeaponPlatformSpec platform) { platform = GetPlatform(weaponId); return platform != null; }
        }
    }
}
