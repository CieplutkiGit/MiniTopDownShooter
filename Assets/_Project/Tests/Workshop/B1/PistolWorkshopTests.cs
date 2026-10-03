using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Application;
using Application.Weapons;
using Application.Workshop;
using Game;
using Game.Workshop.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop.B1
{
    [TestFixture]
    public class PistolWorkshopTests
    {
        private const string PlatformAssetPath = "Assets/_Project/Data/WeaponCustomization/Pistol/Platform_Pistol.asset";
        private const string VisualProfileAssetPath = "Assets/_Project/Data/WeaponCustomization/Pistol/VisualProfile_Pistol.asset";
        private const string GunPrefabPath = "Assets/_Project/Weapons/Gun_Pistol.prefab";

        private static readonly string[] ExpectedPartAssetPaths = new[]
        {
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Barrel_Standard.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Barrel_Extended.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Barrel_Comp.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Magazine_Standard.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Magazine_Extended.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Magazine_Drum.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Grip_Standard.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Grip_Tactical.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Grip_Match.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Slide_Standard.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Slide_Lightweight.asset",
            "Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Slide_Heavy.asset"
        };

        private WeaponPlatformDefinition _platformDef;
        private List<WeaponPartDefinition> _partDefs;
        private WeaponVisualProfile _visualProfile;
        private WeaponBuildResolver _resolver;
        private FakePistolCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _resolver = new WeaponBuildResolver();
            _platformDef = CreatePistolPlatformDefinition();
            _partDefs = CreatePistolPartDefinitions();
            _visualProfile = CreatePistolVisualProfile();

            _catalog = new FakePistolCatalog();
            _catalog.AddPlatform(_platformDef.ToSpec());
            foreach (var part in _partDefs)
            {
                _catalog.AddPart(part.ToSpec());
            }
        }

        [TearDown]
        public void TearDown()
        {
            if (_platformDef != null)
            {
                UnityEngine.Object.DestroyImmediate(_platformDef);
                _platformDef = null;
            }

            if (_partDefs != null)
            {
                for (int i = 0; i < _partDefs.Count; i++)
                {
                    if (_partDefs[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(_partDefs[i]);
                    }
                }
                _partDefs.Clear();
            }

            if (_visualProfile != null)
            {
                UnityEngine.Object.DestroyImmediate(_visualProfile);
                _visualProfile = null;
            }
        }

        #region 1. Authored Asset Files Existence & YAML Content

        [Test]
        public void PlatformAsset_FileExists_AndContainsExpectedConfiguration()
        {
            Assert.IsTrue(File.Exists(PlatformAssetPath), $"Platform asset must exist at {PlatformAssetPath}");
            Assert.IsTrue(File.Exists(PlatformAssetPath + ".meta"), $"Platform .meta must exist at {PlatformAssetPath}.meta");

            string text = File.ReadAllText(PlatformAssetPath);
            Assert.IsTrue(text.Contains("_weaponId: weapon.pistol"), "Platform asset must have weapon.pistol ID");
            Assert.IsTrue(text.Contains(WeaponWorkshopIds.PistolSlots.Barrel), "Platform asset must support barrel slot");
            Assert.IsTrue(text.Contains(WeaponWorkshopIds.PistolSlots.Magazine), "Platform asset must support magazine slot");
            Assert.IsTrue(text.Contains(WeaponWorkshopIds.PistolSlots.Grip), "Platform asset must support grip slot");
            Assert.IsTrue(text.Contains(WeaponWorkshopIds.PistolSlots.Slide), "Platform asset must support slide slot");

            Assert.IsTrue(text.Contains("pistol.barrel.standard"), "Platform must configure default standard barrel");
            Assert.IsTrue(text.Contains("pistol.magazine.standard"), "Platform must configure default standard magazine");
            Assert.IsTrue(text.Contains("pistol.grip.standard"), "Platform must configure default standard grip");
            Assert.IsTrue(text.Contains("pistol.slide.standard"), "Platform must configure default standard slide");

            Assert.IsTrue(text.Contains("_baseDamage: 24"), "Platform must have base damage 24");
            Assert.IsTrue(text.Contains("_baseFireInterval: 0.28"), "Platform must have base fire interval 0.28");
            Assert.IsTrue(text.Contains("_baseMagazineCapacity: 12"), "Platform must have base magazine capacity 12");
            Assert.IsTrue(text.Contains("_baseStartingReserveAmmo: 48"), "Platform must have starting reserve 48");
            Assert.IsTrue(text.Contains("_baseMaxReserveAmmo: 96"), "Platform must have max reserve 96");
            Assert.IsTrue(text.Contains("_baseReloadDuration: 1.1"), "Platform must have reload duration 1.1");
        }

        [Test]
        public void PartAssets_AllTwelveFilesAndMetasExist_OnDisk()
        {
            Assert.AreEqual(12, ExpectedPartAssetPaths.Length);
            foreach (var path in ExpectedPartAssetPaths)
            {
                Assert.IsTrue(File.Exists(path), $"Part asset file missing: {path}");
                Assert.IsTrue(File.Exists(path + ".meta"), $"Part meta file missing: {path}.meta");

                string metaContent = File.ReadAllText(path + ".meta").Trim();
                var lines = metaContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                Assert.AreEqual(2, lines.Length, $"Meta file {path}.meta must have exactly 2 lines.");
                Assert.AreEqual("fileFormatVersion: 2", lines[0].Trim());
                Assert.IsTrue(lines[1].Trim().StartsWith("guid: "), "Line 2 of meta must declare guid.");
            }
        }

        [Test]
        public void MagazineAssetFiles_ConfiguredCapacities_Match12_18_30()
        {
            string stdText = File.ReadAllText("Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Magazine_Standard.asset");
            string extText = File.ReadAllText("Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Magazine_Extended.asset");
            string drumText = File.ReadAllText("Assets/_Project/Data/WeaponCustomization/Pistol/Part_Pistol_Magazine_Drum.asset");

            Assert.IsTrue(stdText.Contains("_magazineCapacityDelta: 0"), "Standard magazine must have delta 0 (12 capacity).");
            Assert.IsTrue(extText.Contains("_magazineCapacityDelta: 6"), "Extended magazine must have delta 6 (18 capacity).");
            Assert.IsTrue(drumText.Contains("_magazineCapacityDelta: 18"), "Drum magazine must have delta 18 (30 capacity).");
        }

        [Test]
        public void VisualProfileAsset_FileExists_AndConfiguresAllTwelveParts()
        {
            Assert.IsTrue(File.Exists(VisualProfileAssetPath), $"Visual profile must exist at {VisualProfileAssetPath}");
            Assert.IsTrue(File.Exists(VisualProfileAssetPath + ".meta"), $"Visual profile meta must exist at {VisualProfileAssetPath}.meta");

            string text = File.ReadAllText(VisualProfileAssetPath);
            Assert.IsTrue(text.Contains("_weaponId: weapon.pistol"), "Visual profile must declare weapon.pistol");
            Assert.IsTrue(text.Contains("_defaultMuzzleOffset:"), "Visual profile must define default muzzle offset");

            string[] expectedPartIds = new[]
            {
                "pistol.barrel.standard", "pistol.barrel.extended", "pistol.barrel.comp",
                "pistol.magazine.standard", "pistol.magazine.extended", "pistol.magazine.drum",
                "pistol.grip.standard", "pistol.grip.tactical", "pistol.grip.match",
                "pistol.slide.standard", "pistol.slide.lightweight", "pistol.slide.heavy"
            };

            foreach (var partId in expectedPartIds)
            {
                Assert.IsTrue(text.Contains(partId), $"Visual profile must contain visual entry for '{partId}'");
            }
        }

        [Test]
        public void GunPistolPrefab_FileContainsMagazineChildUnderVisual()
        {
            Assert.IsTrue(File.Exists(GunPrefabPath), $"Gun_Pistol.prefab must exist at {GunPrefabPath}");
            string text = File.ReadAllText(GunPrefabPath);

            Assert.IsTrue(text.Contains("m_Name: Magazine"), "Gun_Pistol.prefab must define a child named Magazine.");
            Assert.IsTrue(text.Contains("m_Name: Visual"), "Gun_Pistol.prefab must define Visual root.");

            // Verify Magazine component references
            Assert.IsTrue(text.Contains("180476718504203243"), "Magazine GameObject ID must exist in prefab.");
            Assert.IsTrue(text.Contains("280476718504203243"), "Magazine Transform ID must exist in prefab.");
            Assert.IsTrue(text.Contains("380476718504203243"), "Magazine MeshFilter ID must exist in prefab.");
            Assert.IsTrue(text.Contains("480476718504203243"), "Magazine MeshRenderer ID must exist in prefab.");
        }

        #endregion

        #region 2. Platform Definition & Slots

        [Test]
        public void PlatformDefinition_SupportsAllFourFunctionalSlots()
        {
            Assert.AreEqual(WeaponWorkshopIds.Pistol, _platformDef.WeaponId);

            var supportedSlots = _platformDef.SupportedSlots;
            Assert.AreEqual(4, supportedSlots.Count);
            CollectionAssert.AreEquivalent(WeaponWorkshopIds.PistolSlots.All, supportedSlots);
        }

        [Test]
        public void PlatformDefinition_ContainsDefaultPartsForEverySlot()
        {
            var defaultParts = _platformDef.DefaultParts;
            Assert.AreEqual(4, defaultParts.Count);

            var dict = defaultParts.ToDictionary(p => p.SlotId, p => p.PartId, StringComparer.Ordinal);
            Assert.AreEqual("pistol.barrel.standard", dict[WeaponWorkshopIds.PistolSlots.Barrel]);
            Assert.AreEqual("pistol.magazine.standard", dict[WeaponWorkshopIds.PistolSlots.Magazine]);
            Assert.AreEqual("pistol.grip.standard", dict[WeaponWorkshopIds.PistolSlots.Grip]);
            Assert.AreEqual("pistol.slide.standard", dict[WeaponWorkshopIds.PistolSlots.Slide]);
        }

        [Test]
        public void PlatformDefinition_BaseStats_MatchWeaponPistolContract()
        {
            Assert.AreEqual(24f, _platformDef.BaseDamage, 0.001f);
            Assert.AreEqual(0.28f, _platformDef.BaseFireInterval, 0.001f);
            Assert.AreEqual(12, _platformDef.BaseMagazineCapacity);
            Assert.AreEqual(48, _platformDef.BaseStartingReserveAmmo);
            Assert.AreEqual(96, _platformDef.BaseMaxReserveAmmo);
            Assert.AreEqual(1.1f, _platformDef.BaseReloadDuration, 0.001f);
            Assert.AreEqual(1.5f, _platformDef.BaseSpreadAngle, 0.001f);
            Assert.AreEqual(6.0f, _platformDef.MaxSpreadAngle, 0.001f);
            Assert.AreEqual(0.7f, _platformDef.SpreadPerShot, 0.001f);
            Assert.AreEqual(8.0f, _platformDef.SpreadRecoveryPerSecond, 0.001f);
            Assert.AreEqual(45f, _platformDef.Range, 0.001f);
            Assert.AreEqual(20f, _platformDef.ProjectileSpeed, 0.001f);
            Assert.AreEqual(3f, _platformDef.ProjectileLifetime, 0.001f);
            Assert.AreEqual(1, _platformDef.BasePellets);
            Assert.AreEqual(Application.WeaponFireMode.SemiAutomatic, _platformDef.BaseFireMode);
            Assert.AreEqual(Application.WeaponDeliveryMode.Hitscan, _platformDef.DeliveryMode);
            Assert.AreEqual(180f, _platformDef.AimTurnSpeed, 0.001f);
            Assert.IsFalse(_platformDef.InfiniteAmmo);
            Assert.IsTrue(_platformDef.AutoReloadOnEmpty);
            Assert.IsTrue(_platformDef.CancelReloadOnFire);
        }

        #endregion

        #region 3. Twelve Parts & Slots

        [Test]
        public void Parts_ExactlyTwelveAuthoredParts_ThreePerSlot()
        {
            Assert.AreEqual(12, _partDefs.Count);

            var barrelParts = _partDefs.Where(p => p.SlotId == WeaponWorkshopIds.PistolSlots.Barrel).Select(p => p.PartId).ToList();
            var magazineParts = _partDefs.Where(p => p.SlotId == WeaponWorkshopIds.PistolSlots.Magazine).Select(p => p.PartId).ToList();
            var gripParts = _partDefs.Where(p => p.SlotId == WeaponWorkshopIds.PistolSlots.Grip).Select(p => p.PartId).ToList();
            var slideParts = _partDefs.Where(p => p.SlotId == WeaponWorkshopIds.PistolSlots.Slide).Select(p => p.PartId).ToList();

            Assert.AreEqual(3, barrelParts.Count);
            CollectionAssert.AreEquivalent(new[] { "pistol.barrel.standard", "pistol.barrel.extended", "pistol.barrel.comp" }, barrelParts);

            Assert.AreEqual(3, magazineParts.Count);
            CollectionAssert.AreEquivalent(new[] { "pistol.magazine.standard", "pistol.magazine.extended", "pistol.magazine.drum" }, magazineParts);

            Assert.AreEqual(3, gripParts.Count);
            CollectionAssert.AreEquivalent(new[] { "pistol.grip.standard", "pistol.grip.tactical", "pistol.grip.match" }, gripParts);

            Assert.AreEqual(3, slideParts.Count);
            CollectionAssert.AreEquivalent(new[] { "pistol.slide.standard", "pistol.slide.lightweight", "pistol.slide.heavy" }, slideParts);
        }

        [Test]
        public void MagazineParts_CapacitiesMatchSpecification_12_18_30()
        {
            var standardMag = _partDefs.First(p => p.PartId == "pistol.magazine.standard");
            var extendedMag = _partDefs.First(p => p.PartId == "pistol.magazine.extended");
            var drumMag = _partDefs.First(p => p.PartId == "pistol.magazine.drum");

            int baseCap = _platformDef.BaseMagazineCapacity;
            Assert.AreEqual(12, baseCap);

            Assert.AreEqual(0, standardMag.MagazineCapacityDelta);
            Assert.AreEqual(12, baseCap + standardMag.MagazineCapacityDelta);

            Assert.AreEqual(6, extendedMag.MagazineCapacityDelta);
            Assert.AreEqual(18, baseCap + extendedMag.MagazineCapacityDelta);

            Assert.AreEqual(18, drumMag.MagazineCapacityDelta);
            Assert.AreEqual(30, baseCap + drumMag.MagazineCapacityDelta);
        }

        #endregion

        #region 4. Default Build Resolution

        [Test]
        public void DefaultBuild_ResolvesSuccessfully_AndMatchesBasePistolStats()
        {
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();
            var resolution = _resolver.Resolve(defaultBuild, _catalog);

            Assert.IsTrue(resolution.IsValid, $"Default pistol resolution failed: {string.Join(", ", resolution.Errors)}");
            var stats = resolution.Stats;

            Assert.AreEqual(24f, stats.Damage, 0.001f);
            Assert.AreEqual(0.28f, stats.FireInterval, 0.0001f);
            Assert.AreEqual(12, stats.MagazineCapacity);
            Assert.AreEqual(48, stats.StartingReserveAmmo);
            Assert.AreEqual(96, stats.MaxReserveAmmo);
            Assert.AreEqual(1.1f, stats.ReloadDuration, 0.01f);
            Assert.AreEqual(1.5f, stats.BaseSpreadAngle, 0.01f);
            Assert.AreEqual(6.0f, stats.MaxSpreadAngle, 0.01f);
            Assert.AreEqual(0.7f, stats.RecoilPerShot, 0.01f);
            Assert.AreEqual(8.0f, stats.SpreadRecoveryRate, 0.01f);
            Assert.AreEqual(45f, stats.Range, 0.1f);
            Assert.AreEqual(20f, stats.ProjectileSpeed, 0.1f);
            Assert.AreEqual(3f, stats.ProjectileLifetime, 0.01f);
            Assert.AreEqual(1, stats.PelletCount);
            Assert.AreEqual(180f, stats.AimTurnSpeed, 0.1f);
            Assert.AreEqual(Application.WeaponFireMode.SemiAutomatic, stats.FireMode);
            Assert.AreEqual(Application.WeaponDeliveryMode.Hitscan, stats.DeliveryMode);
        }

        #endregion

        #region 5. Trade-Offs

        [Test]
        public void BarrelParts_ExhibitMeaningfulTradeOffs()
        {
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();
            var baseStats = _resolver.Resolve(defaultBuild, _catalog).Stats;

            // Extended Barrel: higher damage and range, but heavier / slower aim turn speed
            var extBuild = defaultBuild.WithSelection(WeaponWorkshopIds.PistolSlots.Barrel, "pistol.barrel.extended");
            var extStats = _resolver.Resolve(extBuild, _catalog).Stats;
            Assert.Greater(extStats.Damage, baseStats.Damage, "Extended barrel should increase damage.");
            Assert.Greater(extStats.Range, baseStats.Range, "Extended barrel should increase range.");
            Assert.Less(extStats.AimTurnSpeed, baseStats.AimTurnSpeed, "Extended barrel should penalize aim turn speed.");

            // Comp Barrel: significantly reduces recoil per shot and max spread, but trades off damage/range
            var compBuild = defaultBuild.WithSelection(WeaponWorkshopIds.PistolSlots.Barrel, "pistol.barrel.comp");
            var compStats = _resolver.Resolve(compBuild, _catalog).Stats;
            Assert.Less(compStats.RecoilPerShot, baseStats.RecoilPerShot, "Compensator should decrease recoil per shot.");
            Assert.Less(compStats.MaxSpreadAngle, baseStats.MaxSpreadAngle, "Compensator should decrease max spread angle.");
            Assert.Less(compStats.Damage, baseStats.Damage, "Compensator trades off damage.");
        }

        [Test]
        public void MagazineParts_ExhibitMeaningfulTradeOffs()
        {
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();
            var baseStats = _resolver.Resolve(defaultBuild, _catalog).Stats;

            // Extended Magazine: 18 rounds, but longer reload
            var extBuild = defaultBuild.WithSelection(WeaponWorkshopIds.PistolSlots.Magazine, "pistol.magazine.extended");
            var extStats = _resolver.Resolve(extBuild, _catalog).Stats;
            Assert.AreEqual(18, extStats.MagazineCapacity);
            Assert.Greater(extStats.ReloadDuration, baseStats.ReloadDuration, "Extended magazine should increase reload duration.");

            // Drum Magazine: 30 rounds, but significantly longer reload and aim turn speed penalty
            var drumBuild = defaultBuild.WithSelection(WeaponWorkshopIds.PistolSlots.Magazine, "pistol.magazine.drum");
            var drumStats = _resolver.Resolve(drumBuild, _catalog).Stats;
            Assert.AreEqual(30, drumStats.MagazineCapacity);
            Assert.Greater(drumStats.ReloadDuration, extStats.ReloadDuration, "Drum magazine should have longest reload duration.");
            Assert.Less(drumStats.AimTurnSpeed, baseStats.AimTurnSpeed, "Drum magazine should reduce aim turn speed.");
        }

        [Test]
        public void GripParts_ExhibitMeaningfulTradeOffs()
        {
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();
            var baseStats = _resolver.Resolve(defaultBuild, _catalog).Stats;

            // Tactical Grip: faster aim turn speed and spread recovery, slight recoil per shot trade-off
            var tacBuild = defaultBuild.WithSelection(WeaponWorkshopIds.PistolSlots.Grip, "pistol.grip.tactical");
            var tacStats = _resolver.Resolve(tacBuild, _catalog).Stats;
            Assert.Greater(tacStats.AimTurnSpeed, baseStats.AimTurnSpeed, "Tactical grip should increase aim turn speed.");
            Assert.Greater(tacStats.SpreadRecoveryRate, baseStats.SpreadRecoveryRate, "Tactical grip should increase spread recovery rate.");
            Assert.Greater(tacStats.RecoilPerShot, baseStats.RecoilPerShot, "Tactical grip trades off recoil per shot.");

            // Match Grip: tighter base spread angle and less recoil per shot, but slower aim turn speed
            var matchBuild = defaultBuild.WithSelection(WeaponWorkshopIds.PistolSlots.Grip, "pistol.grip.match");
            var matchStats = _resolver.Resolve(matchBuild, _catalog).Stats;
            Assert.Less(matchStats.BaseSpreadAngle, baseStats.BaseSpreadAngle, "Match grip should improve base spread angle.");
            Assert.Less(matchStats.RecoilPerShot, baseStats.RecoilPerShot, "Match grip should decrease recoil per shot.");
            Assert.Less(matchStats.AimTurnSpeed, baseStats.AimTurnSpeed, "Match grip should trade off aim turn speed.");
        }

        [Test]
        public void SlideParts_ExhibitMeaningfulTradeOffs()
        {
            var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();
            var baseStats = _resolver.Resolve(defaultBuild, _catalog).Stats;

            // Lightweight Slide: cycles faster and reloads faster, but increased recoil per shot and max spread
            var lightBuild = defaultBuild.WithSelection(WeaponWorkshopIds.PistolSlots.Slide, "pistol.slide.lightweight");
            var lightStats = _resolver.Resolve(lightBuild, _catalog).Stats;
            Assert.Less(lightStats.FireInterval, baseStats.FireInterval, "Lightweight slide should decrease fire interval (faster ROF).");
            Assert.Less(lightStats.ReloadDuration, baseStats.ReloadDuration, "Lightweight slide should decrease reload duration.");
            Assert.Greater(lightStats.RecoilPerShot, baseStats.RecoilPerShot, "Lightweight slide increases recoil per shot.");

            // Heavy Slide: absorbs recoil and decreases max spread, but cycles slower and reloads slower
            var heavyBuild = defaultBuild.WithSelection(WeaponWorkshopIds.PistolSlots.Slide, "pistol.slide.heavy");
            var heavyStats = _resolver.Resolve(heavyBuild, _catalog).Stats;
            Assert.Less(heavyStats.RecoilPerShot, baseStats.RecoilPerShot, "Heavy slide should decrease recoil per shot.");
            Assert.Less(heavyStats.MaxSpreadAngle, baseStats.MaxSpreadAngle, "Heavy slide should decrease max spread angle.");
            Assert.Greater(heavyStats.FireInterval, baseStats.FireInterval, "Heavy slide increases fire interval (slower ROF).");
            Assert.Greater(heavyStats.ReloadDuration, baseStats.ReloadDuration, "Heavy slide increases reload duration.");
        }

        #endregion

        #region 6. Catalog Validation

        [Test]
        public void CatalogValidation_PassesWithoutErrors()
        {
            var report = WeaponCatalogValidation.Validate(new[] { _platformDef }, _partDefs);
            Assert.IsTrue(report.IsValid, report.ToString());
            Assert.IsEmpty(report.Errors);
        }

        #endregion

        #region 7. Visual Profile & Assembly

        [Test]
        public void VisualProfile_ContainsEntriesForAllTwelveParts()
        {
            Assert.IsNotNull(_visualProfile, "Visual profile asset must not be null.");
            Assert.AreEqual(WeaponWorkshopIds.Pistol, _visualProfile.WeaponId);

            foreach (var part in _partDefs)
            {
                bool hasPose = _visualProfile.TryGetPartPose(part.SlotId, part.PartId, out var data);
                Assert.IsTrue(hasPose, $"Visual profile is missing entry for part '{part.PartId}' in slot '{part.SlotId}'.");
                Assert.IsNotNull(data);
                Assert.AreEqual(part.PartId, data.PartId);
                Assert.AreEqual(part.SlotId, data.SlotId);
                Assert.AreNotEqual(Vector3.zero, data.AssembledLocalScale, $"Part '{part.PartId}' should have non-zero scale.");
                Assert.AreNotEqual(Vector3.zero, data.ExplodedLocalOffset, $"Part '{part.PartId}' should have non-zero exploded offset.");
            }
        }

        [Test]
        public void VisualProfile_BarrelMuzzleOffsets_AreDistinctAndValid()
        {
            Vector3 defaultMuzzle = _visualProfile.DefaultMuzzleOffset;
            Assert.AreNotEqual(Vector3.zero, defaultMuzzle);

            Vector3 stdMuzzle = _visualProfile.GetMuzzleOffset("pistol.barrel.standard");
            Vector3 extMuzzle = _visualProfile.GetMuzzleOffset("pistol.barrel.extended");
            Vector3 compMuzzle = _visualProfile.GetMuzzleOffset("pistol.barrel.comp");

            Assert.AreNotEqual(Vector3.zero, stdMuzzle);
            Assert.AreNotEqual(Vector3.zero, extMuzzle);
            Assert.AreNotEqual(Vector3.zero, compMuzzle);

            Assert.Greater(extMuzzle.z, stdMuzzle.z, "Extended barrel muzzle offset Z should be farther than standard barrel.");
        }

        [Test]
        public void WeaponModelAssembler_CanAssemblePistolBuilds()
        {
            var holder = new GameObject("TestPistolAssemblerHolder");
            try
            {
                var assembler = holder.AddComponent<WeaponModelAssembler>();
                var defaultBuild = _platformDef.ToSpec().CreateDefaultBuild();

                assembler.Assemble(defaultBuild, _visualProfile);

                Assert.AreEqual(4, assembler.SpawnedParts.Count, "Assembler should instantiate all 4 functional slots.");
                Assert.IsNotNull(assembler.GetPartObject(WeaponWorkshopIds.PistolSlots.Barrel));
                Assert.IsNotNull(assembler.GetPartObject(WeaponWorkshopIds.PistolSlots.Magazine));
                Assert.IsNotNull(assembler.GetPartObject(WeaponWorkshopIds.PistolSlots.Grip));
                Assert.IsNotNull(assembler.GetPartObject(WeaponWorkshopIds.PistolSlots.Slide));

                // Customize with drum magazine and extended barrel
                var customBuild = defaultBuild
                    .WithSelection(WeaponWorkshopIds.PistolSlots.Magazine, "pistol.magazine.drum")
                    .WithSelection(WeaponWorkshopIds.PistolSlots.Barrel, "pistol.barrel.extended");

                assembler.Assemble(customBuild, _visualProfile);
                Assert.AreEqual(4, assembler.SpawnedParts.Count);

                assembler.Clear();
                Assert.AreEqual(0, assembler.SpawnedParts.Count);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        #endregion

        #region Helper Factories

        private static WeaponPlatformDefinition CreatePistolPlatformDefinition()
        {
            var p = ScriptableObject.CreateInstance<WeaponPlatformDefinition>();
            p.WeaponId = WeaponWorkshopIds.Pistol;
            p.DisplayName = "Pistol";
            p.SupportedSlots = new List<string>(WeaponWorkshopIds.PistolSlots.All);
            p.DefaultParts = new List<SlotPartPair>
            {
                new SlotPartPair(WeaponWorkshopIds.PistolSlots.Barrel, "pistol.barrel.standard"),
                new SlotPartPair(WeaponWorkshopIds.PistolSlots.Magazine, "pistol.magazine.standard"),
                new SlotPartPair(WeaponWorkshopIds.PistolSlots.Grip, "pistol.grip.standard"),
                new SlotPartPair(WeaponWorkshopIds.PistolSlots.Slide, "pistol.slide.standard")
            };
            p.BaseDamage = 24f;
            p.BaseFireInterval = 0.28f;
            p.BaseMagazineCapacity = 12;
            p.BaseStartingReserveAmmo = 48;
            p.BaseMaxReserveAmmo = 96;
            p.BaseReloadDuration = 1.1f;
            p.BaseSpreadAngle = 1.5f;
            p.MaxSpreadAngle = 6.0f;
            p.SpreadPerShot = 0.7f;
            p.SpreadRecoveryPerSecond = 8.0f;
            p.Range = 45f;
            p.ProjectileSpeed = 20f;
            p.ProjectileLifetime = 3f;
            p.BasePellets = 1;
            p.BaseFireMode = Application.WeaponFireMode.SemiAutomatic;
            p.BurstCount = 3;
            p.BurstInterval = 0.08f;
            p.AimTurnSpeed = 180f;
            p.DeliveryMode = Application.WeaponDeliveryMode.Hitscan;
            p.InfiniteAmmo = false;
            p.AutoReloadOnEmpty = true;
            p.CancelReloadOnFire = true;
            return p;
        }

        private static List<WeaponPartDefinition> CreatePistolPartDefinitions()
        {
            var list = new List<WeaponPartDefinition>();

            // Barrel
            var bStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            bStd.PartId = "pistol.barrel.standard";
            bStd.SlotId = WeaponWorkshopIds.PistolSlots.Barrel;
            bStd.DisplayName = "Standard Barrel";
            list.Add(bStd);

            var bExt = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            bExt.PartId = "pistol.barrel.extended";
            bExt.SlotId = WeaponWorkshopIds.PistolSlots.Barrel;
            bExt.DisplayName = "Extended Barrel";
            bExt.DamageDelta = 3f;
            bExt.RangeDelta = 10f;
            bExt.ProjectileSpeedDelta = 6f;
            bExt.BaseSpreadAngleDelta = -0.3f;
            bExt.AimTurnSpeedDelta = -15f;
            list.Add(bExt);

            var bComp = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            bComp.PartId = "pistol.barrel.comp";
            bComp.SlotId = WeaponWorkshopIds.PistolSlots.Barrel;
            bComp.DisplayName = "Compensator Barrel";
            bComp.DamageDelta = -2f;
            bComp.RangeDelta = -5f;
            bComp.SpreadPerShotDelta = -0.25f;
            bComp.MaxSpreadAngleDelta = -1.5f;
            bComp.SpreadRecoveryDelta = 2f;
            list.Add(bComp);

            // Magazine
            var mStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            mStd.PartId = "pistol.magazine.standard";
            mStd.SlotId = WeaponWorkshopIds.PistolSlots.Magazine;
            mStd.DisplayName = "Standard Magazine (12)";
            list.Add(mStd);

            var mExt = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            mExt.PartId = "pistol.magazine.extended";
            mExt.SlotId = WeaponWorkshopIds.PistolSlots.Magazine;
            mExt.DisplayName = "Extended Magazine (18)";
            mExt.MagazineCapacityDelta = 6;
            mExt.MaxReserveAmmoDelta = 24;
            mExt.ReloadDurationDelta = 0.35f;
            mExt.AimTurnSpeedDelta = -5f;
            list.Add(mExt);

            var mDrum = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            mDrum.PartId = "pistol.magazine.drum";
            mDrum.SlotId = WeaponWorkshopIds.PistolSlots.Magazine;
            mDrum.DisplayName = "Drum Magazine (30)";
            mDrum.MagazineCapacityDelta = 18;
            mDrum.MaxReserveAmmoDelta = 48;
            mDrum.ReloadDurationDelta = 0.75f;
            mDrum.AimTurnSpeedDelta = -15f;
            list.Add(mDrum);

            // Grip
            var gStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            gStd.PartId = "pistol.grip.standard";
            gStd.SlotId = WeaponWorkshopIds.PistolSlots.Grip;
            gStd.DisplayName = "Standard Grip";
            list.Add(gStd);

            var gTac = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            gTac.PartId = "pistol.grip.tactical";
            gTac.SlotId = WeaponWorkshopIds.PistolSlots.Grip;
            gTac.DisplayName = "Tactical Grip";
            gTac.AimTurnSpeedDelta = 20f;
            gTac.SpreadRecoveryDelta = 3f;
            gTac.SpreadPerShotDelta = 0.1f;
            list.Add(gTac);

            var gMatch = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            gMatch.PartId = "pistol.grip.match";
            gMatch.SlotId = WeaponWorkshopIds.PistolSlots.Grip;
            gMatch.DisplayName = "Match Grip";
            gMatch.BaseSpreadAngleDelta = -0.5f;
            gMatch.SpreadPerShotDelta = -0.15f;
            gMatch.SpreadRecoveryDelta = 1.5f;
            gMatch.AimTurnSpeedDelta = -10f;
            list.Add(gMatch);

            // Slide
            var sStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            sStd.PartId = "pistol.slide.standard";
            sStd.SlotId = WeaponWorkshopIds.PistolSlots.Slide;
            sStd.DisplayName = "Standard Slide";
            list.Add(sStd);

            var sLight = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            sLight.PartId = "pistol.slide.lightweight";
            sLight.SlotId = WeaponWorkshopIds.PistolSlots.Slide;
            sLight.DisplayName = "Lightweight Slide";
            sLight.FireIntervalDelta = -0.05f;
            sLight.ReloadDurationDelta = -0.15f;
            sLight.SpreadPerShotDelta = 0.25f;
            sLight.MaxSpreadAngleDelta = 1.0f;
            list.Add(sLight);

            var sHeavy = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            sHeavy.PartId = "pistol.slide.heavy";
            sHeavy.SlotId = WeaponWorkshopIds.PistolSlots.Slide;
            sHeavy.DisplayName = "Heavy Slide";
            sHeavy.FireIntervalDelta = 0.05f;
            sHeavy.SpreadPerShotDelta = -0.3f;
            sHeavy.MaxSpreadAngleDelta = -1.5f;
            sHeavy.AimTurnSpeedDelta = -10f;
            sHeavy.ReloadDurationDelta = 0.15f;
            list.Add(sHeavy);

            return list;
        }

        private static WeaponVisualProfile CreatePistolVisualProfile()
        {
            var prof = ScriptableObject.CreateInstance<WeaponVisualProfile>();
            prof.WeaponId = WeaponWorkshopIds.Pistol;
            prof.DefaultMuzzleOffset = new Vector3(0f, 0.1f, 0.36f);

            var parts = CreatePistolPartDefinitions();
            foreach (var part in parts)
            {
                var data = new PartVisualData(part.SlotId, part.PartId)
                {
                    AssembledLocalPosition = Vector3.forward * 0.1f,
                    AssembledLocalRotation = Quaternion.identity,
                    AssembledLocalScale = Vector3.one,
                    ExplodedLocalOffset = Vector3.up * 0.2f,
                    MuzzleOffset = part.PartId == "pistol.barrel.extended"
                        ? new Vector3(0f, 0.1f, 0.44f)
                        : (part.PartId == "pistol.barrel.comp"
                            ? new Vector3(0f, 0.1f, 0.38f)
                            : new Vector3(0f, 0.1f, 0.36f))
                };
                prof.AddOrUpdatePart(data);
            }

            return prof;
        }

        #endregion

        private sealed class FakePistolCatalog : IWeaponCatalog
        {
            private readonly Dictionary<string, WeaponPlatformSpec> _platforms = new Dictionary<string, WeaponPlatformSpec>(StringComparer.Ordinal);
            private readonly Dictionary<string, WeaponPartSpec> _parts = new Dictionary<string, WeaponPartSpec>(StringComparer.Ordinal);

            public void AddPlatform(WeaponPlatformSpec platform) => _platforms[platform.WeaponId] = platform;
            public void AddPart(WeaponPartSpec part) => _parts[part.PartId] = part;

            public WeaponPlatformSpec GetPlatform(string weaponId) => _platforms[weaponId];
            public bool TryGetPlatform(string weaponId, out WeaponPlatformSpec platform) => _platforms.TryGetValue(weaponId, out platform);
            public WeaponPartSpec GetPart(string partId) => _parts[partId];
            public bool TryGetPart(string partId, out WeaponPartSpec part) => _parts.TryGetValue(partId, out part);
            public IReadOnlyList<WeaponPartSpec> GetPartsForSlot(string weaponId, string slotId) => _parts.Values.Where(p => p.SlotId == slotId).ToList().AsReadOnly();
            public IReadOnlyList<string> GetAllWeaponIds() => _platforms.Keys.ToList().AsReadOnly();
        }
    }
}
