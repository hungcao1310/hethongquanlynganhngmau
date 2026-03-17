using BloodBankManager.Data;
using BloodBankManager.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodBankManager.Services
{
    /// <summary>
    /// Service để quản lý hết hạn máu và cảnh báo
    /// - Theo dõi máu sắp hết hạn trong 7 ngày
    /// - FIFO: Nhập trước xuất trước
    /// </summary>
    public interface IBloodExpiryService
    {
        Task<List<Blood>> GetExpiringBloodsAsync(int daysUntilExpiry = 7);
        Task<List<BloodExpiryAlert>> GetActiveAlertsAsync();
        Task CreateExpiryAlertsAsync();
        Task CheckAndMarkExpiredBloodsAsync();
        Task AcknowledgeAlertAsync(int alertId, string? acknowledgedBy);
        Task<Blood?> GetNextAvailableBloodByTypeFIFOAsync(int bloodTypeId);
        Task<BloodInventory> UpdateBloodInventoryAsync(int bloodTypeId);
    }

    public class BloodExpiryService : IBloodExpiryService
    {
        private readonly BloodBankContext _context;
        private readonly ILogger<BloodExpiryService> _logger;

        public BloodExpiryService(BloodBankContext context, ILogger<BloodExpiryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách máu sắp hết hạn trong N ngày
        /// </summary>
        public async Task<List<Blood>> GetExpiringBloodsAsync(int daysUntilExpiry = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(daysUntilExpiry);

            return await _context.Bloods
                .Where(b => b.Status == BloodStatus.Available
                    && b.ExpiryDate <= cutoffDate
                    && b.ExpiryDate > DateTime.UtcNow)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync();
        }

        /// <summary>
        /// Lấy tất cả cảnh báo hoạt động
        /// </summary>
        public async Task<List<BloodExpiryAlert>> GetActiveAlertsAsync()
        {
            return await _context.BloodExpiryAlerts
                .Where(a => a.Status == AlertStatus.Active && !a.IsAcknowledged)
                .Include(a => a.Blood)
                .OrderBy(a => a.Blood.ExpiryDate)
                .ToListAsync();
        }

        /// <summary>
        /// Tạo cảnh báo cho tất cả máu sắp hết hạn
        /// </summary>
        public async Task CreateExpiryAlertsAsync()
        {
            try
            {
                var expiringBloods = await GetExpiringBloodsAsync();

                foreach (var blood in expiringBloods)
                {
                    // Kiểm tra xem cảnh báo đã tồn tại chưa
                    var existingAlert = await _context.BloodExpiryAlerts
                        .FirstOrDefaultAsync(a => a.BloodId == blood.Id && a.Status == AlertStatus.Active);

                    if (existingAlert == null)
                    {
                        var daysUntilExpiry = (int)(blood.ExpiryDate - DateTime.UtcNow).TotalDays;

                        var alert = new BloodExpiryAlert
                        {
                            BloodId = blood.Id,
                            AlertDate = DateTime.UtcNow,
                            DaysUntilExpiry = daysUntilExpiry,
                            Status = AlertStatus.Active
                        };

                        _context.BloodExpiryAlerts.Add(alert);
                        _logger.LogWarning($"Created expiry alert for blood unit {blood.UnitNumber}, expiring in {daysUntilExpiry} days");
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating expiry alerts: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Kiểm tra và đánh dấu máu đã hết hạn
        /// </summary>
        public async Task CheckAndMarkExpiredBloodsAsync()
        {
            try
            {
                var expiredBloods = await _context.Bloods
                    .Where(b => b.ExpiryDate <= DateTime.UtcNow && b.Status != BloodStatus.Discarded)
                    .ToListAsync();

                foreach (var blood in expiredBloods)
                {
                    blood.Status = BloodStatus.Expired;
                    blood.DiscardedAt = DateTime.UtcNow;
                    blood.DiscardReason = "Hết hạn";

                    // Cập nhật cảnh báo
                    var alert = await _context.BloodExpiryAlerts
                        .FirstOrDefaultAsync(a => a.BloodId == blood.Id);

                    if (alert != null)
                    {
                        alert.Status = AlertStatus.Resolved;
                    }

                    _logger.LogInformation($"Marked blood unit {blood.UnitNumber} as expired");
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking and marking expired bloods: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Xác nhận cảnh báo hết hạn
        /// </summary>
        public async Task AcknowledgeAlertAsync(int alertId, string? acknowledgedBy)
        {
            try
            {
                var alert = await _context.BloodExpiryAlerts.FindAsync(alertId);

                if (alert == null)
                {
                    throw new ArgumentException($"Alert with ID {alertId} not found");
                }

                alert.IsAcknowledged = true;
                alert.AcknowledgedAt = DateTime.UtcNow;
                alert.AcknowledgedBy = acknowledgedBy;

                await _context.SaveChangesAsync();
                _logger.LogInformation($"Acknowledged alert {alertId} by {acknowledgedBy}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error acknowledging alert: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lấy máu có sẵn tiếp theo theo nhóm máu (FIFO - Nhập trước xuất trước)
        /// </summary>
        public async Task<Blood?> GetNextAvailableBloodByTypeFIFOAsync(int bloodTypeId)
        {
            return await _context.Bloods
                .Where(b => b.BloodTypeId == bloodTypeId
                    && b.Status == BloodStatus.Available
                    && b.ExpiryDate > DateTime.UtcNow)
                .OrderBy(b => b.CollectionDate) // FIFO: Máu cũ được lấy trước
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Cập nhật tồn kho máu theo nhóm máu
        /// </summary>
        public async Task<BloodInventory> UpdateBloodInventoryAsync(int bloodTypeId)
        {
            try
            {
                var inventory = await _context.BloodInventories
                    .FirstOrDefaultAsync(i => i.BloodTypeId == bloodTypeId);

                if (inventory == null)
                {
                    inventory = new BloodInventory
                    {
                        BloodTypeId = bloodTypeId,
                        TotalUnits = 0,
                        TotalVolume = 0,
                        AvailableUnits = 0,
                        ReservedUnits = 0,
                        LowStockThreshold = 1000, // 1000ml
                        LastUpdated = DateTime.UtcNow
                    };
                    _context.BloodInventories.Add(inventory);
                }

                // Tính toán tồn kho
                var totalBloods = await _context.Bloods
                    .Where(b => b.BloodTypeId == bloodTypeId)
                    .ToListAsync();

                var availableBloods = totalBloods.Where(b => b.Status == BloodStatus.Available).ToList();
                var reservedBloods = totalBloods.Where(b => b.Status == BloodStatus.Reserved).ToList();

                inventory.TotalUnits = totalBloods.Count;
                inventory.TotalVolume = totalBloods.Sum(b => b.Volume);
                inventory.AvailableUnits = availableBloods.Count;
                inventory.ReservedUnits = reservedBloods.Count;
                inventory.LastUpdated = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return inventory;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating blood inventory: {ex.Message}");
                throw;
            }
        }
    }
}
