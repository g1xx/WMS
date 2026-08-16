namespace Warehouse.Application.Services
{
    public interface IRouteOptimizerService
    {
        // Reorders items into a walking-optimized warehouse path: aisles (row / 2)
        // are visited in descending order, alternating the section sort direction
        // each aisle (serpentine). Items whose selected barcode is null/malformed
        // are left in place at the end, since they can't be routed.
        List<T> OptimizeRoute<T>(IEnumerable<T> items, Func<T, string?> locationBarcodeSelector);
    }
}
