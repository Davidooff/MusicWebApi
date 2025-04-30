using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Application.Services;
using MusicWebApi.src.Domain.Entities;
using MusicWebApi.src.Infrastructure.Database;
using MusicWebApi.src.Infrastructure.Redis;
using UAParser;

namespace MusicWebApi.src.Api;

[ApiController]
[Route("users")]
[ServiceFilter(typeof(UsersExceptionFilter))]
public class UsersController : ControllerBase
{
    private readonly UsersRepository _usersService;
    private readonly JwtService _jwtService;
    private readonly AuthService _authService;
    private readonly PasswordHasher<UserAuth> _pwHasher = new PasswordHasher<UserAuth>();

    public UsersController(UsersRepository usersService, JwtService jwtService, TokenRepository tokenRepository, AuthService authService)
    {
        _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    [HttpPost("new")]
    public async Task<IResult> newUser(UserAuth newUser)
    {
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var session = GetSessionInfo(userAgent);
        var newTokens = await _authService.Create(newUser, session);
        return Results.Ok(new {newTokens.accessToken, newTokens.refreshToken});
    }

    [HttpPost("auth")]
    public async Task<IResult> Auth(UserAuth user)
    {
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var session = GetSessionInfo(userAgent);
        var newTokens = await _authService.Auth(user, session);
        return Results.Ok(new { newTokens.accessToken,  newTokens.refreshToken});
    }

    [HttpPatch("updateToken")]
    public async Task<IResult> updateToken([FromBody] string refreshToken)
    {
        var tokens = await _authService.RefreshToken(refreshToken);
        return Results.Ok(tokens);
    }


    /// <summary>
    /// Get user platform from userAgent string
    /// </summary>
    /// <param name="userAgent">userAgent string.</param>
    /// <exception cref="MethodAccessException">
    /// Bot exception
    /// </exception>
    private (string name, ESessionType type) GetSessionInfo(string userAgent)
    {
        var parser = Parser.GetDefault();
        ClientInfo client = parser.Parse(userAgent);

        // Detect device type
        var deviceType = client.Device.Family.ToLower();
        var os = client.OS.Family.ToLower();

        // Classify platform
        if (client.Device.IsSpider) // Skip bots/crawlers
            throw new MethodAccessException();

        ESessionType esession = ESessionType.Other;
        if (deviceType.Contains("mobile"))
            esession = ESessionType.Phone;
        else if (deviceType.Contains("tablet"))
            esession = ESessionType.Tablet;
        else if (deviceType.Contains("desktop") || os.Contains("windows") || os.Contains("mac"))
            esession = ESessionType.PC;

        return (client.UA.Family, esession);
    }
}
