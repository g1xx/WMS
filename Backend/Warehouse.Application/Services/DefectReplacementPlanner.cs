using Warehouse.Domain;

namespace Warehouse.Application.Services
{
    public class DefectReplacementPlanner : IDefectReplacementPlanner
    {
        public DefectReplacementPlan Plan(IEnumerable<Stock> candidateStocks, int quantityNeeded)
        {
            var remaining = quantityNeeded;
            var picks = new List<(Stock Stock, string ZoneCode, int Quantity)>();

            foreach (var stock in candidateStocks)
            {
                if (remaining == 0) break;

                var take = Math.Min(remaining, stock.AvailableQuantity);
                if (take <= 0) continue;

                picks.Add((stock, stock.Location!.ZoneCode, take));
                remaining -= take;
            }

            return new DefectReplacementPlan(picks, remaining);
        }
    }
}
