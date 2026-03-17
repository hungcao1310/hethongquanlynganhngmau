using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankManager.Data;
using BloodBankManager.Models;
using BloodBankManager.Services;

namespace BloodBankManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly BloodBankContext _context;
        private readonly IBloodCompatibilityService _compatibilityService;
        private readonly IBloodExpiryService _expiryService;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(
            BloodBankContext context,
            IBloodCompatibilityService compatibilityService,
            IBloodExpiryService expiryService,
            ILogger<PatientsController> logger)
        {
            _context = context;
            _compatibilityService = compatibilityService;
            _expiryService = expiryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
        {
            try
            {
                var patients = await _context.Patients
                    .Include(p => p.BloodType)
                    .ToListAsync();

                return Ok(patients);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting patients: {ex.Message}");
                return StatusCode(500, new { message = "Error getting patients" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Patient>> CreatePatient(CreatePatientDto dto)
        {
            try
            {
                // Validate blood type exists
                var bloodType = await _context.BloodTypes.FindAsync(dto.BloodTypeId);
                if (bloodType == null)
                {
                    return BadRequest(new { message = "Blood type not found" });
                }

                var patient = new Patient
                {
                    Name = dto.Name,
                    PatientCode = dto.PatientCode,
                    BloodTypeId = dto.BloodTypeId,
                    DateOfBirth = dto.DateOfBirth,
                    Gender = dto.Gender,
                    Hospital = dto.Hospital,
                    Ward = dto.Ward,
                    AdmissionDate = dto.AdmissionDate,
                    PhoneNumber = dto.PhoneNumber,
                    Email = dto.Email,
                    MedicalCondition = dto.MedicalCondition
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, patient);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating patient: {ex.Message}");
                return StatusCode(500, new { message = "Error creating patient" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Patient>> GetPatientById(int id)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.BloodType)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                {
                    return NotFound(new { message = "Patient not found" });
                }

                return Ok(patient);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting patient: {ex.Message}");
                return StatusCode(500, new { message = "Error getting patient" });
            }
        }

        [HttpPost("{id}/transfuse")]
        public async Task<ActionResult<TransfusionResponseDto>> TransfuseBlood(int id, [FromBody] TransfuseBloodDto dto)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.BloodType)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                {
                    return NotFound(new { message = "Patient not found" });
                }

                var blood = await _context.Bloods.FindAsync(dto.BloodId);
                if (blood == null)
                {
                    return BadRequest(new { message = "Blood not found" });
                }

                if (blood.Status != BloodStatus.Available)
                {
                    return BadRequest(new { message = $"Blood is not available. Status: {blood.Status}" });
                }

                if (blood.ExpiryDate <= DateTime.UtcNow)
                {
                    return BadRequest(new { message = "Blood has expired" });
                }

                // Check blood compatibility
                var donorType = blood.BloodType.TypeName;
                var recipientType = patient.BloodType.TypeName;

                if (!_compatibilityService.IsCompatible(donorType, recipientType))
                {
                    return BadRequest(new
                    {
                        message = $"Blood type {donorType} is not compatible with patient blood type {recipientType}",
                        compatibleDonors = _compatibilityService.GetCompatibleDonorTypes(recipientType)
                    });
                }

                // Check quantity
                if (blood.Volume < dto.Quantity)
                {
                    return BadRequest(new { message = $"Requested quantity {dto.Quantity}ml exceeds available {blood.Volume}ml" });
                }

                // Create transfusion record
                var transfusion = new BloodTransfusion
                {
                    PatientId = id,
                    BloodId = dto.BloodId,
                    QuantityTransfused = dto.Quantity,
                    TransfusionDate = DateTime.UtcNow,
                    PerformedBy = dto.PerformedBy,
                    Notes = dto.Notes,
                    Status = TransfusionStatus.Completed
                };

                // Update blood status
                blood.Status = BloodStatus.InUse;
                blood.UsedAt = DateTime.UtcNow;
                blood.Volume -= dto.Quantity;

                if (blood.Volume <= 0)
                {
                    blood.Status = BloodStatus.Discarded;
                    blood.DiscardedAt = DateTime.UtcNow;
                    blood.DiscardReason = "Used completely";
                }

                _context.BloodTransfusions.Add(transfusion);
                await _context.SaveChangesAsync();

                // Update inventory
                await _expiryService.UpdateBloodInventoryAsync(blood.BloodTypeId);

                var response = new TransfusionResponseDto
                {
                    TransfusionId = transfusion.Id,
                    PatientName = patient.Name,
                    BloodType = donorType.ToString(),
                    QuantityTransfused = dto.Quantity,
                    TransfusionDate = transfusion.TransfusionDate,
                    Message = "Transfusion completed successfully"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during transfusion: {ex.Message}");
                return StatusCode(500, new { message = "Error during transfusion" });
            }
        }

        [HttpGet("{id}/transfusions")]
        public async Task<ActionResult<IEnumerable<BloodTransfusion>>> GetPatientTransfusions(int id)
        {
            try
            {
                var patient = await _context.Patients.FindAsync(id);
                if (patient == null)
                {
                    return NotFound(new { message = "Patient not found" });
                }

                var transfusions = await _context.BloodTransfusions
                    .Where(t => t.PatientId == id)
                    .Include(t => t.Blood)
                    .OrderByDescending(t => t.TransfusionDate)
                    .ToListAsync();

                return Ok(transfusions);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting patient transfusions: {ex.Message}");
                return StatusCode(500, new { message = "Error getting patient transfusions" });
            }
        }

        [HttpGet("{id}/compatible-bloods")]
        public async Task<ActionResult<IEnumerable<Blood>>> GetCompatibleBloods(int id)
        {
            try
            {
                var patient = await _context.Patients
                    .Include(p => p.BloodType)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                {
                    return NotFound(new { message = "Patient not found" });
                }

                if (patient.BloodType == null)
                {
                    return BadRequest(new { message = "Patient does not have a blood type assigned" });
                }

                var compatibleTypes = _compatibilityService
                    .GetCompatibleDonorTypes(patient.BloodType.TypeName);

                var compatibleBloods = await _context.Bloods
                    .Where(b => compatibleTypes.Contains(b.BloodType.TypeName)
                        && b.Status == BloodStatus.Available
                        && b.ExpiryDate > DateTime.UtcNow)
                    .OrderBy(b => b.ExpiryDate)
                    .ToListAsync();

                return Ok(compatibleBloods);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting compatible bloods: {ex.Message}");
                return StatusCode(500, new { message = "Error getting compatible bloods" });
            }
        }
    }

    public class CreatePatientDto
    {
        public string? Name { get; set; }
        public string? PatientCode { get; set; }
        public int BloodTypeId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Hospital { get; set; }
        public string? Ward { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? MedicalCondition { get; set; }
    }

    public class TransfuseBloodDto
    {
        public int BloodId { get; set; }
        public double Quantity { get; set; }
        public string? PerformedBy { get; set; }
        public string? Notes { get; set; }
    }

    public class TransfusionResponseDto
    {
        public int TransfusionId { get; set; }
        public string? PatientName { get; set; }
        public string? BloodType { get; set; }
        public double QuantityTransfused { get; set; }
        public DateTime TransfusionDate { get; set; }
        public string? Message { get; set; }
    }
}
