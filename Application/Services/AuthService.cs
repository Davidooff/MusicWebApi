using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using Application.Dto;
using Domain.Entities;
using Application.Exceptions.Auth;
using Infrastructure.MailService;
using Infrastructure.Redis;
using MusicWebApi.src.Infrastructure.Redisk;
using Infrastructure.Datasbase;
using Application.Services;

namespace Application.Services;
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

    private async Task<(string accToken, string refToken)> CreateTokensAndAssignSessionToRedis(string userId)
    {
        var refreshToken = _jwtService.genRefToken(userId);
        string sessionId = await _tokenRepository.CreateNewSession(userId, refreshToken);
        var accessToken = _jwtService.genAccToken(sessionId);
        return (accessToken, refreshToken);
    }

    /// <summary>
    /// Creating user verification tokens and creating El in redis for email verification
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="email"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<string?> CreateUserVerificationTokens(string userId, string email)
    {
        try
        {
            short code = _verifyMailRepo.CreateCode();
            var mailId = _verifyMailRepo.Create(userId, code);
            _MailService.SendCode(code, email);
            await mailId;
            return _jwtService.GenerateToken(userId, 15);
        }
        catch (Exception e)
        {
            _verifyMailRepo.RemoveByUserId(userId);
            throw new Exception("Unable to create user", e);
        }
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
        var token = await CreateUserVerificationTokens(userId, userAuth.Email);

        if (String.IsNullOrEmpty(token))
        {
            await _usersRepository.RemoveAsync(userId);
            throw new Exception("Unable to create user");
        }

        return token;
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

        var tokens = await CreateTokensAndAssignSessionToRedis(userId);
        Session session = new Session()
        {
            RefreshToken = tokens.refToken,
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

        var tokens = await CreateTokensAndAssignSessionToRedis(user.Id);

        user.Sessions = [new Session()
        {
            RefreshToken = tokens.refToken,
            Name = sessionInfo.sessionName,
            SessionType = sessionInfo.sessionType
        }];

        await _usersRepository.UpdateAsync(user.Id, user);

        return tokens;
    }

    /// <summary>
    /// Create tokens (if user is not verified it's return only access token for verification)
    /// </summary>
    /// <param name="userAuth">The amount to charge.</param>
    /// <exception cref="UserNotFound">
    /// User not found
    /// </exception>
    /// <exception cref="WrongPassword">
    /// Wrong password
    /// </exception>
    public async Task<(string accessToken, string? refreshToken)>
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

        if (user.IsVerified)
        {
            var newTokens = await CreateTokensAndAssignSessionToRedis(user.Id);

            user.Sessions.Append(new Session { 
                Name = session.name, 
                SessionType = session.type, 
                RefreshToken = newTokens.refToken 
            });
        
            await _usersRepository.UpdateAsync(user.Id, user);
            return newTokens;
        } else
        {
            var accessToken = await CreateUserVerificationTokens(user.Id, user.Email);

            if (String.IsNullOrEmpty(accessToken))
                throw new Exception("Unable to auth user");
            
            return (accessToken, null);
        }
    }

    public async Task Logout(string accToken)
    {
        string sessionId = _jwtService.GetIdFromToken(accToken); // if id is null, exception will be throw

        if (!await _tokenRepository.RemoveSession(sessionId))
            throw new InvalidToken(accToken);
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
        await _tokenRepository.removeSessionByRefreshToken(refreshToken);
        string userId = _jwtService.GetIdFromToken(refreshToken); // if id is null, exception will be throw

        var newTokens = await CreateTokensAndAssignSessionToRedis(userId);

        bool done = await _usersRepository.RefreshToken(userId, refreshToken, newTokens.refToken);

        if (done is false)
            throw new ExpiredToken(refreshToken);

        return newTokens; // Assuming you want to return the new access token.
    }
}

