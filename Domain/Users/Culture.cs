namespace Domain.Users
{
    public partial class Culture : BaseEntity
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public string Name { get; set; }
    }
}