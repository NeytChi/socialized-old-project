namespace Core
{
    public struct MailSettings
    {
        public string Domen { get; set; }
        public string MailAddress { get; set; }
        public string MailPassword { get; set; }
        public string SmtpAddress { get; set; }
        public int SmtpPort { get; set; }
        public bool Enable { get; set; }
    }
}
