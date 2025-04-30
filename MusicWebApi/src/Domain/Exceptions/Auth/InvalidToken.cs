namespace MusicWebApi.src.Domain.Exceptions.Auth;
public class InvalidToken : Exception
{
    public string Token { get; }
    public InvalidToken(string token) : base() { Token = token; }
}

