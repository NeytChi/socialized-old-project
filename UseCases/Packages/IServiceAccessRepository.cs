using Domain.Packages;

namespace UseCases.Packages
{
    public interface IServiceAccessRepository
    {
        void Create(ServiceAccess serviceAccess);
        void Update(ServiceAccess serviceAccess);
        ServiceAccess GetBy(long packageId);
        ServiceAccess GetByUser(long userId);
    }
}
