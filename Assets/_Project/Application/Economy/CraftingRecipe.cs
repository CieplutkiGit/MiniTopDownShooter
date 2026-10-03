using System;

namespace Application.Economy
{
    public sealed class CraftingRecipe
    {
        public string PartId { get; }
        public int ScrapCost { get; }
        public int AlloyCost { get; }
        public int CoreCost { get; }

        public CraftingRecipe(string partId, int scrapCost, int alloyCost = 0, int coreCost = 0)
        {
            PartId = partId ?? throw new ArgumentNullException(nameof(partId));
            ScrapCost = Math.Max(0, scrapCost);
            AlloyCost = Math.Max(0, alloyCost);
            CoreCost = Math.Max(0, coreCost);
        }

        public bool IsFree => ScrapCost == 0 && AlloyCost == 0 && CoreCost == 0;

        public bool CanAfford(int scrap, int alloy, int core)
        {
            return scrap >= ScrapCost && alloy >= AlloyCost && core >= CoreCost;
        }

        public override string ToString()
        {
            return $"Recipe for {PartId}: {ScrapCost} Scrap, {AlloyCost} Alloy, {CoreCost} Core";
        }
    }
}
