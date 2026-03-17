using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankManager.Data;
using BloodBankManager.Models;

namespace BloodBankManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly BloodBankContext _context;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(BloodBankContext context, ILogger<PaymentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            try
            {
                var payments = await _context.Payments
                    .Include(p => p.Patient)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payments: {ex.Message}");
                return StatusCode(500, new { message = "Error getting payments" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            try
            {
                var payment = await _context.Payments
                    .Include(p => p.Patient)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                return Ok(payment);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payment {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error getting payment" });
            }
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPaymentsByPatient(int patientId)
        {
            try
            {
                var payments = await _context.Payments
                    .Where(p => p.PatientId == patientId)
                    .Include(p => p.Patient)
                    .OrderByDescending(p => p.PaymentDate)
                    .ToListAsync();

                return Ok(payments);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting payments for patient {patientId}: {ex.Message}");
                return StatusCode(500, new { message = "Error getting payments" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Payment>> CreatePayment(Payment payment)
        {
            try
            {
                // Validate patient exists
                var patient = await _context.Patients.FindAsync(payment.PatientId);
                if (patient == null)
                {
                    return BadRequest(new { message = "Patient not found" });
                }

                // Validate amount
                if (payment.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be greater than 0" });
                }

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // Include patient in response
                await _context.Entry(payment).Reference(p => p.Patient).LoadAsync();

                return CreatedAtAction(nameof(GetPayment), new { id = payment.Id }, payment);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating payment: {ex.Message}");
                return StatusCode(500, new { message = "Error creating payment" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePayment(int id, Payment payment)
        {
            if (id != payment.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            try
            {
                var existingPayment = await _context.Payments.FindAsync(id);
                if (existingPayment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                // Update fields
                existingPayment.Amount = payment.Amount;
                existingPayment.PaymentMethod = payment.PaymentMethod;
                existingPayment.Description = payment.Description;
                existingPayment.Status = payment.Status;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating payment {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error updating payment" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting payment {id}: {ex.Message}");
                return StatusCode(500, new { message = "Error deleting payment" });
            }
        }
    }
}