using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace MediCare.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var port = int.Parse(_config["EmailSettings:Port"]);
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var password = _config["EmailSettings:Password"];

            using (var client = new SmtpClient(smtpServer, port))
            {
                client.Credentials = new NetworkCredential(senderEmail, password);
                client.EnableSsl = true;

                var mail = new MailMessage(senderEmail, toEmail, subject, body);
                mail.IsBodyHtml = true;

                client.Send(mail);
            }
        }
    }
}