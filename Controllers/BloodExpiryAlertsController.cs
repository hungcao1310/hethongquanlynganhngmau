using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodBankManager.Data;
using BloodBankManager.Models;
using BloodBankManager.Services;

namespace BloodBankManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BloodExpiryAlertsController : ControllerBase
    {
        private readonly BloodBankContext _context;
        private readonly IBloodExpiryService _expiryService;
        private readonly ILogger<BloodExpiryAlertsController> _logger;

        public BloodExpiryAlertsController(
            BloodBankContext context,
            IBloodExpiryService expiryService,
            ILogger<BloodExpiryAlertsController> logger)
        {
            _context = context;
            _expiryService = expiryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BloodExpiryAlertResponseDto>>> GetAlerts()
        {
            try
            {
                var alerts = await _context.BloodExpiryAlerts
                    .Include(a => a.Blood)
                    .ThenInclude(b => b.BloodType)
                    .OrderByDescending(a => a.AlertDate)
                    .ToListAsync();

                var response = alerts.Select(a => new BloodExpiryAlertResponseDto
                {
                    Id = a.Id,
                    BloodUnitNumber = a.Blood.UnitNumber,
                    BloodType = a.Blood.BloodType.TypeName.ToString(),
                    DaysUntilExpiry = (int)(a.Blood.ExpiryDate - DateTime.UtcNow).TotalDays,
                    ExpiryDate = a.Blood.ExpiryDate,
                    AlertDate = a.AlertDate,
                    IsAcknowledged = a.IsAcknowledged,
                    AcknowledgedAt = a.AcknowledgedAt,
                    AcknowledgedBy = a.AcknowledgedBy,
                    Status = a.Status.ToString()
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting alerts: {ex.Message}");
                return StatusCode(500, new { message = "Error getting alerts" });
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<BloodExpiryAlertResponseDto>>> GetActiveAlerts()
        {
            try
            {
                var alerts = await _expiryService.GetActiveAlertsAsync();

                var response = alerts.Select(a => new BloodExpiryAlertResponseDto
                {
                    Id = a.Id,
                    BloodUnitNumber = a.Blood.UnitNumber,
                    BloodType = a.Blood.BloodType.TypeName.ToString(),
                    DaysUntilExpiry = (int)(a.Blood.ExpiryDate - DateTime.UtcNow).TotalDays,
                    ExpiryDate = a.Blood.ExpiryDate,
                    AlertDate = a.AlertDate,
                    IsAcknowledged = a.IsAcknowledged,
                    AcknowledgedAt = a.AcknowledgedAt,
                    AcknowledgedBy = a.AcknowledgedBy,
                    Status = a.Status.ToString()
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting active alerts: {ex.Message}");
                return StatusCode(500, new { message = "Error getting active alerts" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshAlerts()
        {
            try
            {
                // Check for expired bloods
                await _expiryService.CheckAndMarkExpiredBloodsAsync();

                // Create new alerts for expiring bloods
                await _expiryService.CreateExpiryAlertsAsync();

                var activeAlerts = await _expiryService.GetActiveAlertsAsync();

                return Ok(new
                {
                    message = "Alerts refreshed successfully",
                    activeAlertsCount = activeAlerts.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error refreshing alerts: {ex.Message}");
                return StatusCode(500, new { message = "Error refreshing alerts" });
            }
        }

        [HttpPut("{alertId}/acknowledge")]
        public async Task<IActionResult> AcknowledgeAlert(int alertId, [FromBody] AcknowledgeAlertDto dto)
        {
            try
            {
                await _expiryService.AcknowledgeAlertAsync(alertId, dto.AcknowledgedBy);

                return Ok(new { message = "Alert acknowledged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error acknowledging alert: {ex.Message}");
                return StatusCode(500, new { message = "Error acknowledging alert" });
            }
        }

        [HttpGet("statistics")]
        public async Task<ActionResult<AlertStatisticsDto>> GetAlertStatistics()
        {
            try
            {
                var allAlerts = await _context.BloodExpiryAlerts.ToListAsync();
                var activeAlerts = allAlerts.Where(a => a.Status == AlertStatus.Active).ToList();
                var acknowledgedAlerts = allAlerts.Where(a => a.IsAcknowledged).ToList();
                var resolvedAlerts = allAlerts.Where(a => a.Status == AlertStatus.Resolved).ToList();

                var statistics = new AlertStatisticsDto
                {
                    TotalAlerts = allAlerts.Count,
                    ActiveAlerts = activeAlerts.Count,
                    AcknowledgedAlerts = acknowledgedAlerts.Count,
                    ResolvedAlerts = resolvedAlerts.Count,
                    UnrecognizedAlerts = activeAlerts.Count(a => !a.IsAcknowledged)
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting alert statistics: {ex.Message}");
                return StatusCode(500, new { message = "Error getting alert statistics" });
            }
        }
    }

    public class BloodExpiryAlertResponseDto
    {
        public int Id { get; set; }
        public string? BloodUnitNumber { get; set; }
        public string? BloodType { get; set; }
        public int DaysUntilExpiry { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime AlertDate { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public string? Status { get; set; }
    }

    public class AcknowledgeAlertDto
    {
        public string? AcknowledgedBy { get; set; }
    }

    public class AlertStatisticsDto
    {
        public int TotalAlerts { get; set; }
        public int ActiveAlerts { get; set; }
        public int AcknowledgedAlerts { get; set; }
        public int ResolvedAlerts { get; set; }
        public int UnrecognizedAlerts { get; set; }
    }
}
