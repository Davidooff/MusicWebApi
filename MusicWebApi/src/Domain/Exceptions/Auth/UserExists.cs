namespace MusicWebApi.src.Domain.Exceptions.Auth;
public class UserExists : Exception
{
    public string Email { get; }
    public UserExists(string email): base("User with this Email already exists.") { Email = email; }
}

