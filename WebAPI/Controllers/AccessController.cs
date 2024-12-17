using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UseCases.Appeals;
using UseCases.Appeals.Commands;
using WebAPI.Responses;

namespace WebAPI.Controllers
{
    public class AccessController : ControllerResponseBase
    {
        private IAppealManager AppealManager;

        public AccessController(IAppealManager appealManager)
        {
            AppealManager = appealManager;
        }
        [HttpPost]
        public ActionResult<dynamic> Create(CreateAppealCommand command)
        {
            var result = AppealManager.Create(command);

            return new DataResponse(true, result);
        }
        [HttpGet]
        [ActionName("GetAppealsByUser")]
        public ActionResult<DataResponse> GetAppealsByUser([FromQuery] int since = 0, [FromQuery] int count = 10)
        {
            var userToken = GetAutorizationToken();

            var result = AppealManager.GetAppealsByUser(userToken, since, count);

            return new DataResponse(true, result);
        }
        [HttpGet]
        [Authorize]
        [ActionName("GetAppealsByAdmin")]
        public ActionResult<DataResponse> GetAppealsByAdmin([FromQuery] int since = 0, [FromQuery] int count = 10)
        {
            var userToken = GetAutorizationToken();

            var result = AppealManager.GetAppealsByUser(userToken, since, count);

            return new DataResponse(true, result);
        }
        
        [HttpGet]
        [ActionName("AccessPackages")]
        public ActionResult<dynamic> AccessPackages()
        {
            log.Information("Get access packages");
            return new { success = true, data = new { packages = access.GetPackages() } };
        }
        [HttpGet]
        [ActionName("Discounts")]
        public ActionResult<dynamic> Discounts()
        {
            log.Information("Get discounts");
            return new { success = true, data = new { discounts = access.GetDiscounts() } };
        }
        [HttpPost]
        [ActionName("GetClientToken")]
        public ActionResult<dynamic> GetClientToken(AccessCache cache)
        {
            string clientToken, message = string.Empty; User user;

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
        public User GetNonDeletedUser(string userToken, ref string message)
        {
            User user;

            user = context.Users.Where(u => u.userToken == userToken
                && u.deleted == false).FirstOrDefault();
            if (user == null)
                message = "Server can't define user by token";
            return user;
        }
        public dynamic Return500Error(string message)
        {
            if (Response != null)
                Response.StatusCode = 500;
            log.Warning(message + " IP -> " +
                HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new { success = false, message };
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