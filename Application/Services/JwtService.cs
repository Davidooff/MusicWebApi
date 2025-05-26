using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Application.Exceptions.Auth;
using Domain.Options;

namespace Application.Services;

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

    public (string accessToken, string refreshToken) GenerateJwtTokens(string userId, string sessionId)
    {
        return (GenerateToken(sessionId, _jwtSettings.accessTokenExpiration),
            GenerateToken(userId, _jwtSettings.refreshTokenExpiration));
    }

    public string genAccToken(string sessionId) =>
        GenerateToken(sessionId, _jwtSettings.accessTokenExpiration);

    public string genRefToken(string userId) =>
        GenerateToken(userId, _jwtSettings.refreshTokenExpiration);

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

    public string GetIdFromToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingCredentials.Key,
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateLifetime = true 
        };
        string id;
        try
        {
            id = tokenHandler.ValidateToken(token, tokenValidationParameters, out _)
                .FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidToken(token);
        } catch
        {
            throw new InvalidToken(token);
        }

        return id;
    }
}
