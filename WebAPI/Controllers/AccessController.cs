using Microsoft.AspNetCore.Mvc;
using WebAPI.Responses;
using UseCases.Packages;
using UseCases.Packages.Command;

namespace WebAPI.Controllers
{
    public class AccessController : ControllerResponseBase
    {
        private IPackageManager PackageManager;
        public AccessController(IPackageManager packageManager)
        {
            PackageManager = packageManager;
        }  

        [HttpGet]
        [ActionName("AccessPackages")]
        public ActionResult<DataResponse> AccessPackages()
        {
            return new DataResponse(true, new { Packages =  PackageManager.GetPackageAccess() });
        }
        [HttpGet]
        [ActionName("Discounts")]
        public ActionResult<DataResponse> Discounts()
        {
            return new DataResponse(true, new { Discounts = PackageManager.GetDiscountPackageAccess() });
        }
        [HttpPost]
        [ActionName("GetClientToken")]
        public ActionResult<dynamic> GetClientToken()
        {
            var userToken = GetAutorizationToken();
            
            var result = PackageManager.GetClientTokenForPay(userToken);

            return new DataResponse(true, new { ClientToken = result });
        }
        [HttpPost]
        [ActionName("PackagePay")]
        public ActionResult<dynamic> PackagePay(PayForPackageCommand command)
        {
            PackageManager.PayForPackage(command);

            return new SuccessResponse(true);
        }
    }
}