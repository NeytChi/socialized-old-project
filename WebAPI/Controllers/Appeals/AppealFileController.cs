using Microsoft.AspNetCore.Mvc;
using UseCases.Appeals.Messages;
using WebAPI.Responses;

namespace WebAPI.Controllers.Appeals
{
    public class AppealFileController : ControllerResponseBase
    {
        private IAppealFileManager AppealFileManager;

        public AppealFileController(IAppealFileManager appealFileManager)
        {
            AppealFileManager = appealFileManager;
        }
        [HttpPost]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public ActionResult<DataResponse> Create([FromQuery] long messageId, ICollection<IFormFile> files)
        {
            var result = AppealFileManager.Create(files, messageId);

            return new DataResponse(true, result);
        }
    }
}
