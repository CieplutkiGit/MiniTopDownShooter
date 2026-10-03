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

namespace MiniTopDownShooter.Tests.Workshop.B2
{
    [TestFixture]
    public class RifleWorkshopContentTests
    {
        private const string PlatformAssetPath = "Assets/_Project/Data/WeaponCustomization/Rifle/Platform_Rifle.asset";
        private const string VisualProfileAssetPath = "Assets/_Project/Data/WeaponCustomization/Rifle/VisualProfile_Rifle.asset";
        private const string WeaponRifleAssetPath = "Assets/_Project/Data/Weapons/Weapon_Rifle.asset";
        private const string CustomizationDir = "Assets/_Project/Data/WeaponCustomization/Rifle";

        private static readonly string[] ExpectedPartIds = new[]
        {
            "rifle.barrel.standard",
            "rifle.barrel.long",
            "rifle.barrel.short",
            "rifle.magazine.standard",
            "rifle.magazine.extended",
            "rifle.magazine.drum",
            "rifle.grip.standard",
            "rifle.grip.angled",
            "rifle.grip.vertical",
            "rifle.stock.standard",
            "rifle.stock.heavy",
            "rifle.stock.light"
        };

        private static readonly string[] ExpectedSlotIds = new[]
        {
            WeaponWorkshopIds.RifleSlots.Barrel,
            WeaponWorkshopIds.RifleSlots.Magazine,
            WeaponWorkshopIds.RifleSlots.Grip,
            WeaponWorkshopIds.RifleSlots.Stock
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
        public void Platform_HasRifleWeaponId_AndAssaultRifleDisplayName()
        {
            Assert.IsNotNull(_platformDef, "Platform definition could not be loaded.");
            Assert.AreEqual(WeaponWorkshopIds.Rifle, _platformDef.WeaponId, "Platform WeaponId must match weapon.rifle");
            Assert.AreEqual("Assault Rifle", _platformDef.DisplayName, "Platform DisplayName must be 'Assault Rifle'");
        }

        [Test]
        public void Platform_SupportsAllFourExpectedRifleSlots()
        {
            Assert.IsNotNull(_platformDef.SupportedSlots, "SupportedSlots list is null.");
            Assert.AreEqual(4, _platformDef.SupportedSlots.Count, "Rifle platform must support exactly 4 functional slots.");

            foreach (string slot in ExpectedSlotIds)
            {
                Assert.IsTrue(_platformDef.SupportedSlots.Contains(slot), $"Missing supported slot: {slot}");
            }
        }

        [Test]
        public void Platform_DefaultParts_MatchStandardAttachmentsForFourSlots()
        {
            Assert.IsNotNull(_platformDef.DefaultParts, "DefaultParts list is null.");
            Assert.AreEqual(4, _platformDef.DefaultParts.Count, "Rifle platform must have 4 default part mappings.");

            var defaultPartMap = _platformDef.DefaultParts.ToDictionary(p => p.SlotId, p => p.PartId, StringComparer.Ordinal);

            Assert.AreEqual("rifle.barrel.standard", defaultPartMap[WeaponWorkshopIds.RifleSlots.Barrel]);
            Assert.AreEqual("rifle.magazine.standard", defaultPartMap[WeaponWorkshopIds.RifleSlots.Magazine]);
            Assert.AreEqual("rifle.grip.standard", defaultPartMap[WeaponWorkshopIds.RifleSlots.Grip]);
            Assert.AreEqual("rifle.stock.standard", defaultPartMap[WeaponWorkshopIds.RifleSlots.Stock]);
        }

        #endregion

        #region 2. 12 Parts Authoring and Catalog Verification

        [Test]
        public void Parts_AllTwelvePartFilesExist_WithValidTwoLineMetaFiles()
        {
            Assert.AreEqual(12, _partDefs.Count, "Exactly 12 rifle part definitions must exist in the directory.");

            var discoveredPartIds = _partDefs.Select(p => p.PartId).OrderBy(id => id).ToList();
            var expectedSorted = ExpectedPartIds.OrderBy(id => id).ToList();
            CollectionAssert.AreEqual(expectedSorted, discoveredPartIds);

            var assetFiles = Directory.GetFiles(CustomizationDir, "Part_Rifle_*.asset");
            Assert.AreEqual(12, assetFiles.Length, "Expected exactly 12 Part_Rifle_*.asset files on disk.");

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

            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.RifleSlots.Barrel].Count);
            CollectionAssert.AreEquivalent(
                new[] { "rifle.barrel.standard", "rifle.barrel.long", "rifle.barrel.short" },
                partsBySlot[WeaponWorkshopIds.RifleSlots.Barrel].Select(p => p.PartId));

            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.RifleSlots.Magazine].Count);
            CollectionAssert.AreEquivalent(
                new[] { "rifle.magazine.standard", "rifle.magazine.extended", "rifle.magazine.drum" },
                partsBySlot[WeaponWorkshopIds.RifleSlots.Magazine].Select(p => p.PartId));

            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.RifleSlots.Grip].Count);
            CollectionAssert.AreEquivalent(
                new[] { "rifle.grip.standard", "rifle.grip.angled", "rifle.grip.vertical" },
                partsBySlot[WeaponWorkshopIds.RifleSlots.Grip].Select(p => p.PartId));

            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.RifleSlots.Stock].Count);
            CollectionAssert.AreEquivalent(
                new[] { "rifle.stock.standard", "rifle.stock.heavy", "rifle.stock.light" },
                partsBySlot[WeaponWorkshopIds.RifleSlots.Stock].Select(p => p.PartId));
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
        public void Platform_BaseStats_MatchWeaponRifleAsset()
        {
            // Verify numerical base stats against Weapon_Rifle.asset specifications
            Assert.AreEqual(15f, _platformDef.BaseDamage, 0.001f);
            Assert.AreEqual(0.11f, _platformDef.BaseFireInterval, 0.0001f);
            Assert.AreEqual(30, _platformDef.BaseMagazineCapacity);
            Assert.AreEqual(120, _platformDef.BaseStartingReserveAmmo);
            Assert.AreEqual(240, _platformDef.BaseMaxReserveAmmo);
            Assert.AreEqual(1.55f, _platformDef.BaseReloadDuration, 0.01f);
            Assert.AreEqual(1.2f, _platformDef.BaseSpreadAngle, 0.01f);
            Assert.AreEqual(7.0f, _platformDef.MaxSpreadAngle, 0.01f);
            Assert.AreEqual(0.55f, _platformDef.SpreadPerShot, 0.01f);
            Assert.AreEqual(10f, _platformDef.SpreadRecoveryPerSecond, 0.01f);
            Assert.AreEqual(50f, _platformDef.Range, 0.1f);
            Assert.AreEqual(28f, _platformDef.ProjectileSpeed, 0.1f);
            Assert.AreEqual(2.5f, _platformDef.ProjectileLifetime, 0.01f);
            Assert.AreEqual(1, _platformDef.BasePellets);
            Assert.AreEqual(Application.WeaponFireMode.Automatic, _platformDef.BaseFireMode);
            Assert.AreEqual(Application.WeaponDeliveryMode.Projectile, _platformDef.DeliveryMode);
            Assert.IsFalse(_platformDef.InfiniteAmmo);
            Assert.IsTrue(_platformDef.AutoReloadOnEmpty);
            Assert.IsTrue(_platformDef.CancelReloadOnFire);
        }

        [Test]
        public void DefaultBuild_ResolvedStats_MatchBaseRifleExactly()
        {
            var platformSpec = _platformDef.ToSpec();
            WeaponBuild defaultBuild = platformSpec.CreateDefaultBuild();

            var resolution = _resolver.Resolve(defaultBuild, _catalog);

            Assert.IsTrue(resolution.IsValid, $"Default rifle build failed resolution: {string.Join(", ", resolution.Errors)}");

            ResolvedWeaponStats stats = resolution.Stats;
            Assert.AreEqual(15f, stats.Damage, 0.01f);
            Assert.AreEqual(30, stats.MagazineCapacity);
            Assert.AreEqual(240, stats.MaxReserveAmmo);
            Assert.AreEqual(120, stats.StartingReserveAmmo);
            Assert.AreEqual(0.11f, stats.FireInterval, 0.0001f);
            Assert.AreEqual(1.55f, stats.ReloadDuration, 0.01f);
            Assert.AreEqual(1.2f, stats.BaseSpreadAngle, 0.01f);
            Assert.AreEqual(7.0f, stats.MaxSpreadAngle, 0.01f);
            Assert.AreEqual(0.55f, stats.RecoilPerShot, 0.01f);
            Assert.AreEqual(10f, stats.SpreadRecoveryRate, 0.01f);
            Assert.AreEqual(50f, stats.Range, 0.1f);
            Assert.AreEqual(28f, stats.ProjectileSpeed, 0.1f);
            Assert.AreEqual(2.5f, stats.ProjectileLifetime, 0.01f);
            Assert.AreEqual(1, stats.PelletCount);
            Assert.AreEqual(180f, stats.AimTurnSpeed, 0.1f);
            Assert.AreEqual(Application.WeaponFireMode.Automatic, stats.FireMode);
            Assert.AreEqual(Application.WeaponDeliveryMode.Projectile, stats.DeliveryMode);
        }

        #endregion

        #region 4. Measurable Trade-Offs Across All Slots

        [Test]
        public void TradeOffs_BarrelSlot_HasDistinctMeasurableDeltas()
        {
            var standard = _partDefs.First(p => p.PartId == "rifle.barrel.standard");
            var longB = _partDefs.First(p => p.PartId == "rifle.barrel.long");
            var shortB = _partDefs.First(p => p.PartId == "rifle.barrel.short");

            // Standard is neutral
            Assert.AreEqual(0f, standard.DamageDelta);
            Assert.AreEqual(0f, standard.AimTurnSpeedDelta);

            // Long barrel trade-off: Higher damage and range, but slower aim mobility
            Assert.Greater(longB.DamageDelta, 0f, "Long barrel must increase damage.");
            Assert.Greater(longB.RangeDelta, 0f, "Long barrel must increase range.");
            Assert.Less(longB.BaseSpreadAngleDelta, 0f, "Long barrel must tighten spread.");
            Assert.Less(longB.AimTurnSpeedDelta, 0f, "Long barrel trade-off: aim turn speed must be reduced.");

            // Short barrel trade-off: Higher mobility, but reduced damage, range, and accuracy
            Assert.Greater(shortB.AimTurnSpeedDelta, 0f, "Short barrel must improve aim turn speed.");
            Assert.Less(shortB.DamageDelta, 0f, "Short barrel trade-off: damage must be reduced.");
            Assert.Less(shortB.RangeDelta, 0f, "Short barrel trade-off: range must be reduced.");
            Assert.Greater(shortB.BaseSpreadAngleDelta, 0f, "Short barrel trade-off: base spread must increase.");

            // Build resolutions reflect trade-offs
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var longBuild = defaultBuild.WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long");
            var longStats = _resolver.Resolve(longBuild, _catalog).Stats;
            Assert.AreEqual(18f, longStats.Damage);
            Assert.AreEqual(160f, longStats.AimTurnSpeed);

            var shortBuild = defaultBuild.WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.short");
            var shortStats = _resolver.Resolve(shortBuild, _catalog).Stats;
            Assert.AreEqual(13f, shortStats.Damage);
            Assert.AreEqual(205f, shortStats.AimTurnSpeed);
        }

        [Test]
        public void TradeOffs_MagazineSlot_HasDistinctMeasurableDeltas()
        {
            var standard = _partDefs.First(p => p.PartId == "rifle.magazine.standard");
            var ext = _partDefs.First(p => p.PartId == "rifle.magazine.extended");
            var drum = _partDefs.First(p => p.PartId == "rifle.magazine.drum");

            // Standard is 30 rounds
            Assert.AreEqual(0, standard.MagazineCapacityDelta);
            Assert.AreEqual(0f, standard.ReloadDurationDelta);

            // Extended: +15 capacity (45 total), slower reload (+0.4s)
            Assert.AreEqual(15, ext.MagazineCapacityDelta);
            Assert.Greater(ext.ReloadDurationDelta, 0f, "Extended mag trade-off: reload must be slower.");

            // Drum: +30 capacity (60 total), significantly slower reload (+0.9s), aim turn speed penalty
            Assert.AreEqual(30, drum.MagazineCapacityDelta);
            Assert.Greater(drum.ReloadDurationDelta, ext.ReloadDurationDelta, "Drum reload penalty must exceed extended.");
            Assert.Less(drum.AimTurnSpeedDelta, 0f, "Drum trade-off: heavier weight slows aim speed.");

            // Build resolutions reflect trade-offs
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var extStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.extended"), _catalog).Stats;
            Assert.AreEqual(45, extStats.MagazineCapacity);
            Assert.AreEqual(1.95f, extStats.ReloadDuration, 0.01f);

            var drumStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.drum"), _catalog).Stats;
            Assert.AreEqual(60, drumStats.MagazineCapacity);
            Assert.AreEqual(2.45f, drumStats.ReloadDuration, 0.01f);
            Assert.AreEqual(165f, drumStats.AimTurnSpeed, 0.1f);
        }

        [Test]
        public void TradeOffs_GripSlot_HasDistinctMeasurableDeltas()
        {
            var vertical = _partDefs.First(p => p.PartId == "rifle.grip.vertical");
            var angled = _partDefs.First(p => p.PartId == "rifle.grip.angled");

            // Vertical grip trade-off: Recoil suppression, but handling penalty
            Assert.Less(vertical.SpreadPerShotDelta, 0f, "Vertical grip must reduce recoil per shot.");
            Assert.Less(vertical.MaxSpreadAngleDelta, 0f, "Vertical grip must reduce max spread.");
            Assert.Less(vertical.AimTurnSpeedDelta, 0f, "Vertical grip trade-off: aim turn speed reduced.");

            // Angled grip trade-off: Faster recovery and handling, but slight resting spread trade-off
            Assert.Greater(angled.SpreadRecoveryDelta, 0f, "Angled grip must improve spread recovery.");
            Assert.Greater(angled.AimTurnSpeedDelta, 0f, "Angled grip must improve aim turn speed.");
            Assert.Greater(angled.BaseSpreadAngleDelta, 0f, "Angled grip trade-off: slight increase in resting spread.");

            // Build resolutions reflect trade-offs
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var vertStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.vertical"), _catalog).Stats;
            Assert.AreEqual(0.35f, vertStats.RecoilPerShot, 0.01f);
            Assert.AreEqual(5.5f, vertStats.MaxSpreadAngle, 0.01f);
            Assert.AreEqual(170f, vertStats.AimTurnSpeed, 0.1f);

            var angledStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.angled"), _catalog).Stats;
            Assert.AreEqual(13f, angledStats.SpreadRecoveryRate, 0.01f);
            Assert.AreEqual(190f, angledStats.AimTurnSpeed, 0.1f);
            Assert.AreEqual(1.3f, angledStats.BaseSpreadAngle, 0.01f);
        }

        [Test]
        public void TradeOffs_StockSlot_HasDistinctMeasurableDeltas()
        {
            var heavy = _partDefs.First(p => p.PartId == "rifle.stock.heavy");
            var light = _partDefs.First(p => p.PartId == "rifle.stock.light");

            // Heavy stock trade-off: Stability and lower spread, but sluggish handling
            Assert.Less(heavy.BaseSpreadAngleDelta, 0f, "Heavy stock must tighten base spread.");
            Assert.Less(heavy.MaxSpreadAngleDelta, 0f, "Heavy stock must tighten max spread.");
            Assert.Less(heavy.AimTurnSpeedDelta, 0f, "Heavy stock trade-off: aim turn speed reduced.");

            // Skeleton stock trade-off: Rapid handling, but looser base spread
            Assert.Greater(light.AimTurnSpeedDelta, 0f, "Skeleton stock must improve aim speed.");
            Assert.Greater(light.BaseSpreadAngleDelta, 0f, "Skeleton stock trade-off: base spread increased.");

            // Build resolutions reflect trade-offs
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var heavyStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.heavy"), _catalog).Stats;
            Assert.AreEqual(0.9f, heavyStats.BaseSpreadAngle, 0.01f);
            Assert.AreEqual(5.0f, heavyStats.MaxSpreadAngle, 0.01f);
            Assert.AreEqual(155f, heavyStats.AimTurnSpeed, 0.1f);

            var lightStats = _resolver.Resolve(defaultBuild.WithSelection(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.light"), _catalog).Stats;
            Assert.AreEqual(1.6f, lightStats.BaseSpreadAngle, 0.01f);
            Assert.AreEqual(210f, lightStats.AimTurnSpeed, 0.1f);
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
        public void VisualProfile_HasRifleWeaponId_AndConfiguredReceiver()
        {
            Assert.IsNotNull(_visualProfile);
            Assert.AreEqual(WeaponWorkshopIds.Rifle, _visualProfile.WeaponId);
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
            Vector3 shortOffset = _visualProfile.GetMuzzleOffset("rifle.barrel.short");
            Vector3 standardOffset = _visualProfile.GetMuzzleOffset("rifle.barrel.standard");
            Vector3 longOffset = _visualProfile.GetMuzzleOffset("rifle.barrel.long");

            Assert.Greater(shortOffset.z, 0f);
            Assert.Greater(standardOffset.z, shortOffset.z, "Standard barrel muzzle offset should be farther than short barrel.");
            Assert.Greater(longOffset.z, standardOffset.z, "Long barrel muzzle offset should be farther than standard barrel.");

            // Unknown or null part falls back to DefaultMuzzleOffset
            Assert.AreEqual(_visualProfile.DefaultMuzzleOffset, _visualProfile.GetMuzzleOffset("rifle.barrel.unknown"));
            Assert.AreEqual(_visualProfile.DefaultMuzzleOffset, _visualProfile.GetMuzzleOffset(null));
        }

        #endregion

        #region 6. Workshop Session and Apply Integration

        [Test]
        public void WorkshopSession_CustomizingRifleWithAuthoredContent_AppliesSuccessfully()
        {
            var store = new FakeWeaponBuildStore();
            var target = new FakeWeaponBuildTarget();
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

            var session = new WorkshopSession(
                WeaponWorkshopIds.Rifle,
                defaultBuild,
                _catalog,
                _resolver,
                store,
                target);

            Assert.IsTrue(session.IsValid);
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.AreEqual(15f, session.DraftStats.Damage);
            Assert.AreEqual(30, session.DraftStats.MagazineCapacity);

            // Customize build to heavy marksman configuration
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long");
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.drum");
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.vertical");
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.heavy");

            Assert.IsTrue(session.HasUnappliedChanges);
            Assert.IsTrue(session.IsValid);
            Assert.AreEqual(18f, session.DraftStats.Damage);
            Assert.AreEqual(60, session.DraftStats.MagazineCapacity);
            Assert.AreEqual(2.45f, session.DraftStats.ReloadDuration, 0.01f);

            var applyResult = session.Apply();
            Assert.IsTrue(applyResult.IsSuccess);
            Assert.IsFalse(session.HasUnappliedChanges);

            Assert.AreEqual(1, target.ApplyCallCount);
            Assert.AreEqual(18f, target.CurrentStats.Damage);
            Assert.AreEqual(60, target.CurrentStats.MagazineCapacity);
            Assert.AreEqual("rifle.barrel.long", target.CurrentBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.AreEqual("rifle.magazine.drum", target.CurrentBuild.GetPart(WeaponWorkshopIds.RifleSlots.Magazine));

            // Verify persistence
            Assert.IsTrue(store.Stored.ContainsKey(WeaponWorkshopIds.Rifle));
            Assert.AreEqual("rifle.barrel.long", store.Stored[WeaponWorkshopIds.Rifle].GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
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
                else if (line.StartsWith("- rifle.slot.")) def.SupportedSlots.Add(line.Substring(2).Trim());
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

            var files = Directory.GetFiles(fullDir, "Part_Rifle_*.asset");
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
            // format: {x: 0, y: 0.05, z: 0}
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
            // format: {x: 0, y: 0, z: 0, w: 1}
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
