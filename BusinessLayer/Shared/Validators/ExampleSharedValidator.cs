using FluentValidation;

namespace BusinessLayer.Shared.Validators;

public class ExampleSharedValidator : AbstractValidator<ExampleSharedRequest>
{
    public ExampleSharedValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value is required");
    }
}

public class ExampleSharedRequest
{
    public string Value { get; set; } = string.Empty;
} 