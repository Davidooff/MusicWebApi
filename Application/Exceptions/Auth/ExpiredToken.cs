namespace Application.Exceptions.Auth;

public class ExpiredToken : Exception
{
    public string Token { get; }
    public ExpiredToken(string token) : base("Token is invalid.") { Token = token;  }
}

