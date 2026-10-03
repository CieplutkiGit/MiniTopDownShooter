using System.Collections.Generic;

namespace Application.Weapons
{
    public static class WeaponWorkshopIds
    {
        // Weapon Platform IDs
        public const string Pistol = "weapon.pistol";
        public const string Rifle = "weapon.rifle";
        public const string SMG = "weapon.smg";
        public const string Shotgun = "weapon.shotgun";
        public const string Launcher = "weapon.launcher";

        public static readonly IReadOnlyList<string> AllWeaponIds = new[]
        {
            Pistol,
            Rifle,
            SMG,
            Shotgun,
            Launcher
        };

        // Pistol Slots
        public static class PistolSlots
        {
            public const string Barrel = "pistol.slot.barrel";
            public const string Magazine = "pistol.slot.magazine";
            public const string Grip = "pistol.slot.grip";
            public const string Slide = "pistol.slot.slide";

            public static readonly IReadOnlyList<string> All = new[] { Barrel, Magazine, Grip, Slide };
        }

        // Rifle Slots
        public static class RifleSlots
        {
            public const string Barrel = "rifle.slot.barrel";
            public const string Magazine = "rifle.slot.magazine";
            public const string Grip = "rifle.slot.grip";
            public const string Stock = "rifle.slot.stock";

            public static readonly IReadOnlyList<string> All = new[] { Barrel, Magazine, Grip, Stock };
        }

        // SMG Slots
        public static class SMGSlots
        {
            public const string Barrel = "smg.slot.barrel";
            public const string Magazine = "smg.slot.magazine";
            public const string Grip = "smg.slot.grip";
            public const string Action = "smg.slot.action";

            public static readonly IReadOnlyList<string> All = new[] { Barrel, Magazine, Grip, Action };
        }

        // Shotgun Slots
        public static class ShotgunSlots
        {
            public const string Barrel = "shotgun.slot.barrel";
            public const string Feed = "shotgun.slot.feed";
            public const string Stock = "shotgun.slot.stock";
            public const string Action = "shotgun.slot.action";

            public static readonly IReadOnlyList<string> All = new[] { Barrel, Feed, Stock, Action };
        }

        // Launcher Slots
        public static class LauncherSlots
        {
            public const string Tube = "launcher.slot.tube";
            public const string Drum = "launcher.slot.drum";
            public const string Handles = "launcher.slot.handles";
            public const string Action = "launcher.slot.action";

            public static readonly IReadOnlyList<string> All = new[] { Tube, Drum, Handles, Action };
        }
    }
}
