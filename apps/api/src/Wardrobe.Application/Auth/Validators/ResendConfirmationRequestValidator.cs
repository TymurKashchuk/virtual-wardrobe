using FluentValidation;
using Wardrobe.Application.Auth.Models;

namespace Wardrobe.Application.Auth.Validators;

public class ResendConfirmationRequestValidator : AbstractValidator<ResendConfirmationRequest>
{
    public ResendConfirmationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.");
    }
}
