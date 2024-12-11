namespace UseCases.Users.Commands
{
    public class CreateUserCommand
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Password { get; set; }
        public string CountryName { get; set; }
        public int TimeZone { get; set; }
        public string Culture { get; set; }
    }
}
