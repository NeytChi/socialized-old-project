using WebAPI.Responses;
using Domain.AutoPosting;
using UseCases.AutoPosts;
using UseCases.AutoPosts.Commands;
using UseCases.AutoPosts.AutoPostFiles.Commands;
using UseCases.AutoPosts.AutoPostFiles;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    public class AutoPostController : ControllerResponseBase
    {
        private IAutoPostManager AutoPostManager;
        private IAutoPostFileManager AutoPostFileManager;
        
        public AutoPostController(IAutoPostManager autoPostManager, 
            IAutoPostFileManager autoPostFileManager)
        {
            AutoPostManager = autoPostManager;
            AutoPostFileManager = autoPostFileManager;
        }
        [HttpPost]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public ActionResult<dynamic> Create(List<IFormFile> files, IFormCollection formData)
        {
            var command = JsonSerializer.Deserialize<CreateAutoPostCommand>(formData["command"]);
            for (int i = 0; i < files.Count; i++)
            {
                command.Files.Add(new CreateAutoPostFileCommand
                {
                    FormFile = files[i],
                    Order = (sbyte) i
                });
            }

            AutoPostManager.Create(command);

            return new SuccessResponse(true);
        }
        [HttpGet]
        public ActionResult<dynamic> Get([FromQuery] long accountId, 
            [FromQuery] DateTime from, 
            [FromQuery] DateTime to, 
            [FromQuery] int since = 1, 
            [FromQuery] int count = 10)
        {
            var userToken = GetAutorizationToken();
            var command = new GetAutoPostsCommand
            {
                UserToken = userToken, AccountId = accountId,
                From = from, To = to,
                Since = since, Count = count
            };

            AutoPostManager.Get(command);

            return new SuccessResponse(true);
        }
        [HttpPut]
        public ActionResult<dynamic> Update(UpdateAutoPostCommand command)
        {
            AutoPostManager.Update(command);

            return new SuccessResponse(true);
        }
        [HttpDelete]
        public ActionResult<dynamic> Delete(DeleteAutoPostCommand command)
        {
            AutoPostManager.Delete(command);

            return new SuccessResponse(true);
        }
        [HttpPost]
        [ActionName("AddFiles")]
        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        public ActionResult<dynamic> AddFiles(List<IFormFile> files, IFormCollection formData)
        {
            var command = JsonSerializer.Deserialize<AddRangeAutoPostFileCommand>(formData["command"]);
            for (int i = 0; i < files.Count; i++)
            {
                command.Files.Add(new CreateAutoPostFileCommand
                {
                    FormFile = files[i],
                    Order = (sbyte)i
                });
            }
            return new SuccessResponse(true);
        }
        [HttpDelete]
        [ActionName("DeleteFile")]
        public ActionResult<dynamic> DeleteFile(DeleteAutoPostFileCommand command)
        {
            AutoPostFileManager.Delete(command);

            return new SuccessResponse(true);
        }
    }
}