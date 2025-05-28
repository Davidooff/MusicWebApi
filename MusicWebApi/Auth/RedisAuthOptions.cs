using Microsoft.AspNetCore.Authentication;

namespace MusicWebApi.Auth;
public class RedisAuthOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "RedisAuth";
    public string Scheme => DefaultScheme;
    public string AuthenticationType = DefaultScheme;

    public string AccessTokenPath { get; set; } = null!;
}

