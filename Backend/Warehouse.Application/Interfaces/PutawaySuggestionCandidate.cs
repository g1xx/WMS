using Warehouse.Domain;

namespace Warehouse.Application.Interfaces;

// A candidate destination for a putaway suggestion — one product's Stock row at one
// location, at whatever quantity it currently has (including 0, unlike the old
// GetLocationBarcodesByProductAsync it replaces). PutawayService ranks/filters these
// into SuggestedPutawayLocationDto; this is an intermediate repository-layer shape,
// not the API response itself.
public class PutawaySuggestionCandidate
{
    public Guid LocationId { get; set; }
    public string LocationBarcode { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public string ZoneCode { get; set; } = string.Empty;
    public LocationType LocationType { get; set; }
    public int? MaxDistinctSkus { get; set; }
}
