using System.Net;
using System.Net.Mail;

namespace FootballPredictionGame.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            //var smtpHost = _configuration["Smtp:Host"];
            //var smtpPort = Convert.ToInt32(_configuration["Smtp:Port"]);
            //var smtpUser = _configuration["Smtp:Username"];
            //var smtpPass = _configuration["Smtp:Password"];
            //var fromEmail = _configuration["Smtp:FromEmail"];
            //var fromName = _configuration["Smtp:FromName"];

            var smtpHost = "smtp.office365.com";
            var smtpPort = 587;
            var smtpUser = "transtec360@transcombd.com";
            var smtpPass = "bbmbxkhtpczpgxth";
            var fromEmail = "transtec360 @transcombd.com";
            var fromName = "Transtec 360° Solutions";

            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            using var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
    }
}