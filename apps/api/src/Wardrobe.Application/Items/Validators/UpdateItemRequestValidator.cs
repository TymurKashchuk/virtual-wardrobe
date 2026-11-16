using FluentValidation;
using Wardrobe.Application.Items.Models;

namespace Wardrobe.Application.Items.Validators;

public class UpdateItemRequestValidator : ItemValidatorBase<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        AddCommonItemRules(
            x => x.Name,
            x => x.CategoryId,
            x => x.PrimaryColorId,
            x => x.PrimaryColorId,
            x => x.SecondaryColorIds,
            x => x.Style,
            x => x.Notes);

        When(x => x.NewPhotoStream != null, () =>
        {
            RuleFor(x => x.NewPhotoLength)
                .NotNull().WithMessage("Photo file length is required.")
                .GreaterThan(0).WithMessage("Photo file is empty.")
                .LessThanOrEqualTo(MaxFileSizeInBytes).WithMessage("Photo file size must not exceed 10 MB.");
        });
    }
}
