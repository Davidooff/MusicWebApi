using System.Data;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Domain.Entities;
using MusicWebApi.src.Domain.Exceptions.Auth;
using MusicWebApi.src.Domain.Models;
using MusicWebApi.src.Infrastructure.Database;
using MusicWebApi.src.Infrastructure.MailService;
using MusicWebApi.src.Infrastructure.Redis;
using MusicWebApi.src.Infrastructure.Redisk;

namespace MusicWebApi.src.Application.Services;
/// <summary>
/// All exceptions wich can be thrown is custom exception all them are 
/// described in UserExceptionFilter
/// </summary>
public class AuthService
{
    private readonly UsersRepository _usersRepository;
    private readonly TokenRepository _tokenRepository;
    private readonly JwtService _jwtService;
    private readonly EmailService _MailService;
    private readonly VerifyMailRepo _verifyMailRepo;
    private readonly PasswordHasher<UserAuth> _pwHasher = new PasswordHasher<UserAuth>();
    public AuthService(UsersRepository usersRepository, TokenRepository tokenRepository, JwtService jwtService, VerifyMailRepo verifyMailRepo, EmailService emailService)
    {
        _usersRepository = usersRepository;
        _tokenRepository = tokenRepository;
        _verifyMailRepo = verifyMailRepo;
        _MailService = emailService;
        _jwtService = jwtService;
    }

    private async Task CreateUser(UserRegister user, string? id, Session? session, bool verified = false)
    {
        await _usersRepository.CreateAsync(new UserDB
        {
            Id = id,
            Username = user.Username,
            Email = user.Email,
            Password = _pwHasher.HashPassword(null, user.Password),
            Sessions = session != null ? new[] { session } : Array.Empty<Session>(),
            IsVerified = verified
        });
    }

    /// <summary>
    /// Creating user (Email verification req).
    /// </summary>
    /// <param name="userAuth">The amount to charge.</param>
    /// <exception cref="UserExists">
    /// Thrown when User already created
    /// </exception>
    /// <returns>
    /// Single token to verify email
    /// </returns>
    public async Task<string>
        Create(UserRegister userAuth)
    {
        var user = await _usersRepository.GetAsync(new UserSerchOptions { Email = userAuth.Email });

        if (user != null)
            throw new UserExists(userAuth.Email);

        var userId = ObjectId.GenerateNewId().ToString();

        await CreateUser(userAuth, userId, null);
        try
        {
            short code = _verifyMailRepo.CreateCode();
            var mail = _verifyMailRepo.Create(userId, userAuth.Email, code);
            _MailService.SendCode(code, userAuth.Email);
            await mail;
            return _jwtService.GenerateToken(userId, 15);
        }
        catch (Exception e)
        {
            await _usersRepository.RemoveAsync(userId);
            throw new Exception("Unable to create user", e);
        }
    }

    /// <summary>
    /// Creating user (Email verification not req).
    /// </summary>
    /// <param name="userAuth">The amount to charge.</param>
    /// <exception cref="UserExists">
    /// Thrown when User already created
    /// </exception>
    public async Task<(string accessToken, string refreshToken)> 
        Create(UserRegister userAuth, (string sessionName, ESessionType sessionType) sessionInfo)
    {
        var user = await _usersRepository.GetAsync(new UserSerchOptions { Email = userAuth.Email });

        if (user is not null)
            throw new UserExists(userAuth.Email);

        var userId = ObjectId.GenerateNewId().ToString();

        string sessionId = _tokenRepository.setSession(userId);
        var tokens = _jwtService.GenerateJwtTokens(userId, sessionId);
        Session session = new Session()
        {
            RefreshToken = tokens.refreshToken,
            Name = sessionInfo.sessionName,
            SessionType = sessionInfo.sessionType
        };

        await CreateUser(userAuth, userId, session, verified: true);
        return tokens;
    }

    public async Task<(string accessToken, string refreshToken)> 
        Verify(string id, short code,(string sessionName, ESessionType sessionType) sessionInfo)
    {
        var user = await _usersRepository.GetAsync(new UserSerchOptions { Id = id });

        if (user is null)
            throw new UserNotFound("");

        if (user.IsVerified)
            throw new UserAlreadyVerified();

        var isVerified = await _verifyMailRepo.Verify(id, code);

        if (isVerified is null)
            throw new UserNotFound("");

        if (isVerified == false)
            throw new InvalidCode();

        user.IsVerified = true;
        string sessionId = _tokenRepository.setSession(user.Id);
        var tokens = _jwtService.GenerateJwtTokens(user.Id, sessionId);
        
        user.Sessions = [new Session()
        {
            RefreshToken = tokens.refreshToken,
            Name = sessionInfo.sessionName,
            SessionType = sessionInfo.sessionType
        }];

        await _usersRepository.UpdateAsync(user.Id, user);

        return tokens;
    }

    /// <summary>
    /// Create tokens
    /// </summary>
    /// <param name="userAuth">The amount to charge.</param>
    /// <exception cref="UserNotFound">
    /// User not found
    /// </exception>
    /// <exception cref="WrongPassword">
    /// Wrong password
    /// </exception>
    public async Task<(string accessToken, string refreshToken)>
    Auth(UserAuth userAuth, (string name, ESessionType type) session)
    {
        UserDB? user = await _usersRepository.GetAsync(new UserSerchOptions { Email = userAuth.Email });

        if (user is null || user.Id is null)
            throw new UserNotFound(userAuth.Email);

        var verificationResult = _pwHasher.VerifyHashedPassword(userAuth, user.Password, userAuth.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
            throw new WrongPassword(userAuth.Email);

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            user.Password = _pwHasher.HashPassword(userAuth, userAuth.Password);
        
        string sessionId = _tokenRepository.setSession(user.Id);
        var newTokens = _jwtService.GenerateJwtTokens(user.Id, sessionId);
        
        user.Sessions.Append(new Session { 
            Name = session.name, 
            SessionType = session.type, 
            RefreshToken = newTokens.refreshToken 
        });
        
        await _usersRepository.UpdateAsync(user.Id, user);
        return newTokens;
    }

    /// <summary>
    /// Refresh refreshToken and updating redis and Mongo db.
    /// </summary>
    /// <param name="userAuth">The amount to charge.</param>
    /// <exception cref="InvalidToken">
    /// Thrown when RefreshToken don't have id in payload
    /// </exception>
    /// <exception cref="ExpiredToken">
    /// Thrown when RefreshToken is expired
    /// </exception>
    public async Task<(string accessToken, string refreshToken)> 
        RefreshToken(string refreshToken)
    {
        string userId = _jwtService.GetIdFromToken(refreshToken); // if id is null, exception will be throw

        string newSessionId = _tokenRepository.setSession(userId);
        var newTokens = _jwtService.GenerateJwtTokens(userId, newSessionId);

        bool done = await _usersRepository.RefreshToken(userId, newTokens.refreshToken);

        if (done is false)
            throw new ExpiredToken(refreshToken);

        return newTokens; // Assuming you want to return the new access token.
    }
}

