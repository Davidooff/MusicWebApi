using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MusicWebApi.src.Application.Services;
using MusicWebApi.src.Infrastructure.Services;
using MusicWebApi.src.Application.Options;
using MusicWebApi.src.Infrastructure.Options;
using MusicWebApi.src.Api.Dto;
using MusicWebApi.src.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IValidator<UserAuth>, UserValidator>();
//builder.Services.AddScoped<IValidator<MusicSearch>, MusicSearchValidator>();
builder.Services.AddFluentValidationAutoValidation();

// Add services to the container.
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("Database"));

// Register JwtSettings configuration
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));
builder.Services.AddAuthentication()
.AddJwtBearer("some-scheme", jwtOptions =>
{
    var jwtSecret = builder.Configuration["Jwt:Secret"];
    if (string.IsNullOrEmpty(jwtSecret))
    {
        throw new InvalidOperationException("Jwt:Secret configuration is missing or empty.");
    }

    jwtOptions.Authority = builder.Configuration["Jwt:Authority"];
    jwtOptions.Audience = builder.Configuration["Jwt:Audience"];
    jwtOptions.RequireHttpsMetadata = false;
    jwtOptions.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidAudiences = builder.Configuration.GetSection("Jwt:Audience").Get<string[]>(),
        ValidIssuers = builder.Configuration.GetSection("Jwt:Issuer").Get<string[]>(),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };

    jwtOptions.MapInboundClaims = false;
});

builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<UsersRepository>();
builder.Services.AddSingleton<PlatformsService>();

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.Run();
