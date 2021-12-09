using System.Text;
using MinimalWeb.Dto;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;


namespace MinimalWeb.Services
{
    public interface ITokenService
    {
        string BuildToken(UserDto user);
    }
    public class TokenService : ITokenService
    {
        private readonly string _key;
        private readonly string _audience;
        private readonly SymmetricSecurityKey _securityKey;
        private readonly SigningCredentials _credentials;
        private readonly string _issuer;
        private readonly TimeSpan ExpiryDuration = new(0, 30, 0);

        public TokenService(IConfiguration configuration)
        {
            _key = configuration["Jwt:Key"];
            _issuer = configuration["Jwt:Issuer"];
            _audience = configuration["Jwt:Audience"];

            _securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            _credentials = new SigningCredentials(_securityKey, SecurityAlgorithms.HmacSha256Signature);

        }
        public string BuildToken(UserDto user)
        {
            var claims = new[] { new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };

            var tokenDescriptor = new JwtSecurityToken(_issuer, _audience,
                     claims, expires: DateTime.Now.Add(ExpiryDuration), signingCredentials: _credentials);
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}
