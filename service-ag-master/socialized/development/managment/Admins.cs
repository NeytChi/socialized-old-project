using System;
using System.Web;
using System.Linq;
using System.Security.Claims;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Serilog;
using Serilog.Core;

using Common;
using socialized;
using database.context;
using Models.AdminPanel;

namespace Managment
{
    public class Admins
    {
        private Context context;
        private ProfileCondition profileCondition;
        private MailF mail;
        public Logger log;
        public string emailSetupPassword;
        public Admins(Logger log, Context context)
        {
            this.emailSetupPassword = Program.serverConfiguration().GetValue<string>("email_admin_setup_password");
            this.context = context;
            this.log = log;
            this.profileCondition = new ProfileCondition(log);
            this.mail = new MailF(log);
        }
        public Admins()
        {
            this.context = new Context(false);
            this.log = new LoggerConfiguration()
                .WriteTo.File("./logs/log", rollingInterval: RollingInterval.Day)
                .CreateLogger();
            this.profileCondition = new ProfileCondition(log);
            this.mail = new MailF(log);
        }
        public Admin CreateAdmin(AdminCache cache, ref string message)
        {
            if (CheckAdminInfo(cache, ref message)) {
                Admin admin = new Admin() {
                    adminEmail = cache.admin_email,
                    adminFullname = HttpUtility.UrlDecode(cache.admin_fullname),
                    adminPassword = profileCondition.HashPassword(cache.admin_password),
                    adminRole = "default",
                    passwordToken = profileCondition.CreateHash(10),
                    createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    lastLoginAt = 0,
                };
                context.Admins.Add(admin);
                context.SaveChanges();
                SetupPasswordMail(admin.passwordToken, admin.adminEmail);
                log.Information("Add new admin, id ->" + admin.adminId);
                return admin;
            }
            return null;
        }
        public bool CheckAdminInfo(AdminCache cache, ref string message)
        {
            if (profileCondition.EmailIsTrue(cache.admin_email, ref message)
                && profileCondition.UserNameIsTrue(HttpUtility.UrlDecode(cache.admin_fullname), ref message)) {
                if (GetNonDelete(cache.admin_email, ref message) == null) {
                    return true;
                }
                else 
                    message = "Admin with this email is exist";
            }
            return false;
        }
        public bool SetupPassword(AdminCache cache, ref string message)
        {
            Admin admin = GetNonDeleteByToken(cache.password_token, ref message);
            if (admin != null) {
                if (cache.admin_password.Equals(cache.confirm_password)) {
                    if (profileCondition.PasswordIsTrue(cache.admin_password, ref message)) {
                        admin.adminPassword = profileCondition.HashPassword(cache.admin_password);
                        admin.passwordToken = null;
                        context.Admins.Update(admin);
                        context.SaveChanges();
                        return true;
                    }
                }
                else message = "Passwords are not match to each other.";
            }
            return false;
        }
        public string AuthToken(AdminCache cache, ref string message)
        {
            Admin admin = GetNonDelete(cache.admin_email, ref message);
            if (admin != null) {
                if (profileCondition.VerifyHashedPassword(admin.adminPassword, cache.admin_password))
                    return Token(admin);
                else
                    message = "Wrong password.";
            }
            return string.Empty;
        }
        public void SetupPasswordMail(string passwordToken, string adminEmail)
        {
            mail.SendEmail(adminEmail, "Set up password", emailSetupPassword + passwordToken);
            log.Information("Send mail message to set up password.");
        }
        public bool DeleteAdmin(AdminCache cache, ref string message)
        {
            Admin admin = GetNonDelete(cache.admin_id, ref message);
            if (admin != null) {
                admin.deleted = true;
                context.Admins.Update(admin);
                context.SaveChanges();
                log.Information("Delete admin, id -> " + admin.adminId);
                return true;
            }
            return false;
        }
        public dynamic[] GetNonDeleteAdmins(int adminId, int since, int count)
        {
            log.Information("Get admins, since ->" + since + " count ->" + count + ", admin id ->" + adminId);
            return context.Admins.Where(a => a.adminId != adminId
                && a.deleted == false)
            .Skip(since * count)
            .Take(count)
            .Select(a => new { 
                    admin_id = a.adminId,
                    admin_email = a.adminEmail,
                    admin_fullname = a.adminFullname,
                    admin_role = a.adminRole,
                    created_at = a.createdAt,
                    last_login_at = a.lastLoginAt} )
            .ToArray();
        }
        public dynamic[] GetFollowers(int since, int count)
        {
            log.Information("Get followers, since -> " + since + " count -> " + count);
            return (from follower in context.Followers  
            join user in context.Users on follower.userId equals user.userId into user
            orderby follower.followerId descending
            select new { 
                follower_id = follower.followerId,
                follower_email = follower.followerEmail,
                created_at = follower.createdAt,
                enable_mailing = follower.enableMailing,
                has_user = follower.userId != 0 
                    && user.All(u => u.deleted  == false && u.activate == true) ? true : false
            })
            .Skip(since * count)
            .Take(count)
            .ToArray();
        }
        public dynamic[] GetNonDeleteUsers(int since, int count)
        {
            log.Information($"Get users, since -> {since} count -> {count}");
            return (from user in context.Users  
                join profile in context.UserProfile on user.userId equals profile.userId
                join follower in context.Followers on user.userId equals follower.userId into followers    
            where user.deleted == false
              && user.activate == true 
            orderby user.userId descending
            select new { 
                user_email = user.userEmail,
                country = profile.country,
                registration = user.createdAt,
                activity = user.lastLoginAt,
                sessions_count = (from businessAccount in context.BusinessAccounts
                                where businessAccount.userId == user.userId 
                                    && !businessAccount.deleted
                                select businessAccount).Count(),
                subscription = followers.Count() == 1 ? true : false
            })
            .Skip(since * count)
            .Take(count)
            .ToArray();
        }
        public Admin GetNonDelete(int adminId, ref string message)
        {
            Admin admin = context.Admins.Where(a 
                => a.adminId == adminId
                && a.deleted == false).FirstOrDefault();
            if (admin == null)
                message = "Unknow admin id.";
            return admin;
        }
        public Admin GetNonDelete(string adminEmail, ref string message)
        {
            Admin admin = context.Admins.Where(a 
                => a.adminEmail == adminEmail 
                && a.deleted == false).FirstOrDefault();
            if (admin == null)
                message = "Unknow admin email.";
            return admin;
        }
        public Admin GetNonDeleteByToken(string passwordToken, ref string message)
        {
            Admin admin = context.Admins.Where(a 
                => a.passwordToken == passwordToken 
                && a.deleted == false).FirstOrDefault();
            if (admin == null)
                message = "Unknow admin password token.";
            return admin;
        }
        
        private string Token(Admin admin)
        {
            var identity = GetIdentity(admin);
            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                notBefore: now,
                claims: identity.Claims,
                expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), 
                SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        private ClaimsIdentity GetIdentity(Admin admin)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, admin.adminId.ToString()),
                new Claim(ClaimsIdentity.DefaultNameClaimType, admin.adminEmail),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, admin.adminRole)
            };
            ClaimsIdentity claimsIdentity =
                new ClaimsIdentity(claims, "Bearer Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
            return claimsIdentity;
        }
        public bool RecoveryPassword(string adminEmail, ref string message)
        {
            Admin admin = GetNonDelete(adminEmail, ref message);
            if (admin != null) {
                admin.recoveryCode = profileCondition.CreateCode(6);
                context.Admins.Update(admin);
                context.SaveChanges();
                RecoveryPasswordMail((int)admin.recoveryCode, adminEmail);
                log.Information("Create code for admin recovery password, id -> " + admin.adminId);
                return true;
            }
            return false;
        }
        public void RecoveryPasswordMail(int code, string adminEmail)
        {
            mail.SendEmail(adminEmail, "Recovery password", "Code:" + code);
            log.Information("Send mail message to recovery password.");
        }
        public bool ChangePassword(AdminCache cache, ref string message) 
        {
            Admin admin = context.Admins.Where(a => a.recoveryCode == cache.recovery_code).FirstOrDefault();
            if (admin != null) {
                if (cache.admin_password.Equals(cache.confirm_password)) {
                    if (profileCondition.PasswordIsTrue(cache.admin_password, ref message)) {   
                        admin.adminPassword = profileCondition.HashPassword(cache.admin_password);
                        admin.recoveryCode = null;
                        context.Admins.Update(admin);
                        context.SaveChanges();
                        log.Information("Change password for admin, id -> " + admin.adminId);
                        return true;
                    }
                }
                else 
                    message = "Passwords aren't equal to each other.";
            }
            else 
                message = "Incorrect code entered";
            return false;
        }
    }
}