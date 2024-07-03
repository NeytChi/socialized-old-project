﻿using System;
using socialized;
using System.Net;
using Serilog.Core;
using System.Net.Mail;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;

namespace Common
{
    public class MailF
    {
        public MailF(Logger log)
        {
            this.log = log;
            Init();
        }
        public Logger log;
        public bool emailEnable = true;
        private string GmailServer = "smtp.gmail.com";
        private int GmailPort = 587;
        private string domen = "socialized.space";
        private string mailAddress;
        private string mailPassword;
        private MailAddress from;
        private SmtpClient smtp;

        public void Init()
        {
            var config = Program.serverConfiguration();
            domen = config.GetValue<string>("domen");
            mailAddress = config.GetValue<string>("mail_address");
            mailPassword = config.GetValue<string>("mail_password");
            GmailServer = config.GetValue<string>("smtp_server");
            GmailPort = config.GetValue<int>("smtp_port");
            emailEnable = config.GetValue<bool>("email_enable");
            if (mailAddress != null) {
                smtp = new SmtpClient(GmailServer, GmailPort);
                smtp.Credentials = new NetworkCredential(mailAddress, mailPassword);
                from = new MailAddress(mailAddress, domen);
                smtp.EnableSsl = true;
            }
            ServicePointManager.ServerCertificateValidationCallback =
            delegate (
                object s,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors
            ) {
                return true;
            };
        }
        public async void SendEmail(string email, string subject, string text)
        {
            MailAddress to = new MailAddress(email);
            MailMessage message = new MailMessage(from, to);
            message.Subject = subject;
            message.Body = text;
            message.IsBodyHtml = true;
            try {
                if (emailEnable)
                    await smtp.SendMailAsync(message);
                log.Information("Send message to " + email);
            }
            catch (Exception e) {
                log.Error("Server can't send email to -> " + email );
                log.Error("Exception, -> " + e.Message); 
                log.Error("Inner exception, -> " + e.InnerException?.Message ?? "");
            }
        }
    }
}