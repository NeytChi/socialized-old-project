using System;

namespace Models.SessionComponents
{
    public partial class SessionState
    {
        public SessionState()
        {
        }
        public long stateId { get; set; }
        public long accountId { get; set; }
        public bool stateUsable { get; set; }
        public bool stateChallenger { get; set; }
        public bool stateRelogin { get; set; }
        public bool stateSpammed { get; set; }
        public DateTime spammedStarted { get; set; }
        public DateTime spammedEnd { get; set; }
        public virtual IGAccount account { get; set; }
    }
}
