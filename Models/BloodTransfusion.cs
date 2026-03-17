namespace BloodBankManager.Models
{
    /// <summary>
    /// Records blood transfusions from inventory to patients
    /// </summary>
    public class BloodTransfusion
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public int BloodId { get; set; }
        public double QuantityTransfused { get; set; } // Lượng truyền (ml)
        public DateTime TransfusionDate { get; set; }
        public string? PerformedBy { get; set; } // Người thực hiện
        public string? Notes { get; set; }
        public TransfusionStatus Status { get; set; } = TransfusionStatus.Completed;

        // Navigation
        public Patient? Patient { get; set; }
        public Blood? Blood { get; set; }
    }

    public enum TransfusionStatus
    {
        Pending, // Chờ xử lý
        InProgress, // Đang diễn ra
        Completed, // Hoàn thành
        Failed, // Thất bại
        Cancelled // Hủy
    }
}
