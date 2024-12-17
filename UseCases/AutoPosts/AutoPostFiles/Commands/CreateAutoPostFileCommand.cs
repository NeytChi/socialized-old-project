using Microsoft.AspNetCore.Http;

namespace UseCases.AutoPosts.AutoPostFiles.Commands
{
    public class CreateAutoPostFileCommand : AutoPostFileCommand
    {
        public IFormFile FormFile { get; set; }
    }
}
