namespace UseCases.AutoPosts.Commands
{
    public class DeleteAutoPostCommand
    {
        public string UserToken { get; set; }
        public long AutoPostId { get; set; }
    }
}
