using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TwoFactorAuthProj.Dtos;
using TwoFactorAuthProj.interfaces;

namespace TwoFactorAuthProj.Services
{
    public class TokenService(IConfiguration _config) : ITokenService
    {


        public async Task<string> Createtoken(UserDto user)
        {
            var tokenSecret = _config["jwt.secret"] ?? throw new Exception("Cannot access token key");
            if (tokenSecret.Length < 64) throw new Exception("your token key need to be longer");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecret));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var claims = new List<Claim>()
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.Name),
                new(ClaimTypes.Email, user.Email),
            };

            return WriteToken(claims, creds);

        }

        private string WriteToken(List<Claim> claims, SigningCredentials creds)
        {

            var tokendiscriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokendiscriptor);
            return tokenHandler.WriteToken(token);
        }

       
    }
}
