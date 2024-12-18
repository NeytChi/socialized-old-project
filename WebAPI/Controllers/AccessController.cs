using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    public class AccessController : ControllerResponseBase
    {
        public AccessController()
        {

        }  
        [HttpGet]
        [ActionName("AccessPackages")]
        public ActionResult<dynamic> AccessPackages()
        {
            return new { success = true, data = new { packages = access.GetPackages() } };
        }
        [HttpGet]
        [ActionName("Discounts")]
        public ActionResult<dynamic> Discounts()
        {
            return new { success = true, data = new { discounts = access.GetDiscounts() } };
        }
        [HttpPost]
        [ActionName("GetClientToken")]
        public ActionResult<dynamic> GetClientToken(AccessCache cache)
        {

            if ((user = GetNonDeletedUser(cache.user_token, ref message)) != null)
            {

                clientToken = PackageCondition.gateway.ClientToken.Generate();
                log.Information("Create a new client token for braintree");
                return new { success = true, data = new { client_token = clientToken } };
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("PackagePay")]
        public ActionResult<dynamic> PackagePay(AccessCache cache)
        {
            string message = string.Empty, deviceData = ""; User user;

            if ((user = GetNonDeletedUser(cache.user_token, ref message)) != null)
            {
                PackageAccess package;

                if ((package = access.GetPackageById(cache.package_id)).package_id != 0)
                {
                    decimal price;

                    price = access.CalcPackagePrice(package, cache.month_count);
                    if (access.PayForPackage(price, cache.nonce_token, deviceData, ref message))
                    {
                        access.SetPackage(user.userId, package.package_id, cache.month_count);
                        return new { success = true, data = new { package } };
                    }
                }
                else
                    message = "Server can't define package by package id.";
            }
            return Return500Error(message);
        }
    }
    public struct AccessCache
    {
        public string user_token;
        public string nonce_token;
        public int package_id;
        public int month_count;
    }
}