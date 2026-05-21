using Microsoft.EntityFrameworkCore;
using ModelLayer;
using ModelLayer.Shared;
using ModelLayer.Shared.Entities;

namespace BusinessLayer.Corporate.Commands;

public class CreateUserCustomerAssignmentRequest
{
    public int UserId { get; set; }
    public int CustomerId { get; set; }
    public int? AssignedBy { get; set; }
}

public class UpdateUserCustomerAssignmentRequest
{
    public bool IsActive { get; set; }
}

public class CreateUserCustomerAssignmentCommand
{
    private readonly DBcontext _context;

    public CreateUserCustomerAssignmentCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<int> ExecuteAsync(CreateUserCustomerAssignmentRequest request)
    {
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.UserId);
        if (user == null)
        {
            throw new InvalidOperationException(
                $"User with ID {request.UserId} was not found.");
        }

        if (!user.IsCorporate)
        {
            throw new InvalidOperationException(
                $"User with ID {request.UserId} is not a corporate user. Only corporate users can be assigned to customers.");
        }

        if (!user.IsActive)
        {
            throw new InvalidOperationException(
                $"User with ID {request.UserId} is inactive. Activate the user before assigning customers.");
        }

        var customerExists = await _context.Customers.AnyAsync(c => c.CustomerId == request.CustomerId);
        if (!customerExists)
        {
            throw new InvalidOperationException(
                $"Customer with ID {request.CustomerId} was not found.");
        }

        var duplicate = await _context.UserCustomerAssignments.AnyAsync(a =>
            a.UserId == request.UserId && a.CustomerId == request.CustomerId);
        if (duplicate)
        {
            throw new InvalidOperationException(
                $"User {request.UserId} is already assigned to customer {request.CustomerId}. Use PUT to activate or deactivate the assignment.");
        }

        if (request.AssignedBy.HasValue)
        {
            var assignerExists = await _context.Users.AnyAsync(u => u.UserId == request.AssignedBy.Value);
            if (!assignerExists)
            {
                throw new InvalidOperationException(
                    $"AssignedBy user with ID {request.AssignedBy.Value} was not found.");
            }
        }

        var entity = new UserCustomerAssignment
        {
            UserId = request.UserId,
            CustomerId = request.CustomerId,
            IsActive = true,
            AssignedAt = DateTimeService.GetCostaRicaNow(),
            AssignedBy = request.AssignedBy
        };

        _context.UserCustomerAssignments.Add(entity);
        await _context.SaveChangesAsync();
        return entity.UserCustomerAssignmentId;
    }
}

public class UpdateUserCustomerAssignmentCommand
{
    private readonly DBcontext _context;

    public UpdateUserCustomerAssignmentCommand(DBcontext context)
    {
        _context = context;
    }

    public async Task<bool> ExecuteAsync(int userCustomerAssignmentId, UpdateUserCustomerAssignmentRequest request)
    {
        var entity = await _context.UserCustomerAssignments
            .FirstOrDefaultAsync(a => a.UserCustomerAssignmentId == userCustomerAssignmentId);
        if (entity == null)
            return false;

        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTimeService.GetCostaRicaNow();
        await _context.SaveChangesAsync();
        return true;
    }
}
