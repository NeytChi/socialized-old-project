namespace Domain.Admins
{
    public interface IEmailFollowerRepository
    {
        Follower Update(Follower follower);
        Follower GetByEmail(string email);
    }
}
