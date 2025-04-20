using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Application.Entities;
using MusicWebApi.src.Application.Services;
using MusicWebApi.src.Domain.Entities;
using MusicWebApi.src.Domain.Models;
using MusicWebApi.src.Infrastructure.Database;
using MusicWebApi.src.Infrastructure.Redis;
using UAParser;

namespace MusicWebApi.src.Api;

[ApiController]
[Route("users")]
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
        try
        {
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var session = GetSessionInfo(userAgent);
            var newTokens = await _authService.Create(newUser, session);
            return Results.Ok(new {newTokens.accessToken, newTokens.refreshToken});
        } catch (MethodAccessException) // User is possibly bot (GetSessionInfo)
        {
            return Results.Forbid();
        } catch (ConstraintException)
        {
            return Results.Problem("User already exists");
        } catch 
        {
            return Results.InternalServerError();
        }
    }

    [HttpPost("auth")]
    public async Task<IResult> Auth(UserAuth user)
    {
        try
        {
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var session = GetSessionInfo(userAgent);
            var newTokens = await _authService.Auth(user, session);
            return Results.Ok(new { newTokens.accessToken,  newTokens.refreshToken});
        } catch (MethodAccessException) // Bot data in user agent
        {
            return Results.Forbid();
        }
        catch (InvalidOperationException) // User not found
        {
            return Results.NotFound();
        }
        catch (AccessViolationException) // Password wrong
        {
            return Results.Unauthorized();
        } catch
        {
            return Results.InternalServerError();
        }
    }

    [HttpPatch("updateToken")]
    public async Task<IResult> updateToken([FromBody] string refreshToken)
    {
        var sessionId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (sessionId is null) return Results.Forbid();

        try
        {
            var tokens = await _authService.RefreshToken(sessionId, refreshToken);
            return Results.Ok(tokens);
        } catch (ArgumentNullException) // refresh token don't have user id
        {
            return Results.Forbid();
        }
        catch
        {
            return Results.InternalServerError();
        }
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
