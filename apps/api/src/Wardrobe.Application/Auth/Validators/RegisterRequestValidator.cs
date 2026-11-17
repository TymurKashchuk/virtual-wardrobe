using FluentValidation;
using Wardrobe.Application.Auth.Models;
using Wardrobe.Application.Common.Validation;

namespace Wardrobe.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.");

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("Display name must not exceed 50 characters.");

        RuleFor(x => x.Password).ApplyPasswordRules();
    }
}
