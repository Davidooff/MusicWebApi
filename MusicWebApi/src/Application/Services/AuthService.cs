using System.Data;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Domain.Entities;
using MusicWebApi.src.Domain.Models;
using MusicWebApi.src.Infrastructure.Database;
using MusicWebApi.src.Infrastructure.Redis;

namespace MusicWebApi.src.Application.Services;
public class AuthService
{
    private readonly UsersRepository _usersRepository;
    private readonly TokenRepository _tokenRepository;
    private readonly JwtService _jwtService;
    private readonly PasswordHasher<UserAuth> _pwHasher = new PasswordHasher<UserAuth>();
    public AuthService(UsersRepository usersRepository, TokenRepository tokenRepository, JwtService jwtService)
    {
        _usersRepository = usersRepository;
        _tokenRepository = tokenRepository;
        _jwtService = jwtService;
    }

    private async Task CreateUser(UserAuth user, string? id, Session? session)
    {
        await _usersRepository.CreateAsync(new UserDB
        {
            Id = id,
            Email = user.Email,
            Password = user.Password,
            Sessions = session != null ? new[] { session } : Array.Empty<Session>()
        });
    }

    /// <summary>
    /// Creating user whith out tokens.
    /// </summary>
    /// <param name="userAuth">The amount to charge.</param>
    /// <exception cref="ConstraintException">
    /// Thrown when User already created
    /// </exception>
    /// 
    public async Task
        Create(UserAuth userAuth)
    {
        var user = await _usersRepository.GetAsync(new UserSerchOptions { Email = userAuth.Email });

        if (user != null)
            throw new ConstraintException("User already created");

        await CreateUser(userAuth, null, null);
       }

    /// <summary>
    /// Creating user whith tokens.
    /// </summary>
    /// <param name="userAuth">The amount to charge.</param>
    /// <exception cref="ConstraintException">
    /// Thrown when User already created
    /// </exception>
    public async Task<(string accessToken, string refreshToken)> 
        Create(UserAuth userAuth, (string sessionName, ESessionType sessionType) sessionInfo)
    {
        var user = await _usersRepository.GetAsync(new UserSerchOptions { Email = userAuth.Email });

        if (user is not null) 
            throw new ConstraintException ("User already created");

        var userId = ObjectId.GenerateNewId().ToString();

        string sessionId = _tokenRepository.setSession(userId);
        var tokens = _jwtService.GenerateJwtTokens(userId, sessionId);
        Session session = new Session() { 
            RefreshToken = tokens.refreshToken, 
            Name = sessionInfo.sessionName, 
            SessionType = sessionInfo.sessionType 
        };

        await CreateUser(userAuth, userId, session);
        return tokens;
    }

    /// <summary>
    /// Create tokens
    /// </summary>
    /// <param name="userAuth">The amount to charge.</param>
    /// <exception cref="InvalidOperationException">
    /// User not found
    /// </exception>
    /// <exception cref="AccessViolationException">
    /// Wrong password
    /// </exception>
    public async Task<(string accessToken, string refreshToken)>
    Auth(UserAuth userAuth, (string name, ESessionType type) session)
    {
        UserDB? user = await _usersRepository.GetAsync(new UserSerchOptions { Email = userAuth.Email });

        if (user is null)
            throw new InvalidOperationException("User not found");

        var verificationResult = _pwHasher.VerifyHashedPassword(userAuth, user.Password, userAuth.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
            throw new AccessViolationException(nameof(userAuth));

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Password = _pwHasher.HashPassword(userAuth, userAuth.Password);
        }

        
        string sessionId = _tokenRepository.setSession(user.Id);
        var newTokens = _jwtService.GenerateJwtTokens(user.Id, sessionId);
        user.Sessions.Append(new Session
        { Name = session.name, SessionType = session.type, RefreshToken = newTokens.refreshToken });
        await _usersRepository.UpdateAsync(user.Id, user);
        return newTokens;
    }

    /// <summary>
    /// Refresh refreshToken and updating redis and Mongo db.
    /// </summary>
    /// <param name="userAuth">The amount to charge.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when RefreshToken or accessToken don't have id in payload
    /// </exception>
    public async Task<(string accessToken, string refreshToken)> 
        RefreshToken(string sessionId, string refreshToken)
    {
        string? userId = _jwtService.GetIdFromToken(refreshToken);


        if (userId is null)
            throw new ArgumentNullException(nameof(userId));

        
        _tokenRepository.delletSession(sessionId);

        string newSessionId = _tokenRepository.setSession(userId);
        var newTokens = _jwtService.GenerateJwtTokens(userId, newSessionId);

        bool done = await _usersRepository.RefreshToken(userId, newTokens.refreshToken);


        if (done is false)
            throw new InvalidOperationException("User not or token not found.");

        return newTokens; // Assuming you want to return the new access token.
    }
}

