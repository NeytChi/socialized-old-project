using Serilog;
using Serilog.Core;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;

using Managment;
using Models.Common;
using database.context;

namespace WebAPI.Controllers
{
    [Route("v1.0/[controller]/[action]/")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        public UsersManager manager;
        public UsersController(Context context)
        {
            this.context = context;
            manager = new UsersManager(log, context);
        }
        [HttpPost]
        [ActionName("Registration")]
        public ActionResult<dynamic> Registration(UserCache user)
        {
            string message = string.Empty;
            user.culture = Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US";
            if (manager.RegistrationUser(user, ref message))
                return new
                {
                    success = true,
                    message = GetMessage("registration", Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US")
                };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("RegistrationEmail")]
        public ActionResult<dynamic> RegistrationEmail(UserCache user)
        {
            string message = null;
            if (manager.RegistrationEmail(user.user_email, Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US", ref message))
                return new
                {
                    success = true,
                    message = GetMessage("confirm_email", Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US")
                };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("Login")]
        public ActionResult<dynamic> Login(UserCache cache)
        {
            string message = null;
            User user = manager.Login(cache, ref message);
            if (user != null)
            {
                return new
                {
                    success = true,
                    data = new
                    {
                        user = new
                        {
                            user_id = user.userId,
                            user_fullname = user.userFullName,
                            user_token = user.userToken,
                            user_email = user.userEmail,
                            created_at = user.createdAt,
                            last_login_at = user.lastLoginAt
                        }
                    }
                };
            }
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("LogOut")]
        public ActionResult<dynamic> LogOut(UserCache user)
        {
            string message = null;
            if (manager.LogOut(user.user_token, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("RecoveryPassword")]
        public ActionResult<dynamic> RecoveryPassword(UserCache user)
        {
            string message = null;
            if (manager.RecoveryPassword(user.user_email, Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US", ref message))
                return new
                {
                    success = true,
                    message = GetMessage("code_email", Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US")
                };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("CheckRecoveryCode")]
        public ActionResult<dynamic> CheckRecoveryCode(UserCache user)
        {
            string message = null;
            string recoveryToken = manager.CheckRecoveryCode(user.user_email,
                user.recovery_code, ref message);
            if (recoveryToken != null)
                return new { success = true, data = new { recovery_token = recoveryToken } };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("ChangePassword")]
        public ActionResult<dynamic> ChangePassword(UserCache user)
        {
            string message = null;
            if (manager.ChangePassword(user.recovery_token,
                user.user_password, user.user_confirm_password, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("ChangeOldPassword")]
        public ActionResult<dynamic> ChangeOldPassword(UserCache user)
        {
            string message = null;
            if (manager.ChangeOldPassword(user.user_token, user.old_password, user.new_password, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        [HttpGet]
        [ActionName("Activate")]
        public ActionResult<dynamic> Activate([FromQuery] string hash)
        {
            string message = null;
            if (manager.Activate(hash, ref message))
                return new
                {
                    success = true,
                    message = GetMessage("activate", Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US")
                };
            return Return500Error(message);
        }
        [HttpPost]
        [ActionName("Delete")]
        public ActionResult<dynamic> Delete(UserCache user)
        {
            string message = null;
            if (manager.Delete(user.user_token, ref message))
                return new { success = true };
            return Return500Error(message);
        }
        [HttpGet]
        [ActionName("Locations")]
        public ActionResult<dynamic> Locations()
        {
            string culture = "en_US";
            if (Request.Headers.ContainsKey("Accept-Language"))
            {
                culture = Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US";
            }
            if (culture == "en_US")
                return new { countries = context.countries.OrderBy(x => x.english).Select(x => x.english).ToArray() };
            else
                return new { countries = context.countries.OrderBy(x => x.name).Select(x => x.name).ToArray() };
        }
        public dynamic Return500Error(string key)
        {
            string culture = "en_US";
            if (Response != null)
                Response.StatusCode = 500;
            if (Request.Headers.ContainsKey("Accept-Language"))
            {
                culture = Request.Headers["Accept-Language"].FirstOrDefault() ?? "en_US";
            }
            log.Warning(key + " IP -> " +
                HttpContext?.Connection.RemoteIpAddress.ToString() ?? "");
            return new
            {
                success = false,
                message = GetMessage(key, culture)
            };
        }
        public string GetMessage(string key, string culture)
        {
            string value;

            value = context.Cultures.Where(c
                => c.cultureKey == key
                && c.cultureName == culture).FirstOrDefault()?.cultureValue ?? null;
            if (value == null)
                value = context.Cultures.Where(c
                => c.cultureKey == key
                && c.cultureName == "en_US").First().cultureValue;
            return value;
        }
    }
}