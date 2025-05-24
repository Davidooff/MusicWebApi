namespace MusicWebApi.src.Domain.Entities;
public class TokenRedis
{
    public string UserId { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;

}

