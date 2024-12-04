namespace Domain.Packages
{
    public class DiscountPackage : BaseEntity
    {
        public double Percent { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
    }
}