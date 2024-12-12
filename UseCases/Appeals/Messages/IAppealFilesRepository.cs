using Domain.Admins;

namespace UseCases.Appeals.Messages
{
    public interface IAppealFilesRepository
    {
        ICollection<AppealFile> Create(ICollection<AppealFile> files);
    }
}
