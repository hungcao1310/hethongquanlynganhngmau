using System.Threading.Tasks;

namespace BloodBankManager.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string subject, string body, string toEmail);
    }
}
