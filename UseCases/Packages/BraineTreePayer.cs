using Serilog;
using Braintree;

namespace UseCases.Packages
{
    public class BraineTreePayer : BaseManager
    {
        public static BraintreeGateway gateway;

        public BraineTreePayer(ILogger logger, BrainTreeSettings treeSettings) : base(logger)
        {
            gateway = new BraintreeGateway
            {
                Environment = treeSettings.BraintreeEnvironment == 0 ?
                    Braintree.Environment.SANDBOX : Braintree.Environment.PRODUCTION,
                MerchantId = treeSettings.MerchantId,
                PublicKey = treeSettings.PublicKey,
                PrivateKey = treeSettings.PrivateKey
            };
        }
        public bool PayForPackage(decimal price, string nonceToken, string deviceData, ref string message)
        {
            var request = new TransactionRequest
            {
                Amount = price,
                PaymentMethodNonce = nonceToken,
                DeviceData = deviceData,
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                }
            };
            var result = gateway.Transaction.Sale(request);
            if (result.IsSuccess())
            {
                Logger.Information("Сплачено за пакет.");
                return true;
            }    
            message = result.Message;
            Logger.Error($"Пакет не було сплачено. Помилка={result.Message}.");
            return false;
        }
    }
}
