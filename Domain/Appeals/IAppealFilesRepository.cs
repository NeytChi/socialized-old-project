using Domain.Admins;

namespace Domain.Appeals.Messages
{
    public interface IAppealFilesRepository
    {
        ICollection<AppealFile> Create(ICollection<AppealFile> files);
    }
}
