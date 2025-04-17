using FluentValidation;
using MusicWebApi.Data;
using MusicWebApi.Models;
using MusicWebApi.Services;
using MusicWebApi.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IValidator<UserAuth>, UserValidator>();
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

    jwtOptions.Authority = builder.Configuration["Jwt:Authority"];
    jwtOptions.Audience = builder.Configuration["Jwt:Audience"];
    jwtOptions.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidAudiences = builder.Configuration.GetSection("Jwt:Audience").Get<string[]>(),
        ValidIssuers = builder.Configuration.GetSection("Jwt:Issuer").Get<string[]>()
    };

    jwtOptions.MapInboundClaims = false;
});


builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<UsersService>();

builder.Services.AddControllers();

var app = builder.Build();

//app.UseAuthorization();
app.UseAuthentication();

app.MapControllers();

app.Run();
