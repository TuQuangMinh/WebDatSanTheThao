using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace web.Models
{
    public class Common
    {
        private readonly IConfiguration _configuration;

        public Common(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetEmailTemplate(string templatePath, Dictionary<string, string> replacements)
        {
            try
            {
                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"Template email không tồn tại tại đường dẫn: {templatePath}");
                    return string.Empty;
                }

                string emailTemplate = File.ReadAllText(templatePath);
                foreach (var replacement in replacements)
                {
                    if (string.IsNullOrEmpty(replacement.Value))
                    {
                        Console.WriteLine($"Giá trị thay thế cho {replacement.Key} là rỗng hoặc null.");
                    }
                    emailTemplate = emailTemplate.Replace($"{{{{{replacement.Key}}}}}", replacement.Value ?? "Không có thông tin");
                }
                return emailTemplate;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi đọc template email: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Chi tiết lỗi: {ex.InnerException.Message}");
                }
                return string.Empty;
            }
        }

        public bool SendEmail(string toEmail, string subject, string body, string fromName)
        {
            try
            {
                if (string.IsNullOrEmpty(toEmail))
                {
                    Console.WriteLine("Địa chỉ email nhận rỗng hoặc không hợp lệ.");
                    return false;
                }

                string smtpServer = _configuration["SmtpSettings:Server"];
                int smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
                string smtpUsername = _configuration["SmtpSettings:Username"];
                string smtpPassword = _configuration["SmtpSettings:Password"];
                string senderEmail = _configuration["SmtpSettings:SenderEmail"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword) || string.IsNullOrEmpty(senderEmail))
                {
                    Console.WriteLine("Cấu hình SMTP không đầy đủ.");
                    return false;
                }

                using (var smtpClient = new SmtpClient(smtpServer))
                {
                    smtpClient.Port = smtpPort;
                    smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    smtpClient.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(senderEmail, fromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    smtpClient.Send(mailMessage);
                    Console.WriteLine($"Email đã được gửi thành công tới: {toEmail}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi gửi email tới {toEmail}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Chi tiết lỗi: {ex.InnerException.Message}");
                }
                return false;
            }
        }
    }
}