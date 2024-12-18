using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UseCases.Appeals.Commands;
using UseCases.Appeals;
using WebAPI.Responses;

namespace WebAPI.Controllers
{
    public class AppealsController : ControllerResponseBase
    {
        private IAppealManager AppealManager;

        public AppealsController(IAppealManager appealManager) 
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
    }
}
