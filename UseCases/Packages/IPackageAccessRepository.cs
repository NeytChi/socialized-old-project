using Domain.Packages;

namespace UseCases.Packages
{
    public interface IPackageAccessRepository
    {
        PackageAccess GetFirst();
        PackageAccess GetBy(long packageId);
    }
}
