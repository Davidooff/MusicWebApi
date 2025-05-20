using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Application.Services;
using MusicWebApi.src.Domain.Entities;
using MusicWebApi.src.Domain.Exceptions.Auth;
using MusicWebApi.src.Domain.Options;
using UAParser;

namespace MusicWebApi.src.Api;

[ApiController]
[Route("users")]
[ServiceFilter(typeof(UsersExceptionFilter))]
public class UsersController : ControllerBase
{
    private readonly AuthService _authService;

    private readonly CookieOptions accOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        MaxAge = TimeSpan.FromMinutes(15), 
    };

    private readonly CookieOptions refOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        MaxAge = TimeSpan.FromMinutes(60), 
        Path = "/users/updateToken" 
    };

    private readonly CookieOptions verOptions = new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        MaxAge = TimeSpan.FromMinutes(15),
        Path = "/users/verify"
    };

    private readonly string accessTokenPath;
    private readonly string refreshTokenPath;

    public UsersController(AuthService authService, IOptions<JwtSettings> _jwtSettings)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        accessTokenPath = _jwtSettings.Value.AccessTokenStorage ?? throw new ArgumentNullException(nameof(_jwtSettings));
        refreshTokenPath = _jwtSettings.Value.RefreshTokenStorage ?? throw new ArgumentNullException(nameof(_jwtSettings));

    }


    [HttpPost("new")]
    public async Task<IResult> NewUser(UserRegister newUser)
    {
        string newToken = await _authService.Create(newUser);
        Response.Cookies.Append(accessTokenPath, newToken, verOptions);
        return Results.Ok();
    }

    [Authorize]
    [HttpPost("verify")]
    public async Task<IResult> Verify(CodeVerify codeVerify)
    {
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var session = GetSessionInfo(userAgent);
        Console.WriteLine(userAgent);
        Console.WriteLine(User.ToString());
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidToken();
        Console.WriteLine(User);

        var tokens = await _authService.Verify(userId, codeVerify.Code, session);
        SetTokenCookies(tokens);
        return Results.Ok();
    }

    [HttpPost("auth")]
    public async Task<IResult> Auth(UserAuth user)
    {
        var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
        var session = GetSessionInfo(userAgent);
        var newTokens = await _authService.Auth(user, session);
        SetTokenCookies(newTokens);
        return Results.Ok();
    }

    [HttpPatch("updateToken")]
    public async Task<IResult> updateToken()
    {
        var refreshToken = Request.Cookies[refreshTokenPath];
        if (string.IsNullOrEmpty(refreshToken))
            return Results.Unauthorized();

        var tokens = await _authService.RefreshToken(refreshToken);
        SetTokenCookies(tokens);
        return Results.Ok();
    }

    private void SetTokenCookies((string accessToken, string refreshToken) tokens)
    {
        Response.Cookies.Append(accessTokenPath, tokens.accessToken, accOptions);
        Response.Cookies.Append(refreshTokenPath, tokens.refreshToken, refOptions);
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
