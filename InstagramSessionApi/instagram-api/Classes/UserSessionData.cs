using System;
using InstagramApiSharp.Classes.Models;

namespace InstagramApiSharp.Classes
{
    [Serializable]
    public class SessionData
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public InstaUserShort LoggedInUser { get; set; }

        public string RankToken { get; set; }
        public string CsrfToken { get; set; }
        /// <summary>
        ///     Only for facebook login
        /// </summary>
        public string FacebookUserId { get; set; } = string.Empty;
        /// <summary>
        ///     Only for facebook login
        /// </summary>
        public string FacebookAccessToken { get; set; } = string.Empty;

        public static SessionData Empty => new SessionData();
    }
}