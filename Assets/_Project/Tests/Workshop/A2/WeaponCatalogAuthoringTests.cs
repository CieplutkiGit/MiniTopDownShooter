using System;
using System.Collections.Generic;
using System.Linq;
using Application;
using Application.Weapons;
using Game;
using NUnit.Framework;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop.A2
{
    [TestFixture]
    public class WeaponCatalogAuthoringTests
    {
        private WeaponPlatformDefinition CreateRiflePlatformDefinition()
        {
            var platform = ScriptableObject.CreateInstance<WeaponPlatformDefinition>();
            platform.WeaponId = WeaponWorkshopIds.Rifle;
            platform.DisplayName = "Assault Rifle";
            platform.SupportedSlots = new List<string>(WeaponWorkshopIds.RifleSlots.All);
            platform.DefaultParts = new List<SlotPartPair>
            {
                new SlotPartPair(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard"),
                new SlotPartPair(WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard"),
                new SlotPartPair(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.standard"),
                new SlotPartPair(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard")
            };
            platform.BaseDamage = 15f;
            platform.BaseFireInterval = 0.11f;
            platform.BaseMagazineCapacity = 30;
            platform.BaseStartingReserveAmmo = 120;
            platform.BaseMaxReserveAmmo = 240;
            platform.BaseReloadDuration = 1.55f;
            platform.BaseSpreadAngle = 1.2f;
            platform.MaxSpreadAngle = 7.0f;
            platform.SpreadPerShot = 0.55f;
            platform.SpreadRecoveryPerSecond = 10f;
            platform.Range = 50f;
            platform.ProjectileSpeed = 28f;
            platform.ProjectileLifetime = 2.5f;
            platform.BasePellets = 1;
            platform.BaseFireMode = Application.WeaponFireMode.Automatic;
            platform.BurstCount = 3;
            platform.BurstInterval = 0.08f;
            platform.AimTurnSpeed = 180f;
            platform.DeliveryMode = Application.WeaponDeliveryMode.Projectile;
            platform.InfiniteAmmo = false;
            platform.AutoReloadOnEmpty = true;
            platform.CancelReloadOnFire = true;
            platform.DamageFalloffStart = 10f;
            platform.DamageFalloffEnd = 40f;
            platform.MinDamageRatio = 0.5f;
            return platform;
        }

        private List<WeaponPartDefinition> CreateRiflePartDefinitions()
        {
            var parts = new List<WeaponPartDefinition>();

            // Barrel standard
            var bStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            bStd.PartId = "rifle.barrel.standard";
            bStd.SlotId = WeaponWorkshopIds.RifleSlots.Barrel;
            bStd.DisplayName = "Standard Barrel";
            bStd.Description = "Factory issue barrel.";
            parts.Add(bStd);

            // Barrel long
            var bLong = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            bLong.PartId = "rifle.barrel.long";
            bLong.SlotId = WeaponWorkshopIds.RifleSlots.Barrel;
            bLong.DisplayName = "Long Barrel";
            bLong.DamageDelta = 3f;
            bLong.BaseSpreadAngleDelta = -0.4f;
            bLong.RangeDelta = 15f;
            bLong.ProjectileSpeedDelta = 6f;
            bLong.AimTurnSpeedDelta = -20f;
            bLong.IncompatiblePartIds = new List<string> { "rifle.barrel.short" };
            parts.Add(bLong);

            // Barrel short
            var bShort = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            bShort.PartId = "rifle.barrel.short";
            bShort.SlotId = WeaponWorkshopIds.RifleSlots.Barrel;
            bShort.DisplayName = "Short Barrel";
            bShort.DamageDelta = -2f;
            bShort.BaseSpreadAngleDelta = 0.5f;
            bShort.RangeDelta = -10f;
            bShort.ProjectileSpeedDelta = -4f;
            bShort.AimTurnSpeedDelta = 25f;
            bShort.IncompatiblePartIds = new List<string> { "rifle.barrel.long" };
            parts.Add(bShort);

            // Magazine standard
            var mStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            mStd.PartId = "rifle.magazine.standard";
            mStd.SlotId = WeaponWorkshopIds.RifleSlots.Magazine;
            mStd.DisplayName = "Standard Magazine (30)";
            parts.Add(mStd);

            // Magazine extended
            var mExt = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            mExt.PartId = "rifle.magazine.extended";
            mExt.SlotId = WeaponWorkshopIds.RifleSlots.Magazine;
            mExt.DisplayName = "Extended Magazine (45)";
            mExt.MagazineCapacityDelta = 15;
            mExt.MaxReserveAmmoDelta = 45;
            mExt.ReloadDurationDelta = 0.4f;
            parts.Add(mExt);

            // Grip standard
            var gStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            gStd.PartId = "rifle.grip.standard";
            gStd.SlotId = WeaponWorkshopIds.RifleSlots.Grip;
            gStd.DisplayName = "Standard Grip";
            parts.Add(gStd);

            // Stock standard
            var sStd = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            sStd.PartId = "rifle.stock.standard";
            sStd.SlotId = WeaponWorkshopIds.RifleSlots.Stock;
            sStd.DisplayName = "Standard Stock";
            parts.Add(sStd);

            return parts;
        }

        [Test]
        public void WeaponPartDefinition_ToSpec_ProducesAccurateImmutableSpec()
        {
            var def = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            def.PartId = "test.part.1";
            def.SlotId = "test.slot";
            def.DisplayName = "Test Part";
            def.Description = "A test part for validation.";

            def.DamageDelta = 5f;
            def.FireIntervalDelta = -0.02f;
            def.MagazineCapacityDelta = 10;
            def.MaxReserveAmmoDelta = 30;
            def.ReloadDurationDelta = -0.3f;
            def.BaseSpreadAngleDelta = -0.5f;
            def.MaxSpreadAngleDelta = -1.0f;
            def.SpreadPerShotDelta = -0.1f;
            def.SpreadRecoveryDelta = 2f;
            def.RangeDelta = 12f;
            def.ProjectileSpeedDelta = 5f;
            def.ProjectileLifetimeDelta = 0.5f;
            def.PelletsDelta = 2;
            def.AimTurnSpeedDelta = -15f;

            def.DamageMultiplier = 1.15f;
            def.FireIntervalMultiplier = 0.9f;
            def.ReloadDurationMultiplier = 0.85f;
            def.SpreadMultiplier = 0.8f;
            def.RangeMultiplier = 1.2f;
            def.ProjectileSpeedMultiplier = 1.1f;
            def.AimTurnSpeedMultiplier = 0.95f;

            def.SetFireModeOverride(Application.WeaponFireMode.Burst);
            def.SetBurstCountOverride(4);
            def.SetBurstIntervalOverride(0.06f);
            def.SetDeliveryModeOverride(Application.WeaponDeliveryMode.Hitscan);

            def.IncompatiblePartIds = new List<string> { "incompatible.part.a", "incompatible.part.b" };
            def.RequiredMountTags = new List<string> { "tag.heavy" };
            def.ProvidedMountTags = new List<string> { "tag.optic" };

            WeaponPartSpec spec = def.ToSpec();

            Assert.AreEqual("test.part.1", spec.PartId);
            Assert.AreEqual("test.slot", spec.SlotId);
            Assert.AreEqual("Test Part", spec.DisplayName);
            Assert.AreEqual("A test part for validation.", spec.Description);

            Assert.AreEqual(5f, spec.DamageDelta, 0.001f);
            Assert.AreEqual(-0.02f, spec.FireIntervalDelta, 0.001f);
            Assert.AreEqual(10, spec.MagazineCapacityDelta);
            Assert.AreEqual(30, spec.MaxReserveAmmoDelta);
            Assert.AreEqual(-0.3f, spec.ReloadDurationDelta, 0.001f);
            Assert.AreEqual(-0.5f, spec.BaseSpreadAngleDelta, 0.001f);
            Assert.AreEqual(-1.0f, spec.MaxSpreadAngleDelta, 0.001f);
            Assert.AreEqual(-0.1f, spec.SpreadPerShotDelta, 0.001f);
            Assert.AreEqual(2f, spec.SpreadRecoveryDelta, 0.001f);
            Assert.AreEqual(12f, spec.RangeDelta, 0.001f);
            Assert.AreEqual(5f, spec.ProjectileSpeedDelta, 0.001f);
            Assert.AreEqual(0.5f, spec.ProjectileLifetimeDelta, 0.001f);
            Assert.AreEqual(2, spec.PelletsDelta);
            Assert.AreEqual(-15f, spec.AimTurnSpeedDelta, 0.001f);

            Assert.AreEqual(1.15f, spec.DamageMultiplier, 0.001f);
            Assert.AreEqual(0.9f, spec.FireIntervalMultiplier, 0.001f);
            Assert.AreEqual(0.85f, spec.ReloadDurationMultiplier, 0.001f);
            Assert.AreEqual(0.8f, spec.SpreadMultiplier, 0.001f);
            Assert.AreEqual(1.2f, spec.RangeMultiplier, 0.001f);
            Assert.AreEqual(1.1f, spec.ProjectileSpeedMultiplier, 0.001f);
            Assert.AreEqual(0.95f, spec.AimTurnSpeedMultiplier, 0.001f);

            Assert.AreEqual(Application.WeaponFireMode.Burst, spec.FireModeOverride);
            Assert.AreEqual(4, spec.BurstCountOverride);
            Assert.AreEqual(0.06f, spec.BurstIntervalOverride.Value, 0.001f);
            Assert.AreEqual(Application.WeaponDeliveryMode.Hitscan, spec.DeliveryModeOverride);

            CollectionAssert.AreEqual(new[] { "incompatible.part.a", "incompatible.part.b" }, spec.IncompatiblePartIds);
            CollectionAssert.AreEqual(new[] { "tag.heavy" }, spec.RequiredMountTags);
            CollectionAssert.AreEqual(new[] { "tag.optic" }, spec.ProvidedMountTags);

            // Verify immutability: mutating def lists does not mutate spec collections
            def.IncompatiblePartIds.Add("mutated.part");
            Assert.AreEqual(2, spec.IncompatiblePartIds.Count);
        }

        [Test]
        public void WeaponPlatformDefinition_ToSpec_ProducesAccurateImmutableSpec()
        {
            var def = CreateRiflePlatformDefinition();
            WeaponPlatformSpec spec = def.ToSpec();

            Assert.AreEqual(WeaponWorkshopIds.Rifle, spec.WeaponId);
            Assert.AreEqual("Assault Rifle", spec.DisplayName);
            CollectionAssert.AreEqual(WeaponWorkshopIds.RifleSlots.All, spec.SupportedSlots);

            Assert.AreEqual(4, spec.DefaultParts.Count);
            Assert.AreEqual("rifle.barrel.standard", spec.DefaultParts[WeaponWorkshopIds.RifleSlots.Barrel]);
            Assert.AreEqual("rifle.magazine.standard", spec.DefaultParts[WeaponWorkshopIds.RifleSlots.Magazine]);
            Assert.AreEqual("rifle.grip.standard", spec.DefaultParts[WeaponWorkshopIds.RifleSlots.Grip]);
            Assert.AreEqual("rifle.stock.standard", spec.DefaultParts[WeaponWorkshopIds.RifleSlots.Stock]);

            Assert.AreEqual(15f, spec.BaseDamage, 0.001f);
            Assert.AreEqual(0.11f, spec.BaseFireInterval, 0.001f);
            Assert.AreEqual(30, spec.BaseMagazineCapacity);
            Assert.AreEqual(120, spec.BaseStartingReserveAmmo);
            Assert.AreEqual(240, spec.BaseMaxReserveAmmo);
            Assert.AreEqual(1.55f, spec.BaseReloadDuration, 0.001f);
            Assert.AreEqual(1.2f, spec.BaseSpreadAngle, 0.001f);
            Assert.AreEqual(7.0f, spec.MaxSpreadAngle, 0.001f);
            Assert.AreEqual(0.55f, spec.SpreadPerShot, 0.001f);
            Assert.AreEqual(10f, spec.SpreadRecoveryPerSecond, 0.001f);
            Assert.AreEqual(50f, spec.Range, 0.001f);
            Assert.AreEqual(28f, spec.ProjectileSpeed, 0.001f);
            Assert.AreEqual(2.5f, spec.ProjectileLifetime, 0.001f);
            Assert.AreEqual(1, spec.BasePellets);
            Assert.AreEqual(Application.WeaponFireMode.Automatic, spec.BaseFireMode);
            Assert.AreEqual(3, spec.BurstCount);
            Assert.AreEqual(0.08f, spec.BurstInterval, 0.001f);
            Assert.AreEqual(180f, spec.AimTurnSpeed, 0.001f);
            Assert.AreEqual(Application.WeaponDeliveryMode.Projectile, spec.DeliveryMode);
            Assert.IsFalse(spec.InfiniteAmmo);
            Assert.IsTrue(spec.AutoReloadOnEmpty);
            Assert.IsTrue(spec.CancelReloadOnFire);
            Assert.AreEqual(10f, spec.DamageFalloffStart, 0.001f);
            Assert.AreEqual(40f, spec.DamageFalloffEnd, 0.001f);
            Assert.AreEqual(0.5f, spec.MinDamageRatio, 0.001f);

            var defaultBuild = spec.CreateDefaultBuild();
            Assert.AreEqual(WeaponWorkshopIds.Rifle, defaultBuild.WeaponId);
            Assert.AreEqual("rifle.barrel.standard", defaultBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
        }

        [Test]
        public void WeaponPlatformDefinition_CaptureFromWeaponDefinition_AccuratelyCapturesStats()
        {
            var weaponDef = ScriptableObject.CreateInstance<WeaponDefinition>();
            var platformDef = ScriptableObject.CreateInstance<WeaponPlatformDefinition>();

            var supportedSlots = new List<string> { "slot.1", "slot.2" };
            var defaultParts = new List<SlotPartPair>
            {
                new SlotPartPair("slot.1", "part.1"),
                new SlotPartPair("slot.2", "part.2")
            };

            platformDef.CaptureFromWeaponDefinition(
                weaponDef,
                "weapon.captured",
                "Captured Weapon",
                supportedSlots,
                defaultParts);

            Assert.AreEqual("weapon.captured", platformDef.WeaponId);
            Assert.AreEqual("Captured Weapon", platformDef.DisplayName);
            CollectionAssert.AreEqual(supportedSlots, platformDef.SupportedSlots);
            Assert.AreEqual(2, platformDef.DefaultParts.Count);

            Assert.AreEqual(weaponDef.Damage, platformDef.BaseDamage, 0.001f);
            Assert.AreEqual(weaponDef.FireInterval, platformDef.BaseFireInterval, 0.001f);
            Assert.AreEqual(weaponDef.MagazineSize, platformDef.BaseMagazineCapacity);
            Assert.AreEqual(weaponDef.StartingReserveAmmo, platformDef.BaseStartingReserveAmmo);
            Assert.AreEqual(weaponDef.MaxReserveAmmo, platformDef.BaseMaxReserveAmmo);
            Assert.AreEqual(weaponDef.ReloadDuration, platformDef.BaseReloadDuration, 0.001f);
            Assert.AreEqual(weaponDef.SpreadAngle, platformDef.BaseSpreadAngle, 0.001f);
            Assert.AreEqual(weaponDef.MaxSpreadAngle, platformDef.MaxSpreadAngle, 0.001f);
            Assert.AreEqual(weaponDef.SpreadPerShot, platformDef.SpreadPerShot, 0.001f);
            Assert.AreEqual(weaponDef.SpreadRecoveryPerSecond, platformDef.SpreadRecoveryPerSecond, 0.001f);
            Assert.AreEqual(weaponDef.ProjectileSpeed, platformDef.ProjectileSpeed, 0.001f);
            Assert.AreEqual(weaponDef.ProjectileLifetime, platformDef.ProjectileLifetime, 0.001f);
            Assert.AreEqual(weaponDef.ProjectilesPerShot, platformDef.BasePellets);
            Assert.AreEqual((Application.WeaponFireMode)weaponDef.FireMode, platformDef.BaseFireMode);
            Assert.AreEqual(weaponDef.BurstCount, platformDef.BurstCount);
            Assert.AreEqual(weaponDef.BurstInterval, platformDef.BurstInterval, 0.001f);
            Assert.AreEqual((Application.WeaponDeliveryMode)weaponDef.DeliveryMode, platformDef.DeliveryMode);
            Assert.AreEqual(weaponDef.InfiniteAmmo, platformDef.InfiniteAmmo);
            Assert.AreEqual(weaponDef.AutoReloadOnEmpty, platformDef.AutoReloadOnEmpty);
            Assert.AreEqual(weaponDef.CancelReloadOnFire, platformDef.CancelReloadOnFire);

            WeaponPlatformSpec spec = platformDef.ToSpec();
            Assert.AreEqual("weapon.captured", spec.WeaponId);
            Assert.AreEqual("Captured Weapon", spec.DisplayName);
        }

        [Test]
        public void WeaponCatalog_Lookups_ReturnCorrectPlatformAndPartSpecs()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            var platform = CreateRiflePlatformDefinition();
            var parts = CreateRiflePartDefinitions();

            catalog.Initialize(new[] { platform }, parts);

            // Platform lookups
            WeaponPlatformSpec pSpec = catalog.GetPlatform(WeaponWorkshopIds.Rifle);
            Assert.IsNotNull(pSpec);
            Assert.AreEqual(WeaponWorkshopIds.Rifle, pSpec.WeaponId);

            bool foundPlatform = catalog.TryGetPlatform(WeaponWorkshopIds.Rifle, out var pSpecTry);
            Assert.IsTrue(foundPlatform);
            Assert.IsNotNull(pSpecTry);
            Assert.AreEqual(WeaponWorkshopIds.Rifle, pSpecTry.WeaponId);

            // Part lookups
            WeaponPartSpec partSpec = catalog.GetPart("rifle.barrel.long");
            Assert.IsNotNull(partSpec);
            Assert.AreEqual("rifle.barrel.long", partSpec.PartId);
            Assert.AreEqual(WeaponWorkshopIds.RifleSlots.Barrel, partSpec.SlotId);

            bool foundPart = catalog.TryGetPart("rifle.barrel.long", out var partSpecTry);
            Assert.IsTrue(foundPart);
            Assert.IsNotNull(partSpecTry);
            Assert.AreEqual("rifle.barrel.long", partSpecTry.PartId);

            // Slot query
            var barrelParts = catalog.GetPartsForSlot(WeaponWorkshopIds.Rifle, WeaponWorkshopIds.RifleSlots.Barrel);
            Assert.AreEqual(3, barrelParts.Count);
            CollectionAssert.AreEquivalent(
                new[] { "rifle.barrel.standard", "rifle.barrel.long", "rifle.barrel.short" },
                barrelParts.Select(p => p.PartId));

            // All weapon IDs
            var weaponIds = catalog.GetAllWeaponIds();
            CollectionAssert.AreEqual(new[] { WeaponWorkshopIds.Rifle }, weaponIds);
        }

        [Test]
        public void WeaponCatalog_MissingIds_ThrowOrReturnFalse()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            var platform = CreateRiflePlatformDefinition();
            var parts = CreateRiflePartDefinitions();

            catalog.Initialize(new[] { platform }, parts);

            // Missing platform
            Assert.IsFalse(catalog.TryGetPlatform("weapon.nonexistent", out var pMissing));
            Assert.IsNull(pMissing);
            Assert.Throws<KeyNotFoundException>(() => catalog.GetPlatform("weapon.nonexistent"));

            // Missing part
            Assert.IsFalse(catalog.TryGetPart("part.nonexistent", out var partMissing));
            Assert.IsNull(partMissing);
            Assert.Throws<KeyNotFoundException>(() => catalog.GetPart("part.nonexistent"));

            // Missing slot returns empty list
            var emptyParts = catalog.GetPartsForSlot(WeaponWorkshopIds.Rifle, "rifle.slot.nonexistent");
            Assert.IsNotNull(emptyParts);
            Assert.IsEmpty(emptyParts);

            // Slot not supported by specified platform returns empty list
            var unsupportedSlotParts = catalog.GetPartsForSlot(WeaponWorkshopIds.Rifle, "pistol.slot.slide");
            Assert.IsNotNull(unsupportedSlotParts);
            Assert.IsEmpty(unsupportedSlotParts);

            // Null IDs handling
            Assert.IsFalse(catalog.TryGetPlatform(null, out _));
            Assert.Throws<ArgumentNullException>(() => catalog.GetPlatform(null));

            Assert.IsFalse(catalog.TryGetPart(null, out _));
            Assert.Throws<ArgumentNullException>(() => catalog.GetPart(null));

            Assert.IsEmpty(catalog.GetPartsForSlot(WeaponWorkshopIds.Rifle, null));
        }

        [Test]
        public void WeaponCatalog_DuplicatePlatformIds_Rejected()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            var p1 = CreateRiflePlatformDefinition();
            var p2 = CreateRiflePlatformDefinition(); // Same WeaponId

            var parts = CreateRiflePartDefinitions();

            Assert.Throws<ArgumentException>(() =>
            {
                catalog.Initialize(new[] { p1, p2 }, parts);
            });
        }

        [Test]
        public void WeaponCatalog_DuplicatePartIds_Rejected()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            var platform = CreateRiflePlatformDefinition();

            var part1 = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            part1.PartId = "duplicate.part";
            part1.SlotId = "slot.1";

            var part2 = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            part2.PartId = "duplicate.part"; // Duplicate ID
            part2.SlotId = "slot.1";

            Assert.Throws<ArgumentException>(() =>
            {
                catalog.Initialize(new[] { platform }, new[] { part1, part2 });
            });
        }

        [Test]
        public void WeaponCatalog_NeverExposesMutableScriptableObjects()
        {
            var catalog = ScriptableObject.CreateInstance<WeaponCatalog>();
            var platform = CreateRiflePlatformDefinition();
            var parts = CreateRiflePartDefinitions();

            catalog.Initialize(new[] { platform }, parts);

            var specBefore = catalog.GetPlatform(WeaponWorkshopIds.Rifle);
            Assert.AreEqual(15f, specBefore.BaseDamage, 0.001f);

            // Mutate the original ScriptableObject in memory
            platform.BaseDamage = 999f;

            // Cached spec must be immutable
            var specAfter = catalog.GetPlatform(WeaponWorkshopIds.Rifle);
            Assert.AreEqual(15f, specAfter.BaseDamage, 0.001f);
        }

        [Test]
        public void WeaponCatalogValidation_ValidCatalog_PassesValidation()
        {
            var platform = CreateRiflePlatformDefinition();
            var parts = CreateRiflePartDefinitions();

            var report = WeaponCatalogValidation.Validate(new[] { platform }, parts);

            Assert.IsTrue(report.IsValid, report.ToString());
            Assert.IsEmpty(report.Errors);
        }

        [Test]
        public void WeaponCatalogValidation_MissingDefaultPart_FailsValidation()
        {
            var platform = CreateRiflePlatformDefinition();
            // Remove stock default part
            platform.DefaultParts.RemoveAll(p => p.SlotId == WeaponWorkshopIds.RifleSlots.Stock);

            var parts = CreateRiflePartDefinitions();

            var report = WeaponCatalogValidation.Validate(new[] { platform }, parts);

            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(e => e.Contains("missing default part") && e.Contains(WeaponWorkshopIds.RifleSlots.Stock)));
        }

        [Test]
        public void WeaponCatalogValidation_DefaultPartNotInCatalog_FailsValidation()
        {
            var platform = CreateRiflePlatformDefinition();
            // Set barrel default part to nonexistent part
            int idx = platform.DefaultParts.FindIndex(p => p.SlotId == WeaponWorkshopIds.RifleSlots.Barrel);
            platform.DefaultParts[idx] = new SlotPartPair(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.nonexistent");

            var parts = CreateRiflePartDefinitions();

            var report = WeaponCatalogValidation.Validate(new[] { platform }, parts);

            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(e => e.Contains("does not exist in catalog") && e.Contains("rifle.barrel.nonexistent")));
        }

        [Test]
        public void WeaponCatalogValidation_DefaultPartAssignedToWrongSlot_FailsValidation()
        {
            var platform = CreateRiflePlatformDefinition();
            // Assign magazine part as default for barrel slot
            int idx = platform.DefaultParts.FindIndex(p => p.SlotId == WeaponWorkshopIds.RifleSlots.Barrel);
            platform.DefaultParts[idx] = new SlotPartPair(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.magazine.standard");

            var parts = CreateRiflePartDefinitions();

            var report = WeaponCatalogValidation.Validate(new[] { platform }, parts);

            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(e => e.Contains("belongs to slot")));
        }

        [Test]
        public void WeaponCatalogValidation_PartSlotNotSupportedByAnyPlatform_FailsValidation()
        {
            var platform = CreateRiflePlatformDefinition();
            var parts = CreateRiflePartDefinitions();

            var orphanPart = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            orphanPart.PartId = "alien.laser.lens";
            orphanPart.SlotId = "alien.slot.optics"; // Unsupported slot
            orphanPart.DisplayName = "Alien Lens";
            parts.Add(orphanPart);

            var report = WeaponCatalogValidation.Validate(new[] { platform }, parts);

            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(e => e.Contains("not supported by any platform")));
        }

        [Test]
        public void WeaponCatalogValidation_IncompatiblePartNotFoundInCatalog_FailsValidation()
        {
            var platform = CreateRiflePlatformDefinition();
            var parts = CreateRiflePartDefinitions();

            var barrelLong = parts.First(p => p.PartId == "rifle.barrel.long");
            barrelLong.IncompatiblePartIds.Add("ghost.incompatible.part");

            var report = WeaponCatalogValidation.Validate(new[] { platform }, parts);

            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(e => e.Contains("references incompatible part 'ghost.incompatible.part' which does not exist")));
        }

        [Test]
        public void WeaponCatalogValidation_DuplicateIds_ReportsErrors()
        {
            var platform1 = CreateRiflePlatformDefinition();
            var platform2 = CreateRiflePlatformDefinition(); // Duplicate platform ID

            var parts = CreateRiflePartDefinitions();
            var dupPart = ScriptableObject.CreateInstance<WeaponPartDefinition>();
            dupPart.PartId = "rifle.barrel.standard"; // Duplicate part ID
            dupPart.SlotId = WeaponWorkshopIds.RifleSlots.Barrel;
            parts.Add(dupPart);

            var report = WeaponCatalogValidation.Validate(new[] { platform1, platform2 }, parts);

            Assert.IsFalse(report.IsValid);
            Assert.IsTrue(report.Errors.Any(e => e.Contains("Duplicate platform ID detected")));
            Assert.IsTrue(report.Errors.Any(e => e.Contains("Duplicate part ID detected")));
        }
    }
}
