using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Domain;

namespace WebAPI.Middleware
{
    public class JwtAdmins
    {
        public string Token(Admin admin)
        {
            var identity = GetIdentity(admin);
            var now = DateTime.UtcNow;
            var jwt = new JwtSecurityToken(
                issuer: AuthOptions.ISSUER,
                audience: AuthOptions.AUDIENCE,
                notBefore: now,
                claims: identity.Claims,
                expires: now.Add(TimeSpan.FromMinutes(AuthOptions.LIFETIME)),
                signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(),
                SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
        private ClaimsIdentity GetIdentity(Admin admin)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, admin.Id.ToString()),
                new Claim(ClaimsIdentity.DefaultNameClaimType, admin.Email),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, admin.Role)
            };
            ClaimsIdentity claimsIdentity =
                new ClaimsIdentity(claims, "Bearer Token", ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType);
            return claimsIdentity;
        }
    }
}
