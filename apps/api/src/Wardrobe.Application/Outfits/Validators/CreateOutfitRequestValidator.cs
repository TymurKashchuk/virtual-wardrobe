using FluentValidation;
using Wardrobe.Application.Outfits.Models;

namespace Wardrobe.Application.Outfits.Validators;

public class CreateOutfitRequestValidator : AbstractValidator<CreateOutfitRequest>
{
    public CreateOutfitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Outfit name is required.")
            .MinimumLength(1).WithMessage("Outfit name must be at least 1 character long.")
            .MaximumLength(80).WithMessage("Outfit name must not exceed 80 characters.");

        RuleFor(x => x.ItemIds)
            .NotNull().WithMessage("Item IDs collection is required.")
            .Must(items => items != null && items.Distinct().Count() >= 2)
            .WithMessage("An outfit must contain at least 2 distinct items.");

        RuleFor(x => x.Style)
            .IsInEnum().When(x => x.Style.HasValue)
            .WithMessage("Style is invalid.");

        RuleFor(x => x.Source)
            .IsInEnum()
            .WithMessage("Source is invalid.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }
}
