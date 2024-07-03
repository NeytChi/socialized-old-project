namespace Models.Common
{
    public partial class Culture
    {
        public Culture()
        {
           
        }
        public int cultureId { get; set; }
        public string cultureKey { get; set; }
        public string cultureValue { get; set; }
        public string cultureName { get; set; }
    }
}