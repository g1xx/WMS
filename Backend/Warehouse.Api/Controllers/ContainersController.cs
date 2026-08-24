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

    // Bulk-seeds fresh, empty totes with realistic warehouse barcodes (e.g.
    // HSOD90001, HSOD90002, ...) — mirrors LocationsController.SeedMassLocations().
    // Safe to re-run: any barcode in the requested range that already exists is
    // skipped rather than re-inserted, so it never collides with the unique index.
    [HttpPost("seed-mass-containers")]
    public async Task<IActionResult> SeedMassContainers([FromQuery] int count = 100, [FromQuery] int startingNumber = 90001)
    {
        if (count <= 0)
            return BadRequest("Count must be greater than zero.");

        const string prefix = "HSOD";

        var candidateBarcodes = Enumerable.Range(0, count)
            .Select(offset => $"{prefix}{startingNumber + offset}")
            .ToList();

        var existingBarcodes = await _unitOfWork.Containers.GetExistingBarcodesAsync(candidateBarcodes);

        var newContainers = candidateBarcodes
            .Where(barcode => !existingBarcodes.Contains(barcode))
            .Select(barcode => new Container
            {
                Barcode = barcode,
                Type = ContainerType.Tote,
                Status = ContainerTransitions.FreeStatus
            })
            .ToList();

        if (newContainers.Count == 0)
            return Ok(new { Message = "All requested container barcodes already exist. Nothing to seed." });

        _unitOfWork.Containers.AddRange(newContainers);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            Message = $"Successfully created {newContainers.Count} container(s).",
            Skipped = candidateBarcodes.Count - newContainers.Count
        });
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
