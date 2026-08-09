using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContainersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ContainersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Container>>> GetContainers()
    {
        var containers = await _unitOfWork.Containers.GetAllWithLocationAsync();

        return Ok(containers);
    }

    [HttpGet("free")]
    public async Task<ActionResult<IEnumerable<Container>>> GetFreeContainers()
    {
        var containers = await _unitOfWork.Containers.GetFreeWithLocationAsync();

        return Ok(containers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Container>> GetContainer(Guid id)
    {
        var container = await _unitOfWork.Containers.GetByIdWithLocationAsync(id);

        if (container == null)
        {
            return NotFound($"Container with {id} not found");
        }

        return Ok(container);
    }

    [HttpGet("by-barcode/{barcode}")]
    public async Task<ActionResult<Container>> GetContainerByBarcode(string barcode)
    {
        var container = await _unitOfWork.Containers.GetByBarcodeWithLocationAsync(barcode);

        if (container == null)
        {
            return NotFound();
        }

        return Ok(container);
    }

    [HttpPost]
    public async Task<ActionResult<Container>> CreateContainer(Container container)
    {
        _unitOfWork.Containers.Add(container);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetContainers), new { id = container.Id }, container);
    }

    [HttpPost("move")]
    public async Task<ActionResult> MoveContainer(ContainerMoveDto dto)
    {
        // 1. Look up the container
        var container = await _unitOfWork.Containers.GetByBarcodeAsync(dto.ContainerBarcode);

        if (container == null)
        {
            return NotFound($"Container {dto.ContainerBarcode} was not found.");
        }

        // 2. Look up the destination location
        var destination = await _unitOfWork.Locations.GetByBarcodeAsync(dto.DestinationLocationBarcode);

        if (destination == null)
        {
            return BadRequest($"Destination location {dto.DestinationLocationBarcode} does not exist.");
        }

        // 3. Physically move the container in the database
        container.LocationId = destination.Id;

        await _unitOfWork.SaveChangesAsync();

        return Ok($"Container {dto.ContainerBarcode} successfully moved to {dto.DestinationLocationBarcode}.");
    }
}
