using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailWithSmtpAsync(string toEmail, string subject, string body, string senderEmail, string senderName)
    {
        var smtpHost = _configuration["EmailSettings:SmtpHost"];
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);

        var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? _configuration["EmailSettings:SmtpUser"];
        var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? _configuration["EmailSettings:SmtpPass"];


        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(toEmail);

        using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
        {
            smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPass);
            smtpClient.EnableSsl = true; // Ensure SSL is enabled

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (SmtpException ex)
            {
                throw new Exception($"SMTP error: {ex.Message}");
            }
        }
    }

    public async Task SendEmailsWithSmtpAsync(List<string> toEmails, string subject, string body, string senderEmail, string senderName)
    {
        if (toEmails == null || toEmails.Count == 0)
        {
            throw new ArgumentException("Recipient list cannot be empty.");
        }

        var smtpHost = _configuration["EmailSettings:SmtpHost"];
        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);

        var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? _configuration["EmailSettings:SmtpUser"];
        var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? _configuration["EmailSettings:SmtpPass"];

        var mailMessage = new MailMessage
        {
            From = new MailAddress(senderEmail, senderName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        // ✅ Add multiple recipients
        foreach (var email in toEmails)
        {
            mailMessage.To.Add(email);
        }

        using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
        {
            smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPass);
            smtpClient.EnableSsl = true;

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (SmtpException ex)
            {
                throw new Exception($"SMTP error: {ex.Message}");
            }
        }
    }
}
