using System;
using System.Collections.Generic;
using System.Linq;
using Application;
using Application.Weapons;
using Application.Workshop;
using NUnit.Framework;

namespace MiniTopDownShooter.Tests.Workshop.A1
{
    [TestFixture]
    public class WeaponBuildResolverTests
    {
        private FakeWeaponCatalog _catalog;
        private WeaponBuildResolver _resolver;
        private WeaponPlatformSpec _riflePlatform;

        [SetUp]
        public void SetUp()
        {
            _catalog = WeaponWorkshopTestFixtures.CreateCatalogWithRifle();
            _resolver = new WeaponBuildResolver();
            _riflePlatform = _catalog.GetPlatform(WeaponWorkshopIds.Rifle);
        }

        [Test]
        public void Resolve_NullBuildOrCatalog_ReturnsFailure()
        {
            var resNullBuild = _resolver.Resolve(null, _catalog);
            Assert.IsFalse(resNullBuild.IsValid);
            Assert.That(resNullBuild.Errors.Count, Is.GreaterThan(0));

            var defaultBuild = _riflePlatform.CreateDefaultBuild();
            var resNullCatalog = _resolver.Resolve(defaultBuild, null);
            Assert.IsFalse(resNullCatalog.IsValid);
            Assert.That(resNullCatalog.Errors.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Resolve_UnknownWeaponPlatform_ReturnsFailureWithAffectedId()
        {
            var build = new WeaponBuild("weapon.laser_cannon", new Dictionary<string, string>());
            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Count, Is.GreaterThan(0));
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("weapon.laser_cannon"));
        }

        [Test]
        public void Resolve_DefaultRifleBuild_ReturnsExpectedBaseStats()
        {
            var defaultBuild = _riflePlatform.CreateDefaultBuild();
            var res = _resolver.Resolve(defaultBuild, _catalog);

            Assert.IsTrue(res.IsValid);
            Assert.IsNotNull(res.Stats);

            var stats = res.Stats;
            Assert.AreEqual(15.0f, stats.Damage);
            Assert.AreEqual(0.11f, stats.FireInterval);
            Assert.AreEqual(30, stats.MagazineCapacity);
            Assert.AreEqual(240, stats.MaxReserveAmmo);
            Assert.AreEqual(120, stats.StartingReserveAmmo);
            Assert.AreEqual(1.55f, stats.ReloadDuration);
            Assert.AreEqual(1.2f, stats.BaseSpreadAngle);
            Assert.AreEqual(7.0f, stats.MaxSpreadAngle);
            Assert.AreEqual(0.55f, stats.RecoilPerShot);
            Assert.AreEqual(10.0f, stats.SpreadRecoveryRate);
            Assert.AreEqual(50.0f, stats.Range);
            Assert.AreEqual(28.0f, stats.ProjectileSpeed);
            Assert.AreEqual(2.5f, stats.ProjectileLifetime);
            Assert.AreEqual(1, stats.PelletCount);
            Assert.AreEqual(WeaponFireMode.Automatic, stats.FireMode);
            Assert.AreEqual(180.0f, stats.AimTurnSpeed);
        }

        [Test]
        public void Resolve_RifleWithLongBarrelAndExtendedMag_CalculatesStatsDeterministically()
        {
            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.extended");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsTrue(res.IsValid);
            Assert.IsNotNull(res.Stats);

            var stats = res.Stats;
            // Base Damage: 15 + Long Barrel 3 = 18
            Assert.AreEqual(18.0f, stats.Damage);
            // Magazine: 30 + 15 = 45
            Assert.AreEqual(45, stats.MagazineCapacity);
            // MaxReserve: 240 + 45 = 285
            Assert.AreEqual(285, stats.MaxReserveAmmo);
            // ReloadDuration: 1.55 + 0.4 = 1.95
            Assert.AreEqual(1.95f, stats.ReloadDuration);
            // BaseSpreadAngle: 1.2 - 0.4 = 0.8
            Assert.AreEqual(0.8f, stats.BaseSpreadAngle);
            // Range: 50 + 15 = 65
            Assert.AreEqual(65.0f, stats.Range);
            // ProjectileSpeed: 28 + 6 = 34
            Assert.AreEqual(34.0f, stats.ProjectileSpeed);
            // AimTurnSpeed: 180 - 20 = 160
            Assert.AreEqual(160.0f, stats.AimTurnSpeed);
        }

        [Test]
        public void Resolve_Multipliers_CombineMultiplicatively()
        {
            // Register custom parts with multipliers
            var part1 = new WeaponPartSpec(
                "rifle.barrel.turbo",
                WeaponWorkshopIds.RifleSlots.Barrel,
                "Turbo Barrel",
                damageDelta: 5f,
                damageMultiplier: 1.2f,
                rangeMultiplier: 1.1f);

            var part2 = new WeaponPartSpec(
                "rifle.stock.tuning",
                WeaponWorkshopIds.RifleSlots.Stock,
                "Tuning Stock",
                damageMultiplier: 1.1f,
                rangeMultiplier: 1.2f);

            _catalog.AddPart(part1);
            _catalog.AddPart(part2);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.turbo")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.tuning");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsTrue(res.IsValid);
            // Damage: (15 + 5) * 1.2 * 1.1 = 20 * 1.32 = 26.4
            Assert.AreEqual(26.4f, res.Stats.Damage);
            // Range: 50 * 1.1 * 1.2 = 50 * 1.32 = 66.0
            Assert.AreEqual(66.0f, res.Stats.Range);
        }

        [Test]
        public void Resolve_MissingSlot_FailsResolutionWithSlotId()
        {
            var dict = new Dictionary<string, string>(_riflePlatform.DefaultParts);
            dict.Remove(WeaponWorkshopIds.RifleSlots.Barrel);
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, dict);

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("Missing required slot")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item(WeaponWorkshopIds.RifleSlots.Barrel));
        }

        [Test]
        public void Resolve_UnknownPart_FailsResolutionWithPartId()
        {
            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.obsolete_v1");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("not found in catalog")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.barrel.obsolete_v1"));
        }

        [Test]
        public void Resolve_PartInWrongSlot_FailsResolution()
        {
            // Grip part placed in Barrel slot
            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.grip.angled");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("belongs to slot")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.grip.angled"));
        }

        [Test]
        public void Resolve_UnsupportedSlotInBuild_FailsResolution()
        {
            var dict = new Dictionary<string, string>(_riflePlatform.DefaultParts)
            {
                { "shotgun.slot.feed", "shotgun.feed.tube" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, dict);

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("not supported")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("shotgun.slot.feed"));
        }

        [Test]
        public void Resolve_IncompatibleParts_FailsResolutionWithBothPartIds()
        {
            var heavyBarrel = new WeaponPartSpec(
                "rifle.barrel.ultraheavy",
                WeaponWorkshopIds.RifleSlots.Barrel,
                "Ultraheavy Barrel",
                incompatiblePartIds: new[] { "rifle.stock.light" });

            _catalog.AddPart(heavyBarrel);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.ultraheavy")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.light");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("Incompatible parts")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.barrel.ultraheavy"));
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.stock.light"));
        }

        [Test]
        public void Resolve_MissingRequiredMountTag_FailsResolution()
        {
            var railScope = new WeaponPartSpec(
                "rifle.stock.sniper",
                WeaponWorkshopIds.RifleSlots.Stock,
                "Sniper Stock",
                requiredMountTags: new[] { "tag.picatinny_rail" });

            _catalog.AddPart(railScope);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.sniper");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("requires missing mount tag")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.stock.sniper"));
        }

        [Test]
        public void Resolve_ConflictingFireModeOverrides_FailsResolution()
        {
            var burstGrip = new WeaponPartSpec(
                "rifle.grip.burst_trigger",
                WeaponWorkshopIds.RifleSlots.Grip,
                "Burst Trigger Grip",
                fireModeOverride: WeaponFireMode.Burst);

            var semiStock = new WeaponPartSpec(
                "rifle.stock.match_trigger",
                WeaponWorkshopIds.RifleSlots.Stock,
                "Match Semi Stock",
                fireModeOverride: WeaponFireMode.SemiAutomatic);

            _catalog.AddPart(burstGrip);
            _catalog.AddPart(semiStock);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.burst_trigger")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.match_trigger");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("Conflicting FireMode overrides")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.grip.burst_trigger"));
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.stock.match_trigger"));
        }

        [Test]
        public void Resolve_ConflictingDeliveryModeOverrides_FailsResolution()
        {
            var hitscanBarrel = new WeaponPartSpec(
                "rifle.barrel.laser",
                WeaponWorkshopIds.RifleSlots.Barrel,
                "Laser Barrel",
                deliveryModeOverride: WeaponDeliveryMode.Hitscan);

            var projectileGrip = new WeaponPartSpec(
                "rifle.grip.plasma",
                WeaponWorkshopIds.RifleSlots.Grip,
                "Plasma Injector",
                deliveryModeOverride: WeaponDeliveryMode.Projectile);

            _catalog.AddPart(hitscanBarrel);
            _catalog.AddPart(projectileGrip);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.laser")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.plasma");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("Conflicting DeliveryMode overrides")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.barrel.laser"));
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.grip.plasma"));
        }

        [Test]
        public void Resolve_ConflictingBurstCountOverrides_FailsResolution()
        {
            var grip3 = new WeaponPartSpec(
                "rifle.grip.burst3",
                WeaponWorkshopIds.RifleSlots.Grip,
                "Burst 3 Grip",
                burstCountOverride: 3);

            var stock5 = new WeaponPartSpec(
                "rifle.stock.burst5",
                WeaponWorkshopIds.RifleSlots.Stock,
                "Burst 5 Stock",
                burstCountOverride: 5);

            _catalog.AddPart(grip3);
            _catalog.AddPart(stock5);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.burst3")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.burst5");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("Conflicting BurstCount overrides")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.grip.burst3"));
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.stock.burst5"));
        }

        [Test]
        public void Resolve_ConflictingBurstIntervalOverrides_FailsResolution()
        {
            var gripFast = new WeaponPartSpec(
                "rifle.grip.fastburst",
                WeaponWorkshopIds.RifleSlots.Grip,
                "Fast Burst Grip",
                burstIntervalOverride: 0.04f);

            var stockSlow = new WeaponPartSpec(
                "rifle.stock.slowburst",
                WeaponWorkshopIds.RifleSlots.Stock,
                "Slow Burst Stock",
                burstIntervalOverride: 0.12f);

            _catalog.AddPart(gripFast);
            _catalog.AddPart(stockSlow);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.fastburst")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.slowburst");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsFalse(res.IsValid);
            Assert.That(res.Errors.Any(e => e.Contains("Conflicting BurstInterval overrides")), Is.True);
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.grip.fastburst"));
            Assert.That(res.AffectedSlotOrPartIds, Contains.Item("rifle.stock.slowburst"));
        }

        [Test]
        public void Resolve_MountTagSatisfiedByEquippedPart_Succeeds()
        {
            var railGrip = new WeaponPartSpec(
                "rifle.grip.rail",
                WeaponWorkshopIds.RifleSlots.Grip,
                "Picatinny Grip",
                providedMountTags: new[] { "tag.rail" });

            var laserBarrel = new WeaponPartSpec(
                "rifle.barrel.lasersight",
                WeaponWorkshopIds.RifleSlots.Barrel,
                "Laser Sight Barrel",
                requiredMountTags: new[] { "tag.rail" });

            _catalog.AddPart(railGrip);
            _catalog.AddPart(laserBarrel);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.rail")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.lasersight");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsTrue(res.IsValid);
        }

        [Test]
        public void Resolve_ClampingAndRounding_EnforcedCorrectly()
        {
            var extremeBarrel = new WeaponPartSpec(
                "rifle.barrel.extreme",
                WeaponWorkshopIds.RifleSlots.Barrel,
                "Extreme Barrel",
                damageDelta: -100f,           // Base 15 -> < 1 -> clamped to 1.0
                fireIntervalDelta: -10f,       // Base 0.11 -> < 0.02 -> clamped to 0.02
                reloadDurationDelta: -10f,     // Base 1.55 -> < 0.1 -> clamped to 0.1
                aimTurnSpeedDelta: -500f);     // Base 180 -> < 10 -> clamped to 10.0

            _catalog.AddPart(extremeBarrel);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.extreme");

            var res = _resolver.Resolve(build, _catalog);

            Assert.IsTrue(res.IsValid);
            Assert.AreEqual(1.0f, res.Stats.Damage);
            Assert.AreEqual(0.02f, res.Stats.FireInterval);
            Assert.AreEqual(0.1f, res.Stats.ReloadDuration);
            Assert.AreEqual(10.0f, res.Stats.AimTurnSpeed);
        }

        [Test]
        public void Normalize_NullBuild_ReturnsFirstPlatformDefaultBuild_WithRepairedTrue()
        {
            var repairedBuild = _resolver.Normalize(null, _catalog, out bool repaired);

            Assert.IsTrue(repaired);
            Assert.IsNotNull(repairedBuild);
            Assert.AreEqual(WeaponWorkshopIds.Rifle, repairedBuild.WeaponId);
            Assert.AreEqual("rifle.barrel.standard", repairedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
        }

        [Test]
        public void Normalize_UnknownWeaponId_ReturnsFirstPlatformDefaultBuild_WithRepairedTrue()
        {
            var unknownBuild = new WeaponBuild("weapon.plasma_rifle", new Dictionary<string, string>());
            var repairedBuild = _resolver.Normalize(unknownBuild, _catalog, out bool repaired);

            Assert.IsTrue(repaired);
            Assert.IsNotNull(repairedBuild);
            Assert.AreEqual(WeaponWorkshopIds.Rifle, repairedBuild.WeaponId);
        }

        [Test]
        public void Normalize_MissingSlot_ReplacesWithPlatformDefault_WithRepairedTrue()
        {
            var dict = new Dictionary<string, string>(_riflePlatform.DefaultParts);
            dict.Remove(WeaponWorkshopIds.RifleSlots.Stock);
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, dict);

            var repairedBuild = _resolver.Normalize(build, _catalog, out bool repaired);

            Assert.IsTrue(repaired);
            Assert.AreEqual("rifle.stock.standard", repairedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Stock));
        }

        [Test]
        public void Normalize_UnknownPart_ReplacesWithPlatformDefault_WithRepairedTrue()
        {
            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.deleted_part");

            var repairedBuild = _resolver.Normalize(build, _catalog, out bool repaired);

            Assert.IsTrue(repaired);
            Assert.AreEqual("rifle.barrel.standard", repairedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
        }

        [Test]
        public void Normalize_PartInWrongSlot_ReplacesWithPlatformDefault_WithRepairedTrue()
        {
            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.stock.heavy");

            var repairedBuild = _resolver.Normalize(build, _catalog, out bool repaired);

            Assert.IsTrue(repaired);
            Assert.AreEqual("rifle.barrel.standard", repairedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
        }

        [Test]
        public void Normalize_RemovesUnsupportedSlots_WithRepairedTrue()
        {
            var dict = new Dictionary<string, string>(_riflePlatform.DefaultParts)
            {
                { "pistol.slot.slide", "pistol.slide.standard" }
            };
            var build = new WeaponBuild(WeaponWorkshopIds.Rifle, dict);

            var repairedBuild = _resolver.Normalize(build, _catalog, out bool repaired);

            Assert.IsTrue(repaired);
            Assert.IsFalse(repairedBuild.HasSlot("pistol.slot.slide"));
            Assert.AreEqual(4, repairedBuild.Selections.Count);
        }

        [Test]
        public void Normalize_IncompatibleParts_ReplacesConflictingNonDefaultPart()
        {
            var customBarrel = new WeaponPartSpec(
                "rifle.barrel.custom_comp",
                WeaponWorkshopIds.RifleSlots.Barrel,
                "Custom Comp Barrel",
                incompatiblePartIds: new[] { "rifle.stock.heavy" });

            _catalog.AddPart(customBarrel);

            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.custom_comp")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.heavy");

            var repairedBuild = _resolver.Normalize(build, _catalog, out bool repaired);

            Assert.IsTrue(repaired);
            // One of the conflicting parts was reverted to default
            var res = _resolver.Resolve(repairedBuild, _catalog);
            Assert.IsTrue(res.IsValid);
        }

        [Test]
        public void Normalize_AlreadyValidBuild_ReturnsEqualBuild_WithRepairedFalse()
        {
            var build = _riflePlatform.CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long")
                .WithSelection(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.angled");

            var repairedBuild = _resolver.Normalize(build, _catalog, out bool repaired);

            Assert.IsFalse(repaired);
            Assert.AreEqual(build, repairedBuild);
        }
    }

    [TestFixture]
    public class WorkshopSessionTests
    {
        private FakeWeaponCatalog _catalog;
        private WeaponBuildResolver _resolver;
        private FakeWeaponBuildStore _store;
        private FakeWeaponBuildTarget _target;

        [SetUp]
        public void SetUp()
        {
            _catalog = WeaponWorkshopTestFixtures.CreateCatalogWithRifle();
            _resolver = new WeaponBuildResolver();
            _store = new FakeWeaponBuildStore();
            _target = new FakeWeaponBuildTarget();
        }

        [Test]
        public void Constructor_WithNullInitialBuild_LoadsPlatformDefaultAndResolves()
        {
            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, _target);

            Assert.AreEqual(WeaponWorkshopIds.Rifle, session.WeaponId);
            Assert.IsNotNull(session.CommittedBuild);
            Assert.IsNotNull(session.DraftBuild);
            Assert.AreEqual(session.CommittedBuild, session.DraftBuild);
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.IsTrue(session.IsValid);
            Assert.IsNotNull(session.DraftStats);
            Assert.AreEqual(_target, session.Target);
            Assert.IsFalse(session.LastSaveFailed);
            Assert.IsNull(session.LastSaveError);
        }

        [Test]
        public void SelectPart_UpdatesDraft_ReevaluatesStats_AndFiresSessionChanged()
        {
            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, _target);
            int changeEvents = 0;
            session.SessionChanged += s => changeEvents++;

            session.SelectPart(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long");

            Assert.AreEqual(1, changeEvents);
            Assert.IsTrue(session.HasUnappliedChanges);
            Assert.AreEqual("rifle.barrel.long", session.DraftBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.AreEqual("rifle.barrel.standard", session.CommittedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.AreEqual(18.0f, session.DraftStats.Damage);
        }

        [Test]
        public void SelectPart_InvalidPart_SetsIsValidFalseAndDraftStatsNull()
        {
            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, _target);

            session.SelectPart(WeaponWorkshopIds.RifleSlots.Barrel, "nonexistent.part");

            Assert.IsFalse(session.IsValid);
            Assert.IsNull(session.DraftStats);
            Assert.IsNotNull(session.DraftResolution);
            Assert.IsFalse(session.DraftResolution.IsValid);
            Assert.IsTrue(session.HasUnappliedChanges);
        }

        [Test]
        public void Discard_RevertsDraftToCommitted_AndFiresSessionChanged()
        {
            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, _target);
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long");
            Assert.IsTrue(session.HasUnappliedChanges);

            int eventCount = 0;
            session.SessionChanged += s => eventCount++;

            session.Discard();

            Assert.AreEqual(1, eventCount);
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.AreEqual(session.CommittedBuild, session.DraftBuild);
            Assert.AreEqual("rifle.barrel.standard", session.DraftBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.AreEqual(15.0f, session.DraftStats.Damage);
        }

        [Test]
        public void Apply_WhenInvalid_FailsWithoutApplyingOrCommitting()
        {
            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, _target);
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Barrel, "invalid.barrel");

            var result = session.Apply();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("InvalidDraft", result.ErrorCode);
            Assert.IsTrue(session.HasUnappliedChanges);
            Assert.AreEqual(0, _target.ApplyCallCount);
            Assert.AreEqual("rifle.barrel.standard", session.CommittedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
        }

        [Test]
        public void Apply_WhenTargetRejects_FailsWithoutCommittingOrSaving()
        {
            _target.ShouldRejectApply = true;
            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, _target);
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long");

            var result = session.Apply();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("SimulatedFailure", result.ErrorCode);
            Assert.IsTrue(session.HasUnappliedChanges);
            Assert.AreEqual(1, _target.ApplyCallCount);
            Assert.AreEqual("rifle.barrel.standard", session.CommittedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.AreEqual(0, _store.Stored.Count);
        }

        [Test]
        public void Apply_Success_CommitsBuild_AppliesToTarget_SavesToStore_AndClearsHasUnappliedChanges()
        {
            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, _target);
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long");

            int eventCount = 0;
            session.SessionChanged += s => eventCount++;

            var result = session.Apply();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1, eventCount);
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.AreEqual("rifle.barrel.long", session.CommittedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.AreEqual(1, _target.ApplyCallCount);
            Assert.AreEqual(session.CommittedBuild, _target.CurrentBuild);
            Assert.AreEqual(1, _store.Stored.Count);
            Assert.AreEqual(session.CommittedBuild, _store.Stored[WeaponWorkshopIds.Rifle]);
            Assert.IsFalse(session.LastSaveFailed);
            Assert.IsNull(session.LastSaveError);
        }

        [Test]
        public void Apply_WhenStoreFails_CommitsBuildAndAppliesToTarget_SetsLastSaveFailed()
        {
            _store.FailOnSave = true;
            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, _target);
            session.SelectPart(WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.long");

            var result = session.Apply();

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(session.HasUnappliedChanges);
            Assert.AreEqual("rifle.barrel.long", session.CommittedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Barrel));
            Assert.AreEqual(1, _target.ApplyCallCount);
            Assert.IsTrue(session.LastSaveFailed);
            Assert.AreEqual("Simulated save failure.", session.LastSaveError);
        }

        [Test]
        public void BindTarget_UpdatesTarget_AndFiresSessionChanged()
        {
            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, null);
            Assert.IsNull(session.Target);

            int eventCount = 0;
            session.SessionChanged += s => eventCount++;

            session.BindTarget(_target);

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(_target, session.Target);
        }

        [Test]
        public void SwitchWeapon_LoadsFromStoreIfAvailable_NormalizesAndBindsTarget()
        {
            var savedBuild = _catalog.GetPlatform(WeaponWorkshopIds.Rifle).CreateDefaultBuild()
                .WithSelection(WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.vertical");
            _store.Stored[WeaponWorkshopIds.Rifle] = savedBuild;

            var session = new WorkshopSession(WeaponWorkshopIds.Rifle, null, _catalog, _resolver, _store, null);

            var newTarget = new FakeWeaponBuildTarget();
            int eventCount = 0;
            session.SessionChanged += s => eventCount++;

            session.SwitchWeapon(WeaponWorkshopIds.Rifle, newTarget);

            Assert.AreEqual(1, eventCount);
            Assert.AreEqual(newTarget, session.Target);
            Assert.AreEqual("rifle.grip.vertical", session.CommittedBuild.GetPart(WeaponWorkshopIds.RifleSlots.Grip));
            Assert.AreEqual(session.CommittedBuild, session.DraftBuild);
            Assert.IsFalse(session.HasUnappliedChanges);
        }
    }
}
