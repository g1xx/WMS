using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Warehouse.Domain;
using Warehouse.Infrastructure;
using Warehouse.Api.DTOs;

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
        public async Task<ActionResult<IEnumerable<LocationResponseDto>>> GetLocations()
        {
            var locations = await _context.Locations
                .AsNoTracking()
                .OrderBy(l => l.Aisle)
                .ThenBy(l => l.Rack)
                .Select(l => new LocationResponseDto
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
                })
                .ToListAsync();

            return Ok(locations);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LocationResponseDto>> GetLocation(Guid id)
        {
            var location = await _context.Locations.FindAsync(id);

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

            _context.Locations.Add(location);
            await _context.SaveChangesAsync();

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
            // Валидация: проверяем, что нам вообще что-то передали
            if (createDtos == null || !createDtos.Any())
            {
                return BadRequest("Список локаций не может быть пустым.");
            }

            var locationsToSave = new List<Location>();

            // 1. Маппим и подготавливаем каждую локацию
            foreach (var dto in createDtos)
            {
                var location = new Location
                {
                    Id = Guid.NewGuid(), // Генерируем ID заранее, чтобы использовать его для ответа
                    Type = dto.Type,
                    WarehouseCode = dto.WarehouseCode,
                    Sector = dto.Sector,
                    Floor = dto.Floor,
                    Aisle = dto.Aisle,
                    Rack = dto.Rack,
                    Level = dto.Level,
                    Position = dto.Position
                };

                // Генерация штрихкода
                location.AddressBarcode = $"{location.WarehouseCode}{location.Sector}{location.Floor}{location.Aisle}{location.Rack}{location.Level}{location.Position}".ToLower();

                locationsToSave.Add(location);
            }

            // 2. Используем AddRangeAsync для эффективной массовой вставки в EF Core
            await _context.Locations.AddRangeAsync(locationsToSave);
            await _context.SaveChangesAsync();

            // 3. Формируем список DTO для ответа клиенту
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

            // Возвращаем статус 201 Created со списком созданных объектов
            // Маршрут указывает на GetLocations (получение всех), так как для коллекции нет единого Id
            return CreatedAtAction(nameof(GetLocations), null, responseDtos);
        }

        [HttpPost("seed-mass-locations")]
        public async Task<IActionResult> SeedMassLocations()
        {
            var locations = new List<Location>();

            // 1. Зона MP: от mp30100101a до mp38001107c
            // Алли: 1-80, Стеллажи: 1-11, Полки: 1-7, Места: a,b,c
            locations.AddRange(GenerateZone("m", "p", 3, 1, 80, 1, 11, 1, 7, new[] { "a", "b", "c" }));

            // 2. Зона MR: от mr30100101a до mr35400703c
            // Алли: 1-54, Стеллажи: 1-7, Полки: 1-3, Места: a,b,c
            locations.AddRange(GenerateZone("m", "r", 3, 1, 54, 1, 7, 1, 3, new[] { "a", "b", "c" }));

            // 3. Зона MG: от mg30100101a до mg34300803c
            // Алли: 1-43, Стеллажи: 1-8, Полки: 1-3, Места: a,b,c
            locations.AddRange(GenerateZone("m", "g", 3, 1, 43, 1, 8, 1, 3, new[] { "a", "b", "c" }));

            // Сохраняем все ~25 000 записей в базу за одну транзакцию
            await _context.Locations.AddRangeAsync(locations);
            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Успешно создано {locations.Count} локаций!" });
        }

        // Вспомогательный метод для генерации
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
                            // Форматируем числа с нулями (01, 001, 01)
                            string aisleStr = a.ToString("D2");
                            string rackStr = r.ToString("D3");
                            string levelStr = l.ToString("D2");

                            // Склеиваем штрихкод
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