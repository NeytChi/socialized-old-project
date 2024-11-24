using Serilog;
using System.Linq;
using Serilog.Core;
using Models.Common;
using database.context;
using InstagramService;
using Microsoft.AspNetCore.Mvc;
using Models.SessionComponents;
using System.Collections.Generic;
using Managment;

namespace WebAPI.Controllers
{
    [Route("/auth/")]
    [ApiController]
    public class PingController : ControllerBase
    {
        public Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        public PingController()
        {
        }
        [HttpGet]
        public ActionResult<dynamic> Add()
        {
            return Return200();
        }
        public dynamic Return200()
        {
            if (Response != null)
                Response.StatusCode = 200;
            log.Warning("Ping from facebook IP -> " +
                HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new { success = true };
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
}