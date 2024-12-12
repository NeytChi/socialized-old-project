namespace Domain.Admins
{
    public partial class Admin : BaseEntity
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string Password { get; set; }
        public string TokenForStart { get; set; }
        public DateTime LastLoginAt { get; set; }
        public int? RecoveryCode { get; set; }
    }
}
