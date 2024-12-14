using Core;

namespace UseCases.Users
{
    public interface IEmailMessager
    {
        void SendConfirmEmail(string userEmail, string culture, string userHash);
        void SendRecoveryEmail(string userEmail, string culture, int recoveryCode);
    }
    public class EmailMessanger : IEmailMessager
    {
        private SmtpSender SmtpSender;

        public EmailMessanger(SmtpSender smtpSender) 
        {
            SmtpSender = smtpSender;
        }
        public void SendConfirmEmail(string userEmail, string culture, string userHash)
        {
            /*
            int indexValue = 0;
            string confirmAccount, confirmValue, emailText, positionText, emailName = "eng.html";

            positionText = "\' href=\"";
            confirmValue = userHash;
            confirmAccount = context.Cultures.Where(c
                => c.cultureKey == "confirm_account"
                && c.cultureName == culture).FirstOrDefault()?.cultureValue ?? "";
            if (culture == "ru_RU")
                emailName = "ru.html";
            emailText = File.ReadAllText(emailPath + emailName);

            indexValue = emailText.IndexOf(positionText);
            emailText = emailText.Insert(indexValue + positionText.Length, confirmValue);
            SmtpSender.SendEmail(userEmail, confirmAccount, emailText);*/
        }
        public void SendRecoveryEmail(string userEmail, string culture, int recoveryCode)
        {
            /*
            string recoveryPassword, tRecoveryCode;
            recoveryPassword = context.Cultures.Where(c
                => c.cultureKey == "recovery_password"
                && c.cultureName == culture).FirstOrDefault()?.cultureValue ?? "";
            tRecoveryCode = context.Cultures.Where(c
                => c.cultureKey == "recovery_code"
                && c.cultureName == culture).FirstOrDefault()?.cultureValue ?? "";
            SmtpSender.SendEmail(userEmail, recoveryPassword, tRecoveryCode + recoveryCode);
            */
        }
    }
}
