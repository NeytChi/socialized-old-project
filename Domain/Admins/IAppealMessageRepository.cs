namespace Domain.Admins
{
    public interface IAppealMessageRepository
    {
        void Create(AppealMessage message);
        void Update(AppealMessage message);
        void AddRange(HashSet<AppealFile> appealFiles);
        AppealMessage[] GetAppealMessages(long appealId, int since, int count);
    }
}
