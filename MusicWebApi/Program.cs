using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Application.Dto;
using MusicWebApi.src.Api.Validators;
using Application.Services;
using Domain.Options;
using Infrastructure.Database;
using Infrastructure.MailService;
using Infrastructure.Redis;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using System.Text;
using Infrastructure.Datasbase;
using MusicWebApi.Auth;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IValidator<UserAuth>, UserValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Add services to the container.
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("Database"));

// Register JwtSettings configuration
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.Configure<UserRedisRepoSettings>(
    builder.Configuration.GetSection("UserRedisRepoSettings"));

builder.Services.Configure<TokenRepoSettings>(
    builder.Configuration.GetSection("TokenRepoSettings"));

builder.Services.Configure<VerifyRepoSettings>(
    builder.Configuration.GetSection("VarifyRepoSettings"));

builder.Services.Configure<MailServiceSettings>(
    builder.Configuration.GetSection("MailServiceSettings"));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                  ?? throw new InvalidOperationException("JwtSettings not configured.");
var key = Encoding.ASCII.GetBytes(jwtSettings.Secret);


builder.Services.AddAuthentication(RedisAuthOptions.DefaultScheme)
    .AddScheme<RedisAuthOptions, RedisAuthenticationHandler>(RedisAuthOptions.DefaultScheme, options =>
    {
        options.AccessTokenPath = builder.Configuration["Jwt:AccessTokenStorage"] ?? throw new ArgumentNullException(); 
    });

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
});

//mongo
builder.Services.AddSingleton<UsersRepository>();
builder.Services.AddSingleton<MusicFileRepository>();
builder.Services.AddSingleton<MusicRepository>();
builder.Services.AddSingleton<UserAlbumRepository>();
//redis
builder.Services.AddSingleton<UserRedisRepository>();
builder.Services.AddSingleton<TokenRepository>();
builder.Services.AddSingleton<VerifyMailRepo>();
//services
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<PlatformsService>();
// Filters 
builder.Services.AddScoped<UsersExceptionFilter>();

builder.Services.AddControllers();

var app = builder.Build();

app.UsePathBase(new PathString("/api"));


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


app.UseAuthorization();

app.MapControllers();

app.Run();