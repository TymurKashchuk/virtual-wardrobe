using FluentValidation;
using Wardrobe.Application.Profile.Models;

namespace Wardrobe.Application.Profile.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MinimumLength(2).WithMessage("Display name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("Display name must not exceed 50 characters.");
    }
}
