using System.Linq.Expressions;
using FluentValidation;
using Wardrobe.Domain.Enums;

namespace Wardrobe.Application.Items.Validators;

public abstract class ItemValidatorBase<T> : AbstractValidator<T>
{
    protected const long MaxFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

    protected void AddCommonItemRules(
        Expression<Func<T, string>> nameSelector,
        Expression<Func<T, short>> categoryIdSelector,
        Expression<Func<T, short>> primaryColorIdSelector,
        Func<T, short> getPrimaryColorId,
        Func<T, IReadOnlyList<short>?> getSecondaryColorIds,
        Expression<Func<T, ClothingStyle>> styleSelector,
        Expression<Func<T, string?>> notesSelector)
    {
        RuleFor(nameSelector)
            .NotEmpty().WithMessage("Item name is required.")
            .MinimumLength(1).WithMessage("Item name must be at least 1 character long.")
            .MaximumLength(100).WithMessage("Item name must not exceed 100 characters.");

        RuleFor(categoryIdSelector)
            .GreaterThan((short)0).WithMessage("Category is required.");

        RuleFor(primaryColorIdSelector)
            .GreaterThan((short)0).WithMessage("Primary color is required.");

        RuleFor(x => x)
            .Must(x => HaveValidColorsCount(getPrimaryColorId(x), getSecondaryColorIds(x)))
            .WithMessage("An item must have between 1 and 3 unique colors.");

        RuleFor(styleSelector)
            .IsInEnum().WithMessage("Style is invalid.");

        RuleFor(notesSelector)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.");
    }

    private static bool HaveValidColorsCount(short primaryColorId, IReadOnlyList<short>? secondaryColorIds)
    {
        var secondary = secondaryColorIds ?? Array.Empty<short>();
        if (secondary.Contains(primaryColorId))
        {
            return false;
        }

        var totalUniqueColors = secondary.Distinct().Count() + 1;
        return totalUniqueColors is >= 1 and <= 3;
    }
}
