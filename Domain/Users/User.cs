using Domain.InstagramAccounts;
using Domain.Admins;
using Domain.Packages;

namespace Domain.Users
{
    public partial class User : BaseEntity
    {
        public User()
        {
            IGAccounts = new HashSet<IGAccount>();
        }
        public string TokenForUse { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public DateTime LastLoginAt { get; set; }
        public string HashForActivate { get; set; }
        public bool Activate { get; set; }
        public int? RecoveryCode { get; set; }
        public string RecoveryToken { get; set; }
        public virtual ServiceAccess access { get; set; }
        public virtual Profile profile { get; set; }
        public virtual ICollection<IGAccount> IGAccounts { get; set; }
        public virtual ICollection<Appeal> Appeals { get; set; }
    }
}
