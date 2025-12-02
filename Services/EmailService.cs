using Microsoft.Extensions.Logging; // Added for logging
using Microsoft.Extensions.Options;
using web.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace web.Services
{
    public class EmailService
    {
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> smtpSettings, ILogger<EmailService> logger)
        {
            _smtpSettings = smtpSettings.Value ?? throw new ArgumentNullException(nameof(smtpSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string content)
        {
            try
            {
                if (string.IsNullOrEmpty(toEmail) || !new EmailAddressAttribute().IsValid(toEmail))
                {
                    _logger.LogWarning($"Invalid recipient email: {toEmail}");
                    return false;
                }

                using var message = new MailMessage
                {
                    From = new MailAddress(_smtpSettings.SenderEmail, _smtpSettings.SenderName),
                    Subject = subject,
                    Body = content,
                    IsBodyHtml = true
                };
                message.To.Add(toEmail);

                using var smtp = new SmtpClient(_smtpSettings.Server, _smtpSettings.Port)
                {
                    Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password),
                    EnableSsl = true
                };

                await smtp.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {toEmail} with subject: {subject}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {toEmail}: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetEmailTemplateAsync(string templatePath, Dictionary<string, string> replacements)
        {
            try
            {
                if (!File.Exists(templatePath))
                {
                    _logger.LogError($"Email template not found at {templatePath}");
                    throw new FileNotFoundException($"Không tìm thấy file mẫu tại {templatePath}");
                }

                string content = await File.ReadAllTextAsync(templatePath);
                foreach (var replacement in replacements ?? new Dictionary<string, string>())
                {
                    content = content.Replace($"{{{{{replacement.Key}}}}}", replacement.Value);
                }
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading email template: {ex.Message}");
                return string.Empty;
            }
        }
    }
}