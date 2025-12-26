using FluentValidation;

namespace BusinessLayer.Shared.Validators;

public class CreatePermissionValidator : AbstractValidator<BusinessLayer.Shared.Commands.CreatePermissionRequest>
{
    public CreatePermissionValidator()
    {
        RuleFor(x => x.ResourceId)
            .GreaterThan(0)
            .WithMessage("ResourceId must be greater than 0");

        RuleFor(x => x.ActionId)
            .GreaterThan(0)
            .WithMessage("ActionId must be greater than 0");

        RuleFor(x => x.PermissionName)
            .NotEmpty()
            .WithMessage("Permission name is required")
            .MaximumLength(200)
            .WithMessage("Permission name cannot exceed 200 characters");

        RuleFor(x => x.PermissionKey)
            .NotEmpty()
            .WithMessage("Permission key is required")
            .MaximumLength(100)
            .WithMessage("Permission key cannot exceed 100 characters")
            .Matches("^[a-zA-Z0-9_.-]+$")
            .WithMessage("Permission key can only contain letters, numbers, underscores, dots and hyphens");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}

