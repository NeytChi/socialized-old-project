namespace Domain.Packages
{
    public class PackageAccess : BaseEntity
    {
        public string Name { get; set; }
        public double Price { get; set; }
        public int IGAccounts { get; set; }
        public int Posts { get; set; }
        public int Stories { get; set; }
        public int AnalyticsDays { get; set; }
    }
}