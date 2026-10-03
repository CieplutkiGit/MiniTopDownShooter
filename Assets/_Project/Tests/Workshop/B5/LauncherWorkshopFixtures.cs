using System;
using System.Collections.Generic;
using System.Linq;
using Application;
using Application.Weapons;
using Game;
using Game.Workshop.Presentation;
using UnityEngine;

namespace MiniTopDownShooter.Tests.Workshop.B5
{
    public static class LauncherWorkshopFixtures
    {
        private static T CreateScriptableObject<T>() where T : ScriptableObject
        {
            try
            {
                return ScriptableObject.CreateInstance<T>();
            }
            catch (Exception)
            {
                var obj = (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
                InitFields(obj);
                return obj;
            }
        }

        private static void InitFields(object obj)
        {
            if (obj == null) return;

            // Set m_CachedPtr so UnityEngine.Object operator == evaluates as non-null outside Unity engine
            var objType = typeof(UnityEngine.Object);
            var ptrField = objType.GetField("m_CachedPtr", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (ptrField != null)
            {
                ptrField.SetValue(obj, (IntPtr)1);
            }

            var type = obj.GetType();
            foreach (var field in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
            {
                if (field.FieldType == typeof(List<PartVisualData>))
                    field.SetValue(obj, new List<PartVisualData>());
                else if (field.FieldType == typeof(List<string>))
                    field.SetValue(obj, new List<string>());
                else if (field.FieldType == typeof(List<SlotPartPair>))
                    field.SetValue(obj, new List<SlotPartPair>());
                else if (field.FieldType == typeof(float) && field.Name.EndsWith("Multiplier"))
                    field.SetValue(obj, 1f);
                else if (field.FieldType == typeof(Vector3) && field.Name.EndsWith("Scale"))
                    field.SetValue(obj, Vector3.one);
            }
        }

        public static WeaponPlatformDefinition CreateLauncherPlatformDefinition()
        {
            var platform = CreateScriptableObject<WeaponPlatformDefinition>();
            platform.WeaponId = WeaponWorkshopIds.Launcher;
            platform.DisplayName = "Rotary Grenade Launcher";
            platform.SupportedSlots = new List<string>(WeaponWorkshopIds.LauncherSlots.All);
            platform.DefaultParts = new List<SlotPartPair>
            {
                new SlotPartPair(WeaponWorkshopIds.LauncherSlots.Tube, "launcher.tube.standard"),
                new SlotPartPair(WeaponWorkshopIds.LauncherSlots.Drum, "launcher.drum.standard"),
                new SlotPartPair(WeaponWorkshopIds.LauncherSlots.Handles, "launcher.handles.standard"),
                new SlotPartPair(WeaponWorkshopIds.LauncherSlots.Action, "launcher.action.standard")
            };

            // Base stats matched from Weapon_Launcher.asset
            platform.BaseDamage = 60f;
            platform.BaseFireInterval = 0.85f;
            platform.BaseMagazineCapacity = 4;
            platform.BaseStartingReserveAmmo = 12;
            platform.BaseMaxReserveAmmo = 24;
            platform.BaseReloadDuration = 2.1f;
            platform.BaseSpreadAngle = 0.5f;
            platform.MaxSpreadAngle = 2.0f;
            platform.SpreadPerShot = 0f;
            platform.SpreadRecoveryPerSecond = 6.0f;
            platform.Range = 50f;
            platform.ProjectileSpeed = 14f;
            platform.ProjectileLifetime = 4.0f;
            platform.BasePellets = 1;
            platform.BaseFireMode = Application.WeaponFireMode.SemiAutomatic;
            platform.BurstCount = 3;
            platform.BurstInterval = 0.08f;
            platform.AimTurnSpeed = 180f;
            platform.DeliveryMode = Application.WeaponDeliveryMode.Projectile;
            platform.InfiniteAmmo = false;
            platform.AutoReloadOnEmpty = true;
            platform.CancelReloadOnFire = true;
            platform.DamageFalloffStart = 0f;
            platform.DamageFalloffEnd = 0f;
            platform.MinDamageRatio = 1f;

            return platform;
        }

        public static List<WeaponPartDefinition> CreateLauncherPartDefinitions()
        {
            var parts = new List<WeaponPartDefinition>();

            // Tube Slot
            var tubeStd = CreateScriptableObject<WeaponPartDefinition>();
            tubeStd.PartId = "launcher.tube.standard";
            tubeStd.SlotId = WeaponWorkshopIds.LauncherSlots.Tube;
            tubeStd.DisplayName = "Standard Launch Tube";
            tubeStd.Description = "Factory issue smoothbore launch tube providing balanced projectile velocity and handling.";
            parts.Add(tubeStd);

            var tubeRifled = CreateScriptableObject<WeaponPartDefinition>();
            tubeRifled.PartId = "launcher.tube.rifled";
            tubeRifled.SlotId = WeaponWorkshopIds.LauncherSlots.Tube;
            tubeRifled.DisplayName = "Rifled Launch Tube";
            tubeRifled.Description = "Long rifled barrel imparting spin to launched shells, increasing muzzle velocity, range, and direct impact force at the cost of handling agility.";
            tubeRifled.DamageDelta = 10f;
            tubeRifled.RangeDelta = 20f;
            tubeRifled.ProjectileSpeedDelta = 6f;
            tubeRifled.BaseSpreadAngleDelta = -0.2f;
            tubeRifled.AimTurnSpeedDelta = -30f;
            parts.Add(tubeRifled);

            var tubeShort = CreateScriptableObject<WeaponPartDefinition>();
            tubeShort.PartId = "launcher.tube.short";
            tubeShort.SlotId = WeaponWorkshopIds.LauncherSlots.Tube;
            tubeShort.DisplayName = "Short Launch Tube";
            tubeShort.Description = "Sawed-off lightweight launch tube optimized for snappy weapon traverse and faster reloading, at the cost of range and projectile speed.";
            tubeShort.DamageDelta = -5f;
            tubeShort.RangeDelta = -15f;
            tubeShort.ProjectileSpeedDelta = -4f;
            tubeShort.BaseSpreadAngleDelta = 0.8f;
            tubeShort.AimTurnSpeedDelta = 35f;
            tubeShort.ReloadDurationDelta = -0.3f;
            parts.Add(tubeShort);

            // Drum Slot
            var drumStd = CreateScriptableObject<WeaponPartDefinition>();
            drumStd.PartId = "launcher.drum.standard";
            drumStd.SlotId = WeaponWorkshopIds.LauncherSlots.Drum;
            drumStd.DisplayName = "Standard Drum (4)";
            drumStd.Description = "Factory issue 4-cylinder revolving drum offering standard capacity and reload characteristics.";
            parts.Add(drumStd);

            var drumHighCap = CreateScriptableObject<WeaponPartDefinition>();
            drumHighCap.PartId = "launcher.drum.highcap";
            drumHighCap.SlotId = WeaponWorkshopIds.LauncherSlots.Drum;
            drumHighCap.DisplayName = "High-Capacity Drum (6)";
            drumHighCap.Description = "Expanded 6-cylinder revolving drum granting two additional explosive rounds per cylinder, at the expense of heavier weight and slower reloads.";
            drumHighCap.MagazineCapacityDelta = 2;
            drumHighCap.MaxReserveAmmoDelta = 12;
            drumHighCap.ReloadDurationDelta = 0.6f;
            drumHighCap.AimTurnSpeedDelta = -25f;
            parts.Add(drumHighCap);

            var drumLight = CreateScriptableObject<WeaponPartDefinition>();
            drumLight.PartId = "launcher.drum.lightweight";
            drumLight.SlotId = WeaponWorkshopIds.LauncherSlots.Drum;
            drumLight.DisplayName = "Lightweight Drum (3)";
            drumLight.Description = "Compact 3-chamber cylinder constructed from aircraft-grade alloy for rapid reloads and agile traverse, with reduced ammunition capacity.";
            drumLight.MagazineCapacityDelta = -1;
            drumLight.MaxReserveAmmoDelta = -6;
            drumLight.ReloadDurationDelta = -0.5f;
            drumLight.AimTurnSpeedDelta = 20f;
            parts.Add(drumLight);

            // Handles Slot
            var handlesStd = CreateScriptableObject<WeaponPartDefinition>();
            handlesStd.PartId = "launcher.handles.standard";
            handlesStd.SlotId = WeaponWorkshopIds.LauncherSlots.Handles;
            handlesStd.DisplayName = "Standard Handles";
            handlesStd.Description = "Factory issue polymer front and rear handle configuration offering balanced weapon handling.";
            parts.Add(handlesStd);

            var handlesDual = CreateScriptableObject<WeaponPartDefinition>();
            handlesDual.PartId = "launcher.handles.dual";
            handlesDual.SlotId = WeaponWorkshopIds.LauncherSlots.Handles;
            handlesDual.DisplayName = "Dual Heavy Handles";
            handlesDual.Description = "Heavy-duty reinforced double foregrips that stabilize grenade recoil and tighten shot deviation, at the expense of traverse speed.";
            handlesDual.BaseSpreadAngleDelta = -0.15f;
            handlesDual.MaxSpreadAngleDelta = -0.8f;
            handlesDual.SpreadRecoveryDelta = 2f;
            handlesDual.AimTurnSpeedDelta = -15f;
            parts.Add(handlesDual);

            var handlesErgo = CreateScriptableObject<WeaponPartDefinition>();
            handlesErgo.PartId = "launcher.handles.ergonomic";
            handlesErgo.SlotId = WeaponWorkshopIds.LauncherSlots.Handles;
            handlesErgo.DisplayName = "Ergonomic Handles";
            handlesErgo.Description = "Contoured composite grip surfaces delivering superior manipulation speed, faster weapon traverse, and quicker reloads.";
            handlesErgo.AimTurnSpeedDelta = 30f;
            handlesErgo.SpreadRecoveryDelta = 3f;
            handlesErgo.ReloadDurationDelta = -0.2f;
            parts.Add(handlesErgo);

            // Action Slot
            var actionStd = CreateScriptableObject<WeaponPartDefinition>();
            actionStd.PartId = "launcher.action.standard";
            actionStd.SlotId = WeaponWorkshopIds.LauncherSlots.Action;
            actionStd.DisplayName = "Standard Action";
            actionStd.Description = "Factory issue mechanical indexing action providing reliable cylinder rotation and steady fire cadence.";
            parts.Add(actionStd);

            var actionHair = CreateScriptableObject<WeaponPartDefinition>();
            actionHair.PartId = "launcher.action.hairtrigger";
            actionHair.SlotId = WeaponWorkshopIds.LauncherSlots.Action;
            actionHair.DisplayName = "Hair-Trigger Action";
            actionHair.Description = "Lightened trigger assembly and high-speed indexing ratchet enabling rapid cylinder cycling, but adding noticeable recoil spread per shot.";
            actionHair.FireIntervalDelta = -0.25f;
            actionHair.SpreadPerShotDelta = 0.4f;
            actionHair.MaxSpreadAngleDelta = 1.0f;
            parts.Add(actionHair);

            var actionHeavy = CreateScriptableObject<WeaponPartDefinition>();
            actionHeavy.PartId = "launcher.action.heavy";
            actionHeavy.SlotId = WeaponWorkshopIds.LauncherSlots.Action;
            actionHeavy.DisplayName = "Heavy Reinforced Action";
            actionHeavy.Description = "Heavy-duty high-pressure cylinder lockup maximizing grenade propulsion velocity and kinetic impact, but cycling more deliberately.";
            actionHeavy.DamageDelta = 15f;
            actionHeavy.ProjectileSpeedDelta = 4f;
            actionHeavy.FireIntervalDelta = 0.25f;
            actionHeavy.MaxSpreadAngleDelta = -0.5f;
            parts.Add(actionHeavy);

            return parts;
        }

        public static WeaponVisualProfile CreateLauncherVisualProfile()
        {
            var profile = CreateScriptableObject<WeaponVisualProfile>();
            profile.WeaponId = WeaponWorkshopIds.Launcher;
            profile.DefaultMuzzleOffset = new Vector3(0f, 0.06f, 0.44f);

            // Tube
            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Tube, "launcher.tube.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0.06f, 0.15f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = Vector3.one,
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.4f),
                MuzzleOffset = new Vector3(0f, 0.06f, 0.44f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Tube, "launcher.tube.rifled")
            {
                AssembledLocalPosition = new Vector3(0f, 0.06f, 0.22f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = new Vector3(1f, 1f, 1.4f),
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.5f),
                MuzzleOffset = new Vector3(0f, 0.06f, 0.58f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Tube, "launcher.tube.short")
            {
                AssembledLocalPosition = new Vector3(0f, 0.06f, 0.09f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = new Vector3(1f, 1f, 0.7f),
                ExplodedLocalOffset = new Vector3(0f, 0f, 0.3f),
                MuzzleOffset = new Vector3(0f, 0.06f, 0.32f)
            });

            // Drum
            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Drum, "launcher.drum.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0.04f, -0.1f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = Vector3.one,
                ExplodedLocalOffset = new Vector3(-0.3f, 0f, 0f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Drum, "launcher.drum.highcap")
            {
                AssembledLocalPosition = new Vector3(0f, 0.04f, -0.1f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = new Vector3(1.25f, 1.25f, 1.1f),
                ExplodedLocalOffset = new Vector3(-0.35f, 0f, 0f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Drum, "launcher.drum.lightweight")
            {
                AssembledLocalPosition = new Vector3(0f, 0.04f, -0.1f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = new Vector3(0.85f, 0.85f, 0.9f),
                ExplodedLocalOffset = new Vector3(-0.25f, 0f, 0f)
            });

            // Handles
            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Handles, "launcher.handles.standard")
            {
                AssembledLocalPosition = new Vector3(0f, -0.16f, -0.18f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = Vector3.one,
                ExplodedLocalOffset = new Vector3(0f, -0.35f, 0f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Handles, "launcher.handles.dual")
            {
                AssembledLocalPosition = new Vector3(0f, -0.16f, -0.15f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = new Vector3(1.1f, 1f, 1.2f),
                ExplodedLocalOffset = new Vector3(0f, -0.4f, 0f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Handles, "launcher.handles.ergonomic")
            {
                AssembledLocalPosition = new Vector3(0f, -0.16f, -0.18f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = new Vector3(0.95f, 0.95f, 0.95f),
                ExplodedLocalOffset = new Vector3(0f, -0.35f, 0f)
            });

            // Action
            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Action, "launcher.action.standard")
            {
                AssembledLocalPosition = new Vector3(0f, 0.12f, -0.12f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = Vector3.one,
                ExplodedLocalOffset = new Vector3(0f, 0.3f, -0.1f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Action, "launcher.action.hairtrigger")
            {
                AssembledLocalPosition = new Vector3(0f, 0.12f, -0.12f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = new Vector3(0.9f, 1f, 0.9f),
                ExplodedLocalOffset = new Vector3(0f, 0.35f, -0.1f)
            });

            profile.AddOrUpdatePart(new PartVisualData(WeaponWorkshopIds.LauncherSlots.Action, "launcher.action.heavy")
            {
                AssembledLocalPosition = new Vector3(0f, 0.12f, -0.12f),
                AssembledLocalRotation = Quaternion.identity,
                AssembledLocalScale = new Vector3(1.15f, 1.15f, 1.15f),
                ExplodedLocalOffset = new Vector3(0f, 0.3f, -0.15f)
            });

            return profile;
        }

        public static FakeLauncherCatalog CreateLauncherCatalog()
        {
            var platform = CreateLauncherPlatformDefinition();
            var parts = CreateLauncherPartDefinitions();
            return new FakeLauncherCatalog(platform.ToSpec(), parts);
        }
    }

    public sealed class FakeLauncherCatalog : IWeaponCatalog
    {
        private readonly Dictionary<string, WeaponPlatformSpec> _platforms = new Dictionary<string, WeaponPlatformSpec>();
        private readonly Dictionary<string, WeaponPartSpec> _parts = new Dictionary<string, WeaponPartSpec>();

        public FakeLauncherCatalog(WeaponPlatformSpec platform, IEnumerable<WeaponPartDefinition> partDefs)
        {
            _platforms[platform.WeaponId] = platform;
            foreach (var p in partDefs)
            {
                _parts[p.PartId] = p.ToSpec();
            }
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
            var result = new List<WeaponPartSpec>();
            foreach (var p in _parts.Values)
            {
                if (p.SlotId == slotId) result.Add(p);
            }
            return result;
        }

        public IReadOnlyList<string> GetAllWeaponIds()
        {
            return new List<string>(_platforms.Keys);
        }
    }
}
