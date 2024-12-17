using Domain.GettingSubscribes;
using Domain.InstagramAccounts;
using Serilog;
using UseCases.Exceptions;

namespace UseCases.InstagramAccounts
{
    public class DeleteIgAccountManager : BaseManager
    {
        private IIGAccountRepository AccountRepository;
        private ITaskGettingSubscribesRepository TaskGettingSubscribesRepository;

        public DeleteIgAccountManager(ILogger logger, 
            IIGAccountRepository accountRepository,
            ITaskGettingSubscribesRepository taskGettingSubscribesRepository) : base(logger)
        {
            AccountRepository = accountRepository;
            TaskGettingSubscribesRepository = taskGettingSubscribesRepository;
        }
        public void Delete(long accountId)
        {
            var account = AccountRepository.GetByWithState(accountId);
            if (account == null)
            {
                throw new NotFoundException("Сервер не визначив запис Instagram аккаунту по id.");
            }
            account.IsDeleted = true;
            AccountRepository.Update(account);
            Logger.Information($"Instagram аккаунт був видалений, id={accountId}.");
            StopTasksGettingSubscribes(accountId);
        }
        public void StopTasksGettingSubscribes(long accountId)
        {
            var tasks = TaskGettingSubscribesRepository.GetBy(accountId);
            foreach (var task in tasks)
            {
                task.Deleted = true;
                task.Stopped = true;
                task.Updated = true;
            }
            TaskGettingSubscribesRepository.Update(tasks);
            Logger.Information($"Всі задачі були закриті по Instagram аккаунту, id={accountId}.");
        }
    }
}
