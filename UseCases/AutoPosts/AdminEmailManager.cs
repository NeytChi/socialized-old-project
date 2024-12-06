using Core;
using Serilog;

namespace UseCases.AutoPosts
{
    public class AdminEmailManager :  BaseManager, IAdminEmailManager
    {
        private ISmtpSender SmtpSender;
        public AdminEmailManager(ISmtpSender smtpSender, ILogger logger) : base (logger)
        {
            SmtpSender = smtpSender;
        }
        public void SetupPassword(string tokenForStart, string email)
        {
            SmtpSender.SendEmail(email, "Setup password", tokenForStart);
            Logger.Information("Був відправлений URL для активації вашого адмін аккаунту.");
        }
        public void RecoveryPassword(int code, string email)
        {
            SmtpSender.SendEmail(email, "Вілновлення паролю", $"Code: {code}");
            Logger.Information("Був відправлений 6 знаковий код для відновлення паролю.");
        }
    }
}
