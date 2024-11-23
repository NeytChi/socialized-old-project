using Domain.Statistics;
using Domain.SessionComponents;
using Domain.Admins;

namespace Domain.Users
{
    public partial class User
    {
        public User()
        {
            this.IGAccounts = new HashSet<IGAccount>();
            this.bIGAccounts = new HashSet<BusinessAccount>();
        }
        public int userId { get; set; }
        public string userToken { get; set; }
        public string userEmail { get; set; }
        public string userFullName { get; set; }
        public long createdAt { get; set; }
        public long lastLoginAt { get; set; }
        public string userHash { get; set; }
        public bool activate { get; set; }
        public int? recoveryCode { get; set; }
        public string recoveryToken { get; set; }
        public string userPassword { get; set; }
        public bool deleted { get; set; }
        public virtual ServiceAccess access { get; set; }
        public virtual Profile profile { get; set; }
        public virtual ICollection<IGAccount> IGAccounts { get; set; }
        public virtual ICollection<BusinessAccount> bIGAccounts { get; set; }
        public virtual ICollection<Appeal> Appeals { get; set; }
    }
    public struct UserCache
    {
        public string user_fullname { get; set; }
        public string user_email { get; set; }
        public string user_password { get; set; }
        public string old_password { get; set; }
        public string new_password { get; set; }
        public string user_token { get; set; }
        public int recovery_code { get; set; }
        public string recovery_token { get; set; }
	    public string user_confirm_password { get; set; }
        public string country { get; set; }
        public string culture { get; set; }
        public long timezone_seconds { get; set; }
    }
}
