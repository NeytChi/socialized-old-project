using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebAPI.Responses;

namespace WebAPI.Controllers
{
    public class AdminsController : ControllerResponseBase
    {
        private IAdminManager AdminManager admins;
        public AdminsController()
        {
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
        public ActionResult<dynamic> CreateAdmin(AdminCache cache)
        {
            Admin admin;
            string message = string.Empty;

            if ((admin = admins.CreateAdmin(cache, ref message)) != null)
            {
                return new
                {
                    success = true,
                    data = new
                    {
                        admin_id = admin.adminId,
                        admin_email = admin.adminEmail,
                        admin_fullname = admin.adminFullname,
                        admin_role = admin.adminRole,
                        created_at = admin.createdAt
                    }
                };
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

        [HttpPost]
        [Authorize]
        [ActionName("DeleteAdmin")]
        public ActionResult<dynamic> DeleteAdmin(AdminCache cache)
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