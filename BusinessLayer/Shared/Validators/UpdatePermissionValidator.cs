using FluentValidation;

namespace BusinessLayer.Shared.Validators;

public class UpdatePermissionValidator : AbstractValidator<BusinessLayer.Shared.Commands.UpdatePermissionRequest>
{
    public UpdatePermissionValidator()
    {
        RuleFor(x => x.PermissionName)
            .NotEmpty()
            .WithMessage("Permission name is required")
            .MaximumLength(200)
            .WithMessage("Permission name cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}


