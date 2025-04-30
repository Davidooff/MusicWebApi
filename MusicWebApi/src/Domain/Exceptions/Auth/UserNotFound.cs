namespace MusicWebApi.src.Domain.Exceptions.Auth;

public class UserNotFound : Exception
{
    public string Email { get; }
    public UserNotFound(string email) : base($"User with this Email and Password not found.")
    {
        Email = email;
    }
}

