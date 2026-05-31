using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Corporate.Entities;

namespace BusinessLayer.Corporate.Commands;

public class ConvertLeadToOpportunityCommand
{
    private readonly DBcontext _context;
    private readonly CreateFirstFollowUpCommand _createFirstFollowUpCommand;

    public ConvertLeadToOpportunityCommand(
        DBcontext context,
        CreateFirstFollowUpCommand createFirstFollowUpCommand)
    {
        _context = context;
        _createFirstFollowUpCommand = createFirstFollowUpCommand;
    }

    public async Task<int> ExecuteAsync(ConvertLeadToOpportunityRequest request)
    {
        // 1. Validate lead exists
        var lead = await _context.CustomerSubmissions
            .FirstOrDefaultAsync(s => s.SubmissionID == request.SubmissionId);

        if (lead == null)
            throw new InvalidOperationException(
                $"Lead with ID {request.SubmissionId} not found.");

        // 2. Validate not already converted
        if (lead.ConvertedToOpportunityId.HasValue)
            throw new InvalidOperationException(
                $"Lead with ID {request.SubmissionId} has already been converted to Opportunity ID {lead.ConvertedToOpportunityId.Value}.");

        // 3. Validate assigned user exists and is corporate
        var assignedUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.AssignedToUserId);

        if (assignedUser == null)
            throw new InvalidOperationException(
                $"Assigned user with ID {request.AssignedToUserId} not found.");

        if (!assignedUser.IsCorporate)
            throw new InvalidOperationException(
                $"User with ID {request.AssignedToUserId} is not a corporate user. Only corporate users can be assigned opportunities.");

        if (!assignedUser.IsActive)
            throw new InvalidOperationException(
                $"User with ID {request.AssignedToUserId} is inactive. Activate the user before assigning opportunities.");

        // 4. Validate viewer if provided
        if (request.ViewerUserId.HasValue)
        {
            var viewerUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == request.ViewerUserId.Value);

            if (viewerUser == null)
                throw new InvalidOperationException(
                    $"Viewer user with ID {request.ViewerUserId.Value} not found.");

            if (!viewerUser.IsCorporate)
                throw new InvalidOperationException(
                    $"Viewer user with ID {request.ViewerUserId.Value} is not a corporate user.");

            if (!viewerUser.IsActive)
                throw new InvalidOperationException(
                    $"Viewer user with ID {request.ViewerUserId.Value} is inactive.");
        }

        // 5. Create opportunity
        var opportunity = new Opportunity
        {
            SubmissionId = lead.SubmissionID,
            Status = "Open",
            CurrentStageKey = "first_contact",
            Title = lead.BrandName,
            
            // Copy contact data
            FirstName = lead.FirstName,
            LastName = lead.LastName,
            Email = lead.Email,
            PhoneNumber = lead.PhoneNumber,
            Country = lead.Country,
            BrandName = lead.BrandName,
            NumberOfListings = lead.NumberOfListings,
            ProductPageLink = lead.ProductPageLink,
            StoreLink = lead.StoreLink,
            SelectedPlatform = lead.SelectedPlatform,
            AccountType = lead.AccountType,
            ServiceType = lead.ServiceType,
            AnnualSalesRange = lead.AnnualSalesRange,
            AdvertisingBudgetRange = lead.AdvertisingBudgetRange,
            PromotionalBudgetRange = lead.PromotionalBudgetRange,
            AdditionalDetails = lead.AdditionalDetails,
            
            // Assignment
            AssignedToUserId = request.AssignedToUserId,
            ViewerUserId = request.ViewerUserId,
            
            // Audit
            ConvertedAt = DateTime.UtcNow,
            ConvertedByUserId = request.ConvertedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Opportunities.Add(opportunity);

        // 6. Mark lead as converted
        lead.ConvertedToOpportunityId = opportunity.OpportunityId;

        await _context.SaveChangesAsync();

        // 7. Create first mandatory follow-up (first contact)
        await _createFirstFollowUpCommand.ExecuteAsync(opportunity.OpportunityId, "first_contact");

        return opportunity.OpportunityId;
    }
}

public class ConvertLeadToOpportunityRequest
{
    public int SubmissionId { get; set; }
    public int AssignedToUserId { get; set; }
    public int? ViewerUserId { get; set; }
    public int? ConvertedByUserId { get; set; } // From JWT
}
