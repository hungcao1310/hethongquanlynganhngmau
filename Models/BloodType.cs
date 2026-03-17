namespace BloodBankManager.Models
{
    /// <summary>
    /// Enum for blood types and Rh factor
    /// </summary>
    public enum BloodTypeEnum
    {
        O_NEGATIVE,
        O_POSITIVE,
        A_NEGATIVE,
        A_POSITIVE,
        B_NEGATIVE,
        B_POSITIVE,
        AB_NEGATIVE,
        AB_POSITIVE
    }

    public class BloodType
    {
        public int Id { get; set; }
        public BloodTypeEnum TypeName { get; set; }
        public string? Description { get; set; }

        // Navigation
        public ICollection<Blood> Bloods { get; set; } = new List<Blood>();
        public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}
