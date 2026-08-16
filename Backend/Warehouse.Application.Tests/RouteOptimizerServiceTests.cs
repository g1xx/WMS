using FluentAssertions;
using Warehouse.Application.Services;

namespace Warehouse.Application.Tests;

public class RouteOptimizerServiceTests
{
    private record RoutableItem(string? LocationBarcode);

    private readonly RouteOptimizerService _sut = new();

    [Fact]
    public void OptimizeRoute_MixedAislesAndSections_ProducesPerfectSerpentinePath()
    {
        // Arrange: three aisles (row / 2) in sector "r", floor "3" of warehouse "m".
        // Aisle 20 = rows 40/41, aisle 16 = rows 32/33, aisle 10 = rows 20/21.
        // Sections are scrambled on purpose to prove the sort — not just pass-through.
        var items = new List<RoutableItem>
        {
            new("mr34000501a"), // aisle 20, section 5, row 40
            new("mr33300201a"), // aisle 16, section 2, row 33
            new("mr32100601a"), // aisle 10, section 6, row 21
            new("mr34100301a"), // aisle 20, section 3, row 41
            new("mr34100101b"), // aisle 20, section 1, row 41
            new("mr33200801a"), // aisle 16, section 8, row 32
            new("mr34000101a"), // aisle 20, section 1, row 40
            new("mr32000101a"), // aisle 10, section 1, row 20
            new("mr33300401b"), // aisle 16, section 4, row 33
        };

        // Act
        var result = _sut.OptimizeRoute(items, i => i.LocationBarcode);

        // Assert: aisle 20 has the highest row numbers, so it's visited first, walking
        // UP (sections ascending). Aisle 16 is visited next, walking DOWN (sections
        // descending). Aisle 10 is visited last, walking UP again. Within a tied
        // aisle+section (the two facing rows), the near row (40) sorts before the far
        // row (41).
        result.Select(i => i.LocationBarcode).Should().Equal(
            "mr34000101a", // aisle 20, section 1, row 40
            "mr34100101b", // aisle 20, section 1, row 41
            "mr34100301a", // aisle 20, section 3
            "mr34000501a", // aisle 20, section 5
            "mr33200801a", // aisle 16, section 8
            "mr33300401b", // aisle 16, section 4
            "mr33300201a", // aisle 16, section 2
            "mr32000101a", // aisle 10, section 1
            "mr32100601a"  // aisle 10, section 6
        );
    }

    [Fact]
    public void OptimizeRoute_SingleAisle_SortsSectionsAscending()
    {
        // A single aisle is always the "first" one visited, so it should walk UP.
        var items = new List<RoutableItem>
        {
            new("mr34000901a"),
            new("mr34000101a"),
            new("mr34100501a"),
        };

        var result = _sut.OptimizeRoute(items, i => i.LocationBarcode);

        result.Select(i => i.LocationBarcode).Should().Equal(
            "mr34000101a",
            "mr34100501a",
            "mr34000901a"
        );
    }

    [Fact]
    public void OptimizeRoute_UnparsableOrNullBarcodes_AreAppendedAfterRoutedItemsInOriginalOrder()
    {
        var items = new List<RoutableItem>
        {
            new(null),
            new("mr34000501a"),
            new("too-short"),
            new("mr34000101a"),
        };

        var result = _sut.OptimizeRoute(items, i => i.LocationBarcode);

        result.Select(i => i.LocationBarcode).Should().Equal(
            "mr34000101a",
            "mr34000501a",
            null,
            "too-short"
        );
    }

    [Fact]
    public void OptimizeRoute_EmptyList_ReturnsEmptyList()
    {
        var result = _sut.OptimizeRoute(new List<RoutableItem>(), i => i.LocationBarcode);

        result.Should().BeEmpty();
    }
}
