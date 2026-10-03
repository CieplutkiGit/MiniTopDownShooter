using System.Collections.Generic;
using Application;
using Application.Weapons;
using Game;
using Game.Workshop;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop
{
    [TestFixture]
    public class WeaponWorkshopFinalIntegrationTests
    {
        private FakeWeaponCatalog _catalog;
        private WeaponBuildResolver _resolver;
        private FakeWeaponBuildStore _store;
        private List<GameObject> _spawnedObjects;

        [SetUp]
        public void SetUp()
        {
            _catalog = WeaponWorkshopTestFixtures.CreateCatalogWithRifle();
            _resolver = new WeaponBuildResolver();
            _store = new FakeWeaponBuildStore();
            _spawnedObjects = new List<GameObject>();

            WeaponBuildApplier.SetCatalog(_catalog);
            WeaponBuildApplier.SetResolver(_resolver);
            WeaponBuildApplier.SetStore(_store);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawnedObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawnedObjects.Clear();
        }

        private Gun CreateTestGun(string weaponId)
        {
            var go = new GameObject($"Gun_{weaponId}");
            _spawnedObjects.Add(go);
            var spawnPoint = new GameObject("SpawnPoint").transform;
            spawnPoint.SetParent(go.transform);

            var def = ScriptableObject.CreateInstance<WeaponDefinition>();
            def.name = $"Def_{weaponId}";
            // Set private weaponId via reflection
            typeof(WeaponDefinition).GetField("_weaponId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(def, weaponId);

            var gun = go.AddComponent<Gun>();
            gun.SetSpawnPoint(spawnPoint);
            typeof(Gun).GetField("_definition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(gun, def);

            return gun;
        }

        [Test]
        public void WeaponBuildApplier_AppliesSavedBuild_ToLiveGun()
        {
            // Given a custom saved build in the store
            var savedSelections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.extended" },
                { WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.vertical" },
                { WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.cqb" }
            };
            var savedBuild = new WeaponBuild(WeaponWorkshopIds.Rifle, savedSelections);
            _store.Save(savedBuild);

            // When creating a Gun and applying saved build
            Gun gun = CreateTestGun(WeaponWorkshopIds.Rifle);
            bool applied = WeaponBuildApplier.ApplySavedBuild(gun, _catalog, _resolver, _store);

            // Then it successfully applies and stats reflect extended mag and long barrel
            Assert.IsTrue(applied);
            Assert.IsNotNull(gun.CurrentBuild);
            Assert.AreEqual(WeaponWorkshopIds.Rifle, gun.CurrentBuild.WeaponId);
            Assert.AreEqual("rifle.magazine.extended", gun.CurrentBuild.GetPart(WeaponWorkshopIds.RifleSlots.Magazine));
            Assert.AreEqual("rifle.barrel.long", gun.CurrentBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.AreEqual(45, gun.CurrentStats.MagazineCapacity);
            Assert.AreEqual(18f, gun.CurrentStats.Damage);
        }

        [Test]
        public void WeaponBuildApplier_FallsBackToDefaultPlatformBuild_WhenNoSaveExists()
        {
            // Given no saved build in the store
            Gun gun = CreateTestGun(WeaponWorkshopIds.Rifle);

            // When applying saved build
            bool applied = WeaponBuildApplier.ApplySavedBuild(gun, _catalog, _resolver, _store);

            // Then it applies the platform's default build
            Assert.IsTrue(applied);
            Assert.IsNotNull(gun.CurrentBuild);
            Assert.AreEqual("rifle.magazine.standard", gun.CurrentBuild.GetPart(WeaponWorkshopIds.RifleSlots.Magazine));
            Assert.AreEqual(30, gun.CurrentStats.MagazineCapacity);
            Assert.AreEqual(15f, gun.CurrentStats.Damage);
        }

        [Test]
        public void WeaponLoadout_AppliesSavedBuilds_OnStartAndReset()
        {
            // Given a saved build
            var savedSelections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.extended" },
                { WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.vertical" },
                { WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.cqb" }
            };
            _store.Save(new WeaponBuild(WeaponWorkshopIds.Rifle, savedSelections));

            // Setup loadout with gun
            var loadoutGo = new GameObject("Player_Loadout");
            _spawnedObjects.Add(loadoutGo);
            var loadout = loadoutGo.AddComponent<WeaponLoadout>();

            Gun gun = CreateTestGun(WeaponWorkshopIds.Rifle);
            gun.transform.SetParent(loadoutGo.transform);
            loadout.AddWeapon(gun, equipImmediately: true);

            // Reapply saved builds
            loadout.ApplySavedBuildsToAll();

            Assert.AreEqual(45, gun.CurrentStats.MagazineCapacity);
            Assert.AreEqual(18f, gun.CurrentStats.Damage);

            // When resetting to default, saved builds are reapplied
            loadout.ResetToDefault();
            Assert.AreEqual(45, loadout.ActiveGun.CurrentStats.MagazineCapacity);
            Assert.AreEqual(18f, loadout.ActiveGun.CurrentStats.Damage);
        }

        [Test]
        public void WeaponPickup_TriggersSavedBuildApplication()
        {
            // Given a saved custom build
            var savedSelections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.extended" },
                { WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.vertical" },
                { WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.cqb" }
            };
            _store.Save(new WeaponBuild(WeaponWorkshopIds.Rifle, savedSelections));

            // And a loadout on player
            var playerGo = new GameObject("Player");
            _spawnedObjects.Add(playerGo);
            var collider = playerGo.AddComponent<BoxCollider>();
            var loadout = playerGo.AddComponent<WeaponLoadout>();

            // And a pickup with rifle prefab
            var pickupGo = new GameObject("Pickup_Rifle");
            _spawnedObjects.Add(pickupGo);
            var pickup = pickupGo.AddComponent<WeaponPickup>();
            Gun riflePrefab = CreateTestGun(WeaponWorkshopIds.Rifle);
            pickup.WeaponPrefab = riflePrefab;

            // When pickup trigger fires via reflection/direct call
            var triggerMethod = typeof(WeaponPickup).GetMethod("OnTriggerEnter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            triggerMethod?.Invoke(pickup, new object[] { collider });

            // Then weapon is added to loadout with customized stats applied
            Assert.AreEqual(1, loadout.Count);
            Assert.IsNotNull(loadout.ActiveGun);
            Assert.AreEqual(45, loadout.ActiveGun.CurrentStats.MagazineCapacity);
            Assert.AreEqual(18f, loadout.ActiveGun.CurrentStats.Damage);
        }
    }
}
