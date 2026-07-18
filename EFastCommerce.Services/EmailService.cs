using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Services
{
    public class EmailService : IEmailService
    {
        public EmailService()
        {
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var host = Environment.GetEnvironmentVariable("SMTP_HOST");
            var portString = Environment.GetEnvironmentVariable("SMTP_PORT");
            var username = Environment.GetEnvironmentVariable("SMTP_USERNAME");
            var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");
            var enableSslString = Environment.GetEnvironmentVariable("SMTP_ENABLE_SSL");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portString))
            {
                // Fallback / Mock behavior if SMTP is not configured
                Console.WriteLine($"[EmailService Stub] Would send email to {to}");
                Console.WriteLine($"[EmailService Stub] Subject: {subject}");
                Console.WriteLine($"[EmailService Stub] Body:\n{body}");
                return;
            }

            int port = int.TryParse(portString, out var p) ? p : 587;
            bool enableSsl = bool.TryParse(enableSslString, out var ssl) ? ssl : true;

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(username ?? "noreply@efastcommerce.com", "E-Fast Commerce"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            
            mailMessage.To.Add(to);

            try
            {
                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                // We'll also print to console so dev can continue
                Console.WriteLine($"[EmailService Stub] Would send email to {to}");
                Console.WriteLine($"[EmailService Stub] Subject: {subject}");
                Console.WriteLine($"[EmailService Stub] Body:\n{body}");
            }
        }
    }
}
