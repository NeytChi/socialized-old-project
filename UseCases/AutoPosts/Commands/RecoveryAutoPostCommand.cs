using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.AutoPosts.Commands
{
    public class RecoveryAutoPostCommand : AutoPostCommand
    {
        public string UserToken { get; set; }
        public long AutoPostId { get; set; }
    }
}
