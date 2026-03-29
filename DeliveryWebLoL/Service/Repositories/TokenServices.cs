using DeliveryWebLoL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DeliveryWebLoL.Service
{
    public class TokenServices : ITokenServices
    {
        private readonly IConfiguration _config;
        private readonly byte[] _key;

        public TokenServices(IConfiguration config)
        {
            _config = config;
            _key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing"));
        }

        public string CreateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var creds = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenExpiresMinutes"] ?? "15")),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string CreateRefreshToken()
        {
            // 32 bytes = 256 bits — random, safe for tokens
            var randomBytes = new byte[32];
            RandomNumberGenerator.Fill(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}