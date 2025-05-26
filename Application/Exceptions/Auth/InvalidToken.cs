namespace Application.Exceptions.Auth;
public class InvalidToken : Exception
{
    public string Token { get; }

    public InvalidToken() : base() { }
    public InvalidToken(string token) : base() { Token = token; }
}

