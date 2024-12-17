using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Responses;
using UseCases.Admins;
using UseCases.Admins.Commands;

namespace WebAPI.Controllers
{
    public class AdminsController : ControllerResponseBase
    {
        private IAdminManager AdminManager;
        public AdminsController(IAdminManager adminManager)
        {
            AdminManager = adminManager;
        }
        [HttpPost]
        [ActionName("SignIn")]
        public ActionResult<dynamic> SignIn(AdminCache cache)
        {
            string message = string.Empty;
            string authToken = admins.AuthToken(cache, ref message);
            if (!string.IsNullOrEmpty(authToken))
            {
                return new SuccessResponse(true, new { auth_token = authToken });
            }
            return StatusCode500(message);
        }
        [HttpPost]
        [Authorize]
        [ActionName("Create")]
        public ActionResult<dynamic> Create(CreateAdminCommand command)
        {
            string message = string.Empty;
            var admin = AdminManager.Create(command);
            if (() != null)
            {
                return new DataResponse(true, admin);
            }
            return StatusCode500(message);
        }
        [HttpPost]
        [ActionName("SetupPassword")]
        public ActionResult<dynamic> SetupPassword(AdminCache cache)
        {
            string message = string.Empty;

            if (admins.SetupPassword(cache, ref message))
                return new { success = true, data = new { message = "New password added." } };
            return StatusCode500(message);
        }

        [HttpDelete]
        [Authorize]
        public ActionResult<dynamic> Delete(AdminCache cache)
        {
            string message = string.Empty;
            if (admins.DeleteAdmin(cache, ref message))
                return new { success = true, data = new { message = "Admin was deleted." } };
            return StatusCode500(message);
        }
        [HttpPost]
        [ActionName("RecoveryPassword")]
        public ActionResult<dynamic> RecoveryPassword(AdminCache cache)
        {
            string message = null;
            if (admins.RecoveryPassword(cache.admin_email, ref message))
            {
                return new
                {
                    success = true,
                    message = "Every thing is fine. Check your email to get recovery code."
                };
            }
            return StatusCode500(message);
        }
        [HttpPost]
        [ActionName("ChangePassword")]
        public ActionResult<dynamic> ChangePassword(AdminCache cache)
        {
            string message = null;
            if (admins.ChangePassword(cache, ref message))
            {
                return new { success = true, message = "Your password was changed." };
            }
            return StatusCode500(message);
        }
        [HttpGet]
        [Authorize]
        [ActionName("Admins")]
        public ActionResult<dynamic> GetAdmins([FromQuery] int since = 0, [FromQuery] int count = 10)
        {
            int adminId = int.Parse(HttpContext.User.Claims.First().Value);
            var output = admins.GetNonDeleteAdmins(adminId, since, count);
            return new { success = true, data = output };
        }
        [HttpGet]
        [Authorize]
        [ActionName("Followers")]
        public ActionResult<dynamic> GetFollowers([FromQuery] int since = 0, [FromQuery] int count = 10)
        {
            var output = admins.GetFollowers(since, count);
            return new { success = true, data = output };
        }
        [HttpGet]
        [Authorize]
        [ActionName("Users")]
        public ActionResult<dynamic> GetUsers([FromQuery] int since = 0, [FromQuery] int count = 10)
        {
            var output = admins.GetNonDeleteUsers(since, count);
            return new { success = true, data = output };
        }
    }
}