using System.ComponentModel.DataAnnotations;

namespace web.Models
{
    public class SmtpSettings
    {
        [Required(ErrorMessage = "SMTP server is required.")]
        public string Server { get; set; } = string.Empty;

        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535.")]
        public int Port { get; set; }

        [Required(ErrorMessage = "Sender name is required.")]
        public string SenderName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Sender email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string SenderEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}