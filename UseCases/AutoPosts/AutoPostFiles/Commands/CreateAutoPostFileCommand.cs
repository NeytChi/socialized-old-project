using Microsoft.AspNetCore.Http;
using UseCases.AutoPosts.Commands;

namespace UseCases.AutoPosts.AutoPostFiles.Commands
{
    public class CreateAutoPostFileCommand : AutoPostCommand
    {
        public IFormFile FormFile { get; set; }
    }
}
