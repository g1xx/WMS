using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Warehouse.Api.Controllers;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Application.Services;
using Warehouse.Domain;

namespace Warehouse.Api.Tests;

public class OrdersControllerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IOrderAllocationService> _allocationMock = new();
    private readonly OrdersController _sut;

    public OrdersControllerTests()
    {
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Products).Returns(_productRepositoryMock.Object);
        _allocationMock.Setup(a => a.AllocateOrderAsync(It.IsAny<Guid>())).ReturnsAsync((true, (string?)null));

        _sut = new OrdersController(_unitOfWorkMock.Object, _allocationMock.Object);
    }

    [Fact]
    public async Task CreateOrder_EmptyItems_Returns400AndPersistsNothing()
    {
        // BUG-02: an order with no lines used to be saved and then "allocated" straight into
        // Picking with no pick tasks, where nothing could ever move it on again.
        var dto = new OrderCreateDto { CustomerName = "QA", DestinationAddress = "QA street 1", Items = new() };

        var result = await _sut.CreateOrder(dto);

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().Be("An order must contain at least one item.");
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_UnknownProductIds_Returns400NamingEveryUnknownId()
    {
        // BUG-03: an unknown productId used to reach SaveChanges, violate
        // FK_OrderItems_Products_ProductId and surface as a 500 with no usable message.
        var knownId = Guid.NewGuid();
        var unknownA = Guid.NewGuid();
        var unknownB = Guid.NewGuid();
        _productRepositoryMock
            .Setup(r => r.GetExistingIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new HashSet<Guid> { knownId });

        var dto = new OrderCreateDto
        {
            CustomerName = "QA",
            DestinationAddress = "QA street 1",
            Items = new()
            {
                new() { ProductId = knownId, RequiredQuantity = 1 },
                new() { ProductId = unknownA, RequiredQuantity = 1 },
                new() { ProductId = unknownB, RequiredQuantity = 2 },
            }
        };

        var result = await _sut.CreateOrder(dto);

        var message = result.Should().BeOfType<BadRequestObjectResult>().Which.Value.Should().BeOfType<string>().Subject;
        message.Should().StartWith("Unknown product id(s):")
            .And.Contain(unknownA.ToString())
            .And.Contain(unknownB.ToString())
            .And.NotContain(knownId.ToString());
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task CreateOrder_AllProductsExist_Returns201()
    {
        var productId = Guid.NewGuid();
        _productRepositoryMock
            .Setup(r => r.GetExistingIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(new HashSet<Guid> { productId });

        var dto = new OrderCreateDto
        {
            CustomerName = "QA",
            DestinationAddress = "QA street 1",
            Items = new() { new() { ProductId = productId, RequiredQuantity = 1 } }
        };

        var result = await _sut.CreateOrder(dto);

        result.Should().BeOfType<CreatedAtActionResult>();
        _orderRepositoryMock.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
    }
}
