namespace Application.Exceptions.Auth;
public class UserWasNotVerifyed: Exception
{
    public UserWasNotVerifyed() : base($"User with this Email and Password not found.") { }
}

