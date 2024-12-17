using Microsoft.AspNetCore.Mvc;
using WebAPI.response;

namespace WebAPI.Controllers
{
    [Route(Version.Current + "/[controller]/[action]/")]
    [ApiController]
    public class ControllerResponseBase : ControllerBase
    {
        public Serilog.ILogger Logger;

        public string GetAutorizationToken()
        {
            return HttpContext?.Request.Headers.Where(h => h.Key == "Authorization").Select(h => h.Value).FirstOrDefault();
        }
        public long GetAdminIdByToken()
        {
            return long.Parse(HttpContext.User.Claims.First().Value);
        }
        public ObjectResult StatusCode500(string message)
        {
            return StatusCode(500, new AnswerResponse(false, message));
        }
    }
}