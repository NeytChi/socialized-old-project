using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace Socialized.Core
{
    public class AuthOptions
    {
        public AuthOptions()
        {
            ISSUER = config.GetValue<string>("issuer");
            AUDIENCE = config.GetValue<string>("audience");
            KEY = config.GetValue<string>("auth_key");
            LIFETIME = config.GetValue<int>("auth_lifetime");
        }
        public static string ISSUER;
        public static string AUDIENCE;
        private static string KEY;
        public static int LIFETIME = 1;
        
        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new SymmetricSecurityKey(Encoding.ASCII.GetBytes(KEY));
        }
    }
}