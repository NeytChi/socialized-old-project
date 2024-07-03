using System;
using System.Net;
using InstagramApiSharp.Classes.Android.DeviceInfo;
using System.Collections.Generic;
using InstagramApiSharp.Enums;

namespace InstagramApiSharp.Classes
{
    [Serializable]
    public class StateData
    {
        public AndroidDevice DeviceInfo { get; set; }
        public SessionData UserSession { get; set; }
        public bool IsAuthenticated { get; set; }
        public CookieContainer Cookies { get; set; }
        public List<Cookie> RawCookies { get; set; }
        public InstaApiVersionType? InstaApiVersion { get; set; }
        public InstaTwoFactorLoginInfo _twoFactorInfo { get; set; }
        public InstaChallengeLoginInfo _challengeinfo { get; set; }

    }
}