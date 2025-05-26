using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using MusicWebApi.src.Application.Services;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Api.Validators;
using MusicWebApi.src.Infrastructure.Database;
using MusicWebApi.src.Infrastructure.Redis;
using MusicWebApi.src.Domain.Options;
using MusicWebApi.src.Infrastructure.Redisk;
using MusicWebApi.src.Infrastructure.MailService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = builder.Environment.IsProduction(); // True in production
    options.SaveToken = true; // Not strictly necessary if we read from cookie ourselves, but good practice
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero // Remove clock skew
    };
    // Read token from HttpOnly cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies[jwtSettings.AccessTokenStorage];
            return Task.CompletedTask;
        },
        // Optional: Handle authentication failure (e.g., token expired but not yet refreshed)
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        }   
    };
});

//redis
builder.Services.AddSingleton<UserRedisRepository>();
builder.Services.AddSingleton<TokenRepository>();
builder.Services.AddSingleton<VerifyMailRepo>();
//mongo
builder.Services.AddSingleton<UsersRepository>();
builder.Services.AddSingleton<MusicFileRepository>();
builder.Services.AddSingleton<MusicRepository>();
//services
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<EmailService>();
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<PlatformsService>();
// Filters 
builder.Services.AddScoped<UsersExceptionFilter>();

builder.Services.AddControllers();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}


app.UseAuthentication(); // <<<<<<<< HERE
app.UseAuthorization();  // <<<<<<<< And HERE

app.MapControllers();

app.Run();
