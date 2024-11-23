using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Core;

using database.context;
using socialized.response;

namespace socialized.Controllers
{
    public class ControllerResponseHandler : ControllerBase
    {
        public Context context;
        public Logger log = new LoggerConfiguration()
            .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
        public AnswerResponse StatusCode500(string key)
        {
            string culture = "en_US";
            if (Response != null)
                Response.StatusCode = 500;
            if (Request.Headers.ContainsKey("Accept-Language")) {
                culture = Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US";
            }
            log.Warning(key + " IP -> " + 
                HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new AnswerResponse(false, GetMessage(key, culture));
        }
        public string GetMessage(string key, string culture)
        {
            string value;
            
            value = context.Cultures.Where(c
                => c.cultureKey == key
                && c.cultureName == culture).Select(x => x.cultureValue).FirstOrDefault();
            if (value == null)
                value = context.Cultures.Where(c 
                => c.cultureKey == key
                && c.cultureName == "en_US").Select(x => x.cultureValue).FirstOrDefault();
            if (value == null)
                return key;
            return value;
        }
    }
}