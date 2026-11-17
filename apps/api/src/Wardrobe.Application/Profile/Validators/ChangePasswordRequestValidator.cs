using FluentValidation;
using Wardrobe.Application.Common.Validation;
using Wardrobe.Application.Profile.Models;

namespace Wardrobe.Application.Profile.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).ApplyPasswordRules();
    }
}
