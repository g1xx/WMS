using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Api.DTOs;
using Warehouse.Domain;
using Warehouse.Infrastructure;

namespace Warehouse.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContainersController : ControllerBase
{
    private readonly AppDbContext _context;

    public ContainersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Container>>> GetContainers()
    {
        var containers = await _context.Containers
            .Include(c => c.Location)
            .AsNoTracking() 
            .ToListAsync();

        return Ok(containers);
    }

    [HttpGet("free")]
    public async Task<ActionResult<IEnumerable<Container>>> GetFreeContainers()
    {
        var containers = await _context.Containers
            .Where(c => c.Status == ContainerStatus.New)
            .Include(c => c.Location)
            .AsNoTracking()
            .ToListAsync();

        return Ok(containers);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Container>> GetContainer(Guid id)
    {
        var container = await _context.Containers
            .Include(c => c.Location)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (container == null)
        {
            return NotFound($"Container with {id} not found"); 
        }

        return Ok(container);
    }

    [HttpGet("by-barcode/{barcode}")]
    public async Task<ActionResult<Container>> GetContainerByBarcode(string barcode)
    {
        var container = await _context.Containers
            .Include(c => c.Location)
            .FirstOrDefaultAsync(c => c.Barcode == barcode);

        if (container == null)
        {
            return NotFound();
        }

        return Ok(container);
    }

    [HttpPost]
    public async Task<ActionResult<Container>> CreateContainer(Container container)
    {
        _context.Containers.Add(container);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetContainers), new { id = container.Id }, container);
    }

    [HttpPost("move")]
    public async Task<ActionResult> MoveContainer(ContainerMoveDto dto)
    {
        // 1. Look up the container
        var container = await _context.Containers
            .FirstOrDefaultAsync(c => c.Barcode == dto.ContainerBarcode);

        if (container == null)
        {
            return NotFound($"Container {dto.ContainerBarcode} was not found.");
        }

        // 2. Look up the destination location
        var destination = await _context.Locations
            .FirstOrDefaultAsync(l => l.AddressBarcode == dto.DestinationLocationBarcode);

        if (destination == null)
        {
            return BadRequest($"Destination location {dto.DestinationLocationBarcode} does not exist.");
        }

        // 3. Physically move the container in the database
        container.LocationId = destination.Id;

        await _context.SaveChangesAsync();

        return Ok($"Container {dto.ContainerBarcode} successfully moved to {dto.DestinationLocationBarcode}.");
    }
}