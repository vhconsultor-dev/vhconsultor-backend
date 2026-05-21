using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class CreateUserCustomerAssignmentValidator : AbstractValidator<CreateUserCustomerAssignmentRequest>
{
    public CreateUserCustomerAssignmentValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("User ID is required and must be greater than zero.");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("Customer ID is required and must be greater than zero.");

        RuleFor(x => x.AssignedBy)
            .GreaterThan(0)
            .When(x => x.AssignedBy.HasValue)
            .WithMessage("AssignedBy must be greater than zero when provided.");
    }
}

public class UpdateUserCustomerAssignmentValidator : AbstractValidator<UpdateUserCustomerAssignmentRequest>
{
    public UpdateUserCustomerAssignmentValidator()
    {
        // IsActive: true = activa la asignación, false = inactiva
    }
}
