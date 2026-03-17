using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankManager.Data;
using BloodBankManager.Models;

namespace BloodBankManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BloodInventoryController : ControllerBase
    {
        private readonly BloodBankContext _context;
        private readonly ILogger<BloodInventoryController> _logger;

        public BloodInventoryController(BloodBankContext context, ILogger<BloodInventoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InventoryResponseDto>>> GetInventory()
        {
            try
            {
                var inventory = await _context.BloodInventories
                    .Include(i => i.BloodType)
                    .ToListAsync();

                var response = inventory.Select(i => new InventoryResponseDto
                {
                    Id = i.Id,
                    BloodType = i.BloodType.TypeName.ToString(),
                    TotalUnits = i.TotalUnits,
                    TotalVolume = i.TotalVolume,
                    AvailableUnits = i.AvailableUnits,
                    ReservedUnits = i.ReservedUnits,
                    LowStockThreshold = i.LowStockThreshold,
                    IsLowStock = i.TotalVolume < i.LowStockThreshold,
                    LastUpdated = i.LastUpdated
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting inventory: {ex.Message}");
                return StatusCode(500, new { message = "Error getting inventory" });
            }
        }

        [HttpGet("{bloodTypeId}")]
        public async Task<ActionResult<InventoryResponseDto>> GetInventoryByBloodType(int bloodTypeId)
        {
            try
            {
                var inventory = await _context.BloodInventories
                    .Include(i => i.BloodType)
                    .FirstOrDefaultAsync(i => i.BloodTypeId == bloodTypeId);

                if (inventory == null)
                {
                    return NotFound(new { message = "Inventory not found" });
                }

                var response = new InventoryResponseDto
                {
                    Id = inventory.Id,
                    BloodType = inventory.BloodType.TypeName.ToString(),
                    TotalUnits = inventory.TotalUnits,
                    TotalVolume = inventory.TotalVolume,
                    AvailableUnits = inventory.AvailableUnits,
                    ReservedUnits = inventory.ReservedUnits,
                    LowStockThreshold = inventory.LowStockThreshold,
                    IsLowStock = inventory.TotalVolume < inventory.LowStockThreshold,
                    LastUpdated = inventory.LastUpdated
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting inventory: {ex.Message}");
                return StatusCode(500, new { message = "Error getting inventory" });
            }
        }

        [HttpGet("low-stock/alert")]
        public async Task<ActionResult<IEnumerable<InventoryResponseDto>>> GetLowStockAlerts()
        {
            try
            {
                var lowStockInventories = await _context.BloodInventories
                    .Include(i => i.BloodType)
                    .Where(i => i.TotalVolume < i.LowStockThreshold)
                    .ToListAsync();

                var response = lowStockInventories.Select(i => new InventoryResponseDto
                {
                    Id = i.Id,
                    BloodType = i.BloodType.TypeName.ToString(),
                    TotalUnits = i.TotalUnits,
                    TotalVolume = i.TotalVolume,
                    AvailableUnits = i.AvailableUnits,
                    ReservedUnits = i.ReservedUnits,
                    LowStockThreshold = i.LowStockThreshold,
                    IsLowStock = true,
                    LastUpdated = i.LastUpdated
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting low stock alerts: {ex.Message}");
                return StatusCode(500, new { message = "Error getting low stock alerts" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<InventoryResponseDto>> CreateOrUpdateInventory([FromBody] CreateBloodInventoryDto dto)
        {
            try
            {
                var bloodType = await _context.BloodTypes.FindAsync(dto.BloodTypeId);
                if (bloodType == null)
                {
                    return BadRequest(new { message = "Blood type not found" });
                }

                var inventory = await _context.BloodInventories
                    .FirstOrDefaultAsync(i => i.BloodTypeId == dto.BloodTypeId);

                if (inventory != null)
                {
                    // Update existing
                    inventory.TotalUnits += dto.TotalUnits;
                    inventory.AvailableUnits += dto.AvailableUnits;
                    inventory.ReservedUnits += dto.ReservedUnits;
                    inventory.TotalVolume += dto.TotalVolume;
                    inventory.LowStockThreshold = dto.LowStockThreshold;
                    inventory.LastUpdated = DateTime.UtcNow;
                }
                else
                {
                    // Create new
                    inventory = new BloodInventory
                    {
                        BloodTypeId = dto.BloodTypeId,
                        TotalUnits = dto.TotalUnits,
                        AvailableUnits = dto.AvailableUnits,
                        ReservedUnits = dto.ReservedUnits,
                        TotalVolume = dto.TotalVolume,
                        LowStockThreshold = dto.LowStockThreshold,
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.BloodInventories.Add(inventory);
                }

                await _context.SaveChangesAsync();

                var response = new InventoryResponseDto
                {
                    Id = inventory.Id,
                    BloodType = bloodType.TypeName.ToString(),
                    TotalUnits = inventory.TotalUnits,
                    TotalVolume = inventory.TotalVolume,
                    AvailableUnits = inventory.AvailableUnits,
                    ReservedUnits = inventory.ReservedUnits,
                    LowStockThreshold = inventory.LowStockThreshold,
                    IsLowStock = inventory.TotalVolume < inventory.LowStockThreshold,
                    LastUpdated = inventory.LastUpdated
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating/updating inventory: {ex.Message}");
                return StatusCode(500, new { message = $"Error: {ex.Message}" });
            }
        }

        [HttpPut("{bloodTypeId}/threshold")]
        public async Task<IActionResult> UpdateLowStockThreshold(int bloodTypeId, [FromBody] UpdateThresholdDto dto)
        {
            try
            {
                var inventory = await _context.BloodInventories
                    .FirstOrDefaultAsync(i => i.BloodTypeId == bloodTypeId);

                if (inventory == null)
                {
                    return NotFound(new { message = "Inventory not found" });
                }

                inventory.LowStockThreshold = dto.NewThreshold;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Threshold updated successfully", inventory });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating threshold: {ex.Message}");
                return StatusCode(500, new { message = "Error updating threshold" });
            }
        }
    }

    public class InventoryResponseDto
    {
        public int Id { get; set; }
        public string? BloodType { get; set; }
        public int TotalUnits { get; set; }
        public double TotalVolume { get; set; }
        public int AvailableUnits { get; set; }
        public int ReservedUnits { get; set; }
        public double LowStockThreshold { get; set; }
        public bool IsLowStock { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class UpdateThresholdDto
    {
        public double NewThreshold { get; set; }
    }

    public class CreateBloodInventoryDto
    {
        public int BloodTypeId { get; set; }
        public int TotalUnits { get; set; }
        public int AvailableUnits { get; set; }
        public int ReservedUnits { get; set; }
        public double TotalVolume { get; set; }
        public double LowStockThreshold { get; set; }
    }
}
