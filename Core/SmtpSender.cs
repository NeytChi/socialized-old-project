using Serilog;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Core
{
    public interface ISmtpSender
    {
        void SendEmail(string email, string subject, string text);
    }
    public class SmtpSender : ISmtpSender
    {
        private MailSettings Settings { get; set; }
        public ILogger Logger;
        private MailAddress from;
        private SmtpClient smtp;

        public SmtpSender(ILogger logger, MailSettings mailSettings)
        {
            Logger = logger;
            Settings = mailSettings;
            Setup();
        }
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
                    smtp.SendMailAsync(message);
                }
                Logger.Information($"Був відправиленний лист на адресу={email}.");
            }
            catch (Exception e) 
            {
                Logger.Error($"Сервер не може відправити листа по адресі={email}");
                Logger.Error($"Виключення={e.Message}"); 
                Logger.Error($"Внутрішне виключення={e.InnerException?.Message}");
            }
        }
    }
}