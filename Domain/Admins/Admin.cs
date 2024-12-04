namespace Domain.Admins
{
    public partial class Admin
    {
        public Admin()
        {
            posts = new HashSet<BlogPost>();
            messages = new HashSet<AppealMessage>();
        }
        public int adminId { get; set; }
        public string adminEmail { get; set; }
        public string adminFullname { get; set; }
        public string adminRole { get; set; }
        public string adminPassword { get; set; }
        public string passwordToken { get; set; }
        public long createdAt { get; set; }
        public long lastLoginAt { get; set; }
        public int? recoveryCode { get; set; }
        public bool deleted { get; set; }
        public ICollection<BlogPost> posts { get; set; }
        public ICollection<AppealMessage> messages { get; set; }
    }

}
