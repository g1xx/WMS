using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Warehouse.Application.DTOs;
using Warehouse.Application.Interfaces;
using Warehouse.Domain;

namespace Warehouse.Api.Controllers
{
    // Had no authorization at all until now — the whole warehouse layout was readable
    // anonymously, and the create/seed actions writable. See ProductsController for why
    // this is AnyStaff rather than a bare [Authorize].
    [Authorize(Roles = RoleNames.AnyStaff)]
    [ApiController]
    [Route("api/[controller]")]
    public class LocationsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public LocationsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetLocations()
        {
            var locations = await _unitOfWork.Locations.GetAllOrderedAsync();

            var responseDtos = locations.Select(l => new LocationResponseDto
            {
                Id = l.Id,
                Type = l.Type.ToString(),
                AddressBarcode = l.AddressBarcode,
                WarehouseCode = l.WarehouseCode,
                Sector = l.Sector,
                Floor = l.Floor,
                Aisle = l.Aisle,
                Rack = l.Rack,
                Level = l.Level,
                Position = l.Position
            }).ToList();

            return Ok(responseDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LocationResponseDto>> GetLocation(Guid id)
        {
            var location = await _unitOfWork.Locations.GetByIdAsync(id);

            if (location == null)
            {
                return NotFound();
            }

            var responseDto = new LocationResponseDto
            {
                Id = location.Id,
                Type = location.Type.ToString(),
                AddressBarcode = location.AddressBarcode,
                WarehouseCode = location.WarehouseCode,
                Sector = location.Sector,
                Floor = location.Floor,
                Aisle = location.Aisle,
                Rack = location.Rack,
                Level = location.Level,
                Position = location.Position
            };

            return Ok(responseDto);
        }

        [HttpPost]
        public async Task<ActionResult<LocationResponseDto>> CreateLocation([FromBody] LocationCreateDto createDto)
        {
            var location = new Location
            {
                Type = createDto.Type,
                WarehouseCode = createDto.WarehouseCode,
                Sector = createDto.Sector,
                Floor = createDto.Floor,
                Aisle = createDto.Aisle,
                Rack = createDto.Rack,
                Level = createDto.Level,
                Position = createDto.Position
            };

            location.AddressBarcode = $"{location.WarehouseCode}{location.Sector}{location.Floor}{location.Aisle}{location.Rack}{location.Level}{location.Position}".ToLower();

            _unitOfWork.Locations.Add(location);
            await _unitOfWork.SaveChangesAsync();

            var responseDto = new LocationResponseDto
            {
                Id = location.Id,
                Type = location.Type.ToString(),
                AddressBarcode = location.AddressBarcode,
                WarehouseCode = location.WarehouseCode,
                Sector = location.Sector,
                Floor = location.Floor,
                Aisle = location.Aisle,
                Rack = location.Rack,
                Level = location.Level,
                Position = location.Position
            };

            return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, responseDto);
        }

        [HttpPost("bulk")]
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> CreateLocations([FromBody] IEnumerable<LocationCreateDto> createDtos)
        {
            // Validation: make sure something was actually passed in
            if (createDtos == null || !createDtos.Any())
            {
                return BadRequest("The list of locations cannot be empty.");
            }

            var locationsToSave = new List<Location>();

            // 1. Map and prepare every location
            foreach (var dto in createDtos)
            {
                var location = new Location
                {
                    Id = Guid.NewGuid(), // Generate the ID up front so it can be used in the response
                    Type = dto.Type,
                    WarehouseCode = dto.WarehouseCode,
                    Sector = dto.Sector,
                    Floor = dto.Floor,
                    Aisle = dto.Aisle,
                    Rack = dto.Rack,
                    Level = dto.Level,
                    Position = dto.Position
                };

                // Barcode generation
                location.AddressBarcode = $"{location.WarehouseCode}{location.Sector}{location.Floor}{location.Aisle}{location.Rack}{location.Level}{location.Position}".ToLower();

                locationsToSave.Add(location);
            }

            // 2. Bulk insert
            _unitOfWork.Locations.AddRange(locationsToSave);
            await _unitOfWork.SaveChangesAsync();

            // 3. Build the list of DTOs for the client response
            var responseDtos = locationsToSave.Select(l => new LocationResponseDto
            {
                Id = l.Id,
                Type = l.Type.ToString(),
                AddressBarcode = l.AddressBarcode,
                WarehouseCode = l.WarehouseCode,
                Sector = l.Sector,
                Floor = l.Floor,
                Aisle = l.Aisle,
                Rack = l.Rack,
                Level = l.Level,
                Position = l.Position
            }).ToList();

            // Return 201 Created with the list of created objects.
            // The route points at GetLocations (fetch all), since a collection has no single Id.
            return CreatedAtAction(nameof(GetLocations), null, responseDtos);
        }

        [HttpPost("seed-mass-locations")]
        public async Task<IActionResult> SeedMassLocations()
        {
            var locations = new List<Location>();

            // 1. Zone MP: from mp30100101a to mp38001107c
            // Aisles: 1-80, Racks: 1-11, Levels: 1-7, Positions: a,b,c
            locations.AddRange(GenerateZone("m", "p", 3, 1, 80, 1, 11, 1, 7, new[] { "a", "b", "c" }));

            // 2. Zone MR: from mr30100101a to mr35400703c
            // Aisles: 1-54, Racks: 1-7, Levels: 1-3, Positions: a,b,c
            locations.AddRange(GenerateZone("m", "r", 3, 1, 54, 1, 7, 1, 3, new[] { "a", "b", "c" }));

            // 3. Zone MG: from mg30100101a to mg34300803c
            // Aisles: 1-43, Racks: 1-8, Levels: 1-3, Positions: a,b,c
            locations.AddRange(GenerateZone("m", "g", 3, 1, 43, 1, 8, 1, 3, new[] { "a", "b", "c" }));

            // Save all ~25,000 records to the database in a single transaction
            _unitOfWork.Locations.AddRange(locations);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = $"Successfully created {locations.Count} locations!" });
        }

        // Helper method for generation
        private List<Location> GenerateZone(string wh, string sector, int floor,
            int aisleStart, int aisleEnd, int rackStart, int rackEnd,
            int levelStart, int levelEnd, string[] positions)
        {
            var list = new List<Location>();

            for (int a = aisleStart; a <= aisleEnd; a++)
            {
                for (int r = rackStart; r <= rackEnd; r++)
                {
                    for (int l = levelStart; l <= levelEnd; l++)
                    {
                        foreach (var pos in positions)
                        {
                            // Pad the numbers with zeros (01, 001, 01)
                            string aisleStr = a.ToString("D2");
                            string rackStr = r.ToString("D3");
                            string levelStr = l.ToString("D2");

                            // Assemble the barcode
                            string barcode = $"{wh}{sector}{floor}{aisleStr}{rackStr}{levelStr}{pos}";

                            list.Add(new Location
                            {
                                Id = Guid.NewGuid(),
                                Type = LocationType.Shelf,
                                WarehouseCode = wh,
                                Sector = sector,
                                Floor = floor,
                                Aisle = aisleStr,
                                Rack = rackStr,
                                Level = levelStr,
                                Position = pos,
                                AddressBarcode = barcode
                            });
                        }
                    }
                }
            }

            return list;
        }
    }
}
