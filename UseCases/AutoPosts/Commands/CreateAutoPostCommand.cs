
using Microsoft.AspNetCore.Http;
using UseCases.AutoPosts.AutoPostFiles.Commands;

namespace UseCases.AutoPosts.Commands
{
    public class CreateAutoPostCommand : AutoPostCommand
    {
        public string UserToken { get; set; }
        public ICollection<CreateAutoPostFileCommand> Files { get; set; }
    }
}
