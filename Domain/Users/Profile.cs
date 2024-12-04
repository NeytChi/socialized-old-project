namespace Domain.Users
{
    public partial class Profile : BaseEntity
    {
        public long UserId { get; set; }
        public string CountryName { get; set; }
        public long TimeZone { get; set; }
        public virtual User user { get; set; }
    }
}