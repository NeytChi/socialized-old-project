namespace UseCases.Packages
{
    public struct BrainTreeSettings
    {
        public sbyte BraintreeEnvironment { get; set; }
        public string MerchantId { get; set; }
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
    }
}
