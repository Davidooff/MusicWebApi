using FluentValidation;
using Application.Dto;

namespace MusicWebApi.src.Api.Validators;

public class UserValidator : AbstractValidator<UserAuth>
{
    public UserValidator()
    {
        RuleFor(user => user.Email)
            .NotEmpty();
            //.WithMessage("Username is required.")
            //.Length(3, 20)
            //.WithMessage("Username must be between 3 and 20 characters long.");
        RuleFor(user => user.Password)
            .NotEmpty();
            //.WithMessage("Password is required.")
            //.Length(6, 100)
            //.WithMessage("Password must be between 6 and 100 characters long.")
            //.Must(password =>
            //{
            //    const int minDigits = 4;
            //    int digCounter = 0;
            //    bool hasUpper = false, hasLowwer = false; // Initialize variables to avoid CS0165
            //    foreach (char ch in password)
            //    {
            //        if (char.IsUpper(ch)) hasUpper = true;
            //        if (char.IsLower(ch)) hasLowwer = true;
            //        if (char.IsDigit(ch))
            //        {
            //            digCounter++;
            //        }
            //        if (digCounter >= minDigits && hasUpper && hasLowwer)
            //        {
            //            return true;
            //        }
            //    }
            //    return false; // Ensure a return statement for the Must condition
            //})
            //.WithMessage("Password must contain at least one uppercase and one lowercase letter and at least 4 digits.");
    }
}
