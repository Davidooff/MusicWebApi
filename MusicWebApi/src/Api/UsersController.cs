﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Application.Services;
using MusicWebApi.src.Domain.Models;
using MusicWebApi.src.Infrastructure.Services;

namespace MusicWebApi.src.Api;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly UsersRepository _usersService;
    private readonly JwtService _jwtService;
    private readonly PasswordHasher<UserAuth> _pwHasher = new PasswordHasher<UserAuth>();


    public UsersController(UsersRepository usersService, JwtService jwtService)
    {
        _usersService = usersService ?? throw new ArgumentNullException(nameof(usersService));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
    }

    [HttpPost("new")]
    public async Task<IResult> newUser(UserAuth newUser)
    {
        UserSerchOptions search = new UserSerchOptions { Email = newUser.Email };

        if (await _usersService.GetAsync(search) is not null)
            return Results.Conflict("User with this email already exists");

        await _usersService.CreateAsync(new UserDB
        {
            Email = newUser.Email,
            Password = _pwHasher.HashPassword(newUser, newUser.Password)
        });

        return Results.Ok();
    }

    [HttpPost("auth")]
    public async Task<IResult> Auth(UserAuth user)
    {
        UserDB? userDB = await _usersService.GetAsync(new UserSerchOptions { Email = user.Email });

        if (userDB == null)
        {
            return Results.NotFound("User not found");
        }

        var verificationResult = _pwHasher.VerifyHashedPassword(user, userDB.Password, user.Password);

        if (verificationResult == PasswordVerificationResult.Success)
        {
            var tokens = _jwtService.GenerateJwtTokens(userDB.Id);
            await _usersService.AddToken(userDB.Id, tokens.refreshToken);
            return Results.Ok(new { tokens.accessToken, tokens.refreshToken });
        }

        return Results.Unauthorized();
    }

    [HttpPatch("updateToken")]
    public async Task<IResult> updateToken([FromBody] string refreshToken)
    {
        string id = _jwtService.GetIdFromToken(refreshToken);

        UserDB? userDB = await _usersService.GetAsync(new UserSerchOptions { Id = id });

        if (userDB == null) return Results.NotFound("User not found");

        int tokenIndex = Array.IndexOf(userDB.RefreshToken, refreshToken);
        if (tokenIndex == -1) return Results.NotFound("Token not found");

        var newTokens = _jwtService.GenerateJwtTokens(userDB.Id);

        userDB.RefreshToken[tokenIndex] = newTokens.refreshToken;
        await _usersService.UpdateAsync(userDB.Id, userDB);

        return Results.Ok(new { newTokens.accessToken, newTokens.refreshToken });
    }
}
