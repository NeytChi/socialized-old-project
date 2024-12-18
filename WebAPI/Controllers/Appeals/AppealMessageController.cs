using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using UseCases.Appeals.Messages.Commands;
using UseCases.Appeals.Messages;
using WebAPI.Responses;

namespace WebAPI.Controllers.Appeals
{
    public class AppealMessageController : ControllerResponseBase
    {
        private IAppealMessageManager AppealMessageManager;

        public AppealMessageController(IAppealMessageManager appealMessageManager)
        {
            AppealMessageManager = appealMessageManager;
        }
        [HttpPost]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public ActionResult<SuccessResponse> Create(List<IFormFile> files, IFormCollection formData)
        {
            var command = JsonSerializer.Deserialize<CreateAppealMessageCommand>(formData["command"]);
            command.Files = files;

            AppealMessageManager.Create(command);

            return new SuccessResponse(true);
        }
        [HttpPut]
        public ActionResult<SuccessResponse> Update(UpdateAppealMessageCommand command)
        {
            AppealMessageManager.Update(command);

            return new SuccessResponse(true);
        }
        [HttpDelete]
        public ActionResult<SuccessResponse> Delete(DeleteAppealMessageCommand command)
        {
            AppealMessageManager.Delete(command);

            return new SuccessResponse(true);
        }
    }
}
