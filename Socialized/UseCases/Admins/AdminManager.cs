using Domain.Admins;
using Core;
using Serilog;
using System.Web;

namespace UseCases.Admins
{
    public class AdminManager
    {
        private IAdminRepository adminRepository;
        private ProfileCondition ProfileCondition { get; set; }
        private SmtpSender SmtpSender { get;set; }
        private readonly ILogger Logger;
        
        public AdminManager(ILogger logger, MailSettings mailSettings)
        {
            Logger = logger;
            ProfileCondition = new ProfileCondition(logger);
            SmtpSender = new SmtpSender(logger, mailSettings);
        }
        public Admin CreateAdmin(AdminCache cache, ref string message)
        {
            if (CheckAdminInfo(cache.admin_email, cache.admin_fullname, ref message)) 
            {
                var admin = new Admin() 
                {
                    adminEmail = cache.admin_email,
                    adminFullname = HttpUtility.UrlDecode(cache.admin_fullname),
                    adminPassword = ProfileCondition.HashPassword(cache.admin_password),
                    adminRole = "default",
                    passwordToken = ProfileCondition.CreateHash(10),
                    createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    lastLoginAt = 0,
                };
                
                SetupPasswordMail(admin.passwordToken, admin.adminEmail);
                Logger.Information("Add new admin, id ->" + admin.adminId);
                return admin;
            }
            return null;
        }
        public bool CheckAdminInfo(string email, string fullname, ref string message)
        {
            if (ProfileCondition.EmailIsTrue(email, ref message))
            {
                if (ProfileCondition.UserNameIsTrue(HttpUtility.UrlDecode(fullname), ref message))
                {
                    if (GetNonDelete(email, ref message) == null)
                    {
                        return true;
                    }
                    else
                    {
                        message = "Admin with this email is exist";
                    }
                }
            }
            return false;
        }
        public bool SetupPassword(AdminCache cache, ref string message)
        {
            Admin admin = GetNonDeleteByToken(cache.password_token, ref message);
            if (admin != null) 
            {
                if (cache.admin_password.Equals(cache.confirm_password))
                {
                    if (ProfileCondition.PasswordIsTrue(cache.admin_password, ref message))
                    {
                        admin.adminPassword = ProfileCondition.HashPassword(cache.admin_password);
                        admin.passwordToken = null;
                        adminRepository.Update(admin);
                        return true;
                    }
                }
                else
                {
                    message = "Passwords are not match to each other.";
                }
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
        public void SetupPasswordMail(string passwordToken, string adminEmail)
        {
            SmtpSender.SendEmail(adminEmail, "Setup password", passwordToken);
            Logger.Information("Send mail message to set up password.");
        }
        public bool DeleteAdmin(AdminCache cache, ref string message)
        {
            Admin admin = GetNonDelete(cache.admin_id, ref message);
            if (admin != null) 
            {
                admin.deleted = true;
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
        public Admin GetNonDelete(string adminEmail, ref string message)
        {
            Admin admin = adminRepository.GetByEmail(adminEmail);
            if (admin == null)
                message = "Unknow admin email.";
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
                admin.recoveryCode = ProfileCondition.CreateCode(6);
                adminRepository.Update(admin);
                RecoveryPasswordMail(admin.recoveryCode.Value, adminEmail);
                Logger.Information("Create code for admin recovery password, id -> " + admin.adminId);
                return true;
            }
            return false;
        }
        public void RecoveryPasswordMail(int code, string adminEmail)
        {
            SmtpSender.SendEmail(adminEmail, "Recovery password", "Code: " + code);
            Logger.Information("Send mail message to recovery password.");
        }
        public bool ChangePassword(AdminCache cache, ref string message) 
        {
            var admin = adminRepository.GetByRecoveryCode(cache.recovery_code);
            if (admin != null)
            {
                if (cache.admin_password.Equals(cache.confirm_password))
                {
                    if (ProfileCondition.PasswordIsTrue(cache.admin_password, ref message))
                    {
                        admin.adminPassword = ProfileCondition.HashPassword(cache.admin_password);
                        admin.recoveryCode = null;
                        adminRepository.Update(admin);
                        Logger.Information("Change password for admin, id -> " + admin.adminId);
                        return true;
                    }
                }
                else
                {
                    message = "Passwords aren't equal to each other.";
                }
            }
            else
            {
                message = "Incorrect code entered";
            }
            return false;
        }
    }
}