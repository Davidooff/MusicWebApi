namespace MusicWebApi.src.Domain.Options;

public class RedisSettings
{
    public string EndPoint { get; set; } = null!;
    public int TokenDbIndex { get; set; }
    public int VerifyUserDbIndex { get; set; }
    public short VerificationLimit { get; set; }
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
}

