namespace BloodBankManager.Models
{
    /// <summary>
    /// Represents a blood bag/unit in the blood bank
    /// </summary>
    public class Blood
    {
        public int Id { get; set; }
        public string? UnitNumber { get; set; } // Mã túi máu
        public int BloodTypeId { get; set; }
        public DateTime CollectionDate { get; set; } // Ngày thu máu
        public DateTime ExpiryDate { get; set; } // Ngày hết hạn
        public double Volume { get; set; } // Thể tích (ml)
        public BloodStatus Status { get; set; } = BloodStatus.Available; // Tình trạng
        public string? DonorName { get; set; } // Tên người hiến
        public string? Location { get; set; } // Vị trí lưu giữ
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; } // Ngày sử dụng
        public DateTime? DiscardedAt { get; set; } // Ngày hủy bỏ
        public string? DiscardReason { get; set; } // Lý do hủy bỏ

        // Navigation
        public BloodType? BloodType { get; set; }
    }

    public enum BloodStatus
    {
        Available, // Có sẵn
        InUse, // Đang sử dụng
        Reserved, // Đặt trước
        Expired, // Hết hạn
        Discarded // Hủy bỏ
    }
}
