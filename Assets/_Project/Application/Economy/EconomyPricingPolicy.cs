using System;
using System.Collections.Generic;
using Application.Weapons;

namespace Application.Economy
{
    public static class EconomyPricingPolicy
    {
        // ── Weapon Platform Costs ──────────────────────────────────────────
        private static readonly Dictionary<string, WeaponPlatformCost> PlatformCosts =
            new Dictionary<string, WeaponPlatformCost>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    WeaponWorkshopIds.Pistol,
                    new WeaponPlatformCost(WeaponWorkshopIds.Pistol, "Pistol", requiredLevel: 1, coinCost: 0) // Free starter
                },
                {
                    WeaponWorkshopIds.Rifle,
                    new WeaponPlatformCost(WeaponWorkshopIds.Rifle, "Assault Rifle", requiredLevel: 1, coinCost: 100) // Reachable run 1-2 goal
                },
                {
                    WeaponWorkshopIds.SMG,
                    new WeaponPlatformCost(WeaponWorkshopIds.SMG, "Submachine Gun", requiredLevel: 2, coinCost: 250)
                },
                {
                    WeaponWorkshopIds.Shotgun,
                    new WeaponPlatformCost(WeaponWorkshopIds.Shotgun, "Shotgun", requiredLevel: 3, coinCost: 500)
                },
                {
                    WeaponWorkshopIds.Launcher,
                    new WeaponPlatformCost(WeaponWorkshopIds.Launcher, "Rocket Launcher", requiredLevel: 4, coinCost: 1000)
                }
            };

        // ── Component Sell Prices ──────────────────────────────────────────
        public const int ScrapSellPrice = 5;
        public const int AlloySellPrice = 15;
        public const int CoreSellPrice = 40;

        public static int GetSellPrice(string componentId)
        {
            if (string.Equals(componentId, EconomyComponentExtensions.ScrapId, StringComparison.OrdinalIgnoreCase))
                return ScrapSellPrice;
            if (string.Equals(componentId, EconomyComponentExtensions.AlloyId, StringComparison.OrdinalIgnoreCase))
                return AlloySellPrice;
            if (string.Equals(componentId, EconomyComponentExtensions.CoreId, StringComparison.OrdinalIgnoreCase))
                return CoreSellPrice;

            return 1;
        }

        public static bool IsKnownComponent(string componentId)
        {
            if (string.IsNullOrWhiteSpace(componentId)) return false;
            return string.Equals(componentId, EconomyComponentExtensions.ScrapId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(componentId, EconomyComponentExtensions.AlloyId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(componentId, EconomyComponentExtensions.CoreId, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsKnownPlatform(string weaponId)
        {
            if (string.IsNullOrWhiteSpace(weaponId)) return false;
            return PlatformCosts.ContainsKey(weaponId);
        }

        public static bool TryGetPlatformCost(string weaponId, out WeaponPlatformCost cost)
        {
            if (!string.IsNullOrWhiteSpace(weaponId) && PlatformCosts.TryGetValue(weaponId, out cost))
            {
                return true;
            }
            cost = null;
            return false;
        }

        public static WeaponPlatformCost GetPlatformCost(string weaponId)
        {
            if (!string.IsNullOrEmpty(weaponId) && PlatformCosts.TryGetValue(weaponId, out var cost))
            {
                return cost;
            }

            return new WeaponPlatformCost(weaponId, weaponId, requiredLevel: 1, coinCost: 100);
        }

        public static IReadOnlyCollection<WeaponPlatformCost> GetAllPlatformCosts()
        {
            return PlatformCosts.Values;
        }

        // ── Catalog Provider (Pure C#) ────────────────────────────────────
        private static Func<IWeaponCatalog> s_catalogProvider;
        public static void SetCatalogProvider(Func<IWeaponCatalog> provider) => s_catalogProvider = provider;
        public static IWeaponCatalog ActiveCatalog => s_catalogProvider?.Invoke();

        public static void ResetForTesting()
        {
            s_catalogProvider = null;
        }

        // ── Authored Real Parts Mapping (Fallback when catalog not injected) ──
        private static readonly Dictionary<string, Dictionary<string, HashSet<string>>> AuthoredPartsByWeaponAndSlot =
            new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    WeaponWorkshopIds.Pistol, new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "pistol.slot.barrel", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pistol.barrel.standard", "pistol.barrel.extended", "pistol.barrel.comp" } },
                        { "pistol.slot.magazine", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pistol.magazine.standard", "pistol.magazine.extended", "pistol.magazine.drum" } },
                        { "pistol.slot.grip", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pistol.grip.standard", "pistol.grip.tactical", "pistol.grip.match" } },
                        { "pistol.slot.slide", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "pistol.slide.standard", "pistol.slide.lightweight", "pistol.slide.heavy" } }
                    }
                },
                {
                    WeaponWorkshopIds.Rifle, new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "rifle.slot.barrel", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rifle.barrel.standard", "rifle.barrel.long", "rifle.barrel.short", "rifle.barrel.extended" } },
                        { "rifle.slot.magazine", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rifle.magazine.standard", "rifle.magazine.extended", "rifle.magazine.drum" } },
                        { "rifle.slot.grip", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rifle.grip.standard", "rifle.grip.angled", "rifle.grip.vertical" } },
                        { "rifle.slot.stock", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "rifle.stock.standard", "rifle.stock.heavy", "rifle.stock.light" } }
                    }
                },
                {
                    WeaponWorkshopIds.SMG, new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "smg.slot.barrel", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "smg.barrel.standard", "smg.barrel.long", "smg.barrel.suppressed" } },
                        { "smg.slot.magazine", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "smg.magazine.standard", "smg.magazine.extended", "smg.magazine.drum" } },
                        { "smg.slot.grip", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "smg.grip.standard", "smg.grip.ergonomic", "smg.grip.vertical" } },
                        { "smg.slot.action", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "smg.action.standard", "smg.action.rapid", "smg.action.burst" } }
                    }
                },
                {
                    WeaponWorkshopIds.Shotgun, new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "shotgun.slot.barrel", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shotgun.barrel.standard", "shotgun.barrel.choke", "shotgun.barrel.sawedoff" } },
                        { "shotgun.slot.feed", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shotgun.feed.standard", "shotgun.feed.extendedtube", "shotgun.feed.magfed" } },
                        { "shotgun.slot.stock", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shotgun.stock.standard", "shotgun.stock.tactical", "shotgun.stock.nostock" } },
                        { "shotgun.slot.action", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "shotgun.action.standard", "shotgun.action.heavy", "shotgun.action.hairtrigger" } }
                    }
                },
                {
                    WeaponWorkshopIds.Launcher, new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "launcher.slot.tube", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "launcher.tube.standard", "launcher.tube.rifled", "launcher.tube.short" } },
                        { "launcher.slot.drum", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "launcher.drum.standard", "launcher.drum.highcap", "launcher.drum.lightweight" } },
                        { "launcher.slot.handles", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "launcher.handles.standard", "launcher.handles.dual", "launcher.handles.ergonomic" } },
                        { "launcher.slot.action", new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "launcher.action.standard", "launcher.action.hairtrigger", "launcher.action.heavy" } }
                    }
                }
            };

        // ── Default Base Parts (inherently free/owned with weapon platform) ──
        private static readonly HashSet<string> DefaultPartIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Pistol
            "pistol.barrel.standard",
            "pistol.magazine.standard",
            "pistol.grip.standard",
            "pistol.slide.standard",
            // Rifle
            "rifle.barrel.standard",
            "rifle.magazine.standard",
            "rifle.grip.standard",
            "rifle.stock.standard",
            // SMG
            "smg.barrel.standard",
            "smg.magazine.standard",
            "smg.grip.standard",
            "smg.action.standard",
            // Shotgun
            "shotgun.barrel.standard",
            "shotgun.feed.standard",
            "shotgun.stock.standard",
            "shotgun.action.standard",
            // Launcher
            "launcher.tube.standard",
            "launcher.drum.standard",
            "launcher.handles.standard",
            "launcher.action.standard"
        };

        public static bool IsValidPart(string weaponId, string slotId, string partId, IWeaponCatalog catalog = null)
        {
            if (string.IsNullOrWhiteSpace(partId)) return false;

            catalog = catalog ?? ActiveCatalog;
            if (catalog != null)
            {
                if (!string.IsNullOrWhiteSpace(weaponId) && !catalog.TryGetPlatform(weaponId, out _))
                {
                    return false;
                }

                if (!catalog.TryGetPart(partId, out var spec) || spec == null)
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(slotId) && !string.Equals(spec.SlotId, slotId, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(weaponId))
                {
                    var slotParts = catalog.GetPartsForSlot(weaponId, slotId);
                    if (slotParts != null && slotParts.Count > 0)
                    {
                        bool found = false;
                        for (int i = 0; i < slotParts.Count; i++)
                        {
                            if (slotParts[i] != null && string.Equals(slotParts[i].PartId, partId, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found) return false;
                    }
                }

                return true;
            }

            // Fallback validation against authored parts
            if (!string.IsNullOrWhiteSpace(weaponId))
            {
                if (AuthoredPartsByWeaponAndSlot.TryGetValue(weaponId, out var slots))
                {
                    if (!string.IsNullOrWhiteSpace(slotId))
                    {
                        if (slots.TryGetValue(slotId, out var parts))
                        {
                            return parts.Contains(partId);
                        }
                        return false;
                    }

                    foreach (var parts in slots.Values)
                    {
                        if (parts.Contains(partId)) return true;
                    }
                    return false;
                }
                return false;
            }

            foreach (var slots in AuthoredPartsByWeaponAndSlot.Values)
            {
                foreach (var parts in slots.Values)
                {
                    if (parts.Contains(partId)) return true;
                }
            }

            return false;
        }

        public static bool IsDefaultPart(string partId, string weaponId = null, string slotId = null, IWeaponCatalog catalog = null)
        {
            if (string.IsNullOrWhiteSpace(partId)) return false;

            catalog = catalog ?? ActiveCatalog;
            if (catalog != null && !string.IsNullOrWhiteSpace(weaponId) && catalog.TryGetPlatform(weaponId, out var platform))
            {
                var defaultBuild = platform.CreateDefaultBuild();
                if (defaultBuild?.Selections != null)
                {
                    foreach (var kvp in defaultBuild.Selections)
                    {
                        if ((string.IsNullOrWhiteSpace(slotId) || string.Equals(kvp.Key, slotId, StringComparison.OrdinalIgnoreCase))
                            && string.Equals(kvp.Value, partId, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }

            return DefaultPartIds.Contains(partId);
        }

        public static CraftingRecipe GetPartRecipe(string partId)
        {
            if (string.IsNullOrEmpty(partId) || IsDefaultPart(partId))
            {
                return new CraftingRecipe(partId ?? string.Empty, scrapCost: 0, alloyCost: 0, coreCost: 0);
            }

            string lower = partId.ToLowerInvariant();

            // Tier 3: Rare / High-tech / Powerful upgrades (Requires Core)
            if (lower.Contains("drum") || lower.Contains("plasma") || lower.Contains("explosive") ||
                lower.Contains("heavy") || lower.Contains("overcharged") || lower.Contains("cluster"))
            {
                return new CraftingRecipe(partId, scrapCost: 15, alloyCost: 5, coreCost: 1);
            }

            // Tier 2: Mid-tier upgrades (Requires Alloy)
            if (lower.Contains("extended") || lower.Contains("precision") || lower.Contains("burst") ||
                lower.Contains("marksman") || lower.Contains("suppressor") || lower.Contains("recoil"))
            {
                return new CraftingRecipe(partId, scrapCost: 8, alloyCost: 2, coreCost: 0);
            }

            // Tier 1: Basic component upgrades (Scrap only)
            return new CraftingRecipe(partId, scrapCost: 5, alloyCost: 0, coreCost: 0);
        }
    }
}
