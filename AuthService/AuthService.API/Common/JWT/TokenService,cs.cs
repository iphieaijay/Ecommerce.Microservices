using AuthService.API.Domain;
using AuthService.API.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.API.Common.JWT
{
    
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly AuthServiceDbContext _db;

        public TokenService(IConfiguration config, AuthServiceDbContext db)
        {
            _config = config;
            _db = db;
        }

        public async Task<string> GenerateAccessToken(AppUser user, IList<string> roles)
        {
            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
        };
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                _config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshToken(AppUser user)
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.UtcNow.AddDays(7),
                UserId = user.Id
            };
            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();
            return refreshToken;
        }

        public async Task RevokeRefreshToken(string token)
        {
            var refreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                await _db.SaveChangesAsync();
            }
        }
    }

}
