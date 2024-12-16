
namespace UseCases.AutoPosts.Commands
{
    public class CreateAutoPostCommand : AutoPostCommand
    {
        public string UserToken { get; set; }
        public long AccountId { get; set; }
    }
}
