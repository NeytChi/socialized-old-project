
using Microsoft.AspNetCore.Http;

namespace UseCases.AutoPosts.Commands
{
    public class CreateAutoPostCommand : AutoPostCommand
    {
        public string UserToken { get; set; }
        public ICollection<IFormFile> formFiles { get; set; }
    }
}
