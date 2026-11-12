using FluentValidation;
using Wardrobe.Application.Items.Models;

namespace Wardrobe.Application.Items.Validators;

public class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    private const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

    public CreateItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Item name is required.")
            .MinimumLength(1).WithMessage("Item name must be at least 1 character long.")
            .MaximumLength(100).WithMessage("Item name must not exceed 100 characters.");

        RuleFor(x => x.CategoryId)
            .GreaterThan((short)0).WithMessage("Category is required.");

        RuleFor(x => x.PrimaryColorId)
            .GreaterThan((short)0).WithMessage("Primary color is required.");

        RuleFor(x => x)
            .Must(HaveValidColorsCount)
            .WithMessage("An item must have between 1 and 3 unique colors.");

        RuleFor(x => x.Style)
            .IsInEnum().WithMessage("Style is invalid.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");

        RuleFor(x => x.PhotoStream)
            .NotNull().WithMessage("Photo file is required.");

        RuleFor(x => x.PhotoLength)
            .GreaterThan(0).WithMessage("Photo file is empty.")
            .LessThanOrEqualTo(MaxFileSizeInBytes).WithMessage("Photo file size must not exceed 10 MB.");
    }

    private static bool HaveValidColorsCount(CreateItemRequest request)
    {
        var secondary = request.SecondaryColorIds ?? Array.Empty<short>();
        if (secondary.Contains(request.PrimaryColorId))
        {
            return false;
        }

        var totalUniqueColors = secondary.Distinct().Count() + 1;
        return totalUniqueColors is >= 1 and <= 3;
    }
}
