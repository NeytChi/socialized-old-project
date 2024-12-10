using Domain.Admins;
using Core;
using Serilog;
using System.Web;
using UseCases.AutoPosts;
using UseCases.Exceptions;
using UseCases.Admins.Commands;
using Domain.Users;

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
        public void SetupPassword(SetupPasswordCommand command)
        {
            var admin = adminRepository.GetByPasswordToken(command.Password);
            if (admin == null)
            {
                throw new NotFoundException("Не було знайдено адміна по токену для зміни паролю.");
            }   
            admin.Password = ProfileCondition.HashPassword(command.Password);
            admin.TokenForStart = null;
            adminRepository.Update(admin);
            Logger.Information($"Був налаштован пароль для адміна id={admin.Id}.");
        }
        public Admin Authentication(AuthenticationCommand command, ref string message)
        {
            var admin = adminRepository.GetByEmail(command.Email);
            if (admin == null)
            {
                throw new NotFoundException("Не було знайдено адміна по email-адресі.");
            }
            if (!ProfileCondition.VerifyHashedPassword(admin.Password, command.Password))
            {
                throw new ValidationException("Невірний пароль.");
            }
            Logger.Information($"Був аутентифікований адмін id={admin.Id}.");
            return admin;
            // return Token(admin);
        }
        public void Delete(DeleteAdminCommand command)
        {
            var admin = adminRepository.GetByAdminId(command.AdminId);
            if (admin == null)
            {
                throw new NotFoundException("Не було знайдено адміна по id.");
            }
            admin.IsDeleted = true;
            admin.DeletedAt = DateTime.UtcNow;
            adminRepository.Update(admin);
            Logger.Information($"Адмін був видалений, id={admin.Id}.");
        }
        public Admin[] GetNonDeleteAdmins(int adminId, int since, int count)
        {
            Logger.Information($"Отримано список адмінів, з={since} по={count} адміном id={adminId}.");
            return adminRepository.GetActiveAdmins(adminId, since, count);
        }
        public User[] GetNonDeleteUsers(int since, int count)
        {
            Logger.Information($"Отримано список користувачів, з={since} по={count}.");
            return adminRepository.GetFollowers(since, count);
        }
        public void CreateCodeForRecoveryPassword(string adminEmail, ref string message)
        {
            var admin = adminRepository.GetByEmail(adminEmail);
            if (admin == null)
            {
                throw new NotFoundException("Не було знайдено адміна по email-адресі.");
            }
            admin.RecoveryCode = ProfileCondition.CreateCode(6);
            adminRepository.Update(admin);
            AdminEmailManager.RecoveryPassword(admin.RecoveryCode.Value, admin.Email);
            Logger.Information($"Був створений новий код відновлення паролю адміна, id={admin.Id}.");   
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