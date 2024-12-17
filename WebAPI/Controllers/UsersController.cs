using Microsoft.AspNetCore.Mvc;
using UseCases.Users;
using UseCases.Users.Commands;
using WebAPI.Responses;

namespace WebAPI.Controllers
{
    public class UsersController : ControllerResponseBase
    {
        private IUsersManager UserManager;
        private IUserLoginManager UserLoginManager;
        private IUserPasswordRecoveryManager UserPasswordRecoveryManager;

        public UsersController(IUsersManager usersManager, 
            IUserLoginManager userLoginManager,
            IUserPasswordRecoveryManager userPasswordRecoveryManager)
        {
            UserManager = usersManager;
            UserLoginManager = userLoginManager;
            UserPasswordRecoveryManager = userPasswordRecoveryManager;
        }
        [HttpPost]
        [ActionName("Registration")]
        public ActionResult<dynamic> Registration(CreateUserCommand command)
        {
            UserManager.Create(command);

            return new SuccessResponse(true);
        }
        [HttpPost]
        [ActionName("RegistrationEmail")]
        public ActionResult<dynamic> RegistrationEmail([FromQuery] string email)
        {
            var culture = GetCulture();

            UserManager.RegistrationEmail(email, culture);

            return new SuccessResponse(true);
        }
        [HttpPost]
        [ActionName("Login")]
        public ActionResult<dynamic> Login(LoginUserCommand command)
        {
            var result = UserLoginManager.Login(command);
            
            return new DataResponse(true, result);
        }
        [HttpPost]
        [ActionName("LogOut")]
        public ActionResult<dynamic> LogOut()
        {
            var token = GetAutorizationToken();

            UserLoginManager.LogOut(token);

            return new SuccessResponse(true);
        }
        [HttpPost]
        [ActionName("RecoveryPassword")]
        public ActionResult<dynamic> RecoveryPassword([FromQuery] string email)
        {
            var culture = GetCulture();

            UserPasswordRecoveryManager.RecoveryPassword(email, culture);

            return new SuccessResponse(true);
        }
        [HttpPost]
        [ActionName("CheckRecoveryCode")]
        public ActionResult<dynamic> CheckRecoveryCode(CheckRecoveryCodeCommand command)
        {
            string recoveryToken = UserPasswordRecoveryManager.CheckRecoveryCode(command);

            return new DataResponse(true, new { recovery_token = recoveryToken });
        }
        [HttpPost]
        [ActionName("ChangePassword")]
        public ActionResult<dynamic> ChangePassword(ChangePasswordCommand command)
        {
            UserPasswordRecoveryManager.ChangePassword(command);

            return new SuccessResponse(true);
        }
        [HttpPost]
        [ActionName("ChangeOldPassword")]
        public ActionResult<dynamic> ChangeOldPassword(ChangeOldPasswordCommand command)
        {
            UserPasswordRecoveryManager.ChangeOldPassword(command);

            return new SuccessResponse(true);
        }
        [HttpGet]
        [ActionName("Activate")]
        public ActionResult<dynamic> Activate([FromQuery] string hash)
        {
            UserManager.Activate(hash);

            return new SuccessResponse(true);
        }
        [HttpDelete]
        public ActionResult<dynamic> Delete()
        {
            var userToken = GetAutorizationToken();

            UserManager.Delete(userToken);

            return new SuccessResponse(true);
        }
    }
}