using BloodBankManager.Models;
using BloodBankManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace BloodBankManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public ContactController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ContactRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Compose email body
            var body = $"Tên: {request.Name}\n" +
                       $"Email: {request.Email}\n\n" +
                       request.Message;

            await _emailService.SendEmailAsync($"Liên hệ từ {request.Name}", body, request.SendTo ?? "dinh6601@gmail.com");

            return Ok(new { Message = "Đã gửi email thành công" });
        }
    }

    public class ContactRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        // Optional override for recipient
        public string? SendTo { get; set; }
    }
}
