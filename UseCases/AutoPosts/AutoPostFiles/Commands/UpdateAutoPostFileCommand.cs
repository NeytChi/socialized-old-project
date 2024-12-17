using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.AutoPosts.AutoPostFiles.Commands
{
    public class UpdateAutoPostFileCommand : AutoPostFileCommand
    {
        public long Id { get; set; }
    }
}
