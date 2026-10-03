using System;
using System.Collections.Generic;
using System.Linq;
using Application;
using Application.Weapons;
using Application.Workshop;
using Game;
using Game.Workshop.Presentation;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiniTopDownShooter.Tests.Workshop.B5
{
    [TestFixture]
    public class LauncherWorkshopContentTests
    {
        private WeaponPlatformDefinition _platform;
        private List<WeaponPartDefinition> _parts;
        private WeaponVisualProfile _profile;
        private FakeLauncherCatalog _catalog;
        private WeaponBuildResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _platform = LauncherWorkshopFixtures.CreateLauncherPlatformDefinition();
            _parts = LauncherWorkshopFixtures.CreateLauncherPartDefinitions();
            _profile = LauncherWorkshopFixtures.CreateLauncherVisualProfile();
            _catalog = new FakeLauncherCatalog(_platform.ToSpec(), _parts);
            _resolver = new WeaponBuildResolver();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_platform != null) UnityEngine.Object.DestroyImmediate(_platform);
                if (_parts != null)
                {
                    foreach (var p in _parts)
                    {
                        if (p != null) UnityEngine.Object.DestroyImmediate(p);
                    }
                }
                if (_profile != null) UnityEngine.Object.DestroyImmediate(_profile);
            }
            catch
            {
                // Ignored when running outside Unity native engine runtime
            }
        }

        [Test]
        public void Platform_HasFourSlots_AndCorrectDefaults_AndMatchesBaseStats()
        {
            // Identity
            Assert.AreEqual(WeaponWorkshopIds.Launcher, _platform.WeaponId);
            Assert.AreEqual("Rotary Grenade Launcher", _platform.DisplayName);

            // Supported slots (4 slots)
            Assert.AreEqual(4, _platform.SupportedSlots.Count);
            CollectionAssert.AreEquivalent(
                WeaponWorkshopIds.LauncherSlots.All,
                _platform.SupportedSlots);

            // Default parts
            Assert.AreEqual(4, _platform.DefaultParts.Count);
            var defaultDict = _platform.DefaultParts.ToDictionary(p => p.SlotId, p => p.PartId);
            Assert.AreEqual("launcher.tube.standard", defaultDict[WeaponWorkshopIds.LauncherSlots.Tube]);
            Assert.AreEqual("launcher.drum.standard", defaultDict[WeaponWorkshopIds.LauncherSlots.Drum]);
            Assert.AreEqual("launcher.handles.standard", defaultDict[WeaponWorkshopIds.LauncherSlots.Handles]);
            Assert.AreEqual("launcher.action.standard", defaultDict[WeaponWorkshopIds.LauncherSlots.Action]);

            // Base numerical stats matching Weapon_Launcher.asset
            Assert.AreEqual(60f, _platform.BaseDamage);
            Assert.AreEqual(0.85f, _platform.BaseFireInterval);
            Assert.AreEqual(4, _platform.BaseMagazineCapacity);
            Assert.AreEqual(12, _platform.BaseStartingReserveAmmo);
            Assert.AreEqual(24, _platform.BaseMaxReserveAmmo);
            Assert.AreEqual(2.1f, _platform.BaseReloadDuration);
            Assert.AreEqual(0.5f, _platform.BaseSpreadAngle);
            Assert.AreEqual(2.0f, _platform.MaxSpreadAngle);
            Assert.AreEqual(0f, _platform.SpreadPerShot);
            Assert.AreEqual(6.0f, _platform.SpreadRecoveryPerSecond);
            Assert.AreEqual(50f, _platform.Range);
            Assert.AreEqual(14f, _platform.ProjectileSpeed);
            Assert.AreEqual(4.0f, _platform.ProjectileLifetime);
            Assert.AreEqual(1, _platform.BasePellets);
            Assert.AreEqual(Application.WeaponFireMode.SemiAutomatic, _platform.BaseFireMode);
            Assert.AreEqual(Application.WeaponDeliveryMode.Projectile, _platform.DeliveryMode);
        }

        [Test]
        public void TwelveParts_DefinedWithMeasurableDeltas_AndValidSlots()
        {
            // Exactly 12 parts
            Assert.AreEqual(12, _parts.Count);

            // Exactly 3 parts per slot
            var tubeParts = _parts.Where(p => p.SlotId == WeaponWorkshopIds.LauncherSlots.Tube).ToList();
            var drumParts = _parts.Where(p => p.SlotId == WeaponWorkshopIds.LauncherSlots.Drum).ToList();
            var handlesParts = _parts.Where(p => p.SlotId == WeaponWorkshopIds.LauncherSlots.Handles).ToList();
            var actionParts = _parts.Where(p => p.SlotId == WeaponWorkshopIds.LauncherSlots.Action).ToList();

            Assert.AreEqual(3, tubeParts.Count, "Tube slot should have exactly 3 parts");
            Assert.AreEqual(3, drumParts.Count, "Drum slot should have exactly 3 parts");
            Assert.AreEqual(3, handlesParts.Count, "Handles slot should have exactly 3 parts");
            Assert.AreEqual(3, actionParts.Count, "Action slot should have exactly 3 parts");

            // Verify specific part IDs
            CollectionAssert.AreEquivalent(
                new[] { "launcher.tube.standard", "launcher.tube.rifled", "launcher.tube.short" },
                tubeParts.Select(p => p.PartId));

            CollectionAssert.AreEquivalent(
                new[] { "launcher.drum.standard", "launcher.drum.highcap", "launcher.drum.lightweight" },
                drumParts.Select(p => p.PartId));

            CollectionAssert.AreEquivalent(
                new[] { "launcher.handles.standard", "launcher.handles.dual", "launcher.handles.ergonomic" },
                handlesParts.Select(p => p.PartId));

            CollectionAssert.AreEquivalent(
                new[] { "launcher.action.standard", "launcher.action.hairtrigger", "launcher.action.heavy" },
                actionParts.Select(p => p.PartId));

            // Verify naming conventions and descriptions
            foreach (var part in _parts)
            {
                Assert.IsTrue(part.PartId.StartsWith("launcher.", StringComparison.Ordinal));
                Assert.IsFalse(string.IsNullOrEmpty(part.DisplayName), $"Part '{part.PartId}' missing display name");
                Assert.IsFalse(string.IsNullOrEmpty(part.Description), $"Part '{part.PartId}' missing description");
            }

            // Verify specific drum capacities
            var dStd = drumParts.First(p => p.PartId == "launcher.drum.standard");
            var dHigh = drumParts.First(p => p.PartId == "launcher.drum.highcap");
            var dLight = drumParts.First(p => p.PartId == "launcher.drum.lightweight");

            Assert.AreEqual(0, dStd.MagazineCapacityDelta);
            Assert.AreEqual(2, dHigh.MagazineCapacityDelta); // 4 + 2 = 6
            Assert.AreEqual(-1, dLight.MagazineCapacityDelta); // 4 - 1 = 3
        }

        [Test]
        public void DefaultBuildStats_MatchBaseLauncherWeaponStats()
        {
            var platformSpec = _platform.ToSpec();
            var defaultBuild = platformSpec.CreateDefaultBuild();

            var resolution = _resolver.Resolve(defaultBuild, _catalog);
            Assert.IsTrue(resolution.IsValid, "Default build must be valid");

            var stats = resolution.Stats;
            Assert.AreEqual(60f, stats.Damage, 0.001f);
            Assert.AreEqual(0.85f, stats.FireInterval, 0.0001f);
            Assert.AreEqual(4, stats.MagazineCapacity);
            Assert.AreEqual(12, stats.StartingReserveAmmo);
            Assert.AreEqual(24, stats.MaxReserveAmmo);
            Assert.AreEqual(2.1f, stats.ReloadDuration, 0.001f);
            Assert.AreEqual(0.5f, stats.BaseSpreadAngle, 0.001f);
            Assert.AreEqual(2.0f, stats.MaxSpreadAngle, 0.001f);
            Assert.AreEqual(0f, stats.RecoilPerShot, 0.001f);
            Assert.AreEqual(6.0f, stats.SpreadRecoveryRate, 0.001f);
            Assert.AreEqual(50f, stats.Range, 0.001f);
            Assert.AreEqual(14f, stats.ProjectileSpeed, 0.001f);
            Assert.AreEqual(4.0f, stats.ProjectileLifetime, 0.001f);
            Assert.AreEqual(1, stats.PelletCount);
            Assert.AreEqual(180f, stats.AimTurnSpeed, 0.001f);
            Assert.AreEqual(Application.WeaponFireMode.SemiAutomatic, stats.FireMode);
            Assert.AreEqual(Application.WeaponDeliveryMode.Projectile, stats.DeliveryMode);
        }

        [Test]
        public void TradeOffs_ExistAcrossAllVariantParts()
        {
            var platformSpec = _platform.ToSpec();
            var defaultBuild = platformSpec.CreateDefaultBuild();
            var defaultStats = _resolver.Resolve(defaultBuild, _catalog).Stats;

            // 1. Tube: Rifled (Boosts damage, range, speed; penalizes aim turn speed)
            var rifledBuild = defaultBuild.WithSelection(WeaponWorkshopIds.LauncherSlots.Tube, "launcher.tube.rifled");
            var rifledStats = _resolver.Resolve(rifledBuild, _catalog).Stats;
            Assert.Greater(rifledStats.Damage, defaultStats.Damage);
            Assert.Greater(rifledStats.Range, defaultStats.Range);
            Assert.Greater(rifledStats.ProjectileSpeed, defaultStats.ProjectileSpeed);
            Assert.Less(rifledStats.AimTurnSpeed, defaultStats.AimTurnSpeed);

            // 2. Tube: Short (Boosts aim turn speed, faster reload; penalizes range, velocity, spread)
            var shortBuild = defaultBuild.WithSelection(WeaponWorkshopIds.LauncherSlots.Tube, "launcher.tube.short");
            var shortStats = _resolver.Resolve(shortBuild, _catalog).Stats;
            Assert.Greater(shortStats.AimTurnSpeed, defaultStats.AimTurnSpeed);
            Assert.Less(shortStats.ReloadDuration, defaultStats.ReloadDuration);
            Assert.Less(shortStats.Range, defaultStats.Range);
            Assert.Less(shortStats.ProjectileSpeed, defaultStats.ProjectileSpeed);
            Assert.Greater(shortStats.BaseSpreadAngle, defaultStats.BaseSpreadAngle);

            // 3. Drum: HighCap (Capacity 6, reserve +12; penalizes reload duration and aim turn speed)
            var highCapBuild = defaultBuild.WithSelection(WeaponWorkshopIds.LauncherSlots.Drum, "launcher.drum.highcap");
            var highCapStats = _resolver.Resolve(highCapBuild, _catalog).Stats;
            Assert.AreEqual(6, highCapStats.MagazineCapacity);
            Assert.AreEqual(36, highCapStats.MaxReserveAmmo);
            Assert.Greater(highCapStats.ReloadDuration, defaultStats.ReloadDuration);
            Assert.Less(highCapStats.AimTurnSpeed, defaultStats.AimTurnSpeed);

            // 4. Drum: Lightweight (Capacity 3, faster reload, agile aim; lower reserve)
            var lightBuild = defaultBuild.WithSelection(WeaponWorkshopIds.LauncherSlots.Drum, "launcher.drum.lightweight");
            var lightStats = _resolver.Resolve(lightBuild, _catalog).Stats;
            Assert.AreEqual(3, lightStats.MagazineCapacity);
            Assert.Less(lightStats.ReloadDuration, defaultStats.ReloadDuration);
            Assert.Greater(lightStats.AimTurnSpeed, defaultStats.AimTurnSpeed);
            Assert.Less(lightStats.MaxReserveAmmo, defaultStats.MaxReserveAmmo);

            // 5. Handles: Dual (Tighter spread, better recovery; slower aim swing)
            var dualBuild = defaultBuild.WithSelection(WeaponWorkshopIds.LauncherSlots.Handles, "launcher.handles.dual");
            var dualStats = _resolver.Resolve(dualBuild, _catalog).Stats;
            Assert.Less(dualStats.MaxSpreadAngle, defaultStats.MaxSpreadAngle);
            Assert.Greater(dualStats.SpreadRecoveryRate, defaultStats.SpreadRecoveryRate);
            Assert.Less(dualStats.AimTurnSpeed, defaultStats.AimTurnSpeed);

            // 6. Handles: Ergonomic (Faster aim turn speed, faster reload, better recovery)
            var ergoBuild = defaultBuild.WithSelection(WeaponWorkshopIds.LauncherSlots.Handles, "launcher.handles.ergonomic");
            var ergoStats = _resolver.Resolve(ergoBuild, _catalog).Stats;
            Assert.Greater(ergoStats.AimTurnSpeed, defaultStats.AimTurnSpeed);
            Assert.Less(ergoStats.ReloadDuration, defaultStats.ReloadDuration);
            Assert.Greater(ergoStats.SpreadRecoveryRate, defaultStats.SpreadRecoveryRate);

            // 7. Action: HairTrigger (Faster fire interval; adds spread per shot and larger max spread)
            var hairBuild = defaultBuild.WithSelection(WeaponWorkshopIds.LauncherSlots.Action, "launcher.action.hairtrigger");
            var hairStats = _resolver.Resolve(hairBuild, _catalog).Stats;
            Assert.Less(hairStats.FireInterval, defaultStats.FireInterval);
            Assert.Greater(hairStats.RecoilPerShot, defaultStats.RecoilPerShot);
            Assert.Greater(hairStats.MaxSpreadAngle, defaultStats.MaxSpreadAngle);

            // 8. Action: Heavy (Higher damage and projectile speed; slower fire interval)
            var heavyBuild = defaultBuild.WithSelection(WeaponWorkshopIds.LauncherSlots.Action, "launcher.action.heavy");
            var heavyStats = _resolver.Resolve(heavyBuild, _catalog).Stats;
            Assert.Greater(heavyStats.Damage, defaultStats.Damage);
            Assert.Greater(heavyStats.ProjectileSpeed, defaultStats.ProjectileSpeed);
            Assert.Greater(heavyStats.FireInterval, defaultStats.FireInterval);
        }

        [Test]
        public void VisualProfile_ContainsAllTwelveParts_AndAccurateMuzzleOffsets()
        {
            Assert.AreEqual(WeaponWorkshopIds.Launcher, _profile.WeaponId);
            Assert.AreEqual(new Vector3(0f, 0.06f, 0.44f), _profile.DefaultMuzzleOffset);
            Assert.AreEqual(12, _profile.Parts.Count);

            // Check that every single one of the 12 parts has a valid visual entry
            foreach (var part in _parts)
            {
                bool found = _profile.TryGetPartPose(part.SlotId, part.PartId, out var data);
                Assert.IsTrue(found, $"VisualProfile must contain part entry for '{part.PartId}' in slot '{part.SlotId}'");
                Assert.IsNotNull(data);
                Assert.AreNotEqual(Vector3.zero, data.AssembledLocalScale, $"Part '{part.PartId}' has zero scale");
                Assert.AreNotEqual(Vector3.zero, data.ExplodedLocalOffset, $"Part '{part.PartId}' has zero exploded offset");
            }

            // Verify muzzle offsets driven by tube variants
            Vector3 stdMuzzle = _profile.GetMuzzleOffset("launcher.tube.standard");
            Vector3 rifledMuzzle = _profile.GetMuzzleOffset("launcher.tube.rifled");
            Vector3 shortMuzzle = _profile.GetMuzzleOffset("launcher.tube.short");

            Assert.AreEqual(new Vector3(0f, 0.06f, 0.44f), stdMuzzle);
            Assert.AreEqual(new Vector3(0f, 0.06f, 0.58f), rifledMuzzle);
            Assert.AreEqual(new Vector3(0f, 0.06f, 0.32f), shortMuzzle);

            // Rifled extends further forward than standard; short sits behind standard
            Assert.Greater(rifledMuzzle.z, stdMuzzle.z);
            Assert.Less(shortMuzzle.z, stdMuzzle.z);

            // Fallback for null or unknown part returns default muzzle offset
            Assert.AreEqual(_profile.DefaultMuzzleOffset, _profile.GetMuzzleOffset(null));
            Assert.AreEqual(_profile.DefaultMuzzleOffset, _profile.GetMuzzleOffset("unknown.part"));
        }

        [Test]
        public void CatalogValidation_ReportsZeroErrors()
        {
            var report = WeaponCatalogValidation.Validate(
                new[] { _platform },
                _parts);

            Assert.IsTrue(report.IsValid, $"Catalog validation failed: {report}");
            Assert.AreEqual(0, report.Errors.Count);
        }

        [Test]
        public void WorkshopSession_DraftApplyDiscardWorkflow_WithLauncher()
        {
            var defaultBuild = _platform.ToSpec().CreateDefaultBuild();
            var store = new FakeWeaponBuildStore();
            var target = new FakeWeaponBuildTarget { WeaponId = WeaponWorkshopIds.Launcher };

            var session = new WorkshopSession(
                WeaponWorkshopIds.Launcher,
                defaultBuild,
                _catalog,
                _resolver,
                store,
                target);

            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.IsTrue(session.IsValid);
            Assert.AreEqual(4, session.DraftStats.MagazineCapacity);
            Assert.AreEqual(60f, session.DraftStats.Damage);

            // Select HighCap Drum and Rifled Tube
            session.SelectPart(WeaponWorkshopIds.LauncherSlots.Drum, "launcher.drum.highcap");
            session.SelectPart(WeaponWorkshopIds.LauncherSlots.Tube, "launcher.tube.rifled");
            Assert.IsTrue(session.HasUnappliedChanges);
            Assert.AreEqual(6, session.DraftStats.MagazineCapacity);
            Assert.AreEqual(70f, session.DraftStats.Damage);

            // Apply changes
            var applyResult = session.Apply();
            Assert.IsTrue(applyResult.IsSuccess);
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.AreEqual(session.CommittedBuild, target.CurrentBuild);
            Assert.AreEqual(6, target.CurrentStats.MagazineCapacity);
            Assert.AreEqual(70f, target.CurrentStats.Damage);

            // Simulate shooting with WeaponRuntime
            var runtimeConfig = new WeaponRuntimeConfig
            {
                MagazineSize = target.CurrentStats.MagazineCapacity,
                MaxReserveAmmo = target.CurrentStats.MaxReserveAmmo,
                FireInterval = target.CurrentStats.FireInterval,
                SpreadPerShot = target.CurrentStats.RecoilPerShot,
                BaseSpreadAngle = target.CurrentStats.BaseSpreadAngle,
                MaxSpreadAngle = target.CurrentStats.MaxSpreadAngle,
                InfiniteAmmo = false
            };
            var runtime = new WeaponRuntime(runtimeConfig);
            Assert.AreEqual(6, runtime.Ammo.InMagazine);
            bool fired = runtime.TryFire(1.0f);
            Assert.IsTrue(fired);
            Assert.AreEqual(5, runtime.Ammo.InMagazine);

            // Discard test: select lightweight drum and discard
            session.SelectPart(WeaponWorkshopIds.LauncherSlots.Drum, "launcher.drum.lightweight");
            Assert.AreEqual(3, session.DraftStats.MagazineCapacity);
            Assert.IsTrue(session.HasUnappliedChanges);

            session.Discard();
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.AreEqual("launcher.drum.highcap", session.DraftBuild.GetPart(WeaponWorkshopIds.LauncherSlots.Drum));
            Assert.AreEqual(6, session.DraftStats.MagazineCapacity);
        }

#if UNITY_EDITOR
        [Test]
        public void OnDiskAssets_LoadAndValidate_WhenInEditor()
        {
            const string basePath = "Assets/_Project/Data/WeaponCustomization/Launcher";

            // Load Platform
            var platformAsset = AssetDatabase.LoadAssetAtPath<WeaponPlatformDefinition>($"{basePath}/Platform_Launcher.asset");
            Assert.IsNotNull(platformAsset, "Platform_Launcher.asset should be loadable via AssetDatabase");
            Assert.AreEqual(WeaponWorkshopIds.Launcher, platformAsset.WeaponId);
            Assert.AreEqual(4, platformAsset.SupportedSlots.Count);
            Assert.AreEqual(4, platformAsset.DefaultParts.Count);

            // Load Visual Profile
            var profileAsset = AssetDatabase.LoadAssetAtPath<WeaponVisualProfile>($"{basePath}/VisualProfile_Launcher.asset");
            Assert.IsNotNull(profileAsset, "VisualProfile_Launcher.asset should be loadable via AssetDatabase");
            Assert.AreEqual(WeaponWorkshopIds.Launcher, profileAsset.WeaponId);
            Assert.AreEqual(12, profileAsset.Parts.Count);

            // Load all 12 part assets
            string[] partFilenames = new[]
            {
                "Part_Launcher_Tube_Standard.asset",
                "Part_Launcher_Tube_Rifled.asset",
                "Part_Launcher_Tube_Short.asset",
                "Part_Launcher_Drum_Standard.asset",
                "Part_Launcher_Drum_HighCap.asset",
                "Part_Launcher_Drum_Lightweight.asset",
                "Part_Launcher_Handles_Standard.asset",
                "Part_Launcher_Handles_Dual.asset",
                "Part_Launcher_Handles_Ergonomic.asset",
                "Part_Launcher_Action_Standard.asset",
                "Part_Launcher_Action_HairTrigger.asset",
                "Part_Launcher_Action_Heavy.asset"
            };

            var loadedParts = new List<WeaponPartDefinition>();
            foreach (var fn in partFilenames)
            {
                var partAsset = AssetDatabase.LoadAssetAtPath<WeaponPartDefinition>($"{basePath}/{fn}");
                Assert.IsNotNull(partAsset, $"{fn} should be loadable via AssetDatabase");
                loadedParts.Add(partAsset);
            }
            Assert.AreEqual(12, loadedParts.Count);

            // Validate the loaded assets together
            var report = WeaponCatalogValidation.Validate(new[] { platformAsset }, loadedParts);
            Assert.IsTrue(report.IsValid, $"Validation of on-disk assets failed: {report}");
        }
#endif
    }
}
