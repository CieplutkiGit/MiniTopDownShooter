using System.Collections.Generic;
using Application;
using Application.Economy;
using Application.Flow;
using Application.Weapons;
using Application.Workshop;
using Game;
using Game.Economy;
using Game.Workshop;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Economy
{
    [TestFixture]
    public class EconomyUnlockGatesTests
    {
        private UserProfileData _profile;
        private EconomyService _service;
        private IWeaponCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _profile = new UserProfileData
            {
                Coins = 200,
                Xp = 0,
                Level = 1
            };
            _profile.ValidateAndMigrate();

            _service = new EconomyService(() => _profile, p => true);
            // This test targets the economy ownership gate; keep its catalog fixture
            // deterministic so platform/part authoring state cannot turn PartUnowned
            // into a resolver-level InvalidDraft failure.
            _catalog = new FakeCatalog();
        }

        [Test]
        public void LevelGate_PreventsPurchasingHigherLevelWeapon()
        {
            // SMG requires Level 2. Current is Level 1. Player has 200 coins (SMG costs 250 anyway, but level check fails).
            Assert.IsFalse(_service.CanUnlockWeapon(WeaponWorkshopIds.SMG, out string reason));
            Assert.IsTrue(reason.Contains("Level"));

            var result = _service.TryUnlockWeapon(WeaponWorkshopIds.SMG);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("RequirementsNotMet", result.ErrorCode);
        }

        [Test]
        public void WeaponLoadout_LockedWeapon_CannotBeEquipped()
        {
            GameObject loadoutGo = new GameObject("LoadoutTest");
            WeaponLoadout loadout = loadoutGo.AddComponent<WeaponLoadout>();
            loadout.EconomyPolicy = _service;

            // Create two guns: Pistol (starter/unlocked) and Rifle (locked)
            GameObject pistolGo = new GameObject("Pistol");
            pistolGo.transform.SetParent(loadoutGo.transform);
            Gun pistol = pistolGo.AddComponent<Gun>();
            SetWeaponId(pistol, WeaponWorkshopIds.Pistol);

            GameObject rifleGo = new GameObject("Rifle");
            rifleGo.transform.SetParent(loadoutGo.transform);
            Gun rifle = rifleGo.AddComponent<Gun>();
            SetWeaponId(rifle, WeaponWorkshopIds.Rifle);

            loadout.AddWeapon(pistol, false);
            loadout.AddWeapon(rifle, false);

            // Starter weapon equips successfully
            Assert.IsTrue(loadout.EquipSlot(0));
            Assert.AreEqual(pistol, loadout.ActiveGun);

            // Locked weapon equip returns false
            Assert.IsFalse(loadout.EquipSlot(1));
            Assert.AreEqual(pistol, loadout.ActiveGun, "Active gun must remain pistol when attempting to equip locked rifle");

            Object.DestroyImmediate(loadoutGo);
        }

        [Test]
        public void WorkshopSession_Apply_FailsWhenWeaponIsLocked()
        {
            var resolver = new WeaponBuildResolver();
            var store = new FakeBuildStore();
            _catalog.TryGetPlatform(WeaponWorkshopIds.Rifle, out var platform);
            var build = platform.CreateDefaultBuild();

            var session = new WorkshopSession(
                WeaponWorkshopIds.Rifle,
                build,
                _catalog,
                resolver,
                store,
                economyPolicy: _service);

            ApplyResult result = session.Apply();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("WeaponLocked", result.ErrorCode);
        }

        [Test]
        public void WorkshopSession_Apply_FailsWhenDraftContainsUnownedPart()
        {
            // Unlock Rifle platform
            _profile.Coins = 500;
            _profile.Level = 2;
            _service.TryUnlockWeapon(WeaponWorkshopIds.Rifle);

            var resolver = new WeaponBuildResolver();
            var store = new FakeBuildStore();

            _catalog.TryGetPlatform(WeaponWorkshopIds.Rifle, out var platform);
            var defaultBuild = platform.CreateDefaultBuild();

            var session = new WorkshopSession(
                WeaponWorkshopIds.Rifle,
                defaultBuild,
                _catalog,
                resolver,
                store,
                economyPolicy: _service);

            // Selecting unowned upgraded part
            session.SelectPart("rifle.slot.barrel", "rifle.barrel.extended");

            ApplyResult result = session.Apply();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("PartUnowned", result.ErrorCode);
        }

        [Test]
        public void SaveManagerWeaponBuildStore_RejectsSavingLockedWeaponBuild()
        {
            var store = new SaveManagerWeaponBuildStore(_service);
            var build = new WeaponBuild(WeaponWorkshopIds.SMG, new Dictionary<string, string>());

            SaveResult result = store.Save(build);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.ErrorMessage.Contains("locked"));
        }

        [Test]
        public void SaveManagerWeaponBuildStore_RejectsSavingUnownedPart()
        {
            _profile.UnlockWeapon(WeaponWorkshopIds.Pistol);
            var store = new SaveManagerWeaponBuildStore(_service);

            var build = new WeaponBuild(WeaponWorkshopIds.Pistol, new Dictionary<string, string>
            {
                { "pistol.slot.barrel", "pistol.barrel.extended" } // Unowned part
            });

            SaveResult result = store.Save(build);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.ErrorMessage.Contains("unowned"));
        }

        [Test]
        public void WeaponLoadout_ApplyDeploymentSnapshot_FailsWhenSnapshotContainsUnownedPart()
        {
            GameObject loadoutGo = new GameObject("LoadoutTest");
            WeaponLoadout loadout = loadoutGo.AddComponent<WeaponLoadout>();
            loadout.EconomyPolicy = _service;

            GameObject pistolGo = new GameObject("Pistol");
            pistolGo.transform.SetParent(loadoutGo.transform);
            Gun pistol = pistolGo.AddComponent<Gun>();
            SetWeaponId(pistol, WeaponWorkshopIds.Pistol);
            loadout.AddWeapon(pistol, false);

            _catalog.TryGetPlatform(WeaponWorkshopIds.Pistol, out var platform);
            var unownedBuild = platform.CreateDefaultBuild().WithSelection("pistol.slot.barrel", "pistol.barrel.extended");

            var committed = new Dictionary<string, WeaponBuild>
            {
                { WeaponWorkshopIds.Pistol, unownedBuild }
            };

            var snapshot = new DeploymentLoadoutSnapshot(
                new List<string> { WeaponWorkshopIds.Pistol },
                WeaponWorkshopIds.Pistol,
                committed);

            bool success = loadout.ApplyDeploymentSnapshot(snapshot, _catalog);

            Assert.IsFalse(success, "ApplyDeploymentSnapshot must reject builds containing unowned parts");

            Object.DestroyImmediate(loadoutGo);
        }

        [Test]
        public void WorkshopSession_WithoutExplicitPolicy_InheritsFromEconomyPolicyProvider()
        {
            try
            {
                EconomyPolicyProvider.SetDefaultPolicyProvider(() => _service);

                var resolver = new WeaponBuildResolver();
                var store = new FakeBuildStore();
                _catalog.TryGetPlatform(WeaponWorkshopIds.Pistol, out var platform);
                var defaultBuild = platform.CreateDefaultBuild();

                var session = new WorkshopSession(
                    WeaponWorkshopIds.Pistol,
                    defaultBuild,
                    _catalog,
                    resolver,
                    store,
                    economyPolicy: null);

                Assert.IsNotNull(session.EconomyPolicy);
                session.SelectPart("pistol.slot.barrel", "pistol.barrel.extended");
                ApplyResult result = session.Apply();

                Assert.IsFalse(result.IsSuccess);
                Assert.AreEqual("PartUnowned", result.ErrorCode);
            }
            finally
            {
                EconomyPolicyProvider.ResetForTesting();
            }
        }

        private static void SetWeaponId(Gun gun, string weaponId)
        {
            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.WeaponId = weaponId;
            var field = typeof(Gun).GetField("_definition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(gun, def);
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
