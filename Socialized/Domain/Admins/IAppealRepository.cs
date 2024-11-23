namespace Domain.Admins
{
    public interface IAppealRepository
    {
        void Create(Appeal appeal);
        void Update(Appeal appeal);
        Appeal GetBy(int appealId);
        Appeal GetBy(int appealId, string userToken);
        Appeal[] GetAppealsBy(int since, int count);
        Appeal[] GetAppealsBy(string userToken, int since = 0, int count = 10);
    }
}
