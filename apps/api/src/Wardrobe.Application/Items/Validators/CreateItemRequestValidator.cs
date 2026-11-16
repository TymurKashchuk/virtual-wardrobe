using FluentValidation;
using Wardrobe.Application.Items.Models;

namespace Wardrobe.Application.Items.Validators;

public class CreateItemRequestValidator : ItemValidatorBase<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        AddCommonItemRules(
            x => x.Name,
            x => x.CategoryId,
            x => x.PrimaryColorId,
            x => x.PrimaryColorId,
            x => x.SecondaryColorIds,
            x => x.Style,
            x => x.Notes);

        RuleFor(x => x.PhotoStream)
            .NotNull().WithMessage("Photo file is required.");

        RuleFor(x => x.PhotoLength)
            .GreaterThan(0).WithMessage("Photo file is empty.")
            .LessThanOrEqualTo(MaxFileSizeInBytes).WithMessage("Photo file size must not exceed 10 MB.");
    }
}
