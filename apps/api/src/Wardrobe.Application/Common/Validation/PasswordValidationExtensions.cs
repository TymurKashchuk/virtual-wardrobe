using FluentValidation;

namespace Wardrobe.Application.Common.Validation;

public static class PasswordValidationExtensions
{
    public static IRuleBuilderOptions<T, string> ApplyPasswordRules<T>(
        this IRuleBuilderInitial<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches(@"[a-zA-Z]").WithMessage("Password must contain at least one letter.")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit.");
    }
}
