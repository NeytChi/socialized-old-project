namespace Models.SessionComponents
{
    public struct DiscountPackage
    {
        public int discount_id { get; set; }
		public double discount_percent { get; set; }
		public int discount_day { get; set; }
		public int discount_month { get; set; }
    }
}