namespace MusicWebApi.src.Domain.Exceptions.Auth;

public class WrongPassword : Exception
{
    public string Email { get; }
    public WrongPassword(string email) : base($"User with this Email and Password not found.")
    {
        Email = email;
    }
}


