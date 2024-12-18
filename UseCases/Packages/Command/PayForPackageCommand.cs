namespace UseCases.Packages.Command
{
    public class PayForPackageCommand
    {
        public string UserToken { get; set; }
        public string NonceToken { get; set; }
        public long PackageId { get; set; }
        public int MonthCount { get; set; }
    }
}
