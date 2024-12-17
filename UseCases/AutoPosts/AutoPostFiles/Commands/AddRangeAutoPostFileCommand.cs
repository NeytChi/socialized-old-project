using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.AutoPosts.AutoPostFiles.Commands
{
    public class AddRangeAutoPostFileCommand
    {
        public string UserToken { get; set; }
        public long AutoPostId { get; set; }
        public ICollection<CreateAutoPostFileCommand> Files { get; set; }
    }
}
