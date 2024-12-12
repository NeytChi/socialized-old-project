using Serilog;
using Core.FileControl;
using Domain.Admins;
using Microsoft.AspNetCore.Http;

namespace UseCases.Appeals.Messages
{
    public interface IAppealFileManager
    {
        HashSet<AppealFile> Create(ICollection<IFormFile> upload, long messageId);
    }
    public class AppealFileManager : BaseManager , IAppealFileManager
    {
        private IFileManager FileManager;
        private IAppealFilesRepository AppealFilesRepository;

        public AppealFileManager(ILogger logger, 
            IFileManager fileManager,
            IAppealFilesRepository appealFilesRepository) : base(logger)
        {
            AppealFilesRepository = appealFilesRepository;
        }
        public HashSet<AppealFile> Create(ICollection<IFormFile> upload, long messageId)
        {
            var files = new HashSet<AppealFile>();
            if (upload != null)
            {
                foreach (var file in upload)
                {
                    var saved = new AppealFile();
                    saved.Id = messageId;
                    saved.RelativePath = FileManager.SaveFile(
                        file.OpenReadStream(), "AppealFiles");
                    files.Add(saved);
                }
                AppealFilesRepository.Create(files);
                Logger.Information("Були створені файли для повідомлення в зверненні.");
            }
            return files;
        }
    }
}
