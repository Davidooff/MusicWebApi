using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Application.Exceptions.Auth;

public class UsersExceptionFilter : IExceptionFilter
{
    private readonly ILogger<UsersExceptionFilter> _logger;
    private readonly IWebHostEnvironment _env;

    public UsersExceptionFilter(ILogger<UsersExceptionFilter> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ExpiredToken expiredTokenException)
        {
            _logger.LogWarning($"Token expired: Token = {expiredTokenException.Token}");

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Token expired"
            };

            context.Result = new UnauthorizedObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }
        else if (context.Exception is InvalidToken invalidTokenException)
        {
            _logger.LogWarning($"InvalidToken = {invalidTokenException.Token}");

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
            };

            context.Result = new UnauthorizedObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }
        else if (context.Exception is UserExists userExistsException)
        {
            _logger.LogWarning($"User exists = {userExistsException.Email}");

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User with this email already existst",
                Detail = "Try to use another email, or restore this one"
            };

            context.Result = new ConflictObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }
        else if (context.Exception is UserNotFound or WrongPassword)

        {
            if (context.Exception is UserNotFound user)
            {             
                _logger.LogWarning($"User not found: {user.Email}");
            } else if (context.Exception is WrongPassword wrongPass)
            {
                _logger.LogWarning($"Wrong pass: Email = {wrongPass.Email}");
            }

            var problemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "User with this login and password not found",
                    Detail = "Try to check is you're email is correct, after this check password"
                };

            context.Result = new NotFoundObjectResult(problemDetails);
            context.ExceptionHandled = true;
        } else if (context.Exception is UserAlreadyVerified)
        {
            _logger.LogWarning("UserAlreadyVerified");
      
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "User Already Verified",
                Detail = "Try to login"
            };

            context.Result = new ConflictObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }
        else if (context.Exception is InvalidCode)
        {
            _logger.LogWarning("UserAlreadyVerified");

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status406NotAcceptable,
                Title = "User Already Verified",
                Detail = "Try to login"
            };

            context.Result = new ObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }
        // Handle other exceptions (built-in or custom) here
        else if (!_env.IsDevelopment()) // Only show generic error in production
        {
            _logger.LogError($"An unhandled exception occurred: {context.Exception.Message}");

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = "Please contact support if the issue persists."
            };

            context.Result = new ObjectResult(problemDetails)
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };

            context.ExceptionHandled = true;
        }
    }
}