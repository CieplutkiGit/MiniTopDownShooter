using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Application;
using Application.Weapons;
using Application.Workshop;
using Game;
using Game.Workshop.Presentation;
using MiniTopDownShooter.Tests.Workshop;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiniTopDownShooter.Tests.Workshop.B4
{
    [TestFixture]
    public class ShotgunWorkshopContentTests
    {
        private const string PlatformAssetPath = "Assets/_Project/Data/WeaponCustomization/Shotgun/Platform_Shotgun.asset";
        private const string VisualProfileAssetPath = "Assets/_Project/Data/WeaponCustomization/Shotgun/VisualProfile_Shotgun.asset";
        private const string WeaponShotgunAssetPath = "Assets/_Project/Data/Weapons/Weapon_Shotgun.asset";
        private const string ShotgunPrefabPath = "Assets/_Project/Weapons/Gun_Shotgun.prefab";
        private const string CustomizationDir = "Assets/_Project/Data/WeaponCustomization/Shotgun";

        private static readonly string[] ExpectedPartIds = new[]
        {
            "shotgun.barrel.standard",
            "shotgun.barrel.choke",
            "shotgun.barrel.sawedoff",
            "shotgun.feed.standard",
            "shotgun.feed.extendedtube",
            "shotgun.feed.magfed",
            "shotgun.stock.standard",
            "shotgun.stock.tactical",
            "shotgun.stock.nostock",
            "shotgun.action.standard",
            "shotgun.action.heavy",
            "shotgun.action.hairtrigger"
        };

        private static readonly string[] ExpectedSlotIds = new[]
        {
            WeaponWorkshopIds.ShotgunSlots.Barrel,
            WeaponWorkshopIds.ShotgunSlots.Feed,
            WeaponWorkshopIds.ShotgunSlots.Stock,
            WeaponWorkshopIds.ShotgunSlots.Action
        };

        private WeaponPlatformDefinition _platformDef;
        private List<WeaponPartDefinition> _partDefs;
        private WeaponVisualProfile _visualProfile;
        private WeaponBuildResolver _resolver;
        private FakeWeaponCatalog _catalog;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _resolver = new WeaponBuildResolver();
            _platformDef = LoadOrParsePlatform(PlatformAssetPath);
            _partDefs = LoadOrParseAllParts(CustomizationDir);
            _visualProfile = LoadOrParseVisualProfile(VisualProfileAssetPath);

            _catalog = new FakeWeaponCatalog();
            _catalog.AddPlatform(_platformDef.ToSpec());
            foreach (var part in _partDefs)
            {
                _catalog.AddPart(part.ToSpec());
            }
        }

        #region 1. Platform and Slots Verification

        [Test]
        public void Platform_FileExists_AndHasMetaFile()
        {
            Assert.IsTrue(File.Exists(PlatformAssetPath), $"Platform asset not found at {PlatformAssetPath}");
            Assert.IsTrue(File.Exists(PlatformAssetPath + ".meta"), $"Meta file missing for {PlatformAssetPath}");

            string[] metaLines = File.ReadAllLines(PlatformAssetPath + ".meta")
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
            Assert.AreEqual(2, metaLines.Length, "Meta file must be exactly 2 lines.");
            Assert.IsTrue(metaLines[0].StartsWith("fileFormatVersion:"), "Meta line 1 must be fileFormatVersion.");
            Assert.IsTrue(metaLines[1].StartsWith("guid:"), "Meta line 2 must be guid.");
        }

        [Test]
        public void Platform_HasShotgunWeaponId_AndPumpShotgunDisplayName()
        {
            Assert.IsNotNull(_platformDef, "Platform definition could not be loaded.");
            Assert.AreEqual(WeaponWorkshopIds.Shotgun, _platformDef.WeaponId, "Platform WeaponId must match weapon.shotgun");
            Assert.IsFalse(string.IsNullOrWhiteSpace(_platformDef.DisplayName), "Platform DisplayName must not be empty.");
        }

        [Test]
        public void Platform_SupportsAllFourExpectedShotgunSlots()
        {
            Assert.IsNotNull(_platformDef.SupportedSlots, "SupportedSlots list is null.");
            Assert.AreEqual(4, _platformDef.SupportedSlots.Count, "Shotgun platform must support exactly 4 functional slots.");

            foreach (string slot in ExpectedSlotIds)
            {
                Assert.IsTrue(_platformDef.SupportedSlots.Contains(slot), $"Missing supported slot: {slot}");
            }
        }

        [Test]
        public void Platform_DefaultParts_MatchStandardAttachmentsForFourSlots()
        {
            Assert.IsNotNull(_platformDef.DefaultParts, "DefaultParts list is null.");
            Assert.AreEqual(4, _platformDef.DefaultParts.Count, "Shotgun platform must have 4 default part mappings.");

            var defaultPartMap = _platformDef.DefaultParts.ToDictionary(p => p.SlotId, p => p.PartId, StringComparer.Ordinal);

            Assert.AreEqual("shotgun.barrel.standard", defaultPartMap[WeaponWorkshopIds.ShotgunSlots.Barrel]);
            Assert.AreEqual("shotgun.feed.standard", defaultPartMap[WeaponWorkshopIds.ShotgunSlots.Feed]);
            Assert.AreEqual("shotgun.stock.standard", defaultPartMap[WeaponWorkshopIds.ShotgunSlots.Stock]);
            Assert.AreEqual("shotgun.action.standard", defaultPartMap[WeaponWorkshopIds.ShotgunSlots.Action]);
        }

        #endregion

        #region 2. 12 Parts Authoring and Catalog Verification

        [Test]
        public void Parts_AllTwelvePartFilesExist_WithValidTwoLineMetaFiles()
        {
            Assert.AreEqual(12, _partDefs.Count, "Exactly 12 shotgun part definitions must exist in the directory.");

            var discoveredPartIds = _partDefs.Select(p => p.PartId).OrderBy(id => id).ToList();
            var expectedSorted = ExpectedPartIds.OrderBy(id => id).ToList();
            CollectionAssert.AreEqual(expectedSorted, discoveredPartIds);

            var assetFiles = Directory.GetFiles(CustomizationDir, "Part_Shotgun_*.asset");
            Assert.AreEqual(12, assetFiles.Length, "Expected exactly 12 Part_Shotgun_*.asset files on disk.");

            foreach (string file in assetFiles)
            {
                string metaFile = file + ".meta";
                Assert.IsTrue(File.Exists(metaFile), $"Meta file missing for {file}");
                string[] lines = File.ReadAllLines(metaFile).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
                Assert.AreEqual(2, lines.Length, $"Meta file for {file} must have exactly 2 lines.");
            }
        }

        [Test]
        public void Parts_BelongToTheirRespectiveSlots()
        {
            var partsBySlot = _partDefs.GroupBy(p => p.SlotId).ToDictionary(g => g.Key, g => g.ToList());

            Assert.AreEqual(4, partsBySlot.Count, "Parts should span 4 distinct slots.");

            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.ShotgunSlots.Barrel].Count);
            CollectionAssert.AreEquivalent(
                new[] { "shotgun.barrel.standard", "shotgun.barrel.choke", "shotgun.barrel.sawedoff" },
                partsBySlot[WeaponWorkshopIds.ShotgunSlots.Barrel].Select(p => p.PartId));

            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.ShotgunSlots.Feed].Count);
            CollectionAssert.AreEquivalent(
                new[] { "shotgun.feed.standard", "shotgun.feed.extendedtube", "shotgun.feed.magfed" },
                partsBySlot[WeaponWorkshopIds.ShotgunSlots.Feed].Select(p => p.PartId));

            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.ShotgunSlots.Stock].Count);
            CollectionAssert.AreEquivalent(
                new[] { "shotgun.stock.standard", "shotgun.stock.tactical", "shotgun.stock.nostock" },
                partsBySlot[WeaponWorkshopIds.ShotgunSlots.Stock].Select(p => p.PartId));

            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.ShotgunSlots.Action].Count);
            CollectionAssert.AreEquivalent(
                new[] { "shotgun.action.standard", "shotgun.action.heavy", "shotgun.action.hairtrigger" },
                partsBySlot[WeaponWorkshopIds.ShotgunSlots.Action].Select(p => p.PartId));
        }

        [Test]
        public void Parts_AllHaveDisplayNamesAndDescriptions()
        {
            foreach (var part in _partDefs)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(part.DisplayName), $"Part {part.PartId} has empty DisplayName.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(part.Description), $"Part {part.PartId} has empty Description.");
            }
        }

        [Test]
        public void Catalog_PassesComprehensiveValidation_WithZeroErrors()
        {
            var report = WeaponCatalogValidation.Validate(
                new[] { _platformDef },
                _partDefs);

            Assert.IsTrue(report.IsValid, $"Catalog validation failed:\n{report}");
            Assert.AreEqual(0, report.Errors.Count, "Report must contain 0 errors.");
        }

        #endregion

        #region 3. Base Numerical Stats and Default Build Resolution

        [Test]
        public void Platform_BaseStats_MatchWeaponShotgunAsset()
        {
            Assert.AreEqual(8f, _platformDef.BaseDamage, 0.001f);
            Assert.AreEqual(0.7f, _platformDef.BaseFireInterval, 0.0001f);
            Assert.AreEqual(6, _platformDef.BaseMagazineCapacity);
            Assert.AreEqual(30, _platformDef.BaseStartingReserveAmmo);
            Assert.AreEqual(60, _platformDef.BaseMaxReserveAmmo);
            Assert.AreEqual(1.7f, _platformDef.BaseReloadDuration, 0.01f);
            Assert.AreEqual(10f, _platformDef.BaseSpreadAngle, 0.01f);
            Assert.AreEqual(14f, _platformDef.MaxSpreadAngle, 0.01f);
            Assert.AreEqual(1f, _platformDef.SpreadPerShot, 0.01f);
            Assert.AreEqual(9f, _platformDef.SpreadRecoveryPerSecond, 0.01f);
            Assert.AreEqual(24f, _platformDef.Range, 0.1f);
            Assert.AreEqual(20f, _platformDef.ProjectileSpeed, 0.1f);
            Assert.AreEqual(3f, _platformDef.ProjectileLifetime, 0.01f);
            Assert.AreEqual(8, _platformDef.BasePellets);
            Assert.AreEqual(Application.WeaponFireMode.Shotgun, _platformDef.BaseFireMode);
            Assert.AreEqual(Application.WeaponDeliveryMode.Hitscan, _platformDef.DeliveryMode);
            Assert.IsFalse(_platformDef.InfiniteAmmo);
            Assert.IsTrue(_platformDef.AutoReloadOnEmpty);
            Assert.IsTrue(_platformDef.CancelReloadOnFire);
        }

        [Test]
        public void DefaultBuild_ResolvedStats_MatchBaseShotgunExactly()
        {
            var platformSpec = _platformDef.ToSpec();
            WeaponBuild defaultBuild = platformSpec.CreateDefaultBuild();

            var resolution = _resolver.Resolve(defaultBuild, _catalog);

            Assert.IsTrue(resolution.IsValid, $"Default shotgun build failed resolution: {string.Join(", ", resolution.Errors)}");

            ResolvedWeaponStats stats = resolution.Stats;
            Assert.AreEqual(8f, stats.Damage, 0.01f);
            Assert.AreEqual(6, stats.MagazineCapacity);
            Assert.AreEqual(60, stats.MaxReserveAmmo);
            Assert.AreEqual(30, stats.StartingReserveAmmo);
            Assert.AreEqual(0.7f, stats.FireInterval, 0.0001f);
            Assert.AreEqual(1.7f, stats.ReloadDuration, 0.01f);
            Assert.AreEqual(10f, stats.BaseSpreadAngle, 0.01f);
            Assert.AreEqual(14f, stats.MaxSpreadAngle, 0.01f);
            Assert.AreEqual(1f, stats.RecoilPerShot, 0.01f);
            Assert.AreEqual(9f, stats.SpreadRecoveryRate, 0.01f);
            Assert.AreEqual(24f, stats.Range, 0.1f);
            Assert.AreEqual(20f, stats.ProjectileSpeed, 0.1f);
            Assert.AreEqual(3f, stats.ProjectileLifetime, 0.01f);
            Assert.AreEqual(8, stats.PelletCount);
            Assert.AreEqual(180f, stats.AimTurnSpeed, 0.1f);
            Assert.AreEqual(Application.WeaponFireMode.Shotgun, stats.FireMode);
            Assert.AreEqual(Application.WeaponDeliveryMode.Hitscan, stats.DeliveryMode);
        }

        #endregion

        #region 4. Measurable Trade-Offs and Feed Capacities

        [Test]
        public void FeedParts_Capacities_MatchSpecificationExactCounts()
        {
            var standard = _partDefs.First(p => p.PartId == "shotgun.feed.standard");
            var extended = _partDefs.First(p => p.PartId == "shotgun.feed.extendedtube");
            var magfed = _partDefs.First(p => p.PartId == "shotgun.feed.magfed");

            Assert.AreEqual(0, standard.MagazineCapacityDelta);
            Assert.AreEqual(2, extended.MagazineCapacityDelta);
            Assert.AreEqual(4, magfed.MagazineCapacityDelta);

            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var stdStats = _resolver.Resolve(defaultBuild, _catalog).Stats;
            Assert.AreEqual(6, stdStats.MagazineCapacity, "Standard feed capacity must be 6.");

            var extStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Feed, "shotgun.feed.extendedtube"), _catalog).Stats;
            Assert.AreEqual(8, extStats.MagazineCapacity, "Extended tube capacity must be 8.");

            var magStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Feed, "shotgun.feed.magfed"), _catalog).Stats;
            Assert.AreEqual(10, magStats.MagazineCapacity, "Mag-fed conversion capacity must be 10.");
        }

        [Test]
        public void TradeOffs_BarrelSlot_HasDistinctMeasurableDeltas()
        {
            var standard = _partDefs.First(p => p.PartId == "shotgun.barrel.standard");
            var choke = _partDefs.First(p => p.PartId == "shotgun.barrel.choke");
            var sawedoff = _partDefs.First(p => p.PartId == "shotgun.barrel.sawedoff");

            // Standard is neutral
            Assert.AreEqual(0f, standard.DamageDelta);
            Assert.AreEqual(0f, standard.BaseSpreadAngleDelta);

            // Choke: Tighter spread, longer range, damage bonus, but slower aim mobility
            Assert.Less(choke.BaseSpreadAngleDelta, 0f, "Full choke must tighten base spread.");
            Assert.Less(choke.MaxSpreadAngleDelta, 0f, "Full choke must tighten max spread.");
            Assert.Greater(choke.RangeDelta, 0f, "Full choke must increase effective range.");
            Assert.Less(choke.AimTurnSpeedDelta, 0f, "Full choke trade-off: aim speed must be reduced.");

            // Sawed-Off: Wider spread, shorter range, but rapid aim agility and extra pellets
            Assert.Greater(sawedoff.BaseSpreadAngleDelta, 0f, "Sawed-off must widen spread.");
            Assert.Less(sawedoff.RangeDelta, 0f, "Sawed-off must decrease range.");
            Assert.Greater(sawedoff.AimTurnSpeedDelta, 0f, "Sawed-off must increase aim turn speed.");
            Assert.Greater(sawedoff.PelletsDelta, 0, "Sawed-off must add extra pellets.");

            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var chokeStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Barrel, "shotgun.barrel.choke"), _catalog).Stats;
            Assert.AreEqual(7f, chokeStats.BaseSpreadAngle);
            Assert.AreEqual(32f, chokeStats.Range);
            Assert.AreEqual(165f, chokeStats.AimTurnSpeed);

            var sawedStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Barrel, "shotgun.barrel.sawedoff"), _catalog).Stats;
            Assert.AreEqual(15f, sawedStats.BaseSpreadAngle);
            Assert.AreEqual(16f, sawedStats.Range);
            Assert.AreEqual(215f, sawedStats.AimTurnSpeed);
            Assert.AreEqual(10, sawedStats.PelletCount);
        }

        [Test]
        public void TradeOffs_FeedSlot_HasDistinctMeasurableDeltas()
        {
            var extended = _partDefs.First(p => p.PartId == "shotgun.feed.extendedtube");
            var magfed = _partDefs.First(p => p.PartId == "shotgun.feed.magfed");

            // Extended tube: More ammo, but slower reload and heavier
            Assert.Greater(extended.MagazineCapacityDelta, 0);
            Assert.Greater(extended.ReloadDurationDelta, 0f, "Extended tube reload must be slower.");
            Assert.Less(extended.AimTurnSpeedDelta, 0f, "Extended tube aim speed reduced.");

            // Mag-fed: Fast swap reload, maximum capacity, but bulkier recoil and handling penalty
            Assert.Greater(magfed.MagazineCapacityDelta, extended.MagazineCapacityDelta);
            Assert.Less(magfed.ReloadDurationDelta, 0f, "Mag-fed conversion must reload faster than single shell loading.");
            Assert.Less(magfed.AimTurnSpeedDelta, 0f, "Mag-fed adds bulk reducing aim speed.");
            Assert.Greater(magfed.SpreadPerShotDelta, 0f, "Mag-fed increases recoil per shot.");

            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var extStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Feed, "shotgun.feed.extendedtube"), _catalog).Stats;
            Assert.AreEqual(2.1f, extStats.ReloadDuration, 0.01f);
            Assert.AreEqual(72, extStats.MaxReserveAmmo);

            var magStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Feed, "shotgun.feed.magfed"), _catalog).Stats;
            Assert.AreEqual(1.4f, magStats.ReloadDuration, 0.01f);
            Assert.AreEqual(80, magStats.MaxReserveAmmo);
        }

        [Test]
        public void TradeOffs_StockSlot_HasDistinctMeasurableDeltas()
        {
            var tactical = _partDefs.First(p => p.PartId == "shotgun.stock.tactical");
            var nostock = _partDefs.First(p => p.PartId == "shotgun.stock.nostock");

            // Tactical stock: Recoil dampening and faster recovery, slight weight penalty
            Assert.Less(tactical.SpreadPerShotDelta, 0f, "Tactical stock reduces kick per shot.");
            Assert.Greater(tactical.SpreadRecoveryDelta, 0f, "Tactical stock speeds recovery.");
            Assert.Less(tactical.AimTurnSpeedDelta, 0f, "Tactical stock adds slight bulk.");

            // No stock: High agility, but heavier kick and worse recovery
            Assert.Greater(nostock.AimTurnSpeedDelta, 0f, "Pistol grip only improves aim agility.");
            Assert.Greater(nostock.SpreadPerShotDelta, 0f, "No stock increases kick per shot.");
            Assert.Greater(nostock.MaxSpreadAngleDelta, 0f, "No stock increases max spread angle.");

            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var tacStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Stock, "shotgun.stock.tactical"), _catalog).Stats;
            Assert.AreEqual(0.7f, tacStats.RecoilPerShot, 0.01f);
            Assert.AreEqual(13f, tacStats.SpreadRecoveryRate, 0.01f);
            Assert.AreEqual(170f, tacStats.AimTurnSpeed, 0.1f);

            var noStockStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Stock, "shotgun.stock.nostock"), _catalog).Stats;
            Assert.AreEqual(1.6f, noStockStats.RecoilPerShot, 0.01f);
            Assert.AreEqual(17f, noStockStats.MaxSpreadAngle, 0.01f);
            Assert.AreEqual(210f, noStockStats.AimTurnSpeed, 0.1f);
        }

        [Test]
        public void TradeOffs_ActionSlot_HasDistinctMeasurableDeltas()
        {
            var heavy = _partDefs.First(p => p.PartId == "shotgun.action.heavy");
            var hair = _partDefs.First(p => p.PartId == "shotgun.action.hairtrigger");

            // Heavy magnum action: Higher damage per pellet, slower cycling rate and more recoil
            Assert.Greater(heavy.DamageDelta, 0f, "Heavy action increases pellet damage.");
            Assert.Greater(heavy.FireIntervalDelta, 0f, "Heavy action slows pump cycling.");
            Assert.Greater(heavy.SpreadPerShotDelta, 0f, "Heavy magnum loads increase recoil.");

            // Hair-trigger action: Fast cycling rate, slight pellet damage reduction
            Assert.Less(hair.FireIntervalDelta, 0f, "Hair trigger enables rapid pump cycling.");
            Assert.Less(hair.DamageDelta, 0f, "Hair trigger trade-off: slight damage penalty.");

            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var heavyStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Action, "shotgun.action.heavy"), _catalog).Stats;
            Assert.AreEqual(11f, heavyStats.Damage, 0.01f);
            Assert.AreEqual(0.9f, heavyStats.FireInterval, 0.0001f);

            var hairStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.ShotgunSlots.Action, "shotgun.action.hairtrigger"), _catalog).Stats;
            Assert.AreEqual(7f, hairStats.Damage, 0.01f);
            Assert.AreEqual(0.5f, hairStats.FireInterval, 0.0001f);
        }

        #endregion

        #region 5. Visual Profile and Muzzle Poses

        [Test]
        public void VisualProfile_FileExists_AndHasTwoLineMeta()
        {
            Assert.IsTrue(File.Exists(VisualProfileAssetPath), $"Visual profile asset not found at {VisualProfileAssetPath}");
            Assert.IsTrue(File.Exists(VisualProfileAssetPath + ".meta"), $"Meta file missing for {VisualProfileAssetPath}");

            string[] metaLines = File.ReadAllLines(VisualProfileAssetPath + ".meta")
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();
            Assert.AreEqual(2, metaLines.Length);
            Assert.IsTrue(metaLines[0].StartsWith("fileFormatVersion:"));
            Assert.IsTrue(metaLines[1].StartsWith("guid:"));
        }

        [Test]
        public void VisualProfile_HasShotgunWeaponId_AndConfiguredReceiver()
        {
            Assert.IsNotNull(_visualProfile);
            Assert.AreEqual(WeaponWorkshopIds.Shotgun, _visualProfile.WeaponId);
            Assert.AreNotEqual(Vector3.zero, _visualProfile.ReceiverLocalScale);
            Assert.AreNotEqual(Vector3.zero, _visualProfile.DefaultMuzzleOffset);
        }

        [Test]
        public void VisualProfile_ContainsVisualEntries_ForEveryOneOfTwelveParts()
        {
            Assert.IsNotNull(_visualProfile.Parts);
            Assert.AreEqual(12, _visualProfile.Parts.Count, "Visual profile must contain exactly 12 part entries.");

            foreach (var part in _partDefs)
            {
                bool found = _visualProfile.TryGetPartPose(part.SlotId, part.PartId, out var data);
                Assert.IsTrue(found, $"Visual profile missing entry for part: {part.PartId} in slot {part.SlotId}");
                Assert.IsNotNull(data);
                Assert.AreEqual(part.PartId, data.PartId);
                Assert.AreEqual(part.SlotId, data.SlotId);
                Assert.AreNotEqual(Vector3.zero, data.AssembledLocalPosition, $"Assembled position should be non-zero for {part.PartId}");
                Assert.AreNotEqual(Vector3.zero, data.ExplodedLocalOffset, $"Exploded offset should be non-zero for {part.PartId}");
                Assert.AreNotEqual(Vector3.zero, data.AssembledLocalScale, $"Assembled scale should be non-zero for {part.PartId}");
            }
        }

        [Test]
        public void VisualProfile_MuzzleOffsets_AreDistinctAndOrderedByBarrelLength()
        {
            Vector3 sawedOffset = _visualProfile.GetMuzzleOffset("shotgun.barrel.sawedoff");
            Vector3 standardOffset = _visualProfile.GetMuzzleOffset("shotgun.barrel.standard");
            Vector3 chokeOffset = _visualProfile.GetMuzzleOffset("shotgun.barrel.choke");

            Assert.Greater(sawedOffset.z, 0f);
            Assert.Greater(standardOffset.z, sawedOffset.z, "Standard barrel muzzle offset should be farther than sawed-off barrel.");
            Assert.Greater(chokeOffset.z, standardOffset.z, "Full choke barrel muzzle offset should be farther than standard barrel.");

            // Unknown or null part falls back to DefaultMuzzleOffset
            Assert.AreEqual(_visualProfile.DefaultMuzzleOffset, _visualProfile.GetMuzzleOffset("shotgun.barrel.unknown"));
            Assert.AreEqual(_visualProfile.DefaultMuzzleOffset, _visualProfile.GetMuzzleOffset(null));
        }

        #endregion

        #region 6. Shotgun Prefab Verification

        [Test]
        public void ShotgunPrefab_ContainsFeedAssemblyUnderVisual()
        {
            Assert.IsTrue(File.Exists(ShotgunPrefabPath), $"Shotgun prefab missing at {ShotgunPrefabPath}");
            Assert.IsTrue(File.Exists(ShotgunPrefabPath + ".meta"), $"Meta file missing for {ShotgunPrefabPath}");

            string text = File.ReadAllText(ShotgunPrefabPath);
            Assert.IsTrue(text.Contains("m_Name: FeedTube") || text.Contains("m_Name: FeedAssembly"), "Shotgun prefab Visual hierarchy must contain a clear feed assembly / tube.");

            // Verify GUID preservation
            string metaText = File.ReadAllText(ShotgunPrefabPath + ".meta");
            Assert.IsTrue(metaText.Contains("guid: 14e5cdd08e034101a28990f723403cbd"), "Shotgun prefab GUID must be preserved.");
        }

        #endregion

        #region 7. Workshop Session and Apply Integration

        [Test]
        public void WorkshopSession_CustomizingShotgunWithAuthoredContent_AppliesSuccessfully()
        {
            var store = new FakeWeaponBuildStore();
            var target = new FakeWeaponBuildTarget { WeaponId = WeaponWorkshopIds.Shotgun };
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var session = new WorkshopSession(
                WeaponWorkshopIds.Shotgun,
                defaultBuild,
                _catalog,
                _resolver,
                store,
                target);

            Assert.IsTrue(session.IsValid);
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.AreEqual(8f, session.DraftStats.Damage);
            Assert.AreEqual(6, session.DraftStats.MagazineCapacity);

            // Customize build to CQB room-clearing configuration
            session.SelectPart(WeaponWorkshopIds.ShotgunSlots.Barrel, "shotgun.barrel.sawedoff");
            session.SelectPart(WeaponWorkshopIds.ShotgunSlots.Feed, "shotgun.feed.magfed");
            session.SelectPart(WeaponWorkshopIds.ShotgunSlots.Stock, "shotgun.stock.nostock");
            session.SelectPart(WeaponWorkshopIds.ShotgunSlots.Action, "shotgun.action.heavy");

            Assert.IsTrue(session.HasUnappliedChanges);
            Assert.IsTrue(session.IsValid);
            Assert.AreEqual(11f, session.DraftStats.Damage); // 8 + 3
            Assert.AreEqual(10, session.DraftStats.MagazineCapacity); // 6 + 4
            Assert.AreEqual(10, session.DraftStats.PelletCount); // 8 + 2
            Assert.AreEqual(1.4f, session.DraftStats.ReloadDuration, 0.01f); // 1.7 - 0.3

            var applyResult = session.Apply();
            Assert.IsTrue(applyResult.IsSuccess);
            Assert.IsFalse(session.HasUnappliedChanges);

            Assert.AreEqual(1, target.ApplyCallCount);
            Assert.AreEqual(11f, target.CurrentStats.Damage);
            Assert.AreEqual(10, target.CurrentStats.MagazineCapacity);
            Assert.AreEqual("shotgun.barrel.sawedoff", target.CurrentBuild.GetPart(WeaponWorkshopIds.ShotgunSlots.Barrel));
            Assert.AreEqual("shotgun.feed.magfed", target.CurrentBuild.GetPart(WeaponWorkshopIds.ShotgunSlots.Feed));

            // Verify persistence
            Assert.IsTrue(store.Stored.ContainsKey(WeaponWorkshopIds.Shotgun));
            Assert.AreEqual("shotgun.feed.magfed", store.Stored[WeaponWorkshopIds.Shotgun].GetPart(WeaponWorkshopIds.ShotgunSlots.Feed));
        }

        #endregion

        #region Helpers and Fallback Parsers

        private static WeaponPlatformDefinition LoadOrParsePlatform(string path)
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<WeaponPlatformDefinition>(path);
            if (asset != null) return asset;
#endif
            var def = ScriptableObject.CreateInstance<WeaponPlatformDefinition>();
            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) return def;

            string[] lines = File.ReadAllLines(fullPath);
            string curSlot = null;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("_weaponId:")) def.WeaponId = line.Substring("_weaponId:".Length).Trim();
                else if (line.StartsWith("_displayName:")) def.DisplayName = line.Substring("_displayName:".Length).Trim();
                else if (line.StartsWith("- shotgun.slot.")) def.SupportedSlots.Add(line.Substring(2).Trim());
                else if (line.StartsWith("_slotId:")) curSlot = line.Substring("_slotId:".Length).Trim();
                else if (line.StartsWith("_partId:") && curSlot != null)
                {
                    def.DefaultParts.Add(new SlotPartPair(curSlot, line.Substring("_partId:".Length).Trim()));
                    curSlot = null;
                }
                else if (line.StartsWith("_baseDamage:")) def.BaseDamage = ParseFloat(line);
                else if (line.StartsWith("_baseFireInterval:")) def.BaseFireInterval = ParseFloat(line);
                else if (line.StartsWith("_baseMagazineCapacity:")) def.BaseMagazineCapacity = ParseInt(line);
                else if (line.StartsWith("_baseStartingReserveAmmo:")) def.BaseStartingReserveAmmo = ParseInt(line);
                else if (line.StartsWith("_baseMaxReserveAmmo:")) def.BaseMaxReserveAmmo = ParseInt(line);
                else if (line.StartsWith("_baseReloadDuration:")) def.BaseReloadDuration = ParseFloat(line);
                else if (line.StartsWith("_baseSpreadAngle:")) def.BaseSpreadAngle = ParseFloat(line);
                else if (line.StartsWith("_maxSpreadAngle:")) def.MaxSpreadAngle = ParseFloat(line);
                else if (line.StartsWith("_spreadPerShot:")) def.SpreadPerShot = ParseFloat(line);
                else if (line.StartsWith("_spreadRecoveryPerSecond:")) def.SpreadRecoveryPerSecond = ParseFloat(line);
                else if (line.StartsWith("_range:")) def.Range = ParseFloat(line);
                else if (line.StartsWith("_projectileSpeed:")) def.ProjectileSpeed = ParseFloat(line);
                else if (line.StartsWith("_projectileLifetime:")) def.ProjectileLifetime = ParseFloat(line);
                else if (line.StartsWith("_basePellets:")) def.BasePellets = ParseInt(line);
                else if (line.StartsWith("_baseFireMode:")) def.BaseFireMode = (Application.WeaponFireMode)ParseInt(line);
                else if (line.StartsWith("_burstCount:")) def.BurstCount = ParseInt(line);
                else if (line.StartsWith("_burstInterval:")) def.BurstInterval = ParseFloat(line);
                else if (line.StartsWith("_aimTurnSpeed:")) def.AimTurnSpeed = ParseFloat(line);
                else if (line.StartsWith("_deliveryMode:")) def.DeliveryMode = (Application.WeaponDeliveryMode)ParseInt(line);
                else if (line.StartsWith("_infiniteAmmo:")) def.InfiniteAmmo = ParseInt(line) == 1;
                else if (line.StartsWith("_autoReloadOnEmpty:")) def.AutoReloadOnEmpty = ParseInt(line) == 1;
                else if (line.StartsWith("_cancelReloadOnFire:")) def.CancelReloadOnFire = ParseInt(line) == 1;
            }

            return def;
        }

        private static List<WeaponPartDefinition> LoadOrParseAllParts(string dirPath)
        {
            var list = new List<WeaponPartDefinition>();
            string fullDir = Path.GetFullPath(dirPath);
            if (!Directory.Exists(fullDir)) return list;

            var files = Directory.GetFiles(fullDir, "Part_Shotgun_*.asset");
            foreach (var file in files)
            {
                string relPath = file.Replace('\\', '/');
                int idx = relPath.IndexOf("Assets/", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) relPath = relPath.Substring(idx);

#if UNITY_EDITOR
                var asset = AssetDatabase.LoadAssetAtPath<WeaponPartDefinition>(relPath);
                if (asset != null)
                {
                    list.Add(asset);
                    continue;
                }
#endif
                var def = ScriptableObject.CreateInstance<WeaponPartDefinition>();
                string[] lines = File.ReadAllLines(file);
                foreach (string rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (line.StartsWith("_partId:")) def.PartId = line.Substring("_partId:".Length).Trim();
                    else if (line.StartsWith("_slotId:")) def.SlotId = line.Substring("_slotId:".Length).Trim();
                    else if (line.StartsWith("_displayName:")) def.DisplayName = line.Substring("_displayName:".Length).Trim();
                    else if (line.StartsWith("_description:")) def.Description = line.Substring("_description:".Length).Trim();
                    else if (line.StartsWith("_damageDelta:")) def.DamageDelta = ParseFloat(line);
                    else if (line.StartsWith("_fireIntervalDelta:")) def.FireIntervalDelta = ParseFloat(line);
                    else if (line.StartsWith("_magazineCapacityDelta:")) def.MagazineCapacityDelta = ParseInt(line);
                    else if (line.StartsWith("_maxReserveAmmoDelta:")) def.MaxReserveAmmoDelta = ParseInt(line);
                    else if (line.StartsWith("_reloadDurationDelta:")) def.ReloadDurationDelta = ParseFloat(line);
                    else if (line.StartsWith("_baseSpreadAngleDelta:")) def.BaseSpreadAngleDelta = ParseFloat(line);
                    else if (line.StartsWith("_maxSpreadAngleDelta:")) def.MaxSpreadAngleDelta = ParseFloat(line);
                    else if (line.StartsWith("_spreadPerShotDelta:")) def.SpreadPerShotDelta = ParseFloat(line);
                    else if (line.StartsWith("_spreadRecoveryDelta:")) def.SpreadRecoveryDelta = ParseFloat(line);
                    else if (line.StartsWith("_rangeDelta:")) def.RangeDelta = ParseFloat(line);
                    else if (line.StartsWith("_projectileSpeedDelta:")) def.ProjectileSpeedDelta = ParseFloat(line);
                    else if (line.StartsWith("_projectileLifetimeDelta:")) def.ProjectileLifetimeDelta = ParseFloat(line);
                    else if (line.StartsWith("_pelletsDelta:")) def.PelletsDelta = ParseInt(line);
                    else if (line.StartsWith("_aimTurnSpeedDelta:")) def.AimTurnSpeedDelta = ParseFloat(line);
                    else if (line.StartsWith("_damageMultiplier:")) def.DamageMultiplier = ParseFloat(line);
                    else if (line.StartsWith("_fireIntervalMultiplier:")) def.FireIntervalMultiplier = ParseFloat(line);
                    else if (line.StartsWith("_reloadDurationMultiplier:")) def.ReloadDurationMultiplier = ParseFloat(line);
                    else if (line.StartsWith("_spreadMultiplier:")) def.SpreadMultiplier = ParseFloat(line);
                    else if (line.StartsWith("_rangeMultiplier:")) def.RangeMultiplier = ParseFloat(line);
                    else if (line.StartsWith("_projectileSpeedMultiplier:")) def.ProjectileSpeedMultiplier = ParseFloat(line);
                    else if (line.StartsWith("_aimTurnSpeedMultiplier:")) def.AimTurnSpeedMultiplier = ParseFloat(line);
                }
                list.Add(def);
            }

            return list;
        }

        private static WeaponVisualProfile LoadOrParseVisualProfile(string path)
        {
#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<WeaponVisualProfile>(path);
            if (asset != null) return asset;
#endif
            var profile = ScriptableObject.CreateInstance<WeaponVisualProfile>();
            string fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) return profile;

            string[] lines = File.ReadAllLines(fullPath);
            PartVisualData currentPart = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("_weaponId:"))
                {
                    profile.WeaponId = line.Substring("_weaponId:".Length).Trim();
                }
                else if (line.StartsWith("_defaultMuzzleOffset:"))
                {
                    profile.DefaultMuzzleOffset = ParseVector3(line.Substring("_defaultMuzzleOffset:".Length).Trim());
                }
                else if (line.StartsWith("_receiverLocalScale:"))
                {
                    profile.ReceiverLocalScale = ParseVector3(line.Substring("_receiverLocalScale:".Length).Trim());
                }
                else if (line.StartsWith("- _slotId:"))
                {
                    if (currentPart != null) profile.AddOrUpdatePart(currentPart);
                    currentPart = new PartVisualData { SlotId = line.Substring("- _slotId:".Length).Trim() };
                }
                else if (currentPart != null)
                {
                    if (line.StartsWith("_partId:")) currentPart.PartId = line.Substring("_partId:".Length).Trim();
                    else if (line.StartsWith("_assembledLocalPosition:")) currentPart.AssembledLocalPosition = ParseVector3(line.Substring("_assembledLocalPosition:".Length).Trim());
                    else if (line.StartsWith("_assembledLocalRotation:")) currentPart.AssembledLocalRotation = ParseQuaternion(line.Substring("_assembledLocalRotation:".Length).Trim());
                    else if (line.StartsWith("_assembledLocalScale:")) currentPart.AssembledLocalScale = ParseVector3(line.Substring("_assembledLocalScale:".Length).Trim());
                    else if (line.StartsWith("_explodedLocalOffset:")) currentPart.ExplodedLocalOffset = ParseVector3(line.Substring("_explodedLocalOffset:".Length).Trim());
                    else if (line.StartsWith("_muzzleOffset:")) currentPart.MuzzleOffset = ParseVector3(line.Substring("_muzzleOffset:".Length).Trim());
                }
            }

            if (currentPart != null) profile.AddOrUpdatePart(currentPart);
            return profile;
        }

        private static float ParseFloat(string line)
        {
            string val = line.Substring(line.IndexOf(':') + 1).Trim();
            return float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : 0f;
        }

        private static int ParseInt(string line)
        {
            string val = line.Substring(line.IndexOf(':') + 1).Trim();
            return int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : 0;
        }

        private static Vector3 ParseVector3(string yamlDict)
        {
            yamlDict = yamlDict.Trim('{', '}');
            var tokens = yamlDict.Split(',');
            float x = 0, y = 0, z = 0;
            foreach (var tok in tokens)
            {
                var kv = tok.Split(':');
                if (kv.Length != 2) continue;
                string k = kv[0].Trim();
                float v = float.TryParse(kv[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f;
                if (k == "x") x = v;
                else if (k == "y") y = v;
                else if (k == "z") z = v;
            }
            return new Vector3(x, y, z);
        }

        private static Quaternion ParseQuaternion(string yamlDict)
        {
            yamlDict = yamlDict.Trim('{', '}');
            var tokens = yamlDict.Split(',');
            float x = 0, y = 0, z = 0, w = 1;
            foreach (var tok in tokens)
            {
                var kv = tok.Split(':');
                if (kv.Length != 2) continue;
                string k = kv[0].Trim();
                float v = float.TryParse(kv[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) ? parsed : 0f;
                if (k == "x") x = v;
                else if (k == "y") y = v;
                else if (k == "z") z = v;
                else if (k == "w") w = v;
            }
            return new Quaternion(x, y, z, w);
        }

        #endregion
    }
}
