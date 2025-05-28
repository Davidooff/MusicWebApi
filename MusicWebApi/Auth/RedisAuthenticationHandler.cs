using Application.Services;
using Domain.Options;
using Infrastructure.Redis;
using Jint;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace MusicWebApi.Auth;

public class RedisAuthenticationHandler : AuthenticationHandler<RedisAuthOptions>
{
    private readonly TokenRepository _tokenRepository;
    private readonly JwtService _jwtService;

    public RedisAuthenticationHandler(
        IOptionsMonitor<RedisAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TokenRepository tokenRepository,
        JwtService jwtService)
        : base(options, logger, encoder)
    {
        _tokenRepository = tokenRepository ?? throw new ArgumentNullException(nameof(tokenRepository));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            var token = Request.Cookies[Options.AccessTokenPath];

            if (string.IsNullOrEmpty(token))
            {
                Logger.LogWarning("No token provided in the request cookies");
                return AuthenticateResult.NoResult();
            }

            var sessionId = _jwtService.GetIdFromToken(token);
            if (string.IsNullOrEmpty(sessionId))
            {
                Logger.LogWarning("Invalid token format");
                return AuthenticateResult.Fail("Invalid token format");
            }

            var userId = await _tokenRepository.getUserIdBySeesion(sessionId);
            if (string.IsNullOrEmpty(userId))
            {
                Logger.LogWarning("Session not found or expired");
                return AuthenticateResult.Fail("Session not found or expired");
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("SessionId", sessionId)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            Logger.LogInformation("Authentication successful for user {UserId}", userId);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Authentication failed");
            return AuthenticateResult.Fail("Authentication failed");
        }
    }
}
