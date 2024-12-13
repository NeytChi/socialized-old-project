using Domain.Packages;

namespace UseCases.Packages
{
    public interface IDiscountRepository
    {
        DiscountPackage GetBy(int month);
        // discounts.Where(d => monthCount >= d.discount_month).OrderByDescending(d => d.discount_month).FirstOrDefault()
    }
}
