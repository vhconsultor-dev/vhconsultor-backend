using FluentValidation;
using BusinessLayer.Corporate.Commands;

namespace BusinessLayer.Corporate.Validators;

public class CreateCampaignValidator : AbstractValidator<CreateCampaignRequest>
{
    public CreateCampaignValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BodyContent).NotEmpty();
    }
}

public class UpdateCampaignValidator : AbstractValidator<UpdateCampaignRequest>
{
    public UpdateCampaignValidator()
    {
        RuleFor(x => x.CampaignId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(500);
        RuleFor(x => x.BodyContent).NotEmpty();
    }
}

public class AddCampaignProspectsValidator : AbstractValidator<AddCampaignProspectsRequest>
{
    public AddCampaignProspectsValidator()
    {
        RuleFor(x => x.CampaignId).GreaterThan(0);
        RuleFor(x => x.Prospects).NotEmpty();
        RuleForEach(x => x.Prospects).ChildRules(p =>
        {
            p.RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);
        });
    }
}

public class AddCampaignProspectsFromLeadsValidator : AbstractValidator<AddCampaignProspectsFromLeadsRequest>
{
    public AddCampaignProspectsFromLeadsValidator()
    {
        RuleFor(x => x.CampaignId).GreaterThan(0);
        RuleFor(x => x.LeadIds).NotEmpty();
        RuleForEach(x => x.LeadIds).GreaterThan(0);
    }
}

public class SendCampaignValidator : AbstractValidator<SendCampaignRequest>
{
    public SendCampaignValidator()
    {
        RuleFor(x => x.CampaignId).GreaterThan(0);
        RuleForEach(x => x.AdditionalBccEmails!)
            .EmailAddress()
            .When(x => x.AdditionalBccEmails != null);
    }
}
