namespace BloodBankManager.Models
{
    /// <summary>
    /// Tracks expiry alerts for blood units expiring within 7 days
    /// </summary>
    public class BloodExpiryAlert
    {
        public int Id { get; set; }
        public int BloodId { get; set; }
        public DateTime AlertDate { get; set; } = DateTime.UtcNow;
        public int DaysUntilExpiry { get; set; } // Số ngày còn lại
        public bool IsAcknowledged { get; set; } = false; // Đã xác nhận
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public AlertStatus Status { get; set; } = AlertStatus.Active;

        // Navigation
        public Blood? Blood { get; set; }
    }

    public enum AlertStatus
    {
        Active, // Hoạt động
        Resolved, // Đã xử lý
        Cancelled // Hủy bỏ
    }
}
