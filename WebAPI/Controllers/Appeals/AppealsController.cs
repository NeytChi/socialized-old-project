using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UseCases.Appeals;
using UseCases.Appeals.Commands;
using WebAPI.Responses;

namespace WebAPI.Controllers.Appeals
{
    public class AppealsController : ControllerResponseBase
    {
        private IAppealManager AppealManager;

        public AppealsController(IAppealManager appealManager)
        {
            AppealManager = appealManager;
        }
        [HttpPost]
        public ActionResult<SuccessResponse> Create(CreateAppealCommand command)
        {
            AppealManager.Create(command);

            return new SuccessResponse(true);
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
            var result = AppealManager.GetAppealsByAdmin(since, count);

            return new DataResponse(true, result);
        }
    }
}
