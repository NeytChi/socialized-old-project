namespace Domain.Admins
{
    public interface IAppealRepository
    {
        void Create(Appeal appeal);
        void Update(Appeal appeal);
        Appeal GetBy(long appealId);
        Appeal GetBy(long appealId, string userToken);
        Appeal[] GetAppealsBy(int since, int count);
        Appeal[] GetAppealsBy(string userToken, int since = 0, int count = 10);
    }
}
