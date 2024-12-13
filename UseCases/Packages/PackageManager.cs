using Serilog;
using Domain.Packages;

namespace UseCases.Packages
{
    public class PackageManager : BaseManager
    {
        private IServiceAccessRepository ServiceAccessRepository;
        private IPackageAccessRepository PackageAccessRepository;
        private IDiscountRepository DiscountRepository;
        private IForServerAccessCountingRepository CounterRepository;

        public PackageManager(ILogger logger,
            IServiceAccessRepository serviceAccessRepository,
            IPackageAccessRepository packageAccessRepository,
            IDiscountRepository discountRepository, 
            IForServerAccessCountingRepository autoPostCounterRepository) : base(logger)
        {
            ServiceAccessRepository = serviceAccessRepository;
            PackageAccessRepository = packageAccessRepository;
            DiscountRepository = discountRepository;
            CounterRepository = autoPostCounterRepository;
            
        }
        public ServiceAccess CreateDefaultServiceAccess(long userId)
        {
            var package = PackageAccessRepository.GetFirst();
            var access = new ServiceAccess()
            {
                UserId = userId,
                Available = true,
                Type = package.Id,
                Paid = false
            };
            ServiceAccessRepository.Create(access);
            Logger.Information($"Був створений безкоштовний доступ до сервісу для користувача, id={access.Id}.");
            return access;
        }
        public void SetPackage(long userId, long packageId, int monthCount)
        {
            var access = ServiceAccessRepository.GetBy(userId);
            if (access == null)
            {
                access = CreateDefaultServiceAccess(userId);
            }
            var package = PackageAccessRepository.GetBy(packageId);
            access.Available = true;
            access.Type = package.Id;
            access.Paid = true;
            access.PaidAt = DateTime.UtcNow;
            access.DisableAt = DateTime.UtcNow.AddMonths(monthCount);
            ServiceAccessRepository.Update(access);
            Logger.Information($"Сервісний доступ було оновлено, id={access.Id}.");
        }
        public bool IsServicePackagePersonalize(long userId)
        {
            var access = GetWorkingServiceAccess(userId);

            if (access == null)
            {
                return false;
            }
            var package = PackageAccessRepository.GetBy(access.Type);

            if (package.IGAccounts == -1 
                && package.Stories == -1 
                && package.Posts == -1)
            {
                return true;
            }
            var accounts = CounterRepository.GetAccounts(userId);
            var posts = CounterRepository.Get(userId, true);
            var storyPosts = CounterRepository.Get(userId, false);

            if (accounts.Count > package.IGAccounts)
            {
                Logger.Warning("Instagram аккаунтів більше ніж доступно по пакету.");
                return false;
            }
            if (posts.Count > package.Posts)
            {
                Logger.Warning("Кількість постів перебільшує кількість доступний по сервісному пакету.");
                return false;
            }
            if (storyPosts.Count > package.Stories)
            {
                Logger.Warning("Кількість сторіc перебільшує кількість доступний по сервісному пакету.");
                return false;
            }
            return true;
        }
        public ServiceAccess GetWorkingServiceAccess(long userId)
        {
            var access = ServiceAccessRepository.GetByUser(userId);

            if (access.Type != 1 && access.DisableAt < DateTime.UtcNow)
            {
                SetPackage(userId, 1, -1);
                Logger.Information($"Сервісний пакет закінчив свій термін придатності, id={access.Id}.");
            }
            return access;
        }
        public decimal CalcPackagePrice(PackageAccess package, int monthCount)
        {
            decimal discountPrice = 0;
            var discount = DiscountRepository.GetBy(monthCount);

            if (discount != null)
            {
                discountPrice = (decimal)(package.Price * discount.Month / 100 * discount.Percent);
            }
            Logger.Information($"Порахована ціна на сервісний пакет, id={package.Id}.");
            return (decimal)package.Price * monthCount - discountPrice;
        }
        
    }
}








