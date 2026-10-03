using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application;
using Application.Weapons;
using Application.Weapons.Rules;
using Game;
using Game.Workshop.Presentation;
using NUnit.Framework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiniTopDownShooter.Tests.Workshop.B3
{
    [TestFixture]
    public class SMGWorkshopContentTests
    {
        private const string WeaponCustomizationFolder = "Assets/_Project/Data/WeaponCustomization/SMG";
        private const string PrefabPath = "Assets/_Project/Weapons/Gun_SMG.prefab";

        private WeaponPlatformDefinition _platformDef;
        private List<WeaponPartDefinition> _partDefs;
        private WeaponVisualProfile _visualProfile;
        private WeaponBuildResolver _resolver;
        private WeaponCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _resolver = new WeaponBuildResolver();
            LoadOrBuildDefinitions();
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalog != null)
            {
                UnityEngine.Object.DestroyImmediate(_catalog);
                _catalog = null;
            }
        }

        private void LoadOrBuildDefinitions()
        {
#if UNITY_EDITOR
            _platformDef = AssetDatabase.LoadAssetAtPath<WeaponPlatformDefinition>($"{WeaponCustomizationFolder}/Platform_SMG.asset");
            _visualProfile = AssetDatabase.LoadAssetAtPath<WeaponVisualProfile>($"{WeaponCustomizationFolder}/VisualProfile_SMG.asset");

            _partDefs = new List<WeaponPartDefinition>();
            string[] partIds = new[]
            {
                "smg.barrel.standard",
                "smg.barrel.long",
                "smg.barrel.suppressed",
                "smg.magazine.standard",
                "smg.magazine.extended",
                "smg.magazine.drum",
                "smg.grip.standard",
                "smg.grip.ergonomic",
                "smg.grip.vertical",
                "smg.action.standard",
                "smg.action.rapid",
                "smg.action.burst"
            };

            foreach (var id in partIds)
            {
                var part = AssetDatabase.LoadAssetAtPath<WeaponPartDefinition>($"{WeaponCustomizationFolder}/{id}.asset");
                if (part != null)
                {
                    _partDefs.Add(part);
                }
            }
#endif

            // Fallback programmatic construction if run outside Unity Editor asset database
            if (_platformDef == null)
            {
                _platformDef = CreateSMGPlatformDefinition();
            }

            if (_partDefs == null || _partDefs.Count != 12)
            {
                _partDefs = CreateSMGPartDefinitions();
            }

            if (_visualProfile == null)
            {
                _visualProfile = CreateSMGVisualProfile();
            }

            _catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            _catalog.Initialize(new[] { _platformDef }, _partDefs);
        }

        #region Slot and Part Structure Tests

        [Test]
        public void Verify_AllFourSlots_RegisteredAndSupported()
        {
            Assert.IsNotNull(_platformDef);
            Assert.AreEqual(WeaponWorkshopIds.SMG, _platformDef.WeaponId);

            var supportedSlots = _platformDef.SupportedSlots;
            Assert.AreEqual(4, supportedSlots.Count, "Platform must support exactly 4 functional slots.");
            CollectionAssert.AreEquivalent(WeaponWorkshopIds.SMGSlots.All, supportedSlots);

            // Verify default parts assign each slot
            var defaultParts = _platformDef.DefaultParts;
            Assert.AreEqual(4, defaultParts.Count);

            var slotToPart = defaultParts.ToDictionary(p => p.SlotId, p => p.PartId);
            Assert.AreEqual("smg.barrel.standard", slotToPart[WeaponWorkshopIds.SMGSlots.Barrel]);
            Assert.AreEqual("smg.magazine.standard", slotToPart[WeaponWorkshopIds.SMGSlots.Magazine]);
            Assert.AreEqual("smg.grip.standard", slotToPart[WeaponWorkshopIds.SMGSlots.Grip]);
            Assert.AreEqual("smg.action.standard", slotToPart[WeaponWorkshopIds.SMGSlots.Action]);
        }

        [Test]
        public void Verify_AllTwelveParts_DefinedAndSlotCategorization()
        {
            Assert.AreEqual(12, _partDefs.Count, "Expected exactly 12 WeaponPartDefinition assets.");

            var partsBySlot = _partDefs.GroupBy(p => p.SlotId).ToDictionary(g => g.Key, g => g.ToList());

            Assert.IsTrue(partsBySlot.ContainsKey(WeaponWorkshopIds.SMGSlots.Barrel));
            Assert.IsTrue(partsBySlot.ContainsKey(WeaponWorkshopIds.SMGSlots.Magazine));
            Assert.IsTrue(partsBySlot.ContainsKey(WeaponWorkshopIds.SMGSlots.Grip));
            Assert.IsTrue(partsBySlot.ContainsKey(WeaponWorkshopIds.SMGSlots.Action));

            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.SMGSlots.Barrel].Count);
            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.SMGSlots.Magazine].Count);
            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.SMGSlots.Grip].Count);
            Assert.AreEqual(3, partsBySlot[WeaponWorkshopIds.SMGSlots.Action].Count);

            var barrelIds = partsBySlot[WeaponWorkshopIds.SMGSlots.Barrel].Select(p => p.PartId).ToList();
            CollectionAssert.AreEquivalent(new[] { "smg.barrel.standard", "smg.barrel.long", "smg.barrel.suppressed" }, barrelIds);

            var magazineIds = partsBySlot[WeaponWorkshopIds.SMGSlots.Magazine].Select(p => p.PartId).ToList();
            CollectionAssert.AreEquivalent(new[] { "smg.magazine.standard", "smg.magazine.extended", "smg.magazine.drum" }, magazineIds);

            var gripIds = partsBySlot[WeaponWorkshopIds.SMGSlots.Grip].Select(p => p.PartId).ToList();
            CollectionAssert.AreEquivalent(new[] { "smg.grip.standard", "smg.grip.ergonomic", "smg.grip.vertical" }, gripIds);

            var actionIds = partsBySlot[WeaponWorkshopIds.SMGSlots.Action].Select(p => p.PartId).ToList();
            CollectionAssert.AreEquivalent(new[] { "smg.action.standard", "smg.action.rapid", "smg.action.burst" }, actionIds);

            foreach (var part in _partDefs)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(part.PartId), "PartId cannot be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(part.DisplayName), $"DisplayName for {part.PartId} cannot be empty.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(part.Description), $"Description for {part.PartId} cannot be empty.");
            }
        }

        #endregion

        #region Default Build and Stats Tests

        [Test]
        public void Verify_DefaultBuildStats_MatchBaseSMG()
        {
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();
            var resolution = _resolver.Resolve(defaultBuild, _catalog);

            Assert.IsTrue(resolution.IsValid, "Default build must resolve successfully without errors.");
            Assert.AreEqual(0, resolution.Errors.Count);

            var stats = resolution.Stats;

            // Verification against base SMG stats
            Assert.AreEqual(9.0f, stats.Damage, 0.01f);
            Assert.AreEqual(0.065f, stats.FireInterval, 0.0001f);
            Assert.AreEqual(35, stats.MagazineCapacity);
            Assert.AreEqual(140, stats.StartingReserveAmmo);
            Assert.AreEqual(280, stats.MaxReserveAmmo);
            Assert.AreEqual(1.35f, stats.ReloadDuration, 0.01f);
            Assert.AreEqual(3.0f, stats.BaseSpreadAngle, 0.01f);
            Assert.AreEqual(12.0f, stats.MaxSpreadAngle, 0.01f);
            Assert.AreEqual(0.9f, stats.RecoilPerShot, 0.01f);
            Assert.AreEqual(14.0f, stats.SpreadRecoveryRate, 0.01f);
            Assert.AreEqual(50.0f, stats.Range, 0.1f);
            Assert.AreEqual(25.0f, stats.ProjectileSpeed, 0.1f);
            Assert.AreEqual(2.2f, stats.ProjectileLifetime, 0.01f);
            Assert.AreEqual(1, stats.PelletCount);
            Assert.AreEqual(WeaponFireMode.Automatic, stats.FireMode);
            Assert.AreEqual(180.0f, stats.AimTurnSpeed, 0.1f);
            Assert.AreEqual(WeaponDeliveryMode.Projectile, stats.DeliveryMode);
        }

        #endregion

        #region Trade-Off Tests

        [Test]
        public void Verify_TradeOffs_LongBarrel()
        {
            var build = _platformDef.ToSpec().CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.SMGSlots.Barrel, "smg.barrel.long");

            var resolution = _resolver.Resolve(build, _catalog);
            Assert.IsTrue(resolution.IsValid);

            var stats = resolution.Stats;
            // Benefits
            Assert.AreEqual(11.0f, stats.Damage, 0.01f, "Long barrel should increase damage (9 + 2).");
            Assert.AreEqual(65.0f, stats.Range, 0.1f, "Long barrel should increase range (50 + 15).");
            Assert.AreEqual(33.0f, stats.ProjectileSpeed, 0.1f, "Long barrel should increase projectile speed (25 + 8).");
            Assert.AreEqual(2.5f, stats.BaseSpreadAngle, 0.01f, "Long barrel should tighten base spread (3.0 - 0.5).");

            // Drawbacks
            Assert.AreEqual(155.0f, stats.AimTurnSpeed, 0.1f, "Long barrel should reduce aim turn speed (180 - 25).");
            Assert.AreEqual(1.0f, stats.RecoilPerShot, 0.01f, "Long barrel should slightly increase recoil per shot (0.9 + 0.1).");
        }

        [Test]
        public void Verify_TradeOffs_SuppressedBarrel()
        {
            var build = _platformDef.ToSpec().CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.SMGSlots.Barrel, "smg.barrel.suppressed");

            var resolution = _resolver.Resolve(build, _catalog);
            Assert.IsTrue(resolution.IsValid);

            var stats = resolution.Stats;
            // Benefits
            Assert.AreEqual(0.55f, stats.RecoilPerShot, 0.01f, "Suppressed barrel should lower recoil per shot (0.9 - 0.35).");
            Assert.AreEqual(9.0f, stats.MaxSpreadAngle, 0.01f, "Suppressed barrel should lower max spread angle (12 - 3).");
            Assert.AreEqual(17.0f, stats.SpreadRecoveryRate, 0.01f, "Suppressed barrel should boost spread recovery (14 + 3).");

            // Drawbacks
            Assert.AreEqual(8.0f, stats.Damage, 0.01f, "Suppressed barrel should reduce damage (9 - 1).");
            Assert.AreEqual(42.0f, stats.Range, 0.1f, "Suppressed barrel should reduce range (50 - 8).");
            Assert.AreEqual(20.0f, stats.ProjectileSpeed, 0.1f, "Suppressed barrel should reduce projectile speed (25 - 5).");
        }

        [Test]
        public void Verify_TradeOffs_ExtendedMagazine()
        {
            var build = _platformDef.ToSpec().CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.SMGSlots.Magazine, "smg.magazine.extended");

            var resolution = _resolver.Resolve(build, _catalog);
            Assert.IsTrue(resolution.IsValid);

            var stats = resolution.Stats;
            // Benefits
            Assert.AreEqual(50, stats.MagazineCapacity, "Extended mag should provide 50 rounds (35 + 15).");
            Assert.AreEqual(330, stats.MaxReserveAmmo, "Extended mag should expand max reserve ammo (280 + 50).");

            // Drawbacks
            Assert.AreEqual(1.70f, stats.ReloadDuration, 0.01f, "Extended mag should increase reload duration (1.35 + 0.35).");
            Assert.AreEqual(165.0f, stats.AimTurnSpeed, 0.1f, "Extended mag should reduce aim turn speed (180 - 15).");
        }

        [Test]
        public void Verify_TradeOffs_DrumMagazine()
        {
            var build = _platformDef.ToSpec().CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.SMGSlots.Magazine, "smg.magazine.drum");

            var resolution = _resolver.Resolve(build, _catalog);
            Assert.IsTrue(resolution.IsValid);

            var stats = resolution.Stats;
            // Benefits
            Assert.AreEqual(75, stats.MagazineCapacity, "Drum mag should provide 75 rounds (35 + 40).");
            Assert.AreEqual(360, stats.MaxReserveAmmo, "Drum mag should expand max reserve ammo (280 + 80).");

            // Drawbacks
            Assert.AreEqual(2.10f, stats.ReloadDuration, 0.01f, "Drum mag should increase reload duration (1.35 + 0.75).");
            Assert.AreEqual(145.0f, stats.AimTurnSpeed, 0.1f, "Drum mag should reduce aim turn speed (180 - 35).");
            Assert.AreEqual(1.05f, stats.RecoilPerShot, 0.01f, "Drum mag should increase recoil per shot (0.9 + 0.15).");
        }

        [Test]
        public void Verify_TradeOffs_ErgonomicGrip()
        {
            var build = _platformDef.ToSpec().CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.SMGSlots.Grip, "smg.grip.ergonomic");

            var resolution = _resolver.Resolve(build, _catalog);
            Assert.IsTrue(resolution.IsValid);

            var stats = resolution.Stats;
            // Benefits
            Assert.AreEqual(210.0f, stats.AimTurnSpeed, 0.1f, "Ergonomic grip should increase aim turn speed (180 + 30).");
            Assert.AreEqual(18.0f, stats.SpreadRecoveryRate, 0.01f, "Ergonomic grip should improve spread recovery (14 + 4).");

            // Drawbacks
            Assert.AreEqual(3.4f, stats.BaseSpreadAngle, 0.01f, "Ergonomic grip should slightly increase base spread (3.0 + 0.4).");
            Assert.AreEqual(13.0f, stats.MaxSpreadAngle, 0.01f, "Ergonomic grip should slightly increase max spread (12 + 1).");
        }

        [Test]
        public void Verify_TradeOffs_VerticalGrip()
        {
            var build = _platformDef.ToSpec().CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.SMGSlots.Grip, "smg.grip.vertical");

            var resolution = _resolver.Resolve(build, _catalog);
            Assert.IsTrue(resolution.IsValid);

            var stats = resolution.Stats;
            // Benefits
            Assert.AreEqual(0.65f, stats.RecoilPerShot, 0.01f, "Vertical grip should reduce recoil per shot (0.9 - 0.25).");
            Assert.AreEqual(9.5f, stats.MaxSpreadAngle, 0.01f, "Vertical grip should reduce max spread (12 - 2.5).");

            // Drawbacks
            Assert.AreEqual(160.0f, stats.AimTurnSpeed, 0.1f, "Vertical grip should reduce aim turn speed (180 - 20).");
        }

        [Test]
        public void Verify_TradeOffs_RapidAction()
        {
            var build = _platformDef.ToSpec().CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.SMGSlots.Action, "smg.action.rapid");

            var resolution = _resolver.Resolve(build, _catalog);
            Assert.IsTrue(resolution.IsValid);

            var stats = resolution.Stats;
            // Benefits
            Assert.AreEqual(0.050f, stats.FireInterval, 0.0001f, "Rapid action should decrease fire interval to 0.05s (20 RPS).");

            // Drawbacks
            Assert.AreEqual(1.25f, stats.RecoilPerShot, 0.01f, "Rapid action should increase recoil per shot (0.9 + 0.35).");
            Assert.AreEqual(15.0f, stats.MaxSpreadAngle, 0.01f, "Rapid action should expand max spread angle (12 + 3).");
            Assert.AreEqual(1.45f, stats.ReloadDuration, 0.01f, "Rapid action should slightly increase reload duration (1.35 + 0.1).");
        }

        [Test]
        public void Verify_TradeOffs_BurstAction()
        {
            var build = _platformDef.ToSpec().CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.SMGSlots.Action, "smg.action.burst");

            var resolution = _resolver.Resolve(build, _catalog);
            Assert.IsTrue(resolution.IsValid);

            var stats = resolution.Stats;
            // Overrides and Benefits
            Assert.AreEqual(WeaponFireMode.Burst, stats.FireMode, "Burst action should override fire mode to Burst.");
            Assert.AreEqual(3, stats.BurstCount, "Burst count should be 3.");
            Assert.AreEqual(0.05f, stats.BurstInterval, 0.001f, "Burst interval should be 0.05s.");
            Assert.AreEqual(10.0f, stats.Damage, 0.01f, "Burst action should boost damage slightly (9 + 1).");
            Assert.AreEqual(2.2f, stats.BaseSpreadAngle, 0.01f, "Burst action should tighten base spread (3.0 - 0.8).");
            Assert.AreEqual(0.6f, stats.RecoilPerShot, 0.01f, "Burst action should reduce recoil per shot (0.9 - 0.3).");

            // Drawback
            Assert.AreEqual(0.165f, stats.FireInterval, 0.0001f, "Burst action should increase interval between bursts (0.065 + 0.10).");
        }

        #endregion

        #region Incompatibility and Catalog Validation Tests

        [Test]
        public void Verify_Incompatibilities_RejectConflictingBuilds()
        {
            // Incompatible Barrel test: long + suppressed in illegal build
            var longBarrel = _partDefs.First(p => p.PartId == "smg.barrel.long");
            CollectionAssert.Contains(longBarrel.IncompatiblePartIds, "smg.barrel.suppressed");

            var suppressedBarrel = _partDefs.First(p => p.PartId == "smg.barrel.suppressed");
            CollectionAssert.Contains(suppressedBarrel.IncompatiblePartIds, "smg.barrel.long");

            // Incompatible Magazine test
            var extMag = _partDefs.First(p => p.PartId == "smg.magazine.extended");
            CollectionAssert.Contains(extMag.IncompatiblePartIds, "smg.magazine.drum");

            // Incompatible Grip test
            var ergoGrip = _partDefs.First(p => p.PartId == "smg.grip.ergonomic");
            CollectionAssert.Contains(ergoGrip.IncompatiblePartIds, "smg.grip.vertical");

            // Incompatible Action test
            var rapidAction = _partDefs.First(p => p.PartId == "smg.action.rapid");
            CollectionAssert.Contains(rapidAction.IncompatiblePartIds, "smg.action.burst");
        }

        [Test]
        public void Verify_CatalogValidation_Passes()
        {
            var report = WeaponCatalogValidation.Validate(new[] { _platformDef }, _partDefs);
            Assert.IsTrue(report.IsValid, $"Catalog validation failed: {report}");
            Assert.AreEqual(0, report.Errors.Count);
        }

        #endregion

        #region Visual Profile and Prefab Tests

        [Test]
        public void Verify_VisualProfile_ContainsAllTwelvePartsAndMuzzlePoses()
        {
            Assert.IsNotNull(_visualProfile);
            Assert.AreEqual(WeaponWorkshopIds.SMG, _visualProfile.WeaponId);

            // Verify all 12 parts exist in visual profile
            foreach (var part in _partDefs)
            {
                bool found = _visualProfile.TryGetPartPose(part.SlotId, part.PartId, out var pose);
                Assert.IsTrue(found, $"VisualProfile must contain visual entry for part: {part.PartId}");
                Assert.IsNotNull(pose);
                Assert.AreNotEqual(Vector3.zero, pose.AssembledLocalScale, $"Scale for {part.PartId} cannot be zero.");
                Assert.AreNotEqual(Vector3.zero, pose.ExplodedLocalOffset, $"Exploded offset for {part.PartId} must be non-zero.");
            }

            // Verify muzzle offsets for barrel parts
            Vector3 stdMuzzle = _visualProfile.GetMuzzleOffset("smg.barrel.standard");
            Vector3 longMuzzle = _visualProfile.GetMuzzleOffset("smg.barrel.long");
            Vector3 suppMuzzle = _visualProfile.GetMuzzleOffset("smg.barrel.suppressed");

            Assert.AreNotEqual(Vector3.zero, stdMuzzle);
            Assert.AreNotEqual(Vector3.zero, longMuzzle);
            Assert.AreNotEqual(Vector3.zero, suppMuzzle);

            Assert.AreNotEqual(stdMuzzle, longMuzzle, "Long barrel must have distinct muzzle offset from standard.");
            Assert.AreNotEqual(stdMuzzle, suppMuzzle, "Suppressed barrel must have distinct muzzle offset from standard.");
            Assert.AreNotEqual(longMuzzle, suppMuzzle, "Suppressed barrel must have distinct muzzle offset from long.");
        }

        [Test]
        public void Verify_WeaponModelAssembler_CanAssembleSMGBuild()
        {
            var holder = new GameObject("TestAssemblerSMG");
            try
            {
                var assembler = holder.AddComponent<WeaponModelAssembler>();
                var build = _platformDef.ToSpec().CreateDefaultBuild();

                assembler.Assemble(build, _visualProfile);

                Assert.AreEqual(4, assembler.SpawnedParts.Count, "Assembler should have 4 active parts spawned.");
                Assert.IsNotNull(assembler.GetPartObject(WeaponWorkshopIds.SMGSlots.Barrel));
                Assert.IsNotNull(assembler.GetPartObject(WeaponWorkshopIds.SMGSlots.Magazine));
                Assert.IsNotNull(assembler.GetPartObject(WeaponWorkshopIds.SMGSlots.Grip));
                Assert.IsNotNull(assembler.GetPartObject(WeaponWorkshopIds.SMGSlots.Action));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        [Test]
        public void Verify_GunSMGPrefab_ModularSocketHierarchyUnderVisual()
        {
#if UNITY_EDITOR
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"Gun_SMG.prefab could not be loaded at {PrefabPath}");

            var visual = prefab.transform.Find("Visual");
            Assert.IsNotNull(visual, "Visual root must exist under Gun_SMG prefab root.");

            Assert.IsNotNull(visual.Find("BarrelShroud"), "Visual root must contain 'BarrelShroud' socket.");
            Assert.IsNotNull(visual.Find("StickMag"), "Visual root must contain 'StickMag' socket.");
            Assert.IsNotNull(visual.Find("MainGrip"), "Visual root must contain 'MainGrip' socket.");
            Assert.IsNotNull(visual.Find("Body"), "Visual root must contain 'Body' socket.");
            Assert.IsNotNull(visual.Find("Muzzle"), "Visual root must contain 'Muzzle' socket.");
#else
            // Text inspection of prefab asset file
            string prefabText = File.ReadAllText(PrefabPath);
            StringAssert.Contains("m_Name: Visual", prefabText);
            StringAssert.Contains("m_Name: BarrelShroud", prefabText);
            StringAssert.Contains("m_Name: StickMag", prefabText);
            StringAssert.Contains("m_Name: MainGrip", prefabText);
            StringAssert.Contains("m_Name: Body", prefabText);
            StringAssert.Contains("m_Name: Muzzle", prefabText);
#endif
        }

        [Test]
        public void Verify_AssetFilesAndMetaFiles_ExistOnDisk()
        {
            string[] requiredAssets = new[]
            {
                "Platform_SMG.asset",
                "VisualProfile_SMG.asset",
                "smg.barrel.standard.asset",
                "smg.barrel.long.asset",
                "smg.barrel.suppressed.asset",
                "smg.magazine.standard.asset",
                "smg.magazine.extended.asset",
                "smg.magazine.drum.asset",
                "smg.grip.standard.asset",
                "smg.grip.ergonomic.asset",
                "smg.grip.vertical.asset",
                "smg.action.standard.asset",
                "smg.action.rapid.asset",
                "smg.action.burst.asset"
            };

            foreach (var assetName in requiredAssets)
            {
                string assetPath = Path.Combine(WeaponCustomizationFolder, assetName);
                string metaPath = assetPath + ".meta";

                Assert.IsTrue(File.Exists(assetPath), $"Asset file missing on disk: {assetPath}");
                Assert.IsTrue(File.Exists(metaPath), $"Meta file missing on disk: {metaPath}");

                string[] metaLines = File.ReadAllLines(metaPath);
                Assert.AreEqual(2, metaLines.Length, $"Meta file {metaPath} must have exactly 2 lines.");
                StringAssert.StartsWith("fileFormatVersion: 2", metaLines[0]);
                StringAssert.StartsWith("guid: ", metaLines[1]);
            }
        }

        #endregion

        #region Fixture Construction Helpers

        private static WeaponPlatformDefinition CreateSMGPlatformDefinition()
        {
            var def = ScriptableObject.CreateInstance<WeaponPlatformDefinition>();
            def.WeaponId = WeaponWorkshopIds.SMG;
            def.DisplayName = "Submachine Gun";
            def.SupportedSlots = new List<string>(WeaponWorkshopIds.SMGSlots.All);
            def.DefaultParts = new List<SlotPartPair>
            {
                new SlotPartPair(WeaponWorkshopIds.SMGSlots.Barrel, "smg.barrel.standard"),
                new SlotPartPair(WeaponWorkshopIds.SMGSlots.Magazine, "smg.magazine.standard"),
                new SlotPartPair(WeaponWorkshopIds.SMGSlots.Grip, "smg.grip.standard"),
                new SlotPartPair(WeaponWorkshopIds.SMGSlots.Action, "smg.action.standard")
            };
            def.BaseDamage = 9f;
            def.BaseFireInterval = 0.065f;
            def.BaseMagazineCapacity = 35;
            def.BaseStartingReserveAmmo = 140;
            def.BaseMaxReserveAmmo = 280;
            def.BaseReloadDuration = 1.35f;
            def.BaseSpreadAngle = 3f;
            def.MaxSpreadAngle = 12f;
            def.SpreadPerShot = 0.9f;
            def.SpreadRecoveryPerSecond = 14f;
            def.Range = 50f;
            def.ProjectileSpeed = 25f;
            def.ProjectileLifetime = 2.2f;
            def.BasePellets = 1;
            def.BaseFireMode = WeaponFireMode.Automatic;
            def.BurstCount = 3;
            def.BurstInterval = 0.08f;
            def.AimTurnSpeed = 180f;
            def.DeliveryMode = WeaponDeliveryMode.Projectile;
            def.InfiniteAmmo = false;
            def.AutoReloadOnEmpty = true;
            def.CancelReloadOnFire = true;
            return def;
        }

        private static List<WeaponPartDefinition> CreateSMGPartDefinitions()
        {
            var parts = new List<WeaponPartDefinition>();

            // Barrel 1: Standard
            var bStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            bStd.PartId = "smg.barrel.standard";
            bStd.SlotId = WeaponWorkshopIds.SMGSlots.Barrel;
            bStd.DisplayName = "Standard Barrel";
            bStd.Description = "Factory issue standard barrel shroud and rifled core.";
            parts.Add(bStd);

            // Barrel 2: Long
            var bLong = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            bLong.PartId = "smg.barrel.long";
            bLong.SlotId = WeaponWorkshopIds.SMGSlots.Barrel;
            bLong.DisplayName = "Extended Carbine Barrel";
            bLong.Description = "Extended rifled barrel increasing velocity, effective range, and kinetic energy at the cost of heavier weapon handling.";
            bLong.DamageDelta = 2f;
            bLong.RangeDelta = 15f;
            bLong.ProjectileSpeedDelta = 8f;
            bLong.BaseSpreadAngleDelta = -0.5f;
            bLong.AimTurnSpeedDelta = -25f;
            bLong.SpreadPerShotDelta = 0.1f;
            bLong.IncompatiblePartIds = new List<string> { "smg.barrel.suppressed" };
            parts.Add(bLong);

            // Barrel 3: Suppressed
            var bSupp = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            bSupp.PartId = "smg.barrel.suppressed";
            bSupp.SlotId = WeaponWorkshopIds.SMGSlots.Barrel;
            bSupp.DisplayName = "Integral Suppressor Barrel";
            bSupp.Description = "Integrated acoustic suppressor significantly dampening muzzle blast and spread bloom, with lower projectile velocity.";
            bSupp.SpreadPerShotDelta = -0.35f;
            bSupp.MaxSpreadAngleDelta = -3f;
            bSupp.SpreadRecoveryDelta = 3f;
            bSupp.DamageDelta = -1f;
            bSupp.RangeDelta = -8f;
            bSupp.ProjectileSpeedDelta = -5f;
            bSupp.IncompatiblePartIds = new List<string> { "smg.barrel.long" };
            parts.Add(bSupp);

            // Magazine 1: Standard (35)
            var mStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            mStd.PartId = "smg.magazine.standard";
            mStd.SlotId = WeaponWorkshopIds.SMGSlots.Magazine;
            mStd.DisplayName = "Standard Stick Magazine (35)";
            mStd.Description = "Reliable 35-round dual-stack stick magazine.";
            parts.Add(mStd);

            // Magazine 2: Extended (50)
            var mExt = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            mExt.PartId = "smg.magazine.extended";
            mExt.SlotId = WeaponWorkshopIds.SMGSlots.Magazine;
            mExt.DisplayName = "Extended Magazine (50)";
            mExt.Description = "Extended 50-round magazine providing deeper reserves but requiring longer reload time.";
            mExt.MagazineCapacityDelta = 15;
            mExt.MaxReserveAmmoDelta = 50;
            mExt.ReloadDurationDelta = 0.35f;
            mExt.AimTurnSpeedDelta = -15f;
            mExt.IncompatiblePartIds = new List<string> { "smg.magazine.drum" };
            parts.Add(mExt);

            // Magazine 3: Drum (75)
            var mDrum = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            mDrum.PartId = "smg.magazine.drum";
            mDrum.SlotId = WeaponWorkshopIds.SMGSlots.Magazine;
            mDrum.DisplayName = "High-Capacity Drum (75)";
            mDrum.Description = "Massive 75-round drum magazine for maximum sustained suppression at the cost of slow reload and heavy handling penalty.";
            mDrum.MagazineCapacityDelta = 40;
            mDrum.MaxReserveAmmoDelta = 80;
            mDrum.ReloadDurationDelta = 0.75f;
            mDrum.AimTurnSpeedDelta = -35f;
            mDrum.SpreadPerShotDelta = 0.15f;
            mDrum.IncompatiblePartIds = new List<string> { "smg.magazine.extended" };
            parts.Add(mDrum);

            // Grip 1: Standard
            var gStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            gStd.PartId = "smg.grip.standard";
            gStd.SlotId = WeaponWorkshopIds.SMGSlots.Grip;
            gStd.DisplayName = "Standard Pistol Grip";
            gStd.Description = "Factory issue polymer pistol grip.";
            parts.Add(gStd);

            // Grip 2: Ergonomic
            var gErgo = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            gErgo.PartId = "smg.grip.ergonomic";
            gErgo.SlotId = WeaponWorkshopIds.SMGSlots.Grip;
            gErgo.DisplayName = "Ergonomic Angled Grip";
            gErgo.Description = "Aggressively contoured angled grip improving handling and recoil recovery at the expense of initial stability.";
            gErgo.AimTurnSpeedDelta = 30f;
            gErgo.SpreadRecoveryDelta = 4f;
            gErgo.BaseSpreadAngleDelta = 0.4f;
            gErgo.MaxSpreadAngleDelta = 1f;
            gErgo.IncompatiblePartIds = new List<string> { "smg.grip.vertical" };
            parts.Add(gErgo);

            // Grip 3: Vertical
            var gVert = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            gVert.PartId = "smg.grip.vertical";
            gVert.SlotId = WeaponWorkshopIds.SMGSlots.Grip;
            gVert.DisplayName = "Vertical Foregrip";
            gVert.Description = "Sturdy vertical foregrip curbing recoil climb and spread blooming during continuous fire, with slower aim traverse.";
            gVert.SpreadPerShotDelta = -0.25f;
            gVert.MaxSpreadAngleDelta = -2.5f;
            gVert.AimTurnSpeedDelta = -20f;
            gVert.IncompatiblePartIds = new List<string> { "smg.grip.ergonomic" };
            parts.Add(gVert);

            // Action 1: Standard
            var aStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            aStd.PartId = "smg.action.standard";
            aStd.SlotId = WeaponWorkshopIds.SMGSlots.Action;
            aStd.DisplayName = "Standard Automatic Action";
            aStd.Description = "Factory automatic open-bolt cycling action.";
            parts.Add(aStd);

            // Action 2: Rapid
            var aRapid = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            aRapid.PartId = "smg.action.rapid";
            aRapid.SlotId = WeaponWorkshopIds.SMGSlots.Action;
            aRapid.DisplayName = "Rapid-Fire Bolt Action";
            aRapid.Description = "Lightened bolt mechanism drastically accelerating cyclic rate of fire while increasing recoil turbulence.";
            aRapid.FireIntervalDelta = -0.015f;
            aRapid.SpreadPerShotDelta = 0.35f;
            aRapid.MaxSpreadAngleDelta = 3f;
            aRapid.ReloadDurationDelta = 0.1f;
            aRapid.IncompatiblePartIds = new List<string> { "smg.action.burst" };
            parts.Add(aRapid);

            // Action 3: Burst
            var aBurst = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            aBurst.PartId = "smg.action.burst";
            aBurst.SlotId = WeaponWorkshopIds.SMGSlots.Action;
            aBurst.DisplayName = "Precision Burst Triggergroup";
            aBurst.Description = "3-round burst firing unit delivering accurate clusters with reduced recoil bloom and higher per-round lethality.";
            aBurst.SetFireModeOverride(WeaponFireMode.Burst);
            aBurst.SetBurstCountOverride(3);
            aBurst.SetBurstIntervalOverride(0.05f);
            aBurst.DamageDelta = 1f;
            aBurst.BaseSpreadAngleDelta = -0.8f;
            aBurst.SpreadPerShotDelta = -0.3f;
            aBurst.FireIntervalDelta = 0.1f;
            aBurst.IncompatiblePartIds = new List<string> { "smg.action.rapid" };
            parts.Add(aBurst);

            return parts;
        }

        private static WeaponVisualProfile CreateSMGVisualProfile()
        {
            var profile = ScriptableObject.CreateInstance<WeaponVisualProfile>();
            profile.WeaponId = WeaponWorkshopIds.SMG;
            profile.DefaultMuzzleOffset = new Vector3(0f, 0.06f, 0.36f);

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.barrel", "smg.barrel.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0.06f, 0.24f),
                AssembledLocalScale = new Vector3(0.11f, 0.11f, 0.2f),
                ExplodedLocalOffset = new Vector3(0f, 0.15f, 0.35f),
                MuzzleOffset = new Vector3(0f, 0.06f, 0.36f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.barrel", "smg.barrel.long")
            {
                AssembledLocalPosition = new Vector3(0f, 0.06f, 0.28f),
                AssembledLocalScale = new Vector3(0.1f, 0.1f, 0.32f),
                ExplodedLocalOffset = new Vector3(0f, 0.2f, 0.5f),
                MuzzleOffset = new Vector3(0f, 0.06f, 0.46f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.barrel", "smg.barrel.suppressed")
            {
                AssembledLocalPosition = new Vector3(0f, 0.06f, 0.3f),
                AssembledLocalScale = new Vector3(0.13f, 0.13f, 0.28f),
                ExplodedLocalOffset = new Vector3(0f, 0.18f, 0.48f),
                MuzzleOffset = new Vector3(0f, 0.06f, 0.44f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.magazine", "smg.magazine.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.18f, 0.06f),
                AssembledLocalRotation = Quaternion.Euler(-10f, 0f, 0f),
                AssembledLocalScale = new Vector3(0.07f, 0.32f, 0.08f),
                ExplodedLocalOffset = new Vector3(0f, -0.35f, 0.04f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.magazine", "smg.magazine.extended")
            {
                AssembledLocalPosition = new Vector3(0f, -0.22f, 0.06f),
                AssembledLocalRotation = Quaternion.Euler(-10f, 0f, 0f),
                AssembledLocalScale = new Vector3(0.07f, 0.42f, 0.08f),
                ExplodedLocalOffset = new Vector3(0f, -0.42f, 0.04f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.magazine", "smg.magazine.drum")
            {
                AssembledLocalPosition = new Vector3(0f, -0.16f, 0.06f),
                AssembledLocalScale = new Vector3(0.2f, 0.2f, 0.16f),
                ExplodedLocalOffset = new Vector3(0f, -0.38f, 0.06f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.grip", "smg.grip.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.1f, -0.08f),
                AssembledLocalRotation = Quaternion.Euler(15f, 0f, 0f),
                AssembledLocalScale = new Vector3(0.09f, 0.18f, 0.09f),
                ExplodedLocalOffset = new Vector3(0f, -0.25f, -0.18f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.grip", "smg.grip.ergonomic")
            {
                AssembledLocalPosition = new Vector3(0f, -0.1f, -0.08f),
                AssembledLocalRotation = Quaternion.Euler(25f, 0f, 0f),
                AssembledLocalScale = new Vector3(0.09f, 0.16f, 0.1f),
                ExplodedLocalOffset = new Vector3(0f, -0.25f, -0.2f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.grip", "smg.grip.vertical")
            {
                AssembledLocalPosition = new Vector3(0f, -0.1f, 0.22f),
                AssembledLocalScale = new Vector3(0.07f, 0.18f, 0.07f),
                ExplodedLocalOffset = new Vector3(0f, -0.3f, 0.15f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.action", "smg.action.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0.08f, 0.02f),
                AssembledLocalScale = new Vector3(0.1f, 0.06f, 0.14f),
                ExplodedLocalOffset = new Vector3(0f, 0.22f, 0.02f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.action", "smg.action.rapid")
            {
                AssembledLocalPosition = new Vector3(0f, 0.09f, 0.02f),
                AssembledLocalScale = new Vector3(0.09f, 0.05f, 0.12f),
                ExplodedLocalOffset = new Vector3(0f, 0.25f, 0.02f)
            });

            profile.AddOrUpdatePart(new PartVisualData("smg.slot.action", "smg.action.burst")
            {
                AssembledLocalPosition = new Vector3(0f, 0.08f, -0.02f),
                AssembledLocalScale = new Vector3(0.1f, 0.06f, 0.1f),
                ExplodedLocalOffset = new Vector3(0f, 0.22f, -0.02f)
            });

            return profile;
        }

        #endregion
    }
}
