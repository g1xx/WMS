using Warehouse.Domain;

namespace Warehouse.Application.Services
{
    // What to reserve where, computed from an already-fetched, already-ordered list
    // of candidate stocks. Greedily fills from the front of the list until the
    // quantity needed is covered or the candidates run out.
    public record DefectReplacementPlan(List<(Stock Stock, string ZoneCode, int Quantity)> Picks, int ShortageQuantity);

    public interface IDefectReplacementPlanner
    {
        DefectReplacementPlan Plan(IEnumerable<Stock> candidateStocks, int quantityNeeded);
    }
}
