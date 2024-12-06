using Microsoft.AspNetCore.Mvc;
using WebAPI.response;

namespace WebAPI.Controllers.Admins
{
    [Route(Version.Current + "/[controller]/[action]/")]
    [ApiController]
    public class ControllerResponseBase : ControllerBase
    {
        public Serilog.ILogger Logger;

        public ObjectResult StatusCode500(string message)
        {
            return StatusCode(500, new AnswerResponse(false, message));
        }
    }
}