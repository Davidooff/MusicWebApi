using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MusicWebApi.Models;

namespace MusicWebApi.Services;

public class JwtService
{
    private readonly JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;

    public JwtService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            SecurityAlgorithms.HmacSha256);
    }

    public (string accessToken, string refreshToken) GenerateJwtTokens(string userId)
    {
        return (GenerateToken(userId, _jwtSettings.accessTokenExpiration),
            GenerateToken(userId, _jwtSettings.refreshTokenExpiration));
    }

    public string GenerateToken(string userId, int expirationMinutes)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId)
            }),
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            SigningCredentials = _signingCredentials
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
