using Domain.AutoPosting;
using Serilog;
using System.ComponentModel.DataAnnotations;

namespace UseCases.AutoPosts.AutoPostFiles
{
    public class AutoPostFilesManager : BaseManager
    {
        public AutoPostFilesManager(ILogger logger) : base(logger)
        {

        }
        public void FilesIdIsTrue(ICollection<AutoPostFile> files, List<long> filesId)
        {
            if (files.Count != filesId.Count)
            {
                throw new ValidationException("Кількість файлів не співпадає з кількістью id в масиві.");
            }
            foreach (var postFile in files)
            {
                if (!filesId.Contains(postFile.Id))
                {
                    throw new ValidationException($"Id файлу {postFile.Id} не співпадає з масивом id.");
                }
            }
        }
    }
}
