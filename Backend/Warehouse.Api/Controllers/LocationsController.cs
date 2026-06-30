using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain;
using Warehouse.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace Warehouse.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LocationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Location>>> GetLocations()
        {
            var locations = await _context.Locations
                .AsNoTracking()
                .OrderBy(l => l.Aisle)
                .ThenBy(l => l.Rack)
                .ToListAsync();

            return Ok(locations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Location>> GetLocation(Guid id)
        {
            var location = await _context.Locations.FindAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            return Ok(location);
        }

        [HttpPost]
        public async Task<ActionResult<Location>> CreateLocation(Location location)
        {
            location.AddressBarcode = $"{location.WarehouseCode}{location.Sector}{location.Floor}{location.Aisle}{location.Rack}{location.Level}{location.Position}".ToLower();
            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, location);
        }
    }
}
