namespace UseCases.Admins
{
    public class AdminCommand
    {

    }
    public class AdminCache
    {
        public int admin_id { get; set; }
        public string admin_email { get; set; }
        public string admin_fullname { get; set; }
        public string admin_password { get; set; }
        public string confirm_password { get; set; }
        public string password_token { get; set; }
        public int recovery_code { get; set; }
    }
}
