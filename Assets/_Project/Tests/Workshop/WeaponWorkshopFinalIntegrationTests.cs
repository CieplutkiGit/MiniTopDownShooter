using System.Collections.Generic;
using Application;
using Application.Flow;
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
        private IWeaponCatalog _previousCatalog;
        private IWeaponBuildResolver _previousResolver;
        private IWeaponBuildStore _previousStore;

        [SetUp]
        public void SetUp()
        {
            _previousCatalog = WeaponBuildApplier.DefaultCatalog;
            _previousResolver = WeaponBuildApplier.DefaultResolver;
            _previousStore = WeaponBuildApplier.DefaultStore;
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
            WeaponBuildApplier.SetCatalog(_previousCatalog);
            WeaponBuildApplier.SetResolver(_previousResolver);
            WeaponBuildApplier.SetStore(_previousStore);
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
        public void WeaponLoadout_DeploymentSnapshotAppliesResolvedBuildAndSurvivesReset()
        {
            var player = new GameObject("Player_Deployment");
            _spawnedObjects.Add(player);
            var loadout = player.AddComponent<WeaponLoadout>();
            Gun rifle = CreateTestGun(WeaponWorkshopIds.Rifle);
            Gun pistol = CreateTestGun(WeaponWorkshopIds.Pistol);
            _catalog.AddPlatform(new WeaponPlatformSpec(
                WeaponWorkshopIds.Pistol, "Pistol", new string[0], new Dictionary<string, string>(),
                10f, 0.2f, 12, 60, 120, 1f, 0f, 0f, 0f, 0f,
                30f, 20f, 2f, 1, WeaponFireMode.SemiAutomatic, 1, 0.08f, 180f,
                WeaponDeliveryMode.Projectile));
            rifle.transform.SetParent(player.transform);
            pistol.transform.SetParent(player.transform);
            loadout.AddWeapon(rifle, equipImmediately: false);
            loadout.AddWeapon(pistol, equipImmediately: false);

            var selections = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.extended" },
                { WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.vertical" },
                { WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.cqb" }
            };
            var committed = new Dictionary<string, WeaponBuild>
            {
                { WeaponWorkshopIds.Rifle, new WeaponBuild(WeaponWorkshopIds.Rifle, selections) }
            };
            var incompleteSnapshot = new DeploymentLoadoutSnapshot(
                new[] { WeaponWorkshopIds.Pistol, WeaponWorkshopIds.Rifle },
                WeaponWorkshopIds.Rifle, committed);
            Assert.IsFalse(loadout.ApplyDeploymentSnapshot(incompleteSnapshot, _catalog));
            Assert.IsTrue(_catalog.TryGetPlatform(WeaponWorkshopIds.Pistol, out var pistolPlatform));
            committed.Add(WeaponWorkshopIds.Pistol, pistolPlatform.CreateDefaultBuild());
            var snapshot = new DeploymentLoadoutSnapshot(
                new[] { WeaponWorkshopIds.Pistol, WeaponWorkshopIds.Rifle },
                WeaponWorkshopIds.Rifle,
                committed);

            Assert.IsTrue(loadout.ApplyDeploymentSnapshot(snapshot, _catalog));
            Assert.AreSame(pistol, loadout.Weapons[0]);
            Assert.AreSame(rifle, loadout.Weapons[1]);
            Assert.AreSame(rifle, loadout.ActiveGun);
            Assert.AreEqual(18f, rifle.CurrentStats.Damage);
            Assert.AreEqual(45, rifle.CurrentStats.MagazineCapacity);

            loadout.ResetToDefault();

            Assert.AreEqual(2, loadout.Count);
            Assert.AreSame(pistol, loadout.Weapons[0]);
            Assert.AreSame(rifle, loadout.Weapons[1]);
            Assert.AreSame(rifle, loadout.ActiveGun);
            Assert.AreEqual(18f, rifle.CurrentStats.Damage);
            Assert.AreEqual(45, rifle.CurrentStats.MagazineCapacity);
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
