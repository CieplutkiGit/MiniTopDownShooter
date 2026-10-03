using System;
using System.Collections.Generic;
using System.Linq;
using Application;
using Application.Weapons;
using Application.Workshop;

namespace MiniTopDownShooter.Tests.Workshop
{
    public static class WeaponWorkshopTestFixtures
    {
        public static WeaponPlatformSpec CreateRiflePlatformSpec()
        {
            var defaultParts = new Dictionary<string, string>
            {
                { WeaponWorkshopIds.RifleSlots.Barrel, "rifle.barrel.standard" },
                { WeaponWorkshopIds.RifleSlots.Magazine, "rifle.magazine.standard" },
                { WeaponWorkshopIds.RifleSlots.Grip, "rifle.grip.standard" },
                { WeaponWorkshopIds.RifleSlots.Stock, "rifle.stock.standard" }
            };

            return new WeaponPlatformSpec(
                weaponId: WeaponWorkshopIds.Rifle,
                displayName: "Assault Rifle",
                supportedSlots: WeaponWorkshopIds.RifleSlots.All,
                defaultParts: defaultParts,
                baseDamage: 15f,
                baseFireInterval: 0.11f,
                baseMagazineCapacity: 30,
                baseStartingReserveAmmo: 120,
                baseMaxReserveAmmo: 240,
                baseReloadDuration: 1.55f,
                baseSpreadAngle: 1.2f,
                maxSpreadAngle: 7.0f,
                spreadPerShot: 0.55f,
                spreadRecoveryPerSecond: 10f,
                range: 50f,
                projectileSpeed: 28f,
                projectileLifetime: 2.5f,
                basePellets: 1,
                baseFireMode: WeaponFireMode.Automatic,
                burstCount: 3,
                burstInterval: 0.08f,
                aimTurnSpeed: 180f,
                deliveryMode: WeaponDeliveryMode.Projectile,
                infiniteAmmo: false,
                autoReloadOnEmpty: true,
                cancelReloadOnFire: true);
        }

        public static List<WeaponPartSpec> CreateRifleParts()
        {
            var parts = new List<WeaponPartSpec>
            {
                // Barrel
                new WeaponPartSpec("rifle.barrel.standard", WeaponWorkshopIds.RifleSlots.Barrel, "Standard Barrel"),
                new WeaponPartSpec("rifle.barrel.long", WeaponWorkshopIds.RifleSlots.Barrel, "Long Barrel",
                    damageDelta: 3f, baseSpreadAngleDelta: -0.4f, rangeDelta: 15f, projectileSpeedDelta: 6f,
                    aimTurnSpeedDelta: -20f),
                new WeaponPartSpec("rifle.barrel.short", WeaponWorkshopIds.RifleSlots.Barrel, "Short Barrel",
                    damageDelta: -2f, baseSpreadAngleDelta: 0.5f, rangeDelta: -10f, projectileSpeedDelta: -4f,
                    aimTurnSpeedDelta: 25f),

                // Magazine
                new WeaponPartSpec("rifle.magazine.standard", WeaponWorkshopIds.RifleSlots.Magazine, "Standard Magazine (30)"),
                new WeaponPartSpec("rifle.magazine.extended", WeaponWorkshopIds.RifleSlots.Magazine, "Extended Magazine (45)",
                    magazineCapacityDelta: 15, maxReserveAmmoDelta: 45, reloadDurationDelta: 0.4f),
                new WeaponPartSpec("rifle.magazine.drum", WeaponWorkshopIds.RifleSlots.Magazine, "Drum Magazine (60)",
                    magazineCapacityDelta: 30, maxReserveAmmoDelta: 60, reloadDurationDelta: 0.9f, aimTurnSpeedDelta: -15f),

                // Grip
                new WeaponPartSpec("rifle.grip.standard", WeaponWorkshopIds.RifleSlots.Grip, "Standard Grip"),
                new WeaponPartSpec("rifle.grip.angled", WeaponWorkshopIds.RifleSlots.Grip, "Angled Foregrip",
                    spreadRecoveryDelta: 3f, aimTurnSpeedDelta: 10f),
                new WeaponPartSpec("rifle.grip.vertical", WeaponWorkshopIds.RifleSlots.Grip, "Vertical Grip",
                    spreadPerShotDelta: -0.2f, maxSpreadAngleDelta: -1.5f),

                // Stock
                new WeaponPartSpec("rifle.stock.standard", WeaponWorkshopIds.RifleSlots.Stock, "Standard Stock"),
                new WeaponPartSpec("rifle.stock.heavy", WeaponWorkshopIds.RifleSlots.Stock, "Marksman Stock",
                    baseSpreadAngleDelta: -0.3f, maxSpreadAngleDelta: -2.0f, aimTurnSpeedDelta: -25f),
                new WeaponPartSpec("rifle.stock.light", WeaponWorkshopIds.RifleSlots.Stock, "Skeleton Stock",
                    baseSpreadAngleDelta: 0.4f, aimTurnSpeedDelta: 30f)
            };

            return parts;
        }

        public static FakeWeaponCatalog CreateCatalogWithRifle()
        {
            var catalog = new FakeWeaponCatalog();
            var platform = CreateRiflePlatformSpec();
            catalog.AddPlatform(platform);
            foreach (var part in CreateRifleParts())
            {
                catalog.AddPart(part);
            }
            return catalog;
        }
    }

    public sealed class FakeWeaponCatalog : IWeaponCatalog
    {
        private readonly Dictionary<string, WeaponPlatformSpec> _platforms = new Dictionary<string, WeaponPlatformSpec>(StringComparer.Ordinal);
        private readonly Dictionary<string, WeaponPartSpec> _parts = new Dictionary<string, WeaponPartSpec>(StringComparer.Ordinal);

        public void AddPlatform(WeaponPlatformSpec platform)
        {
            _platforms[platform.WeaponId] = platform;
        }

        public void AddPart(WeaponPartSpec part)
        {
            _parts[part.PartId] = part;
        }

        public WeaponPlatformSpec GetPlatform(string weaponId)
        {
            if (TryGetPlatform(weaponId, out var platform)) return platform;
            throw new KeyNotFoundException($"Platform not found: {weaponId}");
        }

        public bool TryGetPlatform(string weaponId, out WeaponPlatformSpec platform)
        {
            return _platforms.TryGetValue(weaponId, out platform);
        }

        public WeaponPartSpec GetPart(string partId)
        {
            if (TryGetPart(partId, out var part)) return part;
            throw new KeyNotFoundException($"Part not found: {partId}");
        }

        public bool TryGetPart(string partId, out WeaponPartSpec part)
        {
            return _parts.TryGetValue(partId, out part);
        }

        public IReadOnlyList<WeaponPartSpec> GetPartsForSlot(string weaponId, string slotId)
        {
            return _parts.Values.Where(p => p.SlotId == slotId).ToList().AsReadOnly();
        }

        public IReadOnlyList<string> GetAllWeaponIds()
        {
            return _platforms.Keys.ToList().AsReadOnly();
        }
    }

    public sealed class FakeWeaponBuildTarget : IWeaponBuildTarget
    {
        public string WeaponId { get; set; } = WeaponWorkshopIds.Rifle;
        public WeaponBuild CurrentBuild { get; set; }
        public ResolvedWeaponStats CurrentStats { get; set; }
        public bool ShouldRejectApply { get; set; }
        public int ApplyCallCount { get; private set; }

        public ApplyResult TryApply(WeaponBuild build, ResolvedWeaponStats stats)
        {
            ApplyCallCount++;
            if (ShouldRejectApply)
            {
                return ApplyResult.Failure("SimulatedFailure", "Rejected by test target.");
            }

            CurrentBuild = build;
            CurrentStats = stats;
            return ApplyResult.Success();
        }
    }

    public sealed class FakeWeaponBuildStore : IWeaponBuildStore
    {
        public readonly Dictionary<string, WeaponBuild> Stored = new Dictionary<string, WeaponBuild>(StringComparer.Ordinal);
        public bool FailOnSave { get; set; }
        public bool FailOnLoad { get; set; }

        public BuildLoadResult Load(string weaponId)
        {
            if (FailOnLoad) return BuildLoadResult.Failure("Simulated load failure.");
            if (Stored.TryGetValue(weaponId, out var build))
            {
                return BuildLoadResult.Success(build);
            }
            return BuildLoadResult.Failure($"Build not found for: {weaponId}");
        }

        public SaveResult Save(WeaponBuild build)
        {
            if (FailOnSave) return SaveResult.Failure("Simulated save failure.");
            Stored[build.WeaponId] = build;
            return SaveResult.Success();
        }
    }

    public sealed class FakeWeaponPreviewView : IWeaponPreviewView
    {
        public WeaponBuild DisplayedBuild { get; private set; }
        public string SelectedSlot { get; private set; }
        public bool IsExploded { get; private set; }

        public event Action<string> SlotSelected;

        public void ShowBuild(WeaponBuild build) => DisplayedBuild = build;
        public void SelectSlot(string slotId) => SelectedSlot = slotId;
        public void SetExploded(bool exploded) => IsExploded = exploded;

        public void TriggerSlotSelected(string slotId)
        {
            SelectedSlot = slotId;
            SlotSelected?.Invoke(slotId);
        }
    }
}
