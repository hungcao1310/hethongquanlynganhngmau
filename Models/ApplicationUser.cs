using Microsoft.AspNetCore.Identity;

namespace BloodBankManager.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Additional properties can be added here if needed
        public string? FullName { get; set; }
        public string? Role { get; set; } // "Staff" or "Patient"
        public int? PatientId { get; set; } // Link to Patient if role is Patient

        // Navigation
        public Patient? Patient { get; set; }
    }
}