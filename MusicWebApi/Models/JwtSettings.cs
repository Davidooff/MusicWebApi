namespace MusicWebApi.Models;
public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public int accessTokenExpiration { get; set; } = 0;
    public int refreshTokenExpiration { get; set; } = 0;

}
