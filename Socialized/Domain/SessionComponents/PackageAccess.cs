namespace Domain.SessionComponents
{
    public struct PackageAccess
    {
        public int package_id { get; set; }
	    public string package_name { get; set; }
		public double package_price { get; set; }
		public int package_ig_accounts { get; set; }
		public int package_posts { get; set; }
		public int package_stories { get; set; }
		public int analytics_days { get; set; }
    }
}