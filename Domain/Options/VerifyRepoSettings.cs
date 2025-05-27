namespace Domain.Options;

public class VerifyRepoSettings
{
    public string EndPoint { get; set; } = null!;
    public short VerificationLimit { get; set; }
    public short VerifivationTimeLimit { get; set; }
    public string User { get; set; } = null!;
    public string Password { get; set; } = null!;
}

