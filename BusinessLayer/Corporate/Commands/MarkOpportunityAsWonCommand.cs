using Microsoft.EntityFrameworkCore;
using ModelLayer;

namespace BusinessLayer.Corporate.Commands;

public class MarkOpportunityAsWonCommand
{
    private readonly DBcontext _context;

    public MarkOpportunityAsWonCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task ExecuteAsync(MarkOpportunityAsWonRequest request)
    {
        // 1. Validate opportunity exists
        var opportunity = await _context.Opportunities
            .FirstOrDefaultAsync(o => o.OpportunityId == request.OpportunityId);

        if (opportunity == null)
            throw new InvalidOperationException(
                $"Opportunity with ID {request.OpportunityId} not found.");

        // 2. Validate opportunity is Open
        if (opportunity.Status != "Open")
            throw new InvalidOperationException(
                $"Cannot mark opportunity as won. Current status is {opportunity.Status}, must be Open.");

        // 3. Validate customer exists
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId);

        if (customer == null)
            throw new InvalidOperationException(
                $"Customer with ID {request.CustomerId} not found.");

        if (!customer.IsActive)
            throw new InvalidOperationException(
                $"Customer with ID {request.CustomerId} is not active.");

        // 4. Validate contract if provided
        if (request.ContractId.HasValue)
        {
            var contract = await _context.Contracts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ContractId == request.ContractId.Value);

            if (contract == null)
                throw new InvalidOperationException(
                    $"Contract with ID {request.ContractId.Value} not found.");

            if (contract.CustomerId != request.CustomerId)
                throw new InvalidOperationException(
                    $"Contract with ID {request.ContractId.Value} does not belong to customer with ID {request.CustomerId}.");

            // Link contract to opportunity
            contract.OpportunityId = request.OpportunityId;
        }

        // 5. Update opportunity
        opportunity.Status = "Won";
        opportunity.CustomerId = request.CustomerId;
        opportunity.ContractId = request.ContractId;
        opportunity.WonAt = DateTime.UtcNow;
        opportunity.WonByUserId = request.WonByUserId;
        opportunity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}

public class MarkOpportunityAsWonRequest
{
    public int OpportunityId { get; set; }
    public int CustomerId { get; set; }
    public int? ContractId { get; set; }
    public int WonByUserId { get; set; }
}
