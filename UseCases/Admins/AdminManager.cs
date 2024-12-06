using Domain.Admins;
using Core;
using Serilog;
using System.Web;
using UseCases.AutoPosts.Commands;
using UseCases.AutoPosts;

namespace UseCases.Admins
{
    public interface IAdminManager
    {
        Admin Create(CreateAdminCommand command);
    }
    public class AdminManager : BaseManager, IAdminManager
    {
        private IAdminRepository adminRepository;
        private IAdminEmailManager AdminEmailManager;
        private ProfileCondition ProfileCondition = new ProfileCondition();
        
        public AdminManager(ILogger logger, IAdminEmailManager adminEmailManager) : base(logger)
        {
            AdminEmailManager = adminEmailManager;
        }
        public Admin Create(CreateAdminCommand command)
        {
            if (adminRepository.GetByEmail(command.Email) != null)
            {
                throw new DuplicateWaitObjectException($"Admin with email={command.Email} is already exist.");
            }
            var admin = new Admin
            {
                Email = command.Email,
                FirstName = HttpUtility.UrlDecode(command.FirstName),
                LastName = HttpUtility.UrlDecode(command.LastName),
                Password = ProfileCondition.HashPassword(command.Password),
                Role = "default",
                TokenForStart = ProfileCondition.CreateHash(10),
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            AdminEmailManager.SetupPassword(admin.TokenForStart, admin.Email);
            Logger.Information($"Був створений новий адмін, id={admin.Id}.");
            return admin;
        }

        public bool SetupPassword(SetupPasswordCommand command)
        {
            var admin = GetNonDeleteByToken(cache.password_token, ref message);
            if (admin != null) 
            {
                admin.Password = ProfileCondition.HashPassword(command.Password);
                admin.TokenForStart = null;
                adminRepository.Update(admin);
                return true;
            }
            return false;
        }
        public Admin AuthToken(AdminCache cache, ref string message)
        {
            var admin = GetNonDelete(cache.admin_email, ref message);
            if (admin != null) 
            {
                if (ProfileCondition.VerifyHashedPassword(admin.adminPassword, cache.admin_password))
                {
                    return admin;
                    // return Token(admin);
                }
                else
                {
                    message = "Wrong password.";
                }
            }
            return null;
        }
        public bool Delete(DeleteAdminCommand command)
        {
            Admin admin = GetNonDelete(command.AdminId, ref message);
            if (admin != null) 
            {
                admin.IsDeleted = true;
                admin.DeletedAt = DateTime.UtcNow;
                adminRepository.Update(admin);
                Logger.Information("Delete admin, id -> " + admin.adminId);
                return true;
            }
            return false;
        }
        public dynamic[] GetNonDeleteAdmins(int adminId, int since, int count)
        {
            Logger.Information("Get admins, since ->" + since + " count ->" + count + ", admin id ->" + adminId);
            return adminRepository.GetActiveAdmins(adminId, since, count);
        }
        public dynamic[] GetFollowers(int since, int count)
        {
            Logger.Information("Get followers, since -> " + since + " count -> " + count);
            return adminRepository.GetFollowers(since, count);
        }
        public dynamic[] GetNonDeleteUsers(int since, int count)
        {
            Logger.Information($"Get users, since -> {since} count -> {count}");
            return adminRepository.GetFollowers(since, count);
        }
        public Admin GetNonDelete(int adminId, ref string message)
        {
            Admin admin = adminRepository.GetByAdminId(adminId);
            if (admin == null)
                message = "Unknow admin id.";
            return admin;
        }
        public Admin GetNonDeleteByToken(string passwordToken, ref string message)
        {
            Admin admin = adminRepository.GetByPasswordToken(passwordToken);
            if (admin == null)
                message = "Unknow admin password token.";
            return admin;
        }
        public bool RecoveryPassword(string adminEmail, ref string message)
        {
            var admin = GetNonDelete(adminEmail, ref message);
            if (admin != null) 
            {
                admin.RecoveryCode = ProfileCondition.CreateCode(6);
                adminRepository.Update(admin);
                AdminEmailManager.RecoveryPassword(admin.RecoveryCode.Value, admin.Email);
                Logger.Information($"Був створений новий код відновлення паролю адміна, id={admin.Id}.");
                return true;
            }
            return false;
        }
        public void ChangePassword(ChangePasswordCommand command) 
        {
            var admin = adminRepository.GetByRecoveryCode(command.RecoveryCode);
            if (admin == null)
            {
                throw new ArgumentNullException("Сервер не визначив адміна по коду. Неправильний код.");
            }    
            admin.Password = ProfileCondition.HashPassword(command.Password);
            admin.RecoveryCode = null;
            adminRepository.Update(admin);
            Logger.Information($"Був змінений пароль у адміна, id={admin.Id}.");
        }
    }
}