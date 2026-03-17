namespace BloodBankManager.Models
{
    /// <summary>
    /// Tracks overall blood inventory by blood type
    /// </summary>
    public class BloodInventory
    {
        public int Id { get; set; }
        public int BloodTypeId { get; set; }
        public int TotalUnits { get; set; } // Tổng số túi máu
        public double TotalVolume { get; set; } // Tổng thể tích (ml)
        public int AvailableUnits { get; set; } // Số túi có sẵn
        public int ReservedUnits { get; set; } // Số túi đặt trước
        public double LowStockThreshold { get; set; } // Ngưỡng cảnh báo tồn kho thấp (ml)
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation
        public BloodType? BloodType { get; set; }
    }
}
