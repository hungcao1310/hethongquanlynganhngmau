using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankManager.Data;
using BloodBankManager.Models;

namespace BloodBankManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BloodTypesController : ControllerBase
    {
        private readonly BloodBankContext _context;
        private readonly ILogger<BloodTypesController> _logger;

        public BloodTypesController(BloodBankContext context, ILogger<BloodTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BloodType>>> GetBloodTypes()
        {
            try
            {
                var bloodTypes = await _context.BloodTypes.ToListAsync();
                return Ok(bloodTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting blood types: {ex.Message}");
                return StatusCode(500, new { message = "Error getting blood types" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BloodType>> GetBloodType(int id)
        {
            try
            {
                var bloodType = await _context.BloodTypes.FindAsync(id);

                if (bloodType == null)
                {
                    return NotFound(new { message = "Blood type not found" });
                }

                return Ok(bloodType);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting blood type: {ex.Message}");
                return StatusCode(500, new { message = "Error getting blood type" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<BloodType>> CreateBloodType(BloodType bloodType)
        {
            try
            {
                _context.BloodTypes.Add(bloodType);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetBloodType), new { id = bloodType.Id }, bloodType);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating blood type: {ex.Message}");
                return StatusCode(500, new { message = "Error creating blood type" });
            }
        }
    }
}
