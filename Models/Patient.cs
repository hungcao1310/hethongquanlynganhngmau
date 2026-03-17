namespace BloodBankManager.Models
{
    /// <summary>
    /// Represents a patient who needs blood transfusion
    /// </summary>
    public class Patient
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? PatientCode { get; set; } // Mã bệnh nhân
        public int BloodTypeId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Hospital { get; set; }
        public string? Ward { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? MedicalCondition { get; set; } // Tình trạng y tế
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public BloodType? BloodType { get; set; }
        public ICollection<BloodTransfusion> Transfusions { get; set; } = new List<BloodTransfusion>();
    }
}
