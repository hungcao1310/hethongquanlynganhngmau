using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BloodBankManager.Models;
using BloodBankManager.Data;
using Microsoft.EntityFrameworkCore;

namespace BloodBankManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly BloodBankContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            BloodBankContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [HttpPost("register/staff")]
        public async Task<IActionResult> RegisterStaff([FromBody] RegisterStaffRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                Role = "Staff"
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Staff");
                return Ok(new { Message = "Staff registered successfully" });
            }

            return BadRequest(result.Errors);
        }

        [HttpPost("register/patient")]
        public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Generate patient code if not provided
            string patientCode = string.IsNullOrEmpty(request.PatientCode) 
                ? $"BN-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}" 
                : request.PatientCode;

            // Create patient record first
            var patient = new Patient
            {
                Name = request.FullName,
                PatientCode = patientCode,
                BloodTypeId = request.BloodTypeId,
                DateOfBirth = request.DateOfBirth,
                Gender = request.Gender,
                Hospital = request.Hospital,
                Ward = request.Ward,
                AdmissionDate = request.AdmissionDate,
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                MedicalCondition = request.MedicalCondition
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Create user account
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FullName = request.FullName,
                Role = "Patient",
                PatientId = patient.Id
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Patient");
                return Ok(new { Message = "Patient registered successfully" });
            }

            // If user creation failed, remove patient record
            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _signInManager.PasswordSignInAsync(request.Email, request.Password, false, false);
            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Unauthorized("Invalid login attempt");
                }

                var roles = await _userManager.GetRolesAsync(user);
                return Ok(new
                {
                    Message = "Login successful",
                    User = new
                    {
                        user.Id,
                        user.Email,
                        user.FullName,
                        user.Role,
                        user.PatientId
                    },
                    Roles = roles
                });
            }

            return Unauthorized("Invalid login attempt");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Message = "Logged out successfully" });
        }
    }

    public class RegisterStaffRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class RegisterPatientRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PatientCode { get; set; }
        public int BloodTypeId { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Hospital { get; set; }
        public string? Ward { get; set; }
        public DateTime AdmissionDate { get; set; }
        public string? PhoneNumber { get; set; }
        public string? MedicalCondition { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}