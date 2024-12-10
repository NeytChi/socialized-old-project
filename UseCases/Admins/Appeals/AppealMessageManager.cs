using Domain.Admins;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Web;
using UseCases.Exceptions;

namespace UseCases.Admins.Appeals
{
    public class AppealMessageManager : BaseManager
    {
        private IAppealRepository AppealRepository;
        private IAppealManager AppealManager;
        public AppealMessageManager(ILogger logger, 
            IAppealRepository appealRepository, 
            IAppealManager appealManager) : base(logger)
        {
            AppealManager = appealManager;
            AppealRepository = appealRepository;
        }
        public AppealMessage Create(CreateAppealMessageByAdminCommand command)
        {
            var appeal = AppealRepository.GetBy(command.AppealId);
            if (appeal == null)
            {
                throw new NotFoundException("Звернення не було визначенно сервером по id.");
            }
            AppealManager.UpdateAppealToAnswered(appeal);

        }
        public AppealMessage Create(CreateAppealMessageByUserCommand command)
        {
            var appeal = AppealRepository.GetBy(command.AppealId, command.UserToken);
            if (appeal == null)
            {
                throw new NotFoundException("Звернення не було визначенно сервером по id.");
            }

            if (AppealMessageIsTrue(cache, ref message))
            {
                var appealMessage = new AppealMessage()
                {
                    AppealId = appeal.Id,
                    adminId = cache.admin_id,
                    messageText = string.IsNullOrEmpty(cache.appeal_message) ? "" : HttpUtility.UrlDecode(cache.appeal_message),
                    createdAt = DateTimeOffset.UtcNow,
                };
                AppealMessageRepository.Create(appealMessage);
                // appealMessage.files = AddFilesToMessage(cache.files, appealMessage.messageId);
                Logger.Information("Add new appeal message, id -> " + appealMessage.appealId);
                return appealMessage;
            }
            return null;
        }
        public dynamic[] GetAppealMessages(int appealId, int since, int count)
        {
            Logger.Information("Get appeal messages, appeal id -> " + appealId);
            return AppealMessageRepository.GetAppealMessages(appealId, since, count);
        }

        public HashSet<AppealFile> AddFilesToMessage(List<IFormFile> upload, long messageId)
        {
            var files = new HashSet<AppealFile>();
            if (upload != null)
            {
                foreach (var file in upload)
                {
                    var saved = new AppealFile();
                    saved.messageId = messageId;
                    // saved.relativePath = fileManager.SaveFile(file, "support");
                    files.Add(saved);
                }
                AppealMessageRepository.AddRange(files);
                Logger.Information("Save files in support");
            }
            return files;
        }
        public bool AppealFilesIsTrue(ref List<IFormFile> files)
        {
            if (files != null)
            {
                if (files.Count >= 1)
                {
                    if (files.Count > 3)
                    {
                        for (int i = 3; i < files.Count; i++)
                        {
                            files.RemoveAt(i);
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
