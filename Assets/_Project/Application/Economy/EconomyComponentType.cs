using System;

namespace Application.Economy
{
    public enum EconomyComponentType
    {
        Scrap = 0,
        Alloy = 1,
        Core = 2
    }

    public static class EconomyComponentExtensions
    {
        public const string ScrapId = "scrap";
        public const string AlloyId = "alloy";
        public const string CoreId = "core";

        public static string ToId(this EconomyComponentType type)
        {
            switch (type)
            {
                case EconomyComponentType.Scrap: return ScrapId;
                case EconomyComponentType.Alloy: return AlloyId;
                case EconomyComponentType.Core: return CoreId;
                default: return ScrapId;
            }
        }

        public static bool TryParseId(string id, out EconomyComponentType type)
        {
            if (string.Equals(id, ScrapId, StringComparison.OrdinalIgnoreCase))
            {
                type = EconomyComponentType.Scrap;
                return true;
            }
            if (string.Equals(id, AlloyId, StringComparison.OrdinalIgnoreCase))
            {
                type = EconomyComponentType.Alloy;
                return true;
            }
            if (string.Equals(id, CoreId, StringComparison.OrdinalIgnoreCase))
            {
                type = EconomyComponentType.Core;
                return true;
            }

            type = EconomyComponentType.Scrap;
            return false;
        }

        public static string GetDisplayName(this EconomyComponentType type)
        {
            switch (type)
            {
                case EconomyComponentType.Scrap: return "Scrap";
                case EconomyComponentType.Alloy: return "Alloy";
                case EconomyComponentType.Core: return "Energy Core";
                default: return type.ToString();
            }
        }
    }
}
