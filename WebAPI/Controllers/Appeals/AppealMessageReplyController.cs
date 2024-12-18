using Domain.Admins;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UseCases.Appeals.Replies;
using UseCases.Appeals.Replies.Commands;
using WebAPI.Responses;

namespace WebAPI.Controllers.Appeals
{
    public class AppealMessageReplyController
    {
        private IAppealMessageReplyManager AppealMessageReplyManager;

        public AppealMessageReplyController(IAppealMessageReplyManager appealMessageReplyManager)
        {
            AppealMessageReplyManager = appealMessageReplyManager;
        }
        [HttpPost]
        [Authorize]
        public ActionResult<DataResponse> Create(CreateAppealMessageReplyCommand command)
        {
            var result = AppealMessageReplyManager.Create(command);

            return new DataResponse(true, result);
        }
        [HttpPut]
        [Authorize]
        public ActionResult<SuccessResponse> Update(UpdateAppealMessageReplyCommand command)
        {
            AppealMessageReplyManager.Update(command);

            return new SuccessResponse(true);
        }
        [HttpDelete]
        [Authorize]
        public ActionResult<SuccessResponse> Delete(DeleteAppealMessageReplyCommand command)
        {
            AppealMessageReplyManager.Delete(command);

            return new SuccessResponse(true);
        }
    }
}
