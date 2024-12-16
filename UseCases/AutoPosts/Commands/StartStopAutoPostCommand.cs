using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UseCases.AutoPosts.Commands
{
    public class StartStopAutoPostCommand
    {
        public string UserToken { get; set; }
        public long PostId { get; set; }
    }
}
