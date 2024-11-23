using Serilog;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Core
{
    public class SmtpSender
    {
        private MailSettings Settings { get; set; }
        public ILogger Logger;

        public SmtpSender(ILogger logger, MailSettings mailSettings)
        {
            Logger = logger;
            Settings = mailSettings;
            Setup();
        }
        private MailAddress from;
        private SmtpClient smtp;

        private void Setup()
        {
            smtp = new SmtpClient(Settings.SmtpAddress, Settings.SmtpPort);
            smtp.Credentials = new NetworkCredential(Settings.MailAddress, Settings.MailPassword);
            from = new MailAddress(Settings.MailAddress, Settings.Domen);
            smtp.EnableSsl = true;
            ServicePointManager.ServerCertificateValidationCallback =
            delegate (
                object s,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors
            ) 
            {
                return true;
            };
        }
        public async void SendEmail(string email, string subject, string text)
        {
            var to = new MailAddress(email);
            var message = new MailMessage(from, to)
            {
                Subject = subject,
                Body = text,
                IsBodyHtml = true
            };
            try 
            {
                if (Settings.Enable)
                {
                    await smtp.SendMailAsync(message);
                }
                Logger.Information("Send message to " + email);
            }
            catch (Exception e) 
            {
                Logger.Error("Server can't send email to -> " + email );
                Logger.Error("Exception, -> " + e.Message); 
                Logger.Error("Inner exception, -> " + e.InnerException?.Message ?? "");
            }
        }
    }
}