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
}
