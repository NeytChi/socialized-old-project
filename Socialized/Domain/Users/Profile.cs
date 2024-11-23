namespace Domain.Users
{
    public partial class Profile
    {
        public Profile()
        {
           
        }
        public int userId { get; set; }
        public int profileId { get; set; }
        public string country { get; set; }
        public long timezone { get; set; }
        public virtual User user { get; set; }

    }
}