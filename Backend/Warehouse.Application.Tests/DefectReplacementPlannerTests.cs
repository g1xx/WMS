using FluentAssertions;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Application.Tests;

public class DefectReplacementPlannerTests
{
    private readonly DefectReplacementPlanner _sut = new();

    private static Stock BuildStock(string zoneCode, int physicalQuantity, int reservedQuantity = 0)
    {
        var location = new Location
        {
            Id = Guid.NewGuid(),
            AddressBarcode = $"LOC-{Guid.NewGuid():N}",
            WarehouseCode = zoneCode[..1],
            Sector = zoneCode[1..^1],
            Floor = int.Parse(zoneCode[^1..])
        };

        return new Stock
        {
            Id = Guid.NewGuid(),
            LocationId = location.Id,
            Location = location,
            PhysicalQuantity = physicalQuantity,
            ReservedQuantity = reservedQuantity
        };
    }

    [Fact]
    public void Plan_SingleCandidateCoversFullQuantity_ReturnsOnePickAndNoShortage()
    {
        var stock = BuildStock("mp1", physicalQuantity: 10);

        var plan = _sut.Plan(new[] { stock }, quantityNeeded: 5);

        plan.Picks.Should().ContainSingle();
        plan.Picks[0].Stock.Should().Be(stock);
        plan.Picks[0].ZoneCode.Should().Be("mp1");
        plan.Picks[0].Quantity.Should().Be(5);
        plan.ShortageQuantity.Should().Be(0);
    }

    [Fact]
    public void Plan_NoSingleCandidateCoversIt_SpreadsAcrossMultipleCandidatesInOrder()
    {
        var first = BuildStock("mp1", physicalQuantity: 3);
        var second = BuildStock("mp2", physicalQuantity: 10);

        var plan = _sut.Plan(new[] { first, second }, quantityNeeded: 7);

        plan.Picks.Should().HaveCount(2);
        plan.Picks[0].Stock.Should().Be(first);
        plan.Picks[0].Quantity.Should().Be(3);
        plan.Picks[1].Stock.Should().Be(second);
        plan.Picks[1].Quantity.Should().Be(4);
        plan.ShortageQuantity.Should().Be(0);
    }

    [Fact]
    public void Plan_StopsOnceQuantityIsCovered_LeavesLaterCandidatesUntouched()
    {
        var first = BuildStock("mp1", physicalQuantity: 10);
        var second = BuildStock("mp2", physicalQuantity: 10);

        var plan = _sut.Plan(new[] { first, second }, quantityNeeded: 4);

        plan.Picks.Should().ContainSingle();
        plan.Picks[0].Stock.Should().Be(first);
        plan.Picks[0].Quantity.Should().Be(4);
    }

    [Fact]
    public void Plan_TotalAvailableAcrossAllCandidatesIsInsufficient_TakesEverythingAndReportsShortage()
    {
        var first = BuildStock("mp1", physicalQuantity: 2);
        var second = BuildStock("mp2", physicalQuantity: 3);

        var plan = _sut.Plan(new[] { first, second }, quantityNeeded: 10);

        plan.Picks.Should().HaveCount(2);
        plan.Picks[0].Quantity.Should().Be(2);
        plan.Picks[1].Quantity.Should().Be(3);
        plan.ShortageQuantity.Should().Be(5);
    }

    [Fact]
    public void Plan_NoCandidates_ReturnsNoPicksAndFullShortage()
    {
        var plan = _sut.Plan(Enumerable.Empty<Stock>(), quantityNeeded: 6);

        plan.Picks.Should().BeEmpty();
        plan.ShortageQuantity.Should().Be(6);
    }

    [Fact]
    public void Plan_QuantityNeededIsZero_ReturnsNoPicksAndNoShortage()
    {
        var stock = BuildStock("mp1", physicalQuantity: 10);

        var plan = _sut.Plan(new[] { stock }, quantityNeeded: 0);

        plan.Picks.Should().BeEmpty();
        plan.ShortageQuantity.Should().Be(0);
    }

    [Fact]
    public void Plan_CandidateWithNothingAvailable_IsSkippedWithoutStoppingTheScan()
    {
        // Reserved quantity can eat all of a candidate's physical stock — AvailableQuantity
        // is 0, so it must be skipped in favor of the next candidate, not treated as
        // "nothing left to plan."
        var emptyAfterReservation = BuildStock("mp1", physicalQuantity: 5, reservedQuantity: 5);
        var usable = BuildStock("mp2", physicalQuantity: 5);

        var plan = _sut.Plan(new[] { emptyAfterReservation, usable }, quantityNeeded: 4);

        plan.Picks.Should().ContainSingle();
        plan.Picks[0].Stock.Should().Be(usable);
        plan.Picks[0].Quantity.Should().Be(4);
        plan.ShortageQuantity.Should().Be(0);
    }
}
