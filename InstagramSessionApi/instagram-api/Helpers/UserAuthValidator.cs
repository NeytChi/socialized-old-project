/*
 * Developer: Ramtin Jokar [ Ramtinak@live.com ] [ My Telegram Account: https://t.me/ramtinak ]
 * 
 * Github source: https://github.com/ramtinak/InstagramApiSharp
 * Nuget package: https://www.nuget.org/packages/InstagramApiSharp
 * 
 * IRANIAN DEVELOPERS
 */

using InstagramApiSharp.Classes;
using System;
using System.Diagnostics;

namespace InstagramApiSharp.Helpers
{
    public class UserAuthValidate
    {
        public bool IsUserAuthenticated { get; internal set; }
        public SessionData User;
    }
    public static class UserAuthValidator
    {
        public static void Validate(UserAuthValidate userAuthValidate)
        {
            ValidateUser(userAuthValidate.User);
            ValidateLoggedIn(userAuthValidate.IsUserAuthenticated);
        }
        public static void Validate(SessionData user, bool isUserAuthenticated)
        {
            ValidateUser(user);
            ValidateLoggedIn(isUserAuthenticated);
        }
        private static bool ValidateUser(SessionData user)
        {
            if (string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password))
            {
                Debug.WriteLine("user name and password must be specified");
                return false;
            }
            return true;
        }
        private static bool ValidateLoggedIn(bool isUserAuthenticated)
        {
            if (!isUserAuthenticated)
            {
                Debug.WriteLine("user must be authenticated");
                return false;
            }
            return true;
        }
    }
}
