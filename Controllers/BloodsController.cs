using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankManager.Data;
using BloodBankManager.Models;
using BloodBankManager.Services;

namespace BloodBankManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BloodsController : ControllerBase
    {
        private readonly BloodBankContext _context;
        private readonly IBloodExpiryService _expiryService;
        private readonly IBloodCompatibilityService _compatibilityService;
        private readonly ILogger<BloodsController> _logger;

        public BloodsController(
            BloodBankContext context,
            IBloodExpiryService expiryService,
            IBloodCompatibilityService compatibilityService,
            ILogger<BloodsController> logger)
        {
            _context = context;
            _expiryService = expiryService;
            _compatibilityService = compatibilityService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Blood>>> GetBloods()
        {
            try
            {
                var bloods = await _context.Bloods
                    .Include(b => b.BloodType)
                    .OrderBy(b => b.ExpiryDate)
                    .ToListAsync();

                return Ok(bloods);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting bloods: {ex.Message}");
                return StatusCode(500, new { message = "Error getting bloods" });
            }
        }

        [HttpGet("available/{bloodTypeId}")]
        public async Task<ActionResult<IEnumerable<Blood>>> GetAvailableBloodsByType(int bloodTypeId)
        {
            try
            {
                var bloods = await _context.Bloods
                    .Where(b => b.BloodTypeId == bloodTypeId
                        && b.Status == BloodStatus.Available
                        && b.ExpiryDate > DateTime.UtcNow)
                    .OrderBy(b => b.CollectionDate) // FIFO
                    .ToListAsync();

                return Ok(bloods);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting available bloods: {ex.Message}");
                return StatusCode(500, new { message = "Error getting available bloods" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Blood>> CreateBlood(CreateBloodDto dto)
        {
            try
            {
                // Validate blood type exists
                var bloodType = await _context.BloodTypes.FindAsync(dto.BloodTypeId);
                if (bloodType == null)
                {
                    return BadRequest(new { message = "Blood type not found" });
                }

                var blood = new Blood
                {
                    UnitNumber = dto.UnitNumber,
                    BloodTypeId = dto.BloodTypeId,
                    CollectionDate = dto.CollectionDate,
                    ExpiryDate = dto.ExpiryDate,
                    Volume = dto.Volume,
                    DonorName = dto.DonorName,
                    Location = dto.Location,
                    Status = BloodStatus.Available
                };

                _context.Bloods.Add(blood);
                await _context.SaveChangesAsync();

                // Update inventory
                await _expiryService.UpdateBloodInventoryAsync(dto.BloodTypeId);

                return CreatedAtAction(nameof(GetBloodById), new { id = blood.Id }, blood);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating blood: {ex.Message}");
                return StatusCode(500, new { message = "Error creating blood" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Blood>> GetBloodById(int id)
        {
            try
            {
                var blood = await _context.Bloods
                    .Include(b => b.BloodType)
                    .FirstOrDefaultAsync(b => b.Id == id);

                if (blood == null)
                {
                    return NotFound(new { message = "Blood not found" });
                }

                return Ok(blood);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting blood: {ex.Message}");
                return StatusCode(500, new { message = "Error getting blood" });
            }
        }

        [HttpGet("expiring/alert")]
        public async Task<ActionResult<IEnumerable<Blood>>> GetExpiringBloods([FromQuery] int daysUntilExpiry = 7)
        {
            try
            {
                var expiringBloods = await _expiryService.GetExpiringBloodsAsync(daysUntilExpiry);
                return Ok(expiringBloods);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting expiring bloods: {ex.Message}");
                return StatusCode(500, new { message = "Error getting expiring bloods" });
            }
        }

        [HttpPost("create-expiry-alerts")]
        public async Task<IActionResult> CreateExpiryAlerts()
        {
            try
            {
                await _expiryService.CreateExpiryAlertsAsync();
                return Ok(new { message = "Expiry alerts created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating expiry alerts: {ex.Message}");
                return StatusCode(500, new { message = "Error creating expiry alerts" });
            }
        }

        [HttpPost("check-expired")]
        public async Task<IActionResult> CheckAndMarkExpiredBloods()
        {
            try
            {
                await _expiryService.CheckAndMarkExpiredBloodsAsync();
                return Ok(new { message = "Expired bloods checked and marked" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking expired bloods: {ex.Message}");
                return StatusCode(500, new { message = "Error checking expired bloods" });
            }
        }

        [HttpPut("{id}/discard")]
        public async Task<IActionResult> DiscardBlood(int id, [FromBody] DiscardBloodDto dto)
        {
            try
            {
                var blood = await _context.Bloods.FindAsync(id);
                if (blood == null)
                {
                    return NotFound(new { message = "Blood not found" });
                }

                blood.Status = BloodStatus.Discarded;
                blood.DiscardedAt = DateTime.UtcNow;
                blood.DiscardReason = dto.Reason;

                await _context.SaveChangesAsync();

                // Update inventory
                await _expiryService.UpdateBloodInventoryAsync(blood.BloodTypeId);

                return Ok(new { message = "Blood discarded successfully", blood });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error discarding blood: {ex.Message}");
                return StatusCode(500, new { message = "Error discarding blood" });
            }
        }
    }

    public class CreateBloodDto
    {
        public string? UnitNumber { get; set; }
        public int BloodTypeId { get; set; }
        public DateTime CollectionDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public double Volume { get; set; }
        public string? DonorName { get; set; }
        public string? Location { get; set; }
    }

    public class DiscardBloodDto
    {
        public string? Reason { get; set; }
    }
}
